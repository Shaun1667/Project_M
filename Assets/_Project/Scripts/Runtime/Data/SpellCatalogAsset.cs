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
            [Tooltip("남기는 지형 효과 (TerrainCatalog). Bolt = 멈춘 칸, Beam = 지나간 칸, Burst = 폭발 범위")] public TerrainEffectType LeavesTerrain;
            [Tooltip("HUD · 로그에 보이는 이름 (표시 전용, 규칙에 쓰지 않음). 비면 ID")] public string DisplayName;
        }

        [SerializeField] private List<Entry> _spells = new List<Entry>();

        /// <summary>statuses · terrains = 주문이 거는 상태 · 남기는 지형 규칙 (StatusCatalogAsset · TerrainCatalogAsset).</summary>
        public SpellBook ToBook(StatusBook statuses = null, TerrainBook terrains = null, ReactionBook reactions = null)
        {
            var defs = new List<SpellDef>(_spells.Count);
            foreach (var e in _spells)
                defs.Add(new SpellDef(e.Id, e.Element, e.Shape, e.Range, e.Damage, e.ManaCost, e.Radius, e.AppliesStatus, e.StatusChancePercent,
                    e.LeavesTerrain));
            return new SpellBook(defs, statuses, terrains, reactions);
        }

        /// <summary>표시 이름 표 (ID → 이름). 이름이 빈 항목은 넣지 않는다 — 없으면 ID를 쓴다.</summary>
        public Dictionary<string, string> DisplayNames()
        {
            var names = new Dictionary<string, string>();
            foreach (var e in _spells)
                if (!string.IsNullOrEmpty(e.Id) && !string.IsNullOrEmpty(e.DisplayName)) names[e.Id] = e.DisplayName;
            return names;
        }

        /// <summary>에디터 Setup용: 그 ID 항목의 표시 이름이 비어 있을 때만 채운다 (2-5).</summary>
        public bool SetDisplayNameIfEmpty(string id, string displayName)
        {
            for (int i = 0; i < _spells.Count; i++)
            {
                if (_spells[i].Id != id || !string.IsNullOrEmpty(_spells[i].DisplayName)) continue;
                var e = _spells[i];
                e.DisplayName = displayName;
                _spells[i] = e;
                return true;
            }
            return false;
        }

        /// <summary>에디터 Setup용: 그 ID 항목의 지형 필드만 채운다 (2-4에서 기존 항목에 추가할 때). 이미 지형이 있으면 그대로.</summary>
        public bool SetTerrainIfNone(string id, TerrainEffectType terrain)
        {
            for (int i = 0; i < _spells.Count; i++)
            {
                if (_spells[i].Id != id || _spells[i].LeavesTerrain != TerrainEffectType.None) continue;
                var e = _spells[i];
                e.LeavesTerrain = terrain;
                _spells[i] = e;
                return true;
            }
            return false;
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
