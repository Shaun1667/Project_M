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
        }

        [SerializeField] private List<Entry> _statuses = new List<Entry>();

        public StatusBook ToBook()
        {
            var defs = new List<StatusDef>(_statuses.Count);
            foreach (var e in _statuses)
                defs.Add(new StatusDef(e.Type, e.DurationTicks, e.DamagePerTick, e.ExtraActionCostTicks, e.PreventsAction));
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
