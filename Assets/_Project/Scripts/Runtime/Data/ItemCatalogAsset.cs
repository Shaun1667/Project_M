using System;
using System.Collections.Generic;
using ProjectM.Items;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 아이템 규칙 묶음 (Assets/_Project/Data/Items/ItemCatalog.asset, 3-2a). 기준은 BALANCE.md 9번.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Id;
            [Tooltip("인벤토리 · 빠른 칸에 보이는 이름 (표시 전용). 비면 ID")] public string DisplayName;
            [Tooltip("사용하면 하는 일")] public ItemUse Use;
            [Tooltip("마법서(LearnSpell)만: 가르치는 주문 ID (SpellCatalog, 3-5)")] public string SpellId;
            [Header("무기 (EquipWeapon만, 3-7a — SPD 크기, 나누기 전)")]
            [Tooltip("강화 0 최대 피해 — 소검 15 (Docs/장비_데이터.md)")] public int MaxDamage;
            [Tooltip("강화 1단계마다 최대 피해 + — 창 4, 나머지 3")] public int UpgradeEfficiency;
            [Tooltip("명중 배수 — 손도끼 1.32 · 손낫 0.68 · 나머지 1")] public float Accuracy;
            [Tooltip("공격 한 번의 행동 비용 (틱) — 창 2, 나머지 1")] public int AttackCostTicks;
            [Tooltip("공격 거리 (칸) — 창 2, 나머지 1")] public int Reach;
            [Tooltip("강화 0 요구 힘 (3-7b) — SPD 2티어 12. 강화하면 첫 단계 −1, 그 뒤 2단계마다 −1")] public int StrengthRequirement;
            [Tooltip("기습 최소 피해 상승 % (3-7c) — 더크 67, 나머지 0")] public int SurpriseMinPercent;
            [Tooltip("방어 (3-7c) — 육척봉 2(근접 피해 − 0~2), 나머지 0")] public int Defense;
            [Header("갑옷 (EquipArmor만, 3-10a — SPD)")]
            [Tooltip("티어 1 천 ~ 5 판금. 방어 = 강화 ~ 티어 × (2 + 강화), 나누기는 공통 값. 요구 힘은 위 StrengthRequirement")] public int Tier;
            [Header("힘 물약 · 강화 주문서 (3-7b)")]
            [Tooltip("힘 물약 = 힘 +, 강화 주문서 = 강화 + (둘 다 1)")] public int Amount;
            [Header("물약 · 주문서 (3-9, BALANCE 14번)")]
            [Tooltip("Potion · Scroll만: 효과")] public ConsumableEffect Effect;
            [Tooltip("층마다 무작위로 놓을 때의 무게 (SPD). 0 = 무작위로 안 놓음 (힘 · 강화는 보장 수로만)")] public int Weight;
        }

        [SerializeField] private List<Entry> _items = new List<Entry>();
        [Header("무기 공통 (3-7a, BALANCE 13번)")]
        [Tooltip("피해 나누기 — 사용자 값(SPD 크기)을 이 값으로 나눠 반올림 (결정 2026-10-19: 1/3)")]
        [Min(1)] [SerializeField] private int _weaponDamageDivisor = 3;
        [Tooltip("최소 피해 기본 (나누기 전, 강화마다 +1) — SPD 2티어 2")]
        [Min(0)] [SerializeField] private int _weaponMinDamage = 2;
        [Header("갑옷 공통 (3-10a, BALANCE 15번)")]
        [Tooltip("방어 나누기 — SPD 방어 굴림을 이 값으로 나눠 반올림 (결정 2026-10-20: 무기처럼 3)")]
        [Min(1)] [SerializeField] private int _armorDefenseDivisor = 3;
        [Tooltip("힘 1 모자랄 때마다 방어 굴림에서 빼는 양 (SPD 크기, 나누기 전) — SPD 2")]
        [Min(0)] [SerializeField] private int _armorDeficitPenalty = 2;

        public ItemBook ToBook()
        {
            var defs = new List<ItemDef>(_items.Count);
            foreach (var e in _items)
            {
                var weapon = e.Use == ItemUse.EquipWeapon
                    ? new WeaponDef(e.MaxDamage, e.UpgradeEfficiency, Mathf.RoundToInt(e.Accuracy * 100f), e.AttackCostTicks, e.Reach, e.StrengthRequirement, e.SurpriseMinPercent, e.Defense)
                    : null;
                var armor = e.Use == ItemUse.EquipArmor ? new ArmorDef(e.Tier, e.StrengthRequirement) : null;
                defs.Add(new ItemDef(e.Id, e.Use, e.SpellId, weapon, e.Amount, e.Effect, e.Weight, armor));
            }
            return new ItemBook(defs, new WeaponRules(_weaponDamageDivisor, _weaponMinDamage), new ArmorRules(_armorDefenseDivisor, _armorDeficitPenalty));
        }

        /// <summary>ID → 표시 이름 (이름이 빈 항목은 빠짐).</summary>
        public Dictionary<string, string> DisplayNames()
        {
            var names = new Dictionary<string, string>();
            foreach (var e in _items)
                if (!string.IsNullOrEmpty(e.Id) && !string.IsNullOrEmpty(e.DisplayName)) names[e.Id] = e.DisplayName;
            return names;
        }

        /// <summary>에디터 Setup용 (3-7b): 요구 힘이 0인 무기 줄에 value를 넣는다 (3-7a에 만든 줄 — 사용자가 고친 값은 그대로). 바뀌었으면 true.</summary>
        public bool FillWeaponStrength(int value)
        {
            bool changed = false;
            for (int i = 0; i < _items.Count; i++)
            {
                var e = _items[i];
                if (e.Use != ItemUse.EquipWeapon || e.StrengthRequirement != 0) continue;
                e.StrengthRequirement = value;
                _items[i] = e;
                changed = true;
            }
            return changed;
        }

        /// <summary>에디터 Setup용 (3-7c): 그 ID 줄의 기습 % · 방어가 0이면 넣는다 (사용자가 고친 값은 그대로). 바뀌었으면 true.</summary>
        public bool FillWeaponSpecials(string id, int surpriseMinPercent, int defense)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var e = _items[i];
                if (e.Id != id) continue;
                bool changed = false;
                if (e.SurpriseMinPercent == 0 && surpriseMinPercent != 0) { e.SurpriseMinPercent = surpriseMinPercent; changed = true; }
                if (e.Defense == 0 && defense != 0) { e.Defense = defense; changed = true; }
                _items[i] = e;
                return changed;
            }
            return false;
        }

        /// <summary>에디터 Setup용 (3-9): 그 ID 줄의 효과가 None이면 넣는다 (3-7b 힘 물약 · 강화 주문서 줄). 바뀌었으면 true.</summary>
        public bool FillEffect(string id, ConsumableEffect effect)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var e = _items[i];
                if (e.Id != id || e.Effect != ConsumableEffect.None) continue;
                e.Effect = effect;
                _items[i] = e;
                return true;
            }
            return false;
        }

        /// <summary>에디터 Setup용: 같은 ID가 없을 때만 추가한다.</summary>
        public bool AddIfMissing(Entry entry)
        {
            foreach (var e in _items)
                if (e.Id == entry.Id) return false;
            _items.Add(entry);
            return true;
        }
    }
}
