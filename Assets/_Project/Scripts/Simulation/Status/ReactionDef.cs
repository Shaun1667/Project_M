using System;
using System.Collections.Generic;
using ProjectM.Magic;

namespace ProjectM.Status
{
    /// <summary>반응을 일으키는 대상 종류. 끝에만 추가한다 (에셋이 정수로 저장).</summary>
    public enum ReactionTrigger
    {
        /// <summary>맞은 유닛에 걸린 상태 이상.</summary>
        Status = 0,
        /// <summary>주문이 닿은 칸의 지형 효과.</summary>
        Terrain = 1,
    }

    /// <summary>
    /// 반응 하나 = "상태/지형 × 속성 → 결과" 규칙 한 줄 (3-1, REACTIONS.md). 값은 ReactionCatalogAsset에서 온다 — BALANCE.md 8번.
    /// 결과는 종류별 코드가 아니라 이 필드들의 조합으로만 정한다: 추가 피해 · 폭발 · 트리거 소모 · 지형 변환(3-2).
    /// </summary>
    public sealed class ReactionDef
    {
        public string Id { get; }
        /// <summary>"같은 반응은 대상당 1회" 판정 묶음 (예: 독화는 상태 · 지형 두 줄이 한 그룹). 비면 Id.</summary>
        public string Group { get; }
        public ReactionTrigger Trigger { get; }
        /// <summary>Trigger = Status일 때 대상 상태.</summary>
        public StatusType Status { get; }
        /// <summary>Trigger = Terrain일 때 대상 지형.</summary>
        public TerrainEffectType Terrain { get; }
        /// <summary>반응을 일으키는 주문 속성.</summary>
        public Element Element { get; }
        /// <summary>발동 순서 (큰 것 먼저, 원본 4-3).</summary>
        public int Priority { get; }

        /// <summary>대상(상태 반응) · 그 칸 유닛(지형 반응)에게 주는 추가 피해. 상한은 ReactionConfig.</summary>
        public int ExtraDamage { get; }
        /// <summary>발동 뒤 트리거 상태 · 지형을 없앤다.</summary>
        public bool ConsumeTrigger { get; }

        /// <summary>폭발 반지름 (체비셰프, 중심 = 대상 · 그 칸). 0이면 폭발 없음.</summary>
        public int ExplosionRadius { get; }
        /// <summary>폭발 피해 (고정). ExplosionDamageFromRemaining이면 무시.</summary>
        public int ExplosionDamage { get; }
        /// <summary>폭발 피해 = 트리거의 남은 틱 × 1 (유독성 폭발).</summary>
        public bool ExplosionDamageFromRemaining { get; }
        /// <summary>폭발에 맞은 유닛에 거는 상태 (None = 없음) · 확률.</summary>
        public StatusType ExplosionStatus { get; }
        public int ExplosionStatusChancePercent { get; }
        /// <summary>지형 반응: 트리거 지형을 이 지형으로 바꾼다 — 남은 틱 유지 (3-2 오염: 물 → 오염된 물). None = 없음.</summary>
        public TerrainEffectType TransformTerrainTo { get; }

        public ReactionDef(string id, ReactionTrigger trigger, StatusType status, TerrainEffectType terrain, Element element, int priority,
            int extraDamage = 0, bool consumeTrigger = false,
            int explosionRadius = 0, int explosionDamage = 0, bool explosionDamageFromRemaining = false,
            StatusType explosionStatus = StatusType.None, int explosionStatusChancePercent = 100, string group = null,
            TerrainEffectType transformTerrainTo = TerrainEffectType.None)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("반응 ID가 비어 있다.", nameof(id));
            if (trigger == ReactionTrigger.Status && status == StatusType.None) throw new ArgumentException("상태 반응에 상태가 없다: " + id);
            if (trigger == ReactionTrigger.Terrain && terrain == TerrainEffectType.None) throw new ArgumentException("지형 반응에 지형이 없다: " + id);
            if (extraDamage < 0 || explosionRadius < 0 || explosionDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(extraDamage), "피해 · 반지름은 0 이상: " + id);
            if (explosionStatusChancePercent < 0 || explosionStatusChancePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(explosionStatusChancePercent), "확률은 0 ~ 100: " + id);
            if (transformTerrainTo != TerrainEffectType.None && (trigger != ReactionTrigger.Terrain || consumeTrigger || transformTerrainTo == terrain))
                throw new ArgumentException("지형 변환은 지형 반응에서만, 소모와 함께 쓸 수 없고, 다른 지형으로만: " + id);

            Id = id;
            Group = string.IsNullOrEmpty(group) ? id : group;
            Trigger = trigger;
            Status = status;
            Terrain = terrain;
            Element = element;
            Priority = priority;
            ExtraDamage = extraDamage;
            ConsumeTrigger = consumeTrigger;
            ExplosionRadius = explosionRadius;
            ExplosionDamage = explosionDamage;
            ExplosionDamageFromRemaining = explosionDamageFromRemaining;
            ExplosionStatus = explosionStatus;
            ExplosionStatusChancePercent = explosionStatusChancePercent;
            TransformTerrainTo = transformTerrainTo;
        }
    }

    /// <summary>반응 안전장치 (원본 4-5 + D6). 값은 ReactionCatalogAsset — BALANCE.md 8번.</summary>
    public sealed class ReactionConfig
    {
        /// <summary>주문 하나로 일어나는 반응 최대 횟수.</summary>
        public int MaxReactionsPerCast { get; }
        /// <summary>추가 피해 상한 = max(주문 피해, 이 값).</summary>
        public int ExtraDamageCapMin { get; }
        /// <summary>폭발 피해 상한 = max(주문 피해, 이 값) (D6: 폭발은 상한을 올림).</summary>
        public int ExplosionDamageCapMin { get; }

        public ReactionConfig(int maxReactionsPerCast, int extraDamageCapMin, int explosionDamageCapMin)
        {
            if (maxReactionsPerCast < 1) throw new ArgumentOutOfRangeException(nameof(maxReactionsPerCast));
            if (extraDamageCapMin < 0) throw new ArgumentOutOfRangeException(nameof(extraDamageCapMin));
            if (explosionDamageCapMin < 0) throw new ArgumentOutOfRangeException(nameof(explosionDamageCapMin));
            MaxReactionsPerCast = maxReactionsPerCast;
            ExtraDamageCapMin = extraDamageCapMin;
            ExplosionDamageCapMin = explosionDamageCapMin;
        }

        public int ExtraDamageCap(int spellDamage) => Math.Max(spellDamage, ExtraDamageCapMin);
        public int ExplosionDamageCap(int spellDamage) => Math.Max(spellDamage, ExplosionDamageCapMin);
    }

    /// <summary>
    /// 반응 규칙 표. 같은 (트리거 · 대상 · 속성) 조합은 한 줄만. 폭발이 거는 상태가 StatusBook에 없으면 예외.
    /// </summary>
    public sealed class ReactionBook
    {
        private readonly List<ReactionDef> _reactions = new List<ReactionDef>();

        public ReactionConfig Config { get; }
        public int Count => _reactions.Count;
        public IReadOnlyList<ReactionDef> All => _reactions;

        public ReactionBook(IEnumerable<ReactionDef> reactions, ReactionConfig config, StatusBook statuses)
        {
            if (reactions == null) throw new ArgumentNullException(nameof(reactions));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            foreach (var r in reactions)
            {
                if (r == null) throw new ArgumentException("null 반응", nameof(reactions));
                if (_reactions.Exists(x => x.Id == r.Id)) throw new ArgumentException("같은 반응 ID가 두 번: " + r.Id, nameof(reactions));
                if (_reactions.Exists(x => x.Trigger == r.Trigger && x.Status == r.Status && x.Terrain == r.Terrain && x.Element == r.Element))
                    throw new ArgumentException("같은 대상 · 속성 조합이 두 번: " + r.Id, nameof(reactions));
                if (r.ExplosionStatus != StatusType.None && !statuses.TryGet(r.ExplosionStatus, out _))
                    throw new ArgumentException($"반응 {r.Id}의 상태 {r.ExplosionStatus}가 StatusBook에 없다.", nameof(reactions));
                _reactions.Add(r);
            }
        }

        public static ReactionBook Empty => new ReactionBook(Array.Empty<ReactionDef>(), new ReactionConfig(1, 0, 0), new StatusBook(Array.Empty<StatusDef>()));

        public ReactionDef FindStatus(StatusType status, Element element) =>
            _reactions.Find(r => r.Trigger == ReactionTrigger.Status && r.Status == status && r.Element == element);

        public ReactionDef FindTerrain(TerrainEffectType terrain, Element element) =>
            _reactions.Find(r => r.Trigger == ReactionTrigger.Terrain && r.Terrain == terrain && r.Element == element);
    }
}
