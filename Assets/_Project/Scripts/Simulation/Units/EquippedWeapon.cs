using System;

namespace ProjectM.Units
{
    /// <summary>
    /// 유닛이 든 무기의 전투 수치 (3-7a). 장착할 때 아이템 데이터(Items.WeaponDef)에서 계산해 유닛에 둔다 — 공격 판정이 아이템 표를 몰라도 된다.
    /// 저장 대상 (스냅샷 v13). 빈 값(Empty) = 맨손 → 근접 피해는 Unit.AttackDamage 고정, 명중 배수 100%, 비용은 기본 행동 비용, 거리 1.
    /// </summary>
    public readonly struct EquippedWeapon : IEquatable<EquippedWeapon>
    {
        /// <summary>무기 아이템 ID (ItemCatalog). 맨손이면 null.</summary>
        public string ItemId { get; }
        public int MinDamage { get; }
        public int MaxDamage { get; }
        /// <summary>명중 배수 (%) — 손도끼 132 · 손낫 68.</summary>
        public int AccuracyPercent { get; }
        /// <summary>공격 한 번의 행동 비용 (틱) — 창 2.</summary>
        public int AttackCostTicks { get; }
        /// <summary>공격 거리 (칸, 체비셰프) — 창 2. 2칸은 직선 · 대각선이고 사이 칸이 비어 있어야 한다.</summary>
        public int Reach { get; }
        /// <summary>강화 단계 (3-7b) — 내려놓으면 인벤토리 칸에 같이 남는다.</summary>
        public int Upgrade { get; }
        /// <summary>요구 힘 (3-7b) — 모자라면 1당 명중 ÷ 1.5, 남으면 1당 보너스 피해 0~1. 0 = 요구 없음.</summary>
        public int StrengthRequirement { get; }
        /// <summary>기습 때 최소 피해를 (최대 − 최소)의 이 %만큼 올림 (3-7c 더크 67). 0 = 없음.</summary>
        public int SurpriseMinPercent { get; }
        /// <summary>방어 (3-7c 육척봉 2) — 근접 피해를 0~방어 굴림만큼 줄인다.</summary>
        public int Defense { get; }

        public EquippedWeapon(string itemId, int minDamage, int maxDamage, int accuracyPercent, int attackCostTicks, int reach,
            int upgrade = 0, int strengthRequirement = 0, int surpriseMinPercent = 0, int defense = 0)
        {
            if (surpriseMinPercent < 0 || surpriseMinPercent > 100) throw new ArgumentOutOfRangeException(nameof(surpriseMinPercent));
            if (defense < 0) throw new ArgumentOutOfRangeException(nameof(defense));
            SurpriseMinPercent = surpriseMinPercent;
            Defense = defense;
            if (upgrade < 0) throw new ArgumentOutOfRangeException(nameof(upgrade));
            if (strengthRequirement < 0) throw new ArgumentOutOfRangeException(nameof(strengthRequirement));
            Upgrade = upgrade;
            StrengthRequirement = strengthRequirement;
            if (string.IsNullOrEmpty(itemId)) throw new ArgumentException("무기 ID가 비어 있다 — 맨손은 Empty.", nameof(itemId));
            if (minDamage < 0 || maxDamage < minDamage) throw new ArgumentOutOfRangeException(nameof(maxDamage), $"피해 {minDamage}~{maxDamage}");
            if (accuracyPercent < 1) throw new ArgumentOutOfRangeException(nameof(accuracyPercent));
            if (attackCostTicks < 1) throw new ArgumentOutOfRangeException(nameof(attackCostTicks));
            if (reach < 1 || reach > 2) throw new ArgumentOutOfRangeException(nameof(reach), reach, "공격 거리는 1 또는 2.");
            ItemId = itemId;
            MinDamage = minDamage;
            MaxDamage = maxDamage;
            AccuracyPercent = accuracyPercent;
            AttackCostTicks = attackCostTicks;
            Reach = reach;
        }

        public static EquippedWeapon Empty => default;
        public bool IsEmpty => ItemId == null;

        public bool Equals(EquippedWeapon o) => ItemId == o.ItemId && MinDamage == o.MinDamage && MaxDamage == o.MaxDamage
            && AccuracyPercent == o.AccuracyPercent && AttackCostTicks == o.AttackCostTicks && Reach == o.Reach
            && Upgrade == o.Upgrade && StrengthRequirement == o.StrengthRequirement
            && SurpriseMinPercent == o.SurpriseMinPercent && Defense == o.Defense;
        public override bool Equals(object obj) => obj is EquippedWeapon o && Equals(o);
        public override int GetHashCode() => (ItemId?.GetHashCode() ?? 0) ^ (MaxDamage * 397);
        public override string ToString() => IsEmpty ? "(맨손)" : $"{ItemId}+{Upgrade} {MinDamage}~{MaxDamage} 힘 {StrengthRequirement}";
    }
}
