using System;
using System.Collections.Generic;
using ProjectM.Magic;
using ProjectM.Status;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 융합 규칙 표 (Assets/_Project/Data/Fusions/FusionCatalog.asset, 3-2b). 기준은 REACTIONS.md 5번 · BALANCE.md 10번.
    /// 피해 · 모양 · 사거리 · 마나는 두 주문에서 계산한다 (FusionBook.Build) — 여기에는 결과만.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Fusion Catalog", fileName = "FusionCatalog")]
    public sealed class FusionCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Id;
            [Tooltip("로그 · 조준 안내에 보이는 이름 (표시 전용). 비면 ID")] public string DisplayName;
            [Tooltip("순서 상관없음")] public Element ElementA;
            public Element ElementB;
            [Tooltip("남기는 지형 (None = 없음)")] public TerrainEffectType LeavesTerrain;
            [Tooltip("지형 지속 (틱). 0이면 지형 규칙의 지속")] [Min(0)] public int TerrainDurationTicks;
            [Tooltip("맞은 유닛에 거는 상태 (None = 없음)")] public StatusType AppliesStatus;
            [Range(0, 100)] public int StatusChancePercent;
        }

        [SerializeField] private List<Entry> _fusions = new List<Entry>();

        public FusionBook ToBook(StatusBook statuses, TerrainBook terrains)
        {
            var defs = new List<FusionDef>(_fusions.Count);
            foreach (var e in _fusions)
                defs.Add(new FusionDef(e.Id, e.ElementA, e.ElementB, e.LeavesTerrain, e.TerrainDurationTicks, e.AppliesStatus, e.StatusChancePercent));
            return new FusionBook(defs, statuses, terrains);
        }

        /// <summary>ID → 표시 이름 (이름이 빈 항목은 빠짐).</summary>
        public Dictionary<string, string> DisplayNames()
        {
            var names = new Dictionary<string, string>();
            foreach (var e in _fusions)
                if (!string.IsNullOrEmpty(e.Id) && !string.IsNullOrEmpty(e.DisplayName)) names[e.Id] = e.DisplayName;
            return names;
        }

        // 인스펙터에서 새 줄을 넣으면 확률이 0으로 시작한다 — 상태를 고르고 확률을 비워 두면 조용히 안 걸리므로 알린다
        private void OnValidate()
        {
            foreach (var e in _fusions)
                if (e.AppliesStatus != StatusType.None && e.StatusChancePercent == 0)
                    Debug.LogWarning($"[FusionCatalog] {e.Id}: 상태 {e.AppliesStatus}를 거는데 확률이 0%다.", this);
        }

        /// <summary>에디터 Setup용: 같은 ID가 없을 때만 추가한다.</summary>
        public bool AddIfMissing(Entry entry)
        {
            foreach (var e in _fusions)
                if (e.Id == entry.Id) return false;
            _fusions.Add(entry);
            return true;
        }
    }
}
