using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    public class StatusTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10,
            new[] { "fire_basic", "ice_basic", "lightning_basic", "poison_basic" });
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 8, 2, 6);
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        private const int Fire = 0, Ice = 1, Lightning = 2, Poison = 3;

        private static GridMap Room() => GridMovementTests.Parse(
            "############",
            "#..........#",
            "#..........#",
            "#..........#",
            "############");

        /// <summary>행동한 틱을 기록하고 대기만 하는 시험용 AI.</summary>
        private sealed class CountingAi : ICommandSource
        {
            public readonly List<long> ActedTicks = new List<long>();

            public ICommand NextCommand(in TickContext context)
            {
                ActedTicks.Add(context.Tick);
                return new WaitCommand(1);
            }
        }

        private sealed class Session
        {
            public World World;
            public SpellBook Book;
            public TurnClock Clock;
            public Unit Player;
            public readonly List<Unit> Enemies = new List<Unit>();
            public readonly CountingAi Ai = new CountingAi();
            public readonly List<StatusChangedEvent> StatusEvents = new List<StatusChangedEvent>();

            public void Wait(int turns)
            {
                for (int i = 0; i < turns; i++) Clock.SubmitPlayerCommand(PlayerActions.Wait(Config));
            }

            public void Cast(int slot, GridPos target)
            {
                var block = SpellCasting.TryCreateCast(World, Config, Book, Targeting, Sight, slot, target, out var command);
                Assert.That(block, Is.EqualTo(CastBlock.None));
                Clock.SubmitPlayerCommand(command);
            }

            public StatusDef Def(StatusType type)
            {
                Book.Statuses.TryGet(type, out var def);
                return def;
            }
        }

        private static Session Start(ulong seed, GridPos player, params GridPos[] enemies)
        {
            var world = new World(Room(), seed);
            var s = new Session { World = world, Book = SpellTests.BalanceBookWithStatuses(), Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            world.InstallCoreSystems(Sight, Magic, s.Book.Statuses, s.Book.Terrains);
            world.Events.Subscribe<StatusChangedEvent>(s.StatusEvents.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, player, Magic.QuickSlotCount);
            foreach (var p in enemies) s.Enemies.Add(UnitSpawner.SpawnEnemy(world, EnemyDef, p, s.Ai));
            world.Events.Flush();
            return s;
        }

        // 불 한 발 = 즉시 5 + 화상 3틱 × 1 = 8 → 체력 8 적이 화상으로 쓰러진다 (BALANCE 6번)
        [Test]
        public void Fire_BurnsForThreeTicks_AndKills()
        {
            var s = Start(1, new GridPos(1, 2), new GridPos(4, 2));
            var enemy = s.Enemies[0];

            s.Cast(Fire, enemy.Pos);
            Assert.That(enemy.Hp, Is.EqualTo(8 - 5 - 1), "시전한 틱의 ④에서 화상 1");
            Assert.That(enemy.HasStatus(StatusType.Burn), Is.True);
            s.Wait(1);
            Assert.That(enemy.Hp, Is.EqualTo(1));
            s.Wait(1);
            Assert.That(s.World.Units.TryGet(enemy.Id, out _), Is.False, "화상 마지막 틱에 0 → ⑥ 제거");
        }

        [Test]
        public void SameStatus_RefreshesDuration_DoesNotStack()
        {
            var s = Start(1, new GridPos(1, 2), new GridPos(8, 3));
            var enemy = s.Enemies[0];
            var poison = s.Def(StatusType.Poison);

            StatusEffects.Apply(s.World, enemy, poison);
            s.Wait(3);                                   // 5 → 2 남음, 피해 3
            Assert.That(enemy.Hp, Is.EqualTo(5));
            StatusEffects.Apply(s.World, enemy, poison); // 다시 5로 (더 긴 쪽)
            Assert.That(enemy.Statuses.Count, Is.EqualTo(1), "같은 상태는 하나");
            Assert.That(enemy.Statuses[0].Remaining, Is.EqualTo(5));
            s.Wait(1);
            Assert.That(enemy.Hp, Is.EqualTo(4), "틱당 피해는 그대로 1 (강도 증가 없음)");
        }

        // 둔화: 행동 비용 +1틱 → 걸린 동안 2틱마다 행동
        [Test]
        public void Slow_DoublesActionCostWhileActive()
        {
            var s = Start(1, new GridPos(1, 2), new GridPos(10, 3));
            StatusEffects.Apply(s.World, s.Enemies[0], s.Def(StatusType.Slow)); // 틱 0 전에 3틱
            s.Wait(7);                                                          // 틱 0 ~ 6
            Assert.That(s.Ai.ActedTicks, Is.EqualTo(new List<long> { 0, 2, 4, 5, 6 }), "0 · 2는 둔화(비용 2), 틱 2 끝에 풀림");
        }

        // 마비: 2틱 동안 차례가 와도 행동 못 함
        [Test]
        public void Paralyze_SkipsTurnsWhileActive()
        {
            var s = Start(1, new GridPos(1, 2), new GridPos(10, 3));
            StatusEffects.Apply(s.World, s.Enemies[0], s.Def(StatusType.Paralyze));
            s.Wait(4);                                                          // 틱 0 ~ 3
            Assert.That(s.Ai.ActedTicks, Is.EqualTo(new List<long> { 2, 3 }));
        }

        // 마비된 플레이어: 입력을 기다리지 않고 지나가며, 넣어 둔 행동은 풀린 뒤 실행
        [Test]
        public void ParalyzedPlayer_TurnsPassWithoutInput()
        {
            var s = Start(1, new GridPos(1, 2), new GridPos(10, 3));
            StatusEffects.Apply(s.World, s.Player, s.Def(StatusType.Paralyze));
            s.Wait(1);
            Assert.That(s.World.Scheduler.CurrentTick, Is.EqualTo(3), "틱 0 · 1은 마비로 넘어가고 틱 2에 대기 실행");
            Assert.That(s.World.Scheduler.IsWaitingForPlayer, Is.True);
        }

        // 번개 마비 30%: 시드 기반 전투 난수 — 같은 시드면 같은 결과, 많이 굴리면 30% 근처
        [Test]
        public void ParalyzeChance_IsSeededAndNearThirtyPercent()
        {
            int Count(ulong seed)
            {
                var s = Start(seed, new GridPos(1, 2), new GridPos(10, 3));
                var enemy = s.Enemies[0];
                var paralyze = s.Def(StatusType.Paralyze);
                int hits = 0;
                for (int i = 0; i < 1000; i++)
                {
                    enemy.Statuses.Clear();
                    if (StatusEffects.TryApply(s.World, enemy, paralyze, 30)) hits++;
                }
                return hits;
            }

            int a = Count(42);
            Assert.That(Count(42), Is.EqualTo(a), "같은 시드 = 같은 결과");
            Assert.That(a, Is.InRange(250, 350), "30% 근처");

            var s0 = Start(7, new GridPos(1, 2), new GridPos(10, 3));
            s0.World.Rng.Get(RngStream.Combat); // 수열을 만들어 두고 상태 비교
            var before = s0.World.Rng.CaptureStates()[RngStream.Combat];
            Assert.That(StatusEffects.TryApply(s0.World, s0.Enemies[0], s0.Def(StatusType.Burn), 100), Is.True);
            Assert.That(StatusEffects.TryApply(s0.World, s0.Enemies[0], s0.Def(StatusType.Burn), 0), Is.False);
            Assert.That(s0.World.Rng.CaptureStates()[RngStream.Combat], Is.EqualTo(before), "100% · 0%는 난수를 쓰지 않는다");
        }

        [Test]
        public void Lightning_ParalyzesSomeVictims_Deterministically()
        {
            List<bool> Run(ulong seed, out List<int> hps)
            {
                var s = Start(seed, new GridPos(1, 2), new GridPos(2, 2), new GridPos(3, 2), new GridPos(4, 2), new GridPos(5, 2));
                s.Cast(Lightning, new GridPos(2, 2));
                hps = s.Enemies.ConvertAll(e => e.Hp);
                return s.Enemies.ConvertAll(e => e.HasStatus(StatusType.Paralyze));
            }

            var first = Run(11, out var hp);
            Assert.That(Run(11, out _), Is.EqualTo(first), "같은 시드 = 같은 적이 마비");
            Assert.That(hp, Is.EqualTo(new List<int> { 5, 5, 5, 5 }), "4마리 모두 관통 피해 3 (마비는 피해 없음)");
        }

        [Test]
        public void Ice_Slows_Poison_PoisonsArea()
        {
            // 얼음 대상은 독 조준선 (1,2)→(6,2) 밖에 둔다 (투사체는 처음 만난 유닛에서 멈추므로). 독 대상은 사거리 5 안
            var s = Start(1, new GridPos(1, 2), new GridPos(4, 3), new GridPos(6, 2), new GridPos(6, 3));
            s.Cast(Ice, s.Enemies[0].Pos);
            Assert.That(s.Enemies[0].HasStatus(StatusType.Slow), Is.True);
            s.Cast(Poison, s.Enemies[1].Pos);
            Assert.That(s.Enemies[1].HasStatus(StatusType.Poison) && s.Enemies[2].HasStatus(StatusType.Poison), Is.True, "3×3 둘 다 중독");
            Assert.That(s.Enemies[1].Hp, Is.EqualTo(8 - 2 - 1), "즉시 2 + 그 틱 중독 1");
        }

        [Test]
        public void Snapshot_RoundTripsStatuses()
        {
            var s = Start(1, new GridPos(1, 2), new GridPos(8, 3));
            StatusEffects.Apply(s.World, s.Enemies[0], s.Def(StatusType.Poison));
            StatusEffects.Apply(s.World, s.Enemies[0], s.Def(StatusType.Slow));
            s.Wait(1);
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => new CountingAi());

            var e = restored.Units.Get(s.Enemies[0].Id);
            Assert.That(e.Statuses.ConvertAll(x => (x.Type, x.Remaining)),
                Is.EqualTo(new List<(StatusType, int)> { (StatusType.Slow, 2), (StatusType.Poison, 4) }), "종류 순");
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
        }

        [Test]
        public void Defs_RejectInvalidValues()
        {
            Assert.Throws<ArgumentException>(() => new StatusDef(StatusType.None, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatusDef(StatusType.Burn, 0));
            Assert.Throws<ArgumentException>(() => new StatusBook(new[] { new StatusDef(StatusType.Burn, 1), new StatusDef(StatusType.Burn, 2) }));
            Assert.Throws<ArgumentException>(() => new SpellBook(new[]
                { new SpellDef("a", Element.Fire, SpellShape.Bolt, 1, 1, 1, 0, StatusType.Burn) }), "StatusBook에 없는 상태");
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellDef("a", Element.Fire, SpellShape.Bolt, 1, 1, 1, 0, StatusType.Burn, 101));
        }

        // 검산: StatusCatalog 에셋이 BALANCE.md 6번 표와 같은 값인지
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset");
            Assert.That(asset, Is.Not.Null, "StatusCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var book = asset.ToBook();
            var expected = SpellTests.BalanceStatuses();
            Assert.That(book.Count, Is.EqualTo(expected.Count));
            foreach (StatusType type in new[] { StatusType.Burn, StatusType.Slow, StatusType.Poison, StatusType.Paralyze, StatusType.Wet })
            {
                Assert.That(book.TryGet(type, out var a), Is.True, type.ToString());
                expected.TryGet(type, out var e);
                Assert.That((a.DurationTicks, a.DamagePerTick, a.ExtraActionCostTicks, a.PreventsAction),
                    Is.EqualTo((e.DurationTicks, e.DamagePerTick, e.ExtraActionCostTicks, e.PreventsAction)), type.ToString());
            }
        }
    }
}
