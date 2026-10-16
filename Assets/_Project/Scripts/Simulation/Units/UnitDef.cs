using System;

namespace ProjectM.Units
{
    /// <summary>
    /// 유닛 종류 하나의 수치 (Q3 임시값, 결정 2026-10-16). 값은 데이터 에셋(UnitDefAsset)에서 온다 — 기준은 BALANCE.md 3번.
    /// </summary>
    public sealed class UnitDef
    {
        /// <summary>표현 레이어가 모습을 고르는 ID (EntityVisualCatalog).</summary>
        public string VisualId { get; }
        public int MaxHp { get; }
        /// <summary>근접 공격 한 번의 고정 피해.</summary>
        public int AttackDamage { get; }
        /// <summary>시야 반지름 (원형, 벽에 가려짐). 실제 판정은 ISightRule.</summary>
        public int SightRange { get; }

        public UnitDef(string visualId, int maxHp, int attackDamage, int sightRange)
        {
            if (string.IsNullOrEmpty(visualId)) throw new ArgumentException("VisualId가 비어 있다.", nameof(visualId));
            if (maxHp < 1) throw new ArgumentOutOfRangeException(nameof(maxHp), maxHp, "최대 체력은 1 이상이어야 한다.");
            if (attackDamage < 0) throw new ArgumentOutOfRangeException(nameof(attackDamage), attackDamage, "피해는 0 이상이어야 한다.");
            if (sightRange < 1) throw new ArgumentOutOfRangeException(nameof(sightRange), sightRange, "시야 거리는 1 이상이어야 한다.");

            VisualId = visualId;
            MaxHp = maxHp;
            AttackDamage = attackDamage;
            SightRange = sightRange;
        }
    }
}
