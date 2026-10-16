using System;
using System.Collections.Generic;
using ProjectM.Magic;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 주문 데이터 묶음 (Assets/_Project/Data/Spells/SpellCatalog.asset). 기준은 BALANCE.md 5번 (Q4 임시값).
    /// 주문 하나 = 항목 하나. ID는 장착 칸(UnitDefAsset._startingSpells)과 같아야 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Spell Catalog", fileName = "SpellCatalog")]
    public sealed class SpellCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Id;
            public Element Element;
            public SpellShape Shape;
            [Tooltip("사거리 (칸, 대각선 포함)")] [Min(1)] public int Range;
            [Tooltip("맞은 유닛 하나당 피해")] [Min(0)] public int Damage;
            [Min(0)] public int ManaCost;
            [Tooltip("범위 폭발(Burst)만: 반지름. 나머지는 0")] [Min(0)] public int Radius;
        }

        [SerializeField] private List<Entry> _spells = new List<Entry>();

        public SpellBook ToBook()
        {
            var defs = new List<SpellDef>(_spells.Count);
            foreach (var e in _spells)
                defs.Add(new SpellDef(e.Id, e.Element, e.Shape, e.Range, e.Damage, e.ManaCost, e.Radius));
            return new SpellBook(defs);
        }

        /// <summary>에디터 Setup용: 같은 ID가 없을 때만 추가한다 (있으면 사용자가 고친 값을 건드리지 않음).</summary>
        public bool AddIfMissing(Entry entry)
        {
            foreach (var e in _spells)
                if (e.Id == entry.Id) return false;
            _spells.Add(entry);
            return true;
        }
    }
}
