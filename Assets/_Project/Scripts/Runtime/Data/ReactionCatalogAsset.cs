using System;
using System.Collections.Generic;
using ProjectM.Magic;
using ProjectM.Status;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 반응 규칙 표 (Assets/_Project/Data/Reactions/ReactionCatalog.asset, 3-1). 기준은 REACTIONS.md · BALANCE.md 8번.
    /// 반응 하나 = 항목 하나. 마법 조합을 코드에 쓰지 않고 이 표로만 정의한다 (규칙 9).
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Reaction Catalog", fileName = "ReactionCatalog")]
    public sealed class ReactionCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Id;
            [Tooltip("로그에 보이는 이름 (표시 전용). 비면 ID")] public string DisplayName;
            [Tooltip("'같은 반응은 대상당 1회' 묶음. 비면 ID")] public string Group;
            public ReactionTrigger Trigger;
            [Tooltip("Trigger = Status일 때")] public StatusType Status;
            [Tooltip("Trigger = Terrain일 때")] public TerrainEffectType Terrain;
            public Element Element;
            [Tooltip("큰 것 먼저")] public int Priority;
            [Min(0)] public int ExtraDamage;
            public bool ConsumeTrigger;
            [Tooltip("0 = 폭발 없음")] [Min(0)] public int ExplosionRadius;
            [Min(0)] public int ExplosionDamage;
            [Tooltip("폭발 피해 = 트리거의 남은 틱 × 1")] public bool ExplosionDamageFromRemaining;
            public StatusType ExplosionStatus;
            [Range(0, 100)] public int ExplosionStatusChancePercent;
            [Tooltip("지형 반응: 트리거 지형을 이 지형으로 바꾼다 (3-2 오염)")] public TerrainEffectType TransformTerrainTo;
            [Tooltip("변환 뒤 지속 (틱). 0 = 남은 틱 유지 (3-3a 결빙 8 · 해동 12)")] [Min(0)] public int TransformDurationTicks;
            [Tooltip("대상 · 그 칸 유닛에 거는 상태 (3-3a 빙결)")] public StatusType TargetStatus;
            [Range(0, 100)] public int TargetStatusChancePercent;
        }

        [Header("안전장치 (REACTIONS.md 2-5)")]
        [Min(1)] [SerializeField] private int _maxReactionsPerCast = 4;
        [Tooltip("추가 피해 상한 = max(주문 피해, 이 값)")]
        [Min(0)] [SerializeField] private int _extraDamageCapMin = 3;
        [Tooltip("폭발 피해 상한 = max(주문 피해, 이 값)")]
        [Min(0)] [SerializeField] private int _explosionDamageCapMin = 5;

        [SerializeField] private List<Entry> _reactions = new List<Entry>();

        public ReactionBook ToBook(StatusBook statuses)
        {
            var defs = new List<ReactionDef>(_reactions.Count);
            foreach (var e in _reactions)
            {
                defs.Add(new ReactionDef(e.Id, e.Trigger, e.Status, e.Terrain, e.Element, e.Priority,
                    e.ExtraDamage, e.ConsumeTrigger, e.ExplosionRadius, e.ExplosionDamage, e.ExplosionDamageFromRemaining,
                    e.ExplosionStatus, e.ExplosionStatusChancePercent, e.Group, e.TransformTerrainTo, e.TransformDurationTicks,
                    e.TargetStatus, e.TargetStatusChancePercent));
            }
            return new ReactionBook(defs, new ReactionConfig(_maxReactionsPerCast, _extraDamageCapMin, _explosionDamageCapMin), statuses);
        }

        /// <summary>표시 이름 표 (ID → 이름). 이름이 빈 항목은 넣지 않는다.</summary>
        public Dictionary<string, string> DisplayNames()
        {
            var names = new Dictionary<string, string>();
            foreach (var e in _reactions)
                if (!string.IsNullOrEmpty(e.Id) && !string.IsNullOrEmpty(e.DisplayName)) names[e.Id] = e.DisplayName;
            return names;
        }

        /// <summary>에디터 Setup용: 같은 ID가 없을 때만 추가한다 (있으면 사용자가 고친 값을 건드리지 않음).</summary>
        public bool AddIfMissing(Entry entry)
        {
            foreach (var e in _reactions)
                if (e.Id == entry.Id) return false;
            _reactions.Add(entry);
            return true;
        }
    }
}
