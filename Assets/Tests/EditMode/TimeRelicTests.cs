using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.UI;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-28c 시간 유물 (결정 2026-11-05): 껴야 씀 · 빠른 칸 · 게이지(10턴마다 1칸) · 회중시계 · 스톱워치 · 가속기 · 타임머신 옮기기 · 스톱워치 v30 (BALANCE 24번).</summary>
    public class TimeRelicTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);
        private static readonly UnitDef EnemyDef = new UnitDef("rat", 8, 1, 8);
        private static readonly GameConfig Config = new GameConfig(1, 1f);

        private static ItemDef Time(string id, TimeRelicKind kind, int[] gauge, int[] power = null) =>
            new ItemDef(id, ItemUse.Relic, timeKind: kind, timeGauge: gauge, timePower: power);

        private static readonly ItemBook Items = new ItemBook(new[]
        {
            Time("relic_pocket_watch", TimeRelicKind.PocketWatch, new[] { 5, 6, 7, 8 }),
            Time("relic_time_machine", TimeRelicKind.TimeMachine, new[] { 5, 8, 11, 15 }),
            Time("relic_accelerator", TimeRelicKind.Accelerator, new[] { 3, 3, 4, 5 }, new[] { 2, 2, 3, 4 }),
            Time("relic_stopwatch", TimeRelicKind.Stopwatch, new[] { 3, 4, 5, 6 }),
            new ItemDef("food_ration", ItemUse.Food, food: 300),
        }, relicChargeTicks: 10);

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private sealed class HitCommand : ICommand
        {
            private readonly World _world;
            private readonly Unit _target;
            public int CostTicks => 1;
            public HitCommand(World world, Unit target) { _world = world; _target = target; }
            public void Execute(in TickContext context) => Combat.ApplyDamage(_world, context.Actor, _target, 3, DamageCause.Attack);
        }

        private static (World world, Unit player, Unit enemy, TimeRelicClock clock) Start(bool realTime = false)
        {
            var world = new World(GridMovementTests.Parse("#########", "#.......#", "#.......#", "#########"), 3UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            world.SetRelicRules(Items);
            world.Scheduler.AddSystem(TickPhase.ManaRegen, new TimeRelicSystem(world, Items));
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(2, 1), Magic.QuickSlotCount);
            var enemy = UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(3, 1), new IdleAi());
            world.Events.Flush();
            ITickClock inner = realTime ? new RealTimeClock(world.Scheduler, 1f) : (ITickClock)new TurnClock(world.Scheduler, world.PlayerInput);
            return (world, player, enemy, new TimeRelicClock(world, inner, realTime));
        }

        private static void Wear(World world, Unit player, string id)
        {
            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item(id));
            Relics.Equip(world, Items, player, System.Array.FindIndex(player.InventorySlots, e => e.Id == id));
        }

        // 껴야 씀 · 끼면 빈 빠른 칸에 자동 등록 · 처음 게이지 가득 → 쓰면 0 → 비면 Empty
        [Test]
        public void MustBeWorn_AutoQuickSlot_GaugeSpent()
        {
            var (world, player, _, clock) = Start();
            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("relic_stopwatch"));
            Assert.That(TimeRelics.Activate(world, Items, player, "relic_stopwatch"), Is.EqualTo(TimeRelicResult.NotWorn));
            Relics.Equip(world, Items, player, System.Array.FindIndex(player.InventorySlots, e => e.Id == "relic_stopwatch"));
            Assert.That(System.Array.Exists(player.QuickSlots, q => q.Id == "relic_stopwatch"), Is.True, "빠른 칸에 등록");
            Assert.That(TimeRelics.Charge(world, Items, player.Relics[0]), Is.EqualTo(3), "처음 = 가득");
            Assert.That(TimeRelics.Activate(world, Items, player, "relic_stopwatch"), Is.EqualTo(TimeRelicResult.Done));
            Assert.That((player.StopActions, TimeRelics.Charge(world, Items, player.Relics[0])), Is.EqualTo((3, 0)));
            for (int i = 0; i < 3; i++) clock.SubmitPlayerCommand(new WaitCommand(1)); // 행동 3번으로 다 씀
            Assert.That(TimeRelics.Activate(world, Items, player, "relic_stopwatch"), Is.EqualTo(TimeRelicResult.Empty));
        }

        // 스톱워치: 게이지 칸만큼 행동이 틱 없이 — 적에게 피해는 바로, 다 쓰면 다시 턴
        [Test]
        public void Stopwatch_ActionsWithoutTicks()
        {
            var (world, player, enemy, clock) = Start();
            Wear(world, player, "relic_stopwatch");
            TimeRelics.Activate(world, Items, player, "relic_stopwatch");
            long tick = world.Scheduler.CurrentTick;
            for (int i = 0; i < 3; i++) clock.SubmitPlayerCommand(new HitCommand(world, enemy));
            Assert.That(world.Scheduler.CurrentTick, Is.EqualTo(tick), "세계가 멈춤");
            Assert.That(world.Units.TryGet(enemy.Id, out _), Is.False, "피해 9 → 바로 죽음");
            Assert.That(player.StopActions, Is.EqualTo(0));
            clock.SubmitPlayerCommand(new WaitCommand(1));
            Assert.That(world.Scheduler.CurrentTick, Is.EqualTo(tick + 1), "다시 턴");
        }

        // 회중시계: 멈춘 턴만큼 세계가 흐르고 그동안 무적 · 새 상태 안 받음
        [Test]
        public void PocketWatch_FreezesMe_Invulnerable()
        {
            var (world, player, enemy, clock) = Start();
            Wear(world, player, "relic_pocket_watch");
            TimeRelics.Activate(world, Items, player, "relic_pocket_watch");
            Assert.That(player.TimeFrozenTicks, Is.EqualTo(5));
            Combat.ApplyDamage(world, enemy.Id, player, 7, DamageCause.Attack);
            Assert.That(player.Hp, Is.EqualTo(20), "무적");
            long tick = world.Scheduler.CurrentTick;
            clock.AdvanceWhileFrozen();
            Assert.That(world.Scheduler.CurrentTick, Is.EqualTo(tick + 5), "5턴 흐름");
            Assert.That(player.TimeFrozenTicks, Is.EqualTo(0));
            Assert.That(world.Scheduler.IsWaitingForPlayer, Is.True, "다시 내 차례");
        }

        // 가속기: 3턴 동안 한 턴에 2번 (앞 1번은 즉시, 둘째가 턴)
        [Test]
        public void Accelerator_TwoActionsPerTurn()
        {
            var (world, player, _, clock) = Start();
            Wear(world, player, "relic_accelerator");
            TimeRelics.Activate(world, Items, player, "relic_accelerator");
            Assert.That((player.AccelTicks, player.AccelActions), Is.EqualTo((3, 2)));
            long tick = world.Scheduler.CurrentTick;
            clock.SubmitPlayerCommand(new WaitCommand(1));
            Assert.That(world.Scheduler.CurrentTick, Is.EqualTo(tick), "첫 행동은 즉시");
            clock.SubmitPlayerCommand(new WaitCommand(1));
            Assert.That(world.Scheduler.CurrentTick, Is.EqualTo(tick + 1), "둘째가 턴");
            for (int i = 0; i < 4; i++) clock.SubmitPlayerCommand(new WaitCommand(1));
            Assert.That(player.AccelTicks, Is.EqualTo(0), "3턴 뒤 끝");
            long after = world.Scheduler.CurrentTick;
            clock.SubmitPlayerCommand(new WaitCommand(1));
            Assert.That(world.Scheduler.CurrentTick, Is.EqualTo(after + 1), "끝나면 보통");
        }

        // 실시간: 가속기 = 세계가 2배 느림, 스톱워치 = 세계가 멈춤
        [Test]
        public void RealTime_AcceleratorSlowsWorld_StopwatchStops()
        {
            var (world, player, _, clock) = Start(realTime: true);
            Wear(world, player, "relic_accelerator");
            TimeRelics.Activate(world, Items, player, "relic_accelerator");
            long tick = world.Scheduler.CurrentTick;
            clock.Update(1.2f);
            Assert.That(world.Scheduler.CurrentTick, Is.EqualTo(tick), "1.2초 = 0.6틱");
            clock.Update(1.0f);
            Assert.That(world.Scheduler.CurrentTick, Is.EqualTo(tick + 1));
            var (world2, player2, _, clock2) = Start(realTime: true);
            Wear(world2, player2, "relic_stopwatch");
            TimeRelics.Activate(world2, Items, player2, "relic_stopwatch");
            long stopped = world2.Scheduler.CurrentTick;
            clock2.Update(5f);
            Assert.That(world2.Scheduler.CurrentTick, Is.EqualTo(stopped), "스톱워치 = 멈춤");
        }

        // 게이지: 10턴마다 1칸 (최대까지)
        [Test]
        public void Gauge_RechargesEveryTenTurns()
        {
            var (world, player, _, clock) = Start();
            Wear(world, player, "relic_time_machine");
            Assert.That(TimeRelics.SpendRewind(world, Items, player, "relic_time_machine", 5), Is.EqualTo(TimeRelicResult.Done));
            Assert.That(TimeRelics.SpendRewind(world, Items, player, "relic_time_machine", 1), Is.EqualTo(TimeRelicResult.Empty));
            for (int i = 0; i < 20; i++) clock.SubmitPlayerCommand(new WaitCommand(1));
            Assert.That(TimeRelics.Charge(world, Items, player.Relics[0]), Is.EqualTo(2));
            for (int i = 0; i < 100; i++) clock.SubmitPlayerCommand(new WaitCommand(1));
            Assert.That(TimeRelics.Charge(world, Items, player.Relics[0]), Is.EqualTo(5), "최대까지");
        }

        // 타임머신 옮기기: 되돌린 세계에 지금 배낭 · 바닥 아이템 · 게이지를, 시간 효과는 끝냄
        [Test]
        public void CarryOver_KeepsBackpackAndFloorItems()
        {
            var (world, player, _, _) = Start();
            Wear(world, player, "relic_time_machine");
            Wear(world, player, "relic_stopwatch");
            var past = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => new IdleAi());
            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("food_ration"));
            world.FloorItems.Add(new GridPos(5, 2), "food_ration");
            TimeRelics.SpendRewind(world, Items, player, "relic_time_machine", 2);
            PlayerActions.TryGetPlayer(past, out var pastPlayer);
            TimeRelics.Activate(past, Items, pastPlayer, "relic_stopwatch");
            Assert.That(pastPlayer.StopActions, Is.EqualTo(3));
            TimeRelics.CarryOver(world, player, past, pastPlayer);
            Assert.That(System.Array.Exists(pastPlayer.InventorySlots, e => e.Id == "food_ration"), Is.True, "지금 배낭");
            Assert.That(past.FloorItems.Any(new GridPos(5, 2)), Is.True, "지금 바닥");
            Assert.That(TimeRelics.Charge(past, Items, pastPlayer.Relics[0]), Is.EqualTo(3), "쓴 게이지");
            Assert.That(pastPlayer.StopActions, Is.EqualTo(0), "시간 효과 끝");
        }

        // 스냅샷 v30: 정지 · 가속 · 스톱워치 · 게이지 왕복
        [Test]
        public void Snapshot_RoundTripsTimeState()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(30));
            var (world, player, _, clock) = Start();
            Wear(world, player, "relic_accelerator");
            TimeRelics.Activate(world, Items, player, "relic_accelerator");
            clock.SubmitPlayerCommand(new WaitCommand(1)); // 즉시 행동 1번
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => new IdleAi());
            PlayerActions.TryGetPlayer(restored, out var back);
            Assert.That((back.AccelTicks, back.AccelActions, back.AccelUsed), Is.EqualTo((3, 2, 1)));
            Assert.That(restored.Run.TryGetRelicCharge("relic_accelerator", out var c) && c == 0, Is.True);
        }

        // 화면 글
        [Test]
        public void HudText_TimeRelicLines()
        {
            var text = new HudText();
            Assert.That(HudText.TimeKindDesc(TimeRelicKind.Accelerator, 3, 2), Is.EqualTo("시간을 가속 — 3턴 동안 한 턴에 2번 행동"));
            Assert.That(text.TimeRelicNotWorn("relic_x"), Does.Contain("유물 칸에 껴야"));
            Assert.That(HudText.TimeMachineTurns(3), Is.EqualTo("3턴 전"));
        }

        // 프로젝트 에셋 = BALANCE 24번
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var book = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            Assert.That(book.RelicChargeTicks, Is.EqualTo(10));
            void Check(string id, TimeRelicKind kind, int[] gauge, int[] power)
            {
                Assert.That(book.TryGet(id, out var def) && def.TimeKind == kind, Is.True, id);
                Assert.That(def.TimeGauge, Is.EqualTo(gauge), id);
                Assert.That(def.TimePower, Is.EqualTo(power), id);
            }
            Check("relic_pocket_watch", TimeRelicKind.PocketWatch, new[] { 5, 6, 7, 8 }, new int[0]);
            Check("relic_time_machine", TimeRelicKind.TimeMachine, new[] { 5, 8, 11, 15 }, new int[0]);
            Check("relic_accelerator", TimeRelicKind.Accelerator, new[] { 3, 3, 4, 5 }, new[] { 2, 2, 3, 4 });
            Check("relic_stopwatch", TimeRelicKind.Stopwatch, new[] { 3, 4, 5, 6 }, new int[0]);
        }
    }
}
