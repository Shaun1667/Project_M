using System.Collections.Generic;
using ProjectM.Dungeon;
using ProjectM.Magic;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 함정 표 + 배치 수치 (Assets/_Project/Data/Dungeon/TrapCatalog.asset, 3-12a). 기준은 BALANCE.md 17번.
    /// 효과는 SpellCatalog의 함정 주문(trap_*)이 맡고, 주문으로 안 되는 것(순간이동 · 경보)만 Special.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Trap Catalog", fileName = "TrapCatalog")]
    public sealed class TrapCatalogAsset : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public TrapType Type;
            [Tooltip("효과 주문 (SpellCatalog) — 함정 칸 중심으로 시전자 없이. 비면 주문 효과 없음")] public string SpellId;
            public TrapSpecial Special;
            [Tooltip("배치 무게 (클수록 자주)")] [Min(1)] public int Weight;
        }

        [Tooltip("층마다 함정 수")] [Min(0)] [SerializeField] private int _trapsPerFloor = 4;
        [Tooltip("숨겨진 채 놓일 확률 %")] [Range(0, 100)] [SerializeField] private int _hiddenPercent = 85;
        [Tooltip("층 시작 칸에서 최소 거리 (칸)")] [Min(0)] [SerializeField] private int _minDistanceFromStart = 3;
        [SerializeField] private List<Entry> _traps = new List<Entry>();

        [Header("찾기 (3-12b — BALANCE.md 17번)")]
        [Tooltip("찾기 행동(Z)이 확실히 찾는 반경 (칸)")] [Min(0)] [SerializeField] private int _searchRadius = 2;
        [Tooltip("찾기 행동 비용 (틱)")] [Min(1)] [SerializeField] private int _searchCostTicks = 2;
        [Tooltip("저절로 발견: 반경 안의 숨겨진 것마다 매 턴 확률 %")] [Range(0, 100)] [SerializeField] private int _passiveSearchPercent = 10;
        [Tooltip("저절로 발견 반경 (칸)")] [Min(0)] [SerializeField] private int _passiveSearchRadius = 1;

        public SearchConfig ToSearch() => new SearchConfig(_searchRadius, _searchCostTicks, _passiveSearchPercent, _passiveSearchRadius);

        public TrapBook ToBook(SpellBook spells)
        {
            var defs = new List<TrapDef>(_traps.Count);
            foreach (var e in _traps)
                defs.Add(new TrapDef(e.Type, e.SpellId, e.Special, e.Weight));
            return new TrapBook(defs, spells, _trapsPerFloor, _hiddenPercent, _minDistanceFromStart);
        }

        /// <summary>에디터 Setup용: 같은 종류가 없을 때만 추가한다 (사용자가 고친 값은 그대로).</summary>
        public bool AddIfMissing(Entry entry)
        {
            foreach (var e in _traps)
                if (e.Type == entry.Type) return false;
            _traps.Add(entry);
            return true;
        }
    }
}
