using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.UI;
using ProjectM.Units;
using UnityEditor;
using EntityId = ProjectM.Core.EntityId;

namespace ProjectM.Tests
{
    /// <summary>
    /// 3-4b: 보스 층(큰 방 + 임시 보스만) · 보스 처치 = 승리 · 사망 = 패배 · 처치 수 · 사망 원인 · 스냅샷 v10 · 결과 화면 내용 (결정 2026-10-18).
    /// </summary>
    public class BossTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10, new[] { "fire_basic" });
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 8, 2, 6);
        private static readonly UnitDef BossDef = new UnitDef("boss", 60, 5, 10); // BALANCE 3번
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly HudText Text = new HudText(new Dictionary<string, string> { { "fire_basic", "불꽃 화살" } },
            new Dictionary<string, string> { { "toxic_burst", "유독성 폭발" } });

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static readonly ICommandSource Idle = new IdleAi();

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;
            public FloorTransition Stairs;
            public readonly List<RunEndedEvent> Ends = new List<RunEndedEvent>();

            public void Wait(int turns)
            {
                for (int i = 0; i < turns && World.Scheduler.PlayerId.IsValid; i++) Clock.SubmitPlayerCommand(PlayerActions.Wait(Config));
                World.Events.Flush();
            }

            public Unit Boss => World.Units.TryGet(World.Run.BossId, out var b) ? b : null;
        }

        // 2층짜리 판: 1층 → 계단 → 2층 = 보스 층
        private static Session Start(ulong seed = 20261018, bool goToBoss = true)
        {
            var dungeon = new DungeonGenConfig(50, 50, 10, 20, 7, 20, 10, 3, 5, 1000, 10, floorCount: 2);
            var generator = new DungeonGenerator(dungeon);
            DungeonFloor floor = null;
            var world = World.Create(seed, rng =>
            {
                floor = generator.Generate(rng, stairsUp: false, stairsDown: true);
                return floor.Map;
            });
            var vision = world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var spawn = new SpawnConfig(5, 8);
            var s = new Session
            {
                World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput),
                Stairs = new FloorTransition(world, generator, 2, EnemyDef, spawn, (w, id) => Idle, vision, BossDef),
            };
            world.Events.Subscribe<RunEndedEvent>(s.Ends.Add);
            s.Player = FloorPopulator.Populate(world, floor.Start, PlayerDef, EnemyDef, spawn, Idle, Magic.QuickSlotCount, Magic.InventorySlotCount);
            vision.Refresh();
            world.Events.Flush();
            if (goToBoss)
            {
                world.Units.Teleport(s.Player.Id, floor.Exit); // 계단 위에 바로 (걸어가기는 FloorTests)
                Assert.That(s.Stairs.TryUse(), Is.True);
                world.Events.Flush();
            }
            return s;
        }

        [Test]
        public void BossFloor_BigRoom_StairsUp_BossOnly()
        {
            var s = Start();
            var map = s.World.Map;
            Assert.That(s.World.Floor, Is.EqualTo(2));
            Assert.That(map.Find(TileType.StairsDown).HasValue, Is.False);
            Assert.That(map.Get(s.Player.Pos), Is.EqualTo(TileType.StairsUp));
            Assert.That(s.World.Units.Count, Is.EqualTo(2), "플레이어 + 보스만");
            var boss = s.Boss;
            Assert.That(boss, Is.Not.Null);
            Assert.That((boss.MaxHp, boss.AttackDamage, boss.SightRange), Is.EqualTo((60, 5, 10)));
            Assert.That(boss.Pos.Y, Is.EqualTo(s.Player.Pos.Y), "방 양 끝 가운데 줄");
            Assert.That(boss.Pos.X - s.Player.Pos.X, Is.EqualTo(20), "21칸 방의 양 끝");

            int floorCells = 0;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map.IsWalkable(new GridPos(x, y))) floorCells++;
            Assert.That(floorCells, Is.EqualTo(21 * 15 - 4), "21×15 방 − 기둥 4");
        }

        [Test]
        public void KillBoss_Victory_Once()
        {
            var s = Start();
            var boss = s.Boss;
            Combat.ApplyDamage(s.World, s.Player.Id, boss, boss.Hp, DamageCause.Attack);
            s.Wait(1);
            Assert.That(s.World.Run.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(s.Ends.Count, Is.EqualTo(1));
            Assert.That(s.Ends[0].Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(s.World.Run.Kills, Is.EqualTo(1));
            s.Wait(2);
            Assert.That(s.Ends.Count, Is.EqualTo(1), "한 번만");
        }

        // 패배 + 사망 원인 = 마지막으로 받은 피해 (공격 뒤 화상으로 쓰러짐 → 화상)
        [Test]
        public void PlayerDeath_Defeat_LastDamageIsCause()
        {
            var s = Start();
            Combat.ApplyDamage(s.World, s.Boss.Id, s.Player, s.Player.Hp - 1, DamageCause.Attack);
            Assert.That(s.World.Run.LastPlayerDamage.Kind, Is.EqualTo(DamageKind.Attack));
            SpellTests.BalanceStatuses().TryGet(StatusType.Burn, out var burn);
            StatusEffects.Apply(s.World, s.Player, burn);
            s.Wait(1);

            Assert.That(s.World.Run.Outcome, Is.EqualTo(RunOutcome.Defeat));
            Assert.That(s.Ends.Count, Is.EqualTo(1));
            Assert.That(s.World.Run.LastPlayerDamage.Kind, Is.EqualTo(DamageKind.Status));
            Assert.That(s.World.Run.LastPlayerDamage.Status, Is.EqualTo(StatusType.Burn));
            var model = ResultModel.From(s.World, 99, s.Player, Text);
            Assert.That(model.Cause, Is.EqualTo("화상"));
        }

        [Test]
        public void EnemyKill_CountsButNoOutcome()
        {
            var s = Start(goToBoss: false);
            var enemy = s.World.Units.ToSortedList().Find(u => u.Faction == Faction.Enemy);
            Combat.ApplyDamage(s.World, s.Player.Id, enemy, enemy.Hp, DamageCause.Attack);
            s.Wait(1);
            Assert.That(s.World.Run.Kills, Is.EqualTo(1));
            Assert.That(s.World.Run.Outcome, Is.EqualTo(RunOutcome.None));
            Assert.That(s.Ends, Is.Empty);
        }

        // 피해 출처가 이벤트 · 사망 원인 문구로
        [Test]
        public void DeathCause_Texts()
        {
            var player = new EntityId(1);
            var boss = new EntityId(9);
            Assert.That(Text.DeathCause(DamageCause.Attack, boss, boss, player), Is.EqualTo("보스의 공격"));
            Assert.That(Text.DeathCause(DamageCause.Attack, new EntityId(3), boss, player), Is.EqualTo($"적 {new EntityId(3)}의 공격"));
            Assert.That(Text.DeathCause(DamageCause.Spell("fire_basic"), player, boss, player), Is.EqualTo("내 불꽃 화살"));
            Assert.That(Text.DeathCause(DamageCause.Reaction("toxic_burst"), player, boss, player), Is.EqualTo("유독성 폭발 (내 주문의 반응)"));
            Assert.That(Text.DeathCause(DamageCause.FromTerrain(TerrainEffectType.ContaminatedWater), EntityId.None, boss, player), Is.EqualTo("오염된 물"));
            Assert.That(Text.DeathCause(default, EntityId.None, boss, player), Is.EqualTo("알 수 없음"));
        }

        [Test]
        public void Result_Body_Victory()
        {
            var s = Start();
            Combat.ApplyDamage(s.World, s.Player.Id, s.Boss, s.Boss.Hp, DamageCause.Attack);
            s.Wait(1);
            var model = ResultModel.From(s.World, 1234, s.Player, Text);
            Assert.That(model.Title, Is.EqualTo("승리 — 보스를 쓰러뜨렸다"));
            var body = model.Body();
            StringAssert.Contains("도달 층: 2층", body);
            StringAssert.Contains("처치: 1", body);
            StringAssert.Contains("시드: 1234", body);
            StringAssert.Contains("[1] 불꽃 화살", body);
            StringAssert.Contains("장비: 없음", body);
            StringAssert.DoesNotContain("사망 원인", body);
            Assert.That(model.Turns, Is.EqualTo(s.World.Scheduler.CurrentTick));
        }

        // 스냅샷 v10: 한 판 기록 왕복
        [Test]
        public void Snapshot_RoundTripsRunState()
        {
            var s = Start();
            Combat.ApplyDamage(s.World, s.Boss.Id, s.Player, 3, DamageCause.Attack);
            s.Wait(1);
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => Idle);
            Assert.That(restored.Run.BossId, Is.EqualTo(s.World.Run.BossId));
            Assert.That(restored.Run.LastPlayerDamage.Kind, Is.EqualTo(DamageKind.Attack));
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(10)); // 3-4b 기록이 들어간 버전 이상 — 3-5에서 11, 3-6에서 12, 3-7a에서 13, 3-7b에서 14, 3-7c에서 15
        }

        // 검산: Boss.asset · 보스 방 크기가 BALANCE.md와 같은지
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var boss = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/Boss.asset");
            Assert.That(boss, Is.Not.Null, "Boss.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var d = boss.ToDef();
            Assert.That((d.MaxHp, d.AttackDamage, d.SightRange), Is.EqualTo((60, 5, 10)));
            var dungeon = AssetDatabase.LoadAssetAtPath<DungeonGenConfigAsset>("Assets/_Project/Data/Dungeon/DungeonGenConfig.asset").ToConfig();
            Assert.That((dungeon.BossRoomWidth, dungeon.BossRoomHeight), Is.EqualTo((21, 15)));
        }
    }
}
