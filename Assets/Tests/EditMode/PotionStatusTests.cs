using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Dungeon;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Tests
{
    /// <summary>3-13a 물약 4종 (SPD): 신속(이동 두 칸에 1틱) · 투명(적이 못 봄) · 부양(지형 · 함정 · 풀 무시) · 정신 시야(적 감지) — BALANCE.md 6 · 14번.</summary>
    public class PotionStatusTests
    {
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 30, 2, 6);

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;
            public StatusBook Statuses;
            public readonly List<VisibilityChangedEvent> Vision = new List<VisibilityChangedEvent>();

            public void Give(StatusType type)
            {
                Statuses.TryGet(type, out var def);
                StatusEffects.Apply(World, Player, def);
                World.Events.Flush();
            }

            public void Wait()
            {
                Clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
                World.Events.Flush();
            }
        }

        private static Session Start(GridMap map, GridPos player)
        {
            var world = new World(map, 5UL);
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput), Statuses = SpellTests.BalanceStatuses() };
            world.InstallCoreSystems(Sight, Magic, s.Statuses, SpellTests.BalanceTerrains());
            world.Events.Subscribe<VisibilityChangedEvent>(s.Vision.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, player, Magic.QuickSlotCount);
            world.Events.Flush();
            return s;
        }

        private static GridMap Hall() => GridMovementTests.Parse("############", "#..........#", "#..........#", "#..........#", "############");

        // 신속: 한 번 누르면 두 칸, 1틱
        [Test]
        public void Haste_MovesTwoCellsPerTick()
        {
            var s = Start(Hall(), new GridPos(1, 2));
            s.Give(StatusType.Haste);
            long tick = s.World.Scheduler.CurrentTick;
            Assert.That(PlayerActions.TryStep(s.World, StepConfig, new GridPos(1, 0), out var step, s.Statuses), Is.True);
            s.Clock.SubmitPlayerCommand(step);
            Assert.That(s.Player.Pos, Is.EqualTo(new GridPos(3, 2)));
            Assert.That(s.World.Scheduler.CurrentTick - tick, Is.EqualTo(1));

            // 둘째 칸이 막혔으면 한 칸만
            Assert.That(PlayerActions.TryStep(s.World, StepConfig, new GridPos(0, 1), out var up, s.Statuses), Is.True);
            s.Clock.SubmitPlayerCommand(up);
            Assert.That(s.Player.Pos, Is.EqualTo(new GridPos(3, 3)), "(3,4)는 벽");
        }

        [Test]
        public void NoHaste_OneCell()
        {
            var s = Start(Hall(), new GridPos(1, 2));
            Assert.That(PlayerActions.TryStep(s.World, StepConfig, new GridPos(1, 0), out var step, s.Statuses), Is.True);
            s.Clock.SubmitPlayerCommand(step);
            Assert.That(s.Player.Pos, Is.EqualTo(new GridPos(2, 2)));
        }

        // 투명: 옆의 적도 플레이어를 보지 못한다 (기억 없음 · 공격 안 함)
        [Test]
        public void Invisible_EnemyCannotSee()
        {
            var s = Start(Hall(), new GridPos(5, 2));
            var enemy = UnitSpawner.SpawnEnemy(s.World, EnemyDef, new GridPos(6, 2), new MeleeChaserAi(s.World, StepConfig, Sight, s.Statuses));
            s.Give(StatusType.Invisible);
            s.Wait();
            s.Wait();
            Assert.That(enemy.LastSeenTargetPos.HasValue, Is.False);
            Assert.That(s.Player.Hp, Is.EqualTo(PlayerDef.MaxHp), "공격받지 않음");
        }

        // 부양: 화염 지대 위에서도 화상 없음 · 함정 발동 안 함 · 높은 풀 안 밟힘
        [Test]
        public void Levitating_IgnoresGround()
        {
            var map = Hall();
            map.Set(new GridPos(3, 2), TileType.TallGrass);
            var s = Start(map, new GridPos(3, 2));
            s.Give(StatusType.Levitating);
            TerrainEffects.Place(s.World, new GridPos(3, 2), TerrainEffectType.FireZone, 4);
            var book = ReactionTests.Book(TrapTests.BalanceTrapSpells());
            s.World.Scheduler.AddSystem(TickPhase.ManaRegen, new TrapSystem(s.World, TrapTests.BalanceTraps(book), book));
            Traps.Place(s.World, new GridPos(3, 2), TrapType.Fire, hidden: true);
            s.Wait();
            Assert.That(s.Player.HasStatus(StatusType.Burn), Is.False, "지형 무시");
            Assert.That(s.World.Traps.Has(new GridPos(3, 2)), Is.True, "함정 발동 안 함");
            Assert.That(s.World.Map.Get(new GridPos(3, 2)), Is.EqualTo(TileType.TallGrass), "풀 안 밟힘");
        }

        // 정신 시야: 벽 너머 적 칸이 감지된다 (지도는 안 밝힘)
        [Test]
        public void MindVision_SensesEnemiesBehindWalls()
        {
            var map = GridMovementTests.Parse("#########", "#...#...#", "#########");
            var s = Start(map, new GridPos(1, 1));
            UnitSpawner.SpawnEnemy(s.World, EnemyDef, new GridPos(6, 1), new IdleAi());
            s.Give(StatusType.MindVision);
            s.Vision.Clear();
            s.Wait();
            var last = s.Vision[s.Vision.Count - 1];
            Assert.That(last.IsVisible(new GridPos(6, 1)), Is.False, "벽 너머는 안 보임");
            Assert.That(last.IsSensed(new GridPos(6, 1)), Is.True, "적은 감지");
        }

        // 물약을 마시면 그 상태 (지속 = 상태 표 20)
        [Test]
        public void DrinkingPotions_AppliesStatuses()
        {
            var items = new ItemBook(new[]
            {
                new ItemDef("potion_haste", ItemUse.Potion, effect: ConsumableEffect.Haste, weight: 1),
                new ItemDef("potion_invisibility", ItemUse.Potion, effect: ConsumableEffect.Invisibility, weight: 1),
                new ItemDef("potion_levitation", ItemUse.Potion, effect: ConsumableEffect.Levitation, weight: 1),
                new ItemDef("potion_mind_vision", ItemUse.Potion, effect: ConsumableEffect.MindVision, weight: 2),
            });
            var s = Start(Hall(), new GridPos(1, 1));
            var rules = new ConsumableRules(items, ConsumableConfig.Default, s.Statuses, SpellTests.BalanceTerrains());
            foreach (var (id, status) in new[] { ("potion_haste", StatusType.Haste), ("potion_invisibility", StatusType.Invisible),
                         ("potion_levitation", StatusType.Levitating), ("potion_mind_vision", StatusType.MindVision) })
            {
                int index = Inventory.TryAdd(s.World, null, items, s.Player, InventoryRef.Item(id));
                Assert.That(Consumables.Use(s.World, rules, s.Player, index), Is.EqualTo(ConsumeResult.Used), id);
                Assert.That(s.Player.HasStatus(status), Is.True, id);
            }
            Assert.That(s.Player.Statuses.Find(x => x.Type == StatusType.Haste).Remaining, Is.EqualTo(20));
        }

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }
    }
}
