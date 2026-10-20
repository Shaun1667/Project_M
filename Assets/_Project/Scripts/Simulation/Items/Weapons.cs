using System;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Items
{
    /// <summary>
    /// 근접 무기 한 종의 데이터 (3-7a, SPD 2티어 — Docs/장비_데이터.md). 사용자 값(SPD 크기)을 그대로 두고 WeaponRules.DamageDivisor로 나눈다.
    /// </summary>
    public sealed class WeaponDef
    {
        /// <summary>강화 0일 때 최대 피해 (SPD 크기, 나누기 전) — 소검 15.</summary>
        public int MaxDamage { get; }
        /// <summary>강화 1단계마다 최대 피해 증가 (나누기 전) — 창 4, 나머지 3.</summary>
        public int UpgradeEfficiency { get; }
        public int AccuracyPercent { get; }
        public int AttackCostTicks { get; }
        public int Reach { get; }
        /// <summary>강화 0일 때 요구 힘 (3-7b, SPD 2티어 12). 0 = 요구 없음.</summary>
        public int StrengthRequirement { get; }
        /// <summary>기습 최소 피해 상승 % (3-7c 더크 67 — SPD: 최소가 최소~최대의 67% 지점으로).</summary>
        public int SurpriseMinPercent { get; }
        /// <summary>방어 (3-7c 육척봉 "최대 방어력 +2"). 나누기를 적용하지 않음 (결정 2026-10-19: 0~2).</summary>
        public int Defense { get; }

        public WeaponDef(int maxDamage, int upgradeEfficiency, int accuracyPercent, int attackCostTicks, int reach, int strengthRequirement = 0,
            int surpriseMinPercent = 0, int defense = 0)
        {
            if (surpriseMinPercent < 0 || surpriseMinPercent > 100) throw new ArgumentOutOfRangeException(nameof(surpriseMinPercent));
            if (defense < 0) throw new ArgumentOutOfRangeException(nameof(defense));
            SurpriseMinPercent = surpriseMinPercent;
            Defense = defense;
            if (strengthRequirement < 0) throw new ArgumentOutOfRangeException(nameof(strengthRequirement));
            StrengthRequirement = strengthRequirement;
            if (maxDamage < 1) throw new ArgumentOutOfRangeException(nameof(maxDamage), maxDamage, "최대 피해는 1 이상.");
            if (upgradeEfficiency < 0) throw new ArgumentOutOfRangeException(nameof(upgradeEfficiency));
            if (accuracyPercent < 1) throw new ArgumentOutOfRangeException(nameof(accuracyPercent), accuracyPercent, "명중 배수(%)는 1 이상.");
            if (attackCostTicks < 1) throw new ArgumentOutOfRangeException(nameof(attackCostTicks));
            if (reach < 1 || reach > 2) throw new ArgumentOutOfRangeException(nameof(reach), reach, "공격 거리는 1 또는 2.");
            MaxDamage = maxDamage;
            UpgradeEfficiency = upgradeEfficiency;
            AccuracyPercent = accuracyPercent;
            AttackCostTicks = attackCostTicks;
            Reach = reach;
        }
    }

    /// <summary>
    /// 무기 공통 수치 (3-7a 결정 2026-10-19). 최대 = round((최대 피해 + 효율 × 강화) / 나누기), 최소 = max(1, round((최소 기본 + 강화) / 나누기)).
    /// 지금 나누기 3(사용자 값의 1/3 — 주문 피해 2~5와 맞춤), 최소 기본 2(SPD 2티어). BALANCE.md 13번.
    /// </summary>
    public sealed class WeaponRules
    {
        public int DamageDivisor { get; }
        public int MinDamageBase { get; }

        public WeaponRules(int damageDivisor, int minDamageBase)
        {
            if (damageDivisor < 1) throw new ArgumentOutOfRangeException(nameof(damageDivisor));
            if (minDamageBase < 0) throw new ArgumentOutOfRangeException(nameof(minDamageBase));
            DamageDivisor = damageDivisor;
            MinDamageBase = minDamageBase;
        }

        /// <summary>나누지 않음 · 최소 기본 1 (테스트 기본값).</summary>
        public static WeaponRules Identity => new WeaponRules(1, 1);
    }

    /// <summary>
    /// 무기 장착 (3-7a). 장착 = 인벤토리에서 사용, 차례 1 (EquipWeaponCommand). 끼던 무기는 인벤토리로 돌아간다 (빈 칸 — 방금 비운 칸이 있으므로 항상 들어감).
    /// 맨손으로 시작하고, 지금은 벗는 동작이 없다 (다른 무기로 바꾸기만).
    /// </summary>
    public static class Weapons
    {
        /// <summary>데이터 → 유닛에 둘 전투 수치 (강화 단계 upgrade).</summary>
        public static EquippedWeapon Resolve(string itemId, WeaponDef def, WeaponRules rules, int upgrade = 0)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (upgrade < 0) throw new ArgumentOutOfRangeException(nameof(upgrade));
            int max = Math.Max(1, Divide(def.MaxDamage + def.UpgradeEfficiency * upgrade, rules.DamageDivisor));
            int min = Math.Min(max, Math.Max(1, Divide(rules.MinDamageBase + upgrade, rules.DamageDivisor)));
            return new EquippedWeapon(itemId, min, max, def.AccuracyPercent, def.AttackCostTicks, def.Reach,
                upgrade, StrengthRequirement(def, upgrade), def.SurpriseMinPercent, def.Defense);
        }

        /// <summary>
        /// 강화 단계의 요구 힘 (3-7b, 사용자 결정 2026-10-19): 첫 강화에 −1, 그 뒤로 2단계마다 −1 = 요구 힘 − ⌈강화 / 2⌉.
        /// 12 → +1 11 · +2 11 · +3 10 · +4 10 · +5 9. 0 아래로는 안 내려감.
        /// </summary>
        public static int StrengthRequirement(WeaponDef def, int upgrade) =>
            def.StrengthRequirement == 0 ? 0 : Math.Max(0, def.StrengthRequirement - (upgrade + 1) / 2);

        /// <summary>a / b를 반올림 (0.5는 위로 — 6.67 → 7, 0.67 → 1).</summary>
        private static int Divide(int a, int b) => (int)Math.Round((double)a / b, MidpointRounding.AwayFromZero);

        /// <summary>인벤토리 칸 index의 무기를 든다 (틱 안 — EquipWeaponCommand가 부름). 무기가 아니면 false.</summary>
        public static bool Equip(World world, ItemBook items, Unit unit, int inventoryIndex)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (!TryGetWeaponAt(items, unit, inventoryIndex, out var itemId, out var def)) return false;

            int upgrade = unit.InventorySlots[inventoryIndex].Upgrade;
            var previous = unit.Weapon;
            Inventory.RemoveAt(world, unit, inventoryIndex);
            if (!previous.IsEmpty) Inventory.TryAdd(world, null, items, unit, InventoryRef.Item(previous.ItemId, previous.Upgrade)); // 3-7b: 강화 단계와 함께
            unit.Weapon = Resolve(itemId, def, items.WeaponRules, upgrade);
            world.Events.Publish(new WeaponEquippedEvent(unit.Id, unit.Weapon, previous.ItemId));
            return true;
        }

        public static bool TryGetWeaponAt(ItemBook items, Unit unit, int inventoryIndex, out string itemId, out WeaponDef def)
        {
            itemId = null;
            def = null;
            if (inventoryIndex < 0 || inventoryIndex >= unit.InventorySlots.Length) return false;
            var entry = unit.InventorySlots[inventoryIndex];
            if (entry.Kind != InventoryKind.Item || !items.TryGet(entry.Id, out var item) || item.Use != ItemUse.EquipWeapon) return false;
            itemId = entry.Id;
            def = item.Weapon;
            return true;
        }
    }

    /// <summary>무기를 드는 행동 (3-7a, 차례 1 — 결정 2026-10-19). 실행 때 그 칸에 같은 무기가 없으면 차례만 쓴다.</summary>
    public sealed class EquipWeaponCommand : ICommand
    {
        private readonly World _world;
        private readonly ItemBook _items;
        private readonly int _inventoryIndex;
        private readonly InventoryRef _entry;

        public int CostTicks { get; }

        public EquipWeaponCommand(World world, ItemBook items, int inventoryIndex, string itemId, int costTicks)
            : this(world, items, inventoryIndex, InventoryRef.Item(itemId), costTicks)
        {
        }

        /// <param name="entry">그 칸에 있어야 하는 항목 (강화 단계 포함, 3-7b).</param>
        public EquipWeaponCommand(World world, ItemBook items, int inventoryIndex, InventoryRef entry, int costTicks)
        {
            if (costTicks < 1) throw new ArgumentOutOfRangeException(nameof(costTicks));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _inventoryIndex = inventoryIndex;
            _entry = entry;
            CostTicks = costTicks;
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var unit) || !unit.IsAlive) return;
            if (_inventoryIndex < 0 || _inventoryIndex >= unit.InventorySlots.Length) return;
            if (unit.InventorySlots[_inventoryIndex] != _entry) return;
            Weapons.Equip(_world, _items, unit, _inventoryIndex);
        }
    }

    /// <summary>힘이 바뀌었다 (3-7b 힘 물약). Strength = 바뀐 뒤, Gained = 오른 양.</summary>
    public sealed class StrengthChangedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public int Strength { get; }
        public int Gained { get; }

        public StrengthChangedEvent(EntityId unit, int strength, int gained)
        {
            Unit = unit;
            Strength = strength;
            Gained = gained;
        }
    }

    /// <summary>든 무기가 강화됐다 (3-7b 강화 주문서). Weapon = 강화 뒤 수치.</summary>
    public sealed class WeaponUpgradedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public EquippedWeapon Weapon { get; }

        public WeaponUpgradedEvent(EntityId unit, EquippedWeapon weapon)
        {
            Unit = unit;
            Weapon = weapon;
        }
    }

    /// <summary>무기를 들었다 (3-7a). Previous = 인벤토리로 돌아간 무기 ID (맨손이었으면 null).</summary>
    public sealed class WeaponEquippedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public EquippedWeapon Weapon { get; }
        public string Previous { get; }

        public WeaponEquippedEvent(EntityId unit, EquippedWeapon weapon, string previous)
        {
            Unit = unit;
            Weapon = weapon;
            Previous = previous;
        }
    }
}
