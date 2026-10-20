using System;

namespace ProjectM.Units
{
    /// <summary>
    /// 유닛이 입은 갑옷의 수치 (3-10a, SPD). 입을 때 아이템 데이터(Items.ArmorDef)에서 계산해 유닛에 둔다 — 방어 판정이 아이템 표를 몰라도 된다.
    /// 방어 범위는 SPD 크기(나누기 전)로 두고, 굴린 뒤 힘 모자람 벌점을 빼고 DefenseDivisor로 나눈다 (MeleeWeapon.ApplyDefense).
    /// 저장 대상 (스냅샷 v17). Empty = 맨몸.
    /// </summary>
    public readonly struct EquippedArmor : IEquatable<EquippedArmor>
    {
        /// <summary>갑옷 아이템 ID (ItemCatalog). 맨몸이면 null.</summary>
        public string ItemId { get; }
        /// <summary>티어 (SPD 1 천 ~ 5 판금).</summary>
        public int Tier { get; }
        /// <summary>강화 단계 — 벗으면 인벤토리 칸에 같이 남는다.</summary>
        public int Upgrade { get; }
        /// <summary>요구 힘 — 모자라면 1당 방어 굴림 − DeficitPenalty · 회피 ÷ 1.5.</summary>
        public int StrengthRequirement { get; }
        /// <summary>방어 굴림 최소 · 최대 (SPD 크기, 나누기 전) — 최소 = 강화, 최대 = 티어 × (2 + 강화).</summary>
        public int DefenseMin { get; }
        public int DefenseMax { get; }
        /// <summary>힘 1 모자랄 때마다 방어 굴림에서 빼는 양 (SPD 크기 — SPD 2).</summary>
        public int DeficitPenalty { get; }
        /// <summary>방어 굴림을 나누는 값 (무기 피해 나누기와 같음 — 3).</summary>
        public int DefenseDivisor { get; }

        public EquippedArmor(string itemId, int tier, int upgrade, int strengthRequirement, int defenseMin, int defenseMax, int deficitPenalty, int defenseDivisor)
        {
            if (string.IsNullOrEmpty(itemId)) throw new ArgumentException("갑옷 ID가 비어 있다 — 맨몸은 Empty.", nameof(itemId));
            if (tier < 1) throw new ArgumentOutOfRangeException(nameof(tier));
            if (upgrade < 0) throw new ArgumentOutOfRangeException(nameof(upgrade));
            if (strengthRequirement < 0) throw new ArgumentOutOfRangeException(nameof(strengthRequirement));
            if (defenseMin < 0 || defenseMax < defenseMin) throw new ArgumentOutOfRangeException(nameof(defenseMax), $"방어 {defenseMin}~{defenseMax}");
            if (deficitPenalty < 0) throw new ArgumentOutOfRangeException(nameof(deficitPenalty));
            if (defenseDivisor < 1) throw new ArgumentOutOfRangeException(nameof(defenseDivisor));
            ItemId = itemId;
            Tier = tier;
            Upgrade = upgrade;
            StrengthRequirement = strengthRequirement;
            DefenseMin = defenseMin;
            DefenseMax = defenseMax;
            DeficitPenalty = deficitPenalty;
            DefenseDivisor = defenseDivisor;
        }

        public static EquippedArmor Empty => default;
        public bool IsEmpty => ItemId == null;

        /// <summary>화면용 방어 범위 (나눈 뒤, 힘 벌점 없음) — "방어 0~1".</summary>
        public int ShownMin => Divide(DefenseMin, DefenseDivisor);
        public int ShownMax => Divide(DefenseMax, DefenseDivisor);

        internal static int Divide(int a, int b) => b <= 1 ? a : (int)Math.Round((double)a / b, MidpointRounding.AwayFromZero);

        public bool Equals(EquippedArmor o) => ItemId == o.ItemId && Tier == o.Tier && Upgrade == o.Upgrade && StrengthRequirement == o.StrengthRequirement
            && DefenseMin == o.DefenseMin && DefenseMax == o.DefenseMax && DeficitPenalty == o.DeficitPenalty && DefenseDivisor == o.DefenseDivisor;
        public override bool Equals(object obj) => obj is EquippedArmor o && Equals(o);
        public override int GetHashCode() => (ItemId?.GetHashCode() ?? 0) ^ (DefenseMax * 397) ^ Upgrade;
        public override string ToString() => IsEmpty ? "(맨몸)" : $"{ItemId}+{Upgrade} 방어 {DefenseMin}~{DefenseMax}/{DefenseDivisor} 힘 {StrengthRequirement}";
    }
}
