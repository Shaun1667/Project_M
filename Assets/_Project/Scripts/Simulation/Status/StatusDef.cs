using System;
using System.Collections.Generic;

namespace ProjectM.Status
{
    /// <summary>상태 이상 종류 (2-3). 끝에만 추가한다 (에셋이 정수로 저장). None = 상태 없음.</summary>
    public enum StatusType
    {
        None = 0,
        Burn = 1,
        Slow = 2,
        Poison = 3,
        Paralyze = 4,
    }

    /// <summary>
    /// 상태 이상 하나의 규칙 데이터 (2-3, Q5 임시값). 값은 StatusCatalogAsset에서 온다 — 기준은 BALANCE.md 6번.
    /// 효과는 종류별 코드가 아니라 이 필드들로만 정한다: 틱당 피해 · 행동 비용 추가 · 행동 불가.
    /// </summary>
    public sealed class StatusDef
    {
        public StatusType Type { get; }
        /// <summary>걸렸을 때의 지속 (틱). 걸린 틱의 ④ 단계부터 센다.</summary>
        public int DurationTicks { get; }
        /// <summary>④ 상태 처리 단계마다 받는 피해.</summary>
        public int DamagePerTick { get; }
        /// <summary>걸려 있는 동안 모든 행동 비용에 더하는 틱 (둔화).</summary>
        public int ExtraActionCostTicks { get; }
        /// <summary>걸려 있는 동안 차례가 와도 행동하지 못한다 (마비).</summary>
        public bool PreventsAction { get; }

        public StatusDef(StatusType type, int durationTicks, int damagePerTick = 0, int extraActionCostTicks = 0, bool preventsAction = false)
        {
            if (type == StatusType.None) throw new ArgumentException("None은 상태가 아니다.", nameof(type));
            if (durationTicks < 1) throw new ArgumentOutOfRangeException(nameof(durationTicks), durationTicks, "지속은 1틱 이상이어야 한다.");
            if (damagePerTick < 0) throw new ArgumentOutOfRangeException(nameof(damagePerTick), damagePerTick, "틱당 피해는 0 이상이어야 한다.");
            if (extraActionCostTicks < 0) throw new ArgumentOutOfRangeException(nameof(extraActionCostTicks), extraActionCostTicks, "행동 비용 추가는 0 이상이어야 한다.");

            Type = type;
            DurationTicks = durationTicks;
            DamagePerTick = damagePerTick;
            ExtraActionCostTicks = extraActionCostTicks;
            PreventsAction = preventsAction;
        }
    }

    /// <summary>상태 종류 → 규칙 데이터. 같은 종류가 두 번 나오면 예외.</summary>
    public sealed class StatusBook
    {
        private readonly Dictionary<StatusType, StatusDef> _defs = new Dictionary<StatusType, StatusDef>();

        public StatusBook(IEnumerable<StatusDef> defs)
        {
            if (defs == null) throw new ArgumentNullException(nameof(defs));
            foreach (var def in defs)
            {
                if (def == null) throw new ArgumentException("null 상태", nameof(defs));
                if (_defs.ContainsKey(def.Type)) throw new ArgumentException("같은 상태가 두 번: " + def.Type, nameof(defs));
                _defs.Add(def.Type, def);
            }
        }

        public int Count => _defs.Count;

        public bool TryGet(StatusType type, out StatusDef def) => _defs.TryGetValue(type, out def);
    }

    /// <summary>유닛에 걸린 상태 하나 (저장 대상). Remaining = 남은 ④ 처리 횟수.</summary>
    public sealed class StatusInstance
    {
        public StatusType Type { get; }
        public int Remaining { get; internal set; }

        public StatusInstance(StatusType type, int remaining)
        {
            Type = type;
            Remaining = remaining;
        }
    }
}
