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
            [Tooltip("그 칸에 있는 유닛이 매 틱 받는 피해 (3-2 오염된 물)")] [Min(0)] public int DamagePerTick;
            [Tooltip("이 지형이 있는 칸에는 놓이지 않는다 (3-2: 물 타일은 화염 지대 위에 안 깔림)")] public TerrainEffectType BlockedBy;
        }

        [SerializeField] private List<Entry> _terrains = new List<Entry>();

        [Header("불 번짐 (3-11b — BALANCE.md 7번)")]
        [Tooltip("풀이 타는 동안 놓이는 화염 지대 지속 (틱) — 풀 점화 반응의 지속과 같게")] [Min(1)] [SerializeField] private int _grassBurnTicks = 3;
        [Tooltip("한 틱에 새로 불이 붙는 최대 칸 수")] [Min(1)] [SerializeField] private int _fireSpreadMaxPerTick = 8;

        public FireSpreadConfig ToFireSpread() => new FireSpreadConfig(_grassBurnTicks, _fireSpreadMaxPerTick);

        public TerrainBook ToBook(StatusBook statuses)
        {
            var defs = new List<TerrainDef>(_terrains.Count);
            foreach (var e in _terrains)
                defs.Add(new TerrainDef(e.Type, e.DurationTicks, e.AppliesStatus, e.DamagePerTick, e.BlockedBy));
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
