using System;
using System.Collections.Generic;
using ProjectM.Status;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 상태 이상 규칙 묶음 (Assets/_Project/Data/Statuses/StatusCatalog.asset). 기준은 BALANCE.md 6번 (Q5 임시값).
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Status Catalog", fileName = "StatusCatalog")]
    public sealed class StatusCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public StatusType Type;
            [Tooltip("지속 (틱)")] [Min(1)] public int DurationTicks;
            [Tooltip("④ 단계마다 받는 피해")] [Min(0)] public int DamagePerTick;
            [Tooltip("걸린 동안 행동 비용에 더하는 틱 (둔화)")] [Min(0)] public int ExtraActionCostTicks;
            [Tooltip("걸린 동안 행동 불가 (마비)")] public bool PreventsAction;
            [Tooltip("이동 한 번에 가는 최대 칸 수 (3-13a 신속 = 2). 0 · 1 = 보통")] [Min(0)] public int MoveSteps;
            [Tooltip("적이 보지 못함 (3-13a 투명)")] public bool HiddenFromEnemies;
            [Tooltip("지형 효과 · 함정 · 풀 밟기를 받지 않음 (3-13a 부양)")] public bool Levitates;
            [Tooltip("층의 모든 적이 보임 (3-13a 정신 시야)")] public bool SensesEnemies;
            [Tooltip("피해를 받으면 풀림 (3-13b 잠)")] public bool WakesOnDamage;
            [Tooltip("플레이어에게서 도망 (3-13b 공포)")] public bool Flees;
        }

        [SerializeField] private List<Entry> _statuses = new List<Entry>();

        public StatusBook ToBook()
        {
            var defs = new List<StatusDef>(_statuses.Count);
            foreach (var e in _statuses)
                defs.Add(new StatusDef(e.Type, e.DurationTicks, e.DamagePerTick, e.ExtraActionCostTicks, e.PreventsAction,
                    Math.Max(1, e.MoveSteps), e.HiddenFromEnemies, e.Levitates, e.SensesEnemies, e.WakesOnDamage, e.Flees));
            return new StatusBook(defs);
        }

        /// <summary>에디터 Setup용: 같은 종류가 없을 때만 추가한다.</summary>
        public bool AddIfMissing(Entry entry)
        {
            foreach (var e in _statuses)
                if (e.Type == entry.Type) return false;
            _statuses.Add(entry);
            return true;
        }
    }
}
