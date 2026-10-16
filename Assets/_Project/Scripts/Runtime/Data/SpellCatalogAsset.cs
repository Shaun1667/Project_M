using System;
using System.Collections.Generic;
using ProjectM.Magic;
using ProjectM.Status;
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
            [Tooltip("맞은 유닛에 거는 상태 이상 (StatusCatalog). None = 없음")] public StatusType AppliesStatus;
            [Tooltip("상태가 걸릴 확률 % (맞은 유닛마다 따로)")] [Range(0, 100)] public int StatusChancePercent;
        }

        [SerializeField] private List<Entry> _spells = new List<Entry>();

        /// <summary>statuses = 주문이 거는 상태 규칙 (StatusCatalogAsset.ToBook()).</summary>
        public SpellBook ToBook(StatusBook statuses = null)
        {
            var defs = new List<SpellDef>(_spells.Count);
            foreach (var e in _spells)
                defs.Add(new SpellDef(e.Id, e.Element, e.Shape, e.Range, e.Damage, e.ManaCost, e.Radius, e.AppliesStatus, e.StatusChancePercent));
            return new SpellBook(defs, statuses);
        }

        /// <summary>에디터 Setup용: 그 ID 항목의 상태 이상 필드만 채운다 (2-3에서 기존 항목에 추가할 때). 이미 상태가 있으면 그대로.</summary>
        public bool SetStatusIfNone(string id, StatusType status, int chancePercent)
        {
            for (int i = 0; i < _spells.Count; i++)
            {
                if (_spells[i].Id != id || _spells[i].AppliesStatus != StatusType.None) continue;
                var e = _spells[i];
                e.AppliesStatus = status;
                e.StatusChancePercent = chancePercent;
                _spells[i] = e;
                return true;
            }
            return false;
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
