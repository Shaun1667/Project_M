using System;
using System.Collections.Generic;

namespace ProjectM.Units
{
    /// <summary>
    /// 유닛 종류 하나의 수치 (Q3 임시값, 결정 2026-10-16). 값은 데이터 에셋(UnitDefAsset)에서 온다 — 기준은 BALANCE.md 3번 · 4번.
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
        /// <summary>최대 마나 (2-1). 0이면 마나 없음 — 회복도 없다.</summary>
        public int MaxMana { get; }
        /// <summary>시작할 때 장착 칸 앞쪽부터 채우는 주문 ID (2-1). 주문 내용은 2-2 주문 데이터.</summary>
        public IReadOnlyList<string> StartingSpells { get; }
        /// <summary>명중 (3-7, SPD attackSkill). 근접 공격 굴림 0~명중.</summary>
        public int Accuracy { get; }
        /// <summary>회피 (3-7, SPD defenseSkill). 0이면 근접 공격이 항상 맞는다.</summary>
        public int Evasion { get; }
        /// <summary>처치 경험치 (3-6, SPD EXP).</summary>
        public int ExpReward { get; }
        /// <summary>경험치를 주는 플레이어 최고 레벨 (3-6, SPD maxLvl).</summary>
        public int ExpMaxLevel { get; }
        /// <summary>힘 (3-7b, SPD — 플레이어 10). 무기를 들지 않는 적은 0.</summary>
        public int Strength { get; }
        /// <summary>처음 입는 갑옷 아이템 ID (3-10a, SPD 천 갑옷). null = 맨몸.</summary>
        public string StartingArmor { get; }

        public UnitDef(string visualId, int maxHp, int attackDamage, int sightRange, int maxMana = 0, IReadOnlyList<string> startingSpells = null,
            int accuracy = 0, int evasion = 0, int expReward = 0, int expMaxLevel = 0, int strength = 0, string startingArmor = null)
        {
            StartingArmor = string.IsNullOrEmpty(startingArmor) ? null : startingArmor;
            if (strength < 0) throw new ArgumentOutOfRangeException(nameof(strength), strength, "힘은 0 이상이어야 한다.");
            Strength = strength;
            if (accuracy < 0) throw new ArgumentOutOfRangeException(nameof(accuracy), accuracy, "명중은 0 이상이어야 한다.");
            if (evasion < 0) throw new ArgumentOutOfRangeException(nameof(evasion), evasion, "회피는 0 이상이어야 한다.");
            if (expReward < 0) throw new ArgumentOutOfRangeException(nameof(expReward), expReward, "경험치는 0 이상이어야 한다.");
            if (expMaxLevel < 0) throw new ArgumentOutOfRangeException(nameof(expMaxLevel), expMaxLevel, "레벨은 0 이상이어야 한다.");
            Accuracy = accuracy;
            Evasion = evasion;
            ExpReward = expReward;
            ExpMaxLevel = expMaxLevel;
            if (string.IsNullOrEmpty(visualId)) throw new ArgumentException("VisualId가 비어 있다.", nameof(visualId));
            if (maxHp < 1) throw new ArgumentOutOfRangeException(nameof(maxHp), maxHp, "최대 체력은 1 이상이어야 한다.");
            if (attackDamage < 0) throw new ArgumentOutOfRangeException(nameof(attackDamage), attackDamage, "피해는 0 이상이어야 한다.");
            if (sightRange < 1) throw new ArgumentOutOfRangeException(nameof(sightRange), sightRange, "시야 거리는 1 이상이어야 한다.");
            if (maxMana < 0) throw new ArgumentOutOfRangeException(nameof(maxMana), maxMana, "최대 마나는 0 이상이어야 한다.");

            var spells = new List<string>();
            if (startingSpells != null)
            {
                foreach (var spell in startingSpells)
                {
                    if (string.IsNullOrEmpty(spell)) throw new ArgumentException("시작 주문 ID가 비어 있다.", nameof(startingSpells));
                    spells.Add(spell);
                }
            }

            VisualId = visualId;
            MaxHp = maxHp;
            AttackDamage = attackDamage;
            SightRange = sightRange;
            MaxMana = maxMana;
            StartingSpells = spells;
        }
    }
}
