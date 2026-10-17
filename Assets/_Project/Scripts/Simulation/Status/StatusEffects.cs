using System;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Status
{
    /// <summary>
    /// 상태 이상 적용 · 처리 규칙 (2-3). 상태를 바꾸는 곳은 여기 하나.
    /// 같은 상태 중첩 = 지속 갱신 (남은 틱을 더 긴 쪽으로, 강도는 그대로 — 결정 2026-10-16, 규칙 10).
    /// </summary>
    public static class StatusEffects
    {
        public static void Apply(World world, Unit unit, StatusDef def)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (!unit.IsAlive) return;

            var existing = unit.Statuses.Find(s => s.Type == def.Type);
            if (existing != null)
            {
                existing.Remaining = Math.Max(existing.Remaining, def.DurationTicks);
                world.Events.Publish(new StatusChangedEvent(unit.Id, def.Type, existing.Remaining, true));
                return;
            }

            var instance = new StatusInstance(def.Type, def.DurationTicks);
            int index = unit.Statuses.FindIndex(s => s.Type > def.Type);
            if (index < 0) unit.Statuses.Add(instance);
            else unit.Statuses.Insert(index, instance);
            world.Events.Publish(new StatusChangedEvent(unit.Id, def.Type, instance.Remaining, true));
        }

        /// <summary>상태를 푼다 (반응의 트리거 소모 등). 없으면 아무 일 없음. 풀렸으면 true.</summary>
        public static bool Remove(World world, Unit unit, StatusType type)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            int index = unit.Statuses.FindIndex(s => s.Type == type);
            if (index < 0) return false;
            unit.Statuses.RemoveAt(index);
            world.Events.Publish(new StatusChangedEvent(unit.Id, type, 0, false));
            return true;
        }

        /// <summary>chancePercent(0~100) 확률로 건다. 100 미만이면 전투 난수(RngStream.Combat)를 한 번 굴린다. 걸렸으면 true.</summary>
        public static bool TryApply(World world, Unit unit, StatusDef def, int chancePercent)
        {
            if (chancePercent <= 0) return false;
            if (chancePercent < 100 && world.Rng.Get(RngStream.Combat).Range(0, 100) >= chancePercent) return false;
            Apply(world, unit, def);
            return true;
        }

        /// <summary>행동할 수 있는지: PreventsAction 상태(마비)가 걸려 있으면 false.</summary>
        public static bool CanAct(World world, StatusBook book, EntityId id)
        {
            if (!world.Units.TryGet(id, out var unit)) return true;
            foreach (var s in unit.Statuses)
            {
                if (book.TryGet(s.Type, out var def) && def.PreventsAction) return false;
            }
            return true;
        }

        /// <summary>행동 비용 조정: 걸린 상태들의 ExtraActionCostTicks를 더한다 (둔화).</summary>
        public static int AdjustCost(World world, StatusBook book, EntityId id, int cost)
        {
            if (!world.Units.TryGet(id, out var unit)) return cost;
            foreach (var s in unit.Statuses)
            {
                if (book.TryGet(s.Type, out var def)) cost += def.ExtraActionCostTicks;
            }
            return cost;
        }
    }

    /// <summary>
    /// ④ 상태 처리 (GDD 3-3): 유닛 EntityId 순 · 상태 종류 순으로 틱당 피해 → 남은 틱 1 감소 → 0이면 해제.
    /// 피해로 체력 0이 된 유닛은 ⑥ 사망 판정에서 빠진다. 상태를 갖지 않는다.
    /// </summary>
    public sealed class StatusSystem : ITickSystem
    {
        private readonly World _world;
        private readonly StatusBook _book;

        public StatusSystem(World world, StatusBook book)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _book = book ?? throw new ArgumentNullException(nameof(book));
        }

        public void Run(in TickContext context)
        {
            foreach (var unit in _world.Units.ToSortedList())
            {
                for (int i = 0; i < unit.Statuses.Count; i++)
                {
                    var status = unit.Statuses[i];
                    if (unit.IsAlive && _book.TryGet(status.Type, out var def) && def.DamagePerTick > 0)
                        Combat.ApplyDamage(_world, EntityId.None, unit, def.DamagePerTick);

                    status.Remaining--;
                    _world.Events.Publish(new StatusChangedEvent(unit.Id, status.Type, status.Remaining, false));
                    if (status.Remaining <= 0)
                    {
                        unit.Statuses.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 상태가 바뀌었다: Applied = 새로 걸림 · 갱신, 아니면 ④에서 1 줄어듦. Remaining 0 = 풀림.
    /// </summary>
    public sealed class StatusChangedEvent : ISimEvent
    {
        public EntityId Id { get; }
        public StatusType Type { get; }
        public int Remaining { get; }
        public bool Applied { get; }

        public StatusChangedEvent(EntityId id, StatusType type, int remaining, bool applied)
        {
            Id = id;
            Type = type;
            Remaining = remaining;
            Applied = applied;
        }
    }
}
