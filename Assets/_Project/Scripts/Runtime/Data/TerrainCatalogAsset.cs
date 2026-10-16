using System;
using System.Collections.Generic;
using ProjectM.Status;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 지형 효과 규칙 묶음 (Assets/_Project/Data/Terrains/TerrainCatalog.asset). 기준은 BALANCE.md 7번 (Q8 임시값).
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Terrain Catalog", fileName = "TerrainCatalog")]
    public sealed class TerrainCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TerrainEffectType Type;
            [Tooltip("지속 (틱)")] [Min(1)] public int DurationTicks;
            [Tooltip("그 칸에 있는 유닛에게 매 틱 거는 상태 (StatusCatalog)")] public StatusType AppliesStatus;
        }

        [SerializeField] private List<Entry> _terrains = new List<Entry>();

        public TerrainBook ToBook(StatusBook statuses)
        {
            var defs = new List<TerrainDef>(_terrains.Count);
            foreach (var e in _terrains)
                defs.Add(new TerrainDef(e.Type, e.DurationTicks, e.AppliesStatus));
            return new TerrainBook(defs, statuses);
        }

        /// <summary>에디터 Setup용: 같은 종류가 없을 때만 추가한다.</summary>
        public bool AddIfMissing(Entry entry)
        {
            foreach (var e in _terrains)
                if (e.Type == entry.Type) return false;
            _terrains.Add(entry);
            return true;
        }
    }
}
