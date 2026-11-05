using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Items
{
    /// <summary>쓰는 시간 유물 종류 (3-28c). 끝에만 추가한다.</summary>
    public enum TimeRelicKind
    {
        None = 0,
        /// <summary>마도구 회중시계: 내 시간을 멈춘다 — 게이지 칸 = 멈춘 턴, 그동안 행동 못 함 · 무적 · 내 상태 · 허기 · 회복 멈춤. 세계는 흐른다.</summary>
        PocketWatch = 1,
        /// <summary>타임머신: 세계를 n턴 전으로 되돌린다 (n ≤ 게이지 · 기록) — 배낭 · 유물 · 게이지 · 바닥 아이템 · 에테르는 지금 그대로. 되돌리기는 Runtime(기록 · 다시 연결).</summary>
        TimeMachine = 2,
        /// <summary>가속기: 게이지 전부를 써서 그 턴 수 동안 한 턴에 TimePower번 행동. 실시간 모드에서는 세계가 TimePower배 느려진다.</summary>
        Accelerator = 3,
        /// <summary>스톱워치: 나를 뺀 모든 것의 시간을 멈춘다 — 게이지 칸 = 멈춘 세계에서 할 수 있는 행동 수 (틱 없음, 피해는 바로).</summary>
        Stopwatch = 4,
    }

    public enum TimeRelicResult
    {
        Done = 0,
        NotTimeRelic = 1,
        /// <summary>유물 칸에 끼지 않았다 (결정 2026-11-05 — 사용자: 껴야 쓸 수 있음).</summary>
        NotWorn = 2,
        Empty = 3,
        AlreadyActive = 4,
    }

    /// <summary>
    /// 시간 유물 (3-28c, 결정 2026-11-05 — 사용자): 회중시계 · 타임머신 · 가속기 · 스톱워치. 유물 칸에 껴야 쓰고, 빠른 칸에 등록해 발동한다 (틱 밖).
    /// 게이지 = RunState 유물 ID별 칸 (처음 = 가득, 스냅샷 v30), RelicChargeTicks 틱마다 1칸 (TimeRelicSystem — 틱 번호로, 상태 없음).
    /// 진행 중 효과는 Unit 필드 (TimeFrozenTicks · AccelTicks/Actions/Used · StopActions) — 규칙은 여기 한 곳, 시계는 TimeRelicClock.
    /// </summary>
    public static class TimeRelics
    {
        public static bool IsTimeRelic(ItemBook items, string id, out ItemDef def)
        {
            def = null;
            return items != null && id != null && items.TryGet(id, out def) && def.Use == ItemUse.Relic && def.TimeKind != TimeRelicKind.None;
        }

        private static int At(IReadOnlyList<int> values, int upgrade) =>
            values == null || values.Count == 0 ? 0 : values[Math.Min(Math.Max(0, upgrade), values.Count - 1)];

        /// <summary>게이지 최대 (강화 단계 값).</summary>
        public static int MaxCharge(ItemBook items, InventoryRef relic) =>
            IsTimeRelic(items, relic.Id, out var def) ? At(def.TimeGauge, relic.Upgrade) : 0;

        /// <summary>가속기 한 턴 행동 수 (강화 단계 값, 최소 2).</summary>
        public static int Power(ItemBook items, InventoryRef relic) =>
            IsTimeRelic(items, relic.Id, out var def) ? Math.Max(2, At(def.TimePower, relic.Upgrade)) : 2;

        /// <summary>지금 게이지 (아직 기록이 없으면 가득).</summary>
        public static int Charge(World world, ItemBook items, InventoryRef relic) =>
            world.Run.TryGetRelicCharge(relic.Id, out var c) ? Math.Min(c, MaxCharge(items, relic)) : MaxCharge(items, relic);

        /// <summary>낀 유물 중 그 종류의 칸 번호, 없으면 −1.</summary>
        public static int WornSlot(Unit unit, string relicId)
        {
            for (int i = 0; i < unit.Relics.Length; i++)
                if (!unit.Relics[i].IsEmpty && unit.Relics[i].Id == relicId) return i;
            return -1;
        }

        public static bool IsFrozen(World world, EntityId id) => world.Units.TryGet(id, out var u) && u.TimeFrozenTicks > 0;
        public static bool IsWorldStopped(World world) => PlayerActions.TryGetPlayer(world, out var p) && p.StopActions > 0;

        /// <summary>
        /// 발동 (틱 밖 — 입력이 부르고 이벤트를 비운다). 타임머신은 여기서 쓰지 않는다 (SpendCharge + Runtime 되돌리기).
        /// 회중시계 = 게이지 전부 → 멈춘 턴 · 가속기 = 게이지 전부 → 가속 턴 · 스톱워치 = 게이지 전부 → 행동 수.
        /// </summary>
        public static TimeRelicResult Activate(World world, ItemBook items, Unit player, string relicId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (!IsTimeRelic(items, relicId, out var def) || def.TimeKind == TimeRelicKind.TimeMachine) return TimeRelicResult.NotTimeRelic;
            int slot = WornSlot(player, relicId);
            if (slot < 0) return TimeRelicResult.NotWorn;
            var relic = player.Relics[slot];
            int charge = Charge(world, items, relic);
            if (charge <= 0) return TimeRelicResult.Empty;
            if (player.TimeFrozenTicks > 0 || player.StopActions > 0 || (def.TimeKind == TimeRelicKind.Accelerator && player.AccelTicks > 0))
                return TimeRelicResult.AlreadyActive;

            world.Run.SetRelicCharge(relicId, 0);
            switch (def.TimeKind)
            {
                case TimeRelicKind.PocketWatch: player.TimeFrozenTicks = charge; break;
                case TimeRelicKind.Accelerator:
                    player.AccelTicks = charge;
                    player.AccelActions = Power(items, relic);
                    player.AccelUsed = 0;
                    break;
                case TimeRelicKind.Stopwatch: player.StopActions = charge; break;
            }
            world.Events.Publish(new TimeRelicUsedEvent(player.Id, relicId, def.TimeKind, charge, Power(items, relic)));
            return TimeRelicResult.Done;
        }

        /// <summary>타임머신 (Runtime이 되돌리기 전에): 낀 타임머신이고 게이지 ≥ turns면 turns칸을 쓴다. 쓸 수 있는 최대 턴 = 지금 게이지.</summary>
        public static TimeRelicResult SpendRewind(World world, ItemBook items, Unit player, string relicId, int turns)
        {
            if (!IsTimeRelic(items, relicId, out var def) || def.TimeKind != TimeRelicKind.TimeMachine) return TimeRelicResult.NotTimeRelic;
            int slot = WornSlot(player, relicId);
            if (slot < 0) return TimeRelicResult.NotWorn;
            int charge = Charge(world, items, player.Relics[slot]);
            if (turns < 1 || charge < turns) return TimeRelicResult.Empty;
            world.Run.SetRelicCharge(relicId, charge - turns);
            return TimeRelicResult.Done;
        }

        /// <summary>
        /// 타임머신 되돌리기 뒤 (3-28c, 결정 2026-11-05 — "소모된 도구 제외"): 되돌린 세계의 플레이어에게 지금의 배낭 · 빠른 칸 · 유물 · 게이지 · 에테르 · 금화,
        /// 지금 층의 바닥 아이템을 옮긴다 (주운 것이 바닥에 다시 생기거나 쓴 물약이 돌아오지 않게). 시간 효과(정지 · 가속 · 스톱워치)는 끝낸다. 이벤트는 내지 않는다 (부른 쪽이 화면에 알림).
        /// </summary>
        public static void CarryOver(World from, Unit fromPlayer, World to, Unit toPlayer)
        {
            if (from == null || fromPlayer == null || to == null || toPlayer == null) throw new ArgumentNullException(nameof(from));
            for (int i = 0; i < toPlayer.InventorySlots.Length && i < fromPlayer.InventorySlots.Length; i++) toPlayer.InventorySlots[i] = fromPlayer.InventorySlots[i];
            for (int i = 0; i < toPlayer.QuickSlots.Length && i < fromPlayer.QuickSlots.Length; i++) toPlayer.QuickSlots[i] = fromPlayer.QuickSlots[i];
            int hpGain = toPlayer.MaxHp - fromPlayer.MaxHp; // 유물(생명석) 차이는 지금 기준
            for (int i = 0; i < toPlayer.Relics.Length && i < fromPlayer.Relics.Length; i++) toPlayer.Relics[i] = fromPlayer.Relics[i];
            if (hpGain != 0)
            {
                toPlayer.MaxHp = Math.Max(1, toPlayer.MaxHp - hpGain);
                toPlayer.Hp = Math.Max(1, Math.Min(toPlayer.Hp, toPlayer.MaxHp));
            }
            toPlayer.TimeFrozenTicks = 0;
            toPlayer.AccelTicks = 0;
            toPlayer.AccelActions = 0;
            toPlayer.AccelUsed = 0;
            toPlayer.StopActions = 0;
            to.Run.SetRelicCharges(from.Run.RelicCharges);
            to.Run.Ether = from.Run.Ether;
            to.Run.Gold = from.Run.Gold;
            if (to.Floor == from.Floor)
            {
                var keep = new List<FloorItem>(from.FloorItems.All);
                foreach (var item in new List<FloorItem>(to.FloorItems.All)) to.FloorItems.Remove(item.Pos, item.ItemId);
                foreach (var item in keep) to.FloorItems.Add(item.Pos, item.ItemId, item.Quantity, item.Price);
            }
        }

        // ── 시계 쪽 판단 (TimeRelicClock이 부른다) ──

        /// <summary>스톱워치 중 행동 하나: 즉시 실행 · 남은 행동 −1. 끝나면 TimeRelicEndedEvent.</summary>
        internal static void UseStopAction(World world, Unit player)
        {
            player.StopActions = Math.Max(0, player.StopActions - 1);
            if (player.StopActions == 0) world.Events.Publish(new TimeRelicEndedEvent(player.Id, TimeRelicKind.Stopwatch));
        }
    }

    /// <summary>
    /// ⑤ 시간 유물 (3-28c): 회중시계 · 가속 남은 틱 −1 (끝나면 TimeRelicEndedEvent) · 가속 턴의 즉시 행동 수 초기화 ·
    /// RelicChargeTicks 틱마다 낀 시간 유물 게이지 +1 (최대까지). 틱 번호로만 판단 — 상태를 갖지 않는다.
    /// </summary>
    public sealed class TimeRelicSystem : ITickSystem
    {
        private readonly World _world;
        private readonly ItemBook _items;

        public TimeRelicSystem(World world, ItemBook items)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public void Run(in TickContext context)
        {
            if (!PlayerActions.TryGetPlayer(_world, out var player) || !player.IsAlive) return;
            if (player.TimeFrozenTicks > 0 && --player.TimeFrozenTicks == 0)
                _world.Events.Publish(new TimeRelicEndedEvent(player.Id, TimeRelicKind.PocketWatch));
            if (player.AccelTicks > 0)
            {
                player.AccelUsed = 0;
                if (--player.AccelTicks == 0)
                {
                    player.AccelActions = 0;
                    _world.Events.Publish(new TimeRelicEndedEvent(player.Id, TimeRelicKind.Accelerator));
                }
            }
            if ((context.Tick + 1) % _items.RelicChargeTicks != 0) return;
            foreach (var relic in player.Relics)
            {
                if (relic.IsEmpty || !TimeRelics.IsTimeRelic(_items, relic.Id, out _)) continue;
                int max = TimeRelics.MaxCharge(_items, relic);
                int now = TimeRelics.Charge(_world, _items, relic);
                if (now < max) _world.Run.SetRelicCharge(relic.Id, now + 1);
            }
        }
    }

    /// <summary>
    /// 시간 유물 시계 (3-28c): 판의 시계(턴제 · 실시간)를 감싼다 — 상태는 모두 플레이어 유닛에 (규칙 6).
    /// 스톱워치 중 = 행동은 즉시 · 틱 없음 · 남은 행동 −1, 세계(Update)도 멈춤. 가속 중 = 턴제는 한 턴에 AccelActions번(앞 n−1번은 즉시, 마지막이 턴),
    /// 실시간은 세계가 AccelActions배 느려짐(Update 시간을 나눔). 회중시계 중 = 턴제는 행동 없이 틱이 흐른다(AdvanceWhileFrozen).
    /// </summary>
    public sealed class TimeRelicClock : ITickClock
    {
        private readonly World _world;
        private readonly ITickClock _inner;
        private readonly bool _realTime;

        public ITickClock Inner => _inner;

        public TimeRelicClock(World world, ITickClock inner, bool realTime)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _realTime = realTime;
        }

        public void SubmitPlayerCommand(ICommand command)
        {
            if (PlayerActions.TryGetPlayer(_world, out var player))
            {
                if (player.StopActions > 0)
                {
                    if (_world.Scheduler.ExecutePlayerNow(command)) TimeRelics.UseStopAction(_world, player);
                    return;
                }
                if (!_realTime && player.AccelTicks > 0 && player.AccelUsed < player.AccelActions - 1)
                {
                    if (_world.Scheduler.ExecutePlayerNow(command)) player.AccelUsed++;
                    return;
                }
            }
            _inner.SubmitPlayerCommand(command);
        }

        public void Update(float deltaSeconds)
        {
            if (!PlayerActions.TryGetPlayer(_world, out var player)) return;
            if (player.StopActions > 0) return; // 스톱워치: 세계가 멈춤
            float scale = _realTime && player.AccelTicks > 0 && player.AccelActions > 1 ? 1f / player.AccelActions : 1f;
            _inner.Update(deltaSeconds * scale);
        }

        /// <summary>턴제 회중시계: 플레이어가 멈춘 동안 틱을 흘린다 (발동 직후 입력이 부른다). 실시간은 할 일 없음.</summary>
        public void AdvanceWhileFrozen()
        {
            if (_realTime) return;
            int guard = 0;
            while (PlayerActions.TryGetPlayer(_world, out var player) && player.TimeFrozenTicks > 0 && guard++ < TurnClock.MaxTicksPerSubmit)
                _world.Scheduler.Tick();
        }
    }

    /// <summary>시간 유물을 썼다 (3-28c — HUD 로그 · 안내). Amount = 쓴 게이지(턴 · 행동 수), Power = 가속 행동 수.</summary>
    public sealed class TimeRelicUsedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public string RelicId { get; }
        public TimeRelicKind Kind { get; }
        public int Amount { get; }
        public int Power { get; }

        public TimeRelicUsedEvent(EntityId unit, string relicId, TimeRelicKind kind, int amount, int power)
        {
            Unit = unit;
            RelicId = relicId;
            Kind = kind;
            Amount = amount;
            Power = power;
        }
    }

    /// <summary>시간 유물 효과가 끝났다 (3-28c — 로그).</summary>
    public sealed class TimeRelicEndedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public TimeRelicKind Kind { get; }

        public TimeRelicEndedEvent(EntityId unit, TimeRelicKind kind)
        {
            Unit = unit;
            Kind = kind;
        }
    }
}
