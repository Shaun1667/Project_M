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
        /// <summary>젖음 (3-2): 그 자체 효과 없음 — 반응의 트리거 (증발, 3-3 빙결 · 감전).</summary>
        Wet = 5,
        /// <summary>빙결 (3-3a): 행동 불가 — 젖음 × 얼음 · 결빙 · 얼음 + 물 융합으로 확정. 불(해동) · 번개(파쇄)로 풀린다. 면역 없음 (결정 2026-10-18).</summary>
        Frozen = 6,
        /// <summary>신속 (3-13a 신속 물약): 이동 한 번에 두 칸까지 (MoveSteps 2) — 결정 2026-11-04 "이동 두 번에 1틱".</summary>
        Haste = 7,
        /// <summary>투명 (3-13a 투명 물약): 적이 보지 못한다 (HiddenFromEnemies).</summary>
        Invisible = 8,
        /// <summary>부양 (3-13a 부양 물약): 지형 효과 · 함정 · 풀 밟기를 받지 않는다 (Levitates).</summary>
        Levitating = 9,
        /// <summary>정신 시야 (3-13a 정신 시야 물약): 층의 모든 적이 보인다 (SensesEnemies).</summary>
        MindVision = 10,
    }

    /// <summary>
    /// 상태 이상 하나의 규칙 데이터 (2-3, Q5 임시값). 값은 StatusCatalogAsset에서 온다 — 기준은 BALANCE.md 6번.
    /// 효과는 종류별 코드가 아니라 이 필드들로만 정한다: 틱당 피해 · 행동 비용 추가 · 행동 불가 · 이동 칸 수 · 적에게 숨음 · 부양 · 적 감지 (3-13a).
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
        /// <summary>이동 한 번에 가는 최대 칸 수 (3-13a 신속 = 2). 1 = 보통.</summary>
        public int MoveSteps { get; }
        /// <summary>적이 이 유닛을 보지 못한다 (3-13a 투명).</summary>
        public bool HiddenFromEnemies { get; }
        /// <summary>떠 있다 — 지형 효과 · 함정 · 풀 밟기를 받지 않는다 (3-13a 부양).</summary>
        public bool Levitates { get; }
        /// <summary>층의 모든 적이 화면에 보인다 (3-13a 정신 시야 — 플레이어만).</summary>
        public bool SensesEnemies { get; }

        public StatusDef(StatusType type, int durationTicks, int damagePerTick = 0, int extraActionCostTicks = 0, bool preventsAction = false,
            int moveSteps = 1, bool hiddenFromEnemies = false, bool levitates = false, bool sensesEnemies = false)
        {
            if (moveSteps < 1) throw new ArgumentOutOfRangeException(nameof(moveSteps), moveSteps, "이동 칸 수는 1 이상이어야 한다.");
            MoveSteps = moveSteps;
            HiddenFromEnemies = hiddenFromEnemies;
            Levitates = levitates;
            SensesEnemies = sensesEnemies;
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
