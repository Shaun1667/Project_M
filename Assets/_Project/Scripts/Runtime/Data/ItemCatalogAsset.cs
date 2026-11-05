using System;
using System.Collections.Generic;
using ProjectM.Items;
using ProjectM.Units;
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
            [Tooltip("한 번 공격의 타격 수 (3-22a) — 너클 2, 나머지 0 = 1")] [Min(0)] public int HitsPerAttack;
            [Header("갑옷 (EquipArmor만, 3-10a — SPD)")]
            [Tooltip("티어 1 천 ~ 5 판금. 방어 = 강화 ~ 티어 × (2 + 강화), 나누기는 공통 값. 요구 힘은 위 StrengthRequirement")] public int Tier;
            [Header("힘 물약 · 강화 주문서 (3-7b)")]
            [Tooltip("힘 물약 = 힘 +, 강화 주문서 = 강화 + (둘 다 1)")] public int Amount;
            [Header("물약 · 주문서 (3-9, BALANCE 14번)")]
            [Tooltip("Potion · Scroll만: 효과")] public ConsumableEffect Effect;
            [Tooltip("층마다 무작위로 놓을 때의 무게 (SPD). 0 = 무작위로 안 놓음 (힘 · 강화는 보장 수로만)")] public int Weight;
            [Header("음식 (3-14)")]
            [Tooltip("Food만: 먹으면 줄어드는 허기 — 식량 300 · 빵 · 고기 150")] public int Food;
            [Tooltip("먹으면 나쁜 상태가 걸릴 수 있음 (정체불명 고기)")] public bool Risky;
            [Tooltip("바닥에서 불을 맞으면 바뀌는 아이템 (구운 고기)")] public string FireTransformsTo;
            [Tooltip("바닥에서 얼음을 맞으면 바뀌는 아이템 (얼린 고기)")] public string IceTransformsTo;
            [Tooltip("값 (3-15c 상점 — 사기 = 값 × 사는 %, 팔기 = 값 × 파는 %). 0 = 못 팜")] [Min(0)] public int Value;
            [Header("던지는 무기 (3-16a)")]
            [Min(0)] public int ThrowMinDamage;
            [Min(0)] public int ThrowMaxDamage;
            [Tooltip("던질 때 부러질 확률 %")] [Range(0, 100)] public int BreakPercent;
            [Tooltip("씨앗(Seed)만: 자라는 식물 (TrapCatalog)")] public ProjectM.Dungeon.TrapType PlantTrap;
            [Tooltip("씨앗(Seed)만: 연금 솥에서 나오는 물약 (3-16c)")] public string BrewsInto;
            [Header("유물 (Relic만, 3-28a — BALANCE 24번)")]
            [Tooltip("끼면 켜지는 효과 (특성 효과 표와 같음 — Values[강화 단계], 대상 주문 \"*\" = 모든 주문 · \"*elemental\" = 속성 주문)")] public List<ClassCatalogAsset.TalentEffectEntry> RelicEffects;
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
                    ? new WeaponDef(e.MaxDamage, e.UpgradeEfficiency, Mathf.RoundToInt(e.Accuracy * 100f), e.AttackCostTicks, e.Reach, e.StrengthRequirement, e.SurpriseMinPercent, e.Defense, Math.Max(1, e.HitsPerAttack))
                    : null;
                var armor = e.Use == ItemUse.EquipArmor ? new ArmorDef(e.Tier, e.StrengthRequirement) : null;
                defs.Add(new ItemDef(e.Id, e.Use, e.SpellId, weapon, e.Amount, e.Effect, e.Weight, armor, e.Food, e.Risky, e.FireTransformsTo, e.IceTransformsTo, e.Value,
                    e.ThrowMinDamage, e.ThrowMaxDamage, e.BreakPercent, e.PlantTrap, e.BrewsInto, RelicEffectsOf(e)));
            }
            return new ItemBook(defs, new WeaponRules(_weaponDamageDivisor, _weaponMinDamage), new ArmorRules(_armorDefenseDivisor, _armorDeficitPenalty));
        }

        private static List<TalentEffectDef> RelicEffectsOf(Entry e)
        {
            var list = new List<TalentEffectDef>();
            if (e.Use != ItemUse.Relic || e.RelicEffects == null) return list;
            foreach (var r in e.RelicEffects)
                list.Add(new TalentEffectDef(r.Effect, r.Values ?? new List<int>(), r.SpellIds ?? new List<string>(), r.WeaponId, r.Status));
            return list;
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

        /// <summary>에디터 Setup용 (3-16c): 씨앗의 연금 결과가 비어 있으면 채운다. 바뀌었으면 true.</summary>
        public bool SetBrewIfEmpty(string id, string potionId)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Id != id || !string.IsNullOrEmpty(_items[i].BrewsInto)) continue;
                var e = _items[i];
                e.BrewsInto = potionId;
                _items[i] = e;
                return true;
            }
            return false;
        }

        /// <summary>에디터 Setup용 (3-15c): 값이 0인 줄만 쓰임새별 값으로 채운다 (사용자가 고친 값은 그대로). 바뀌었으면 true.</summary>
        public bool FillZeroValues(System.Func<Entry, int> valueOf)
        {
            bool changed = false;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Value != 0) continue;
                var e = _items[i];
                e.Value = valueOf(e);
                if (e.Value == 0) continue;
                _items[i] = e;
                changed = true;
            }
            return changed;
        }

        /// <summary>에디터 Setup용: 같은 ID가 없을 때만 추가한다.</summary>
        public bool AddIfMissing(Entry entry)
        {
            foreach (var e in _items)
                if (e.Id == entry.Id) return false;
            _items.Add(entry);
            return true;
        }

        /// <summary>3-28b: 유물 첫 효과 줄의 강화 단계별 값 (값이 하나뿐일 때만 — 사용자가 고친 표는 그대로). 바꿨으면 true.</summary>
        public bool FillRelicValues(string id, params int[] values)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var e = _items[i];
                if (e.Id != id || e.Use != ItemUse.Relic || e.RelicEffects == null || e.RelicEffects.Count == 0) continue;
                var effect = e.RelicEffects[0];
                if (effect.Values != null && effect.Values.Count > 1) return false;
                effect.Values = new List<int>(values);
                e.RelicEffects[0] = effect;
                _items[i] = e;
                return true;
            }
            return false;
        }
    }
}
