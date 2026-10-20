using System;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Items
{
    /// <summary>갑옷 한 종의 데이터 (3-10a, SPD — 천 1 · 가죽 2 · 사슬 3 · 비늘 4 · 판금 5티어).</summary>
    public sealed class ArmorDef
    {
        public int Tier { get; }
        /// <summary>강화 0 요구 힘 (SPD 8 + 2 × 티어 = 10 · 12 · 14 · 16 · 18).</summary>
        public int StrengthRequirement { get; }

        public ArmorDef(int tier, int strengthRequirement)
        {
            if (tier < 1) throw new ArgumentOutOfRangeException(nameof(tier), tier, "티어는 1 이상.");
            if (strengthRequirement < 0) throw new ArgumentOutOfRangeException(nameof(strengthRequirement));
            Tier = tier;
            StrengthRequirement = strengthRequirement;
        }
    }

    /// <summary>갑옷 공통 수치 (3-10a, BALANCE 15번): 방어 나누기(무기와 같은 3) · 힘 모자람 1당 방어 벌점(SPD 2).</summary>
    public sealed class ArmorRules
    {
        public int DefenseDivisor { get; }
        public int DeficitPenalty { get; }

        public ArmorRules(int defenseDivisor, int deficitPenalty)
        {
            if (defenseDivisor < 1) throw new ArgumentOutOfRangeException(nameof(defenseDivisor));
            if (deficitPenalty < 0) throw new ArgumentOutOfRangeException(nameof(deficitPenalty));
            DefenseDivisor = defenseDivisor;
            DeficitPenalty = deficitPenalty;
        }

        /// <summary>나누지 않음 · 벌점 2 (테스트 기본값).</summary>
        public static ArmorRules Identity => new ArmorRules(1, 2);
    }

    /// <summary>
    /// 갑옷 (3-10a, 결정 2026-10-20 — SPD 5종 · 무기처럼 ÷3). 입기 = 인벤토리에서 사용, 차례 1 (EquipWeaponCommand가 무기 · 갑옷 공용).
    /// 입던 갑옷은 인벤토리로. 플레이어는 천 갑옷을 입고 시작한다 (UnitDef.StartingArmor → Wear).
    /// 방어 = SPD Armor.DRMin~DRMax(강화 ~ 티어 × (2 + 강화)) 굴림 − 힘 모자람 × 벌점 → ÷ 나누기 (MeleeWeapon.ApplyDefense, 근접만).
    /// 힘이 모자라면 회피도 1당 ÷ 1.5 (Combat.RollHit). 요구 힘은 무기와 같은 규칙 (첫 강화 −1, 그 뒤 2단계마다 −1).
    /// </summary>
    public static class Armors
    {
        public static EquippedArmor Resolve(string itemId, ArmorDef def, ArmorRules rules, int upgrade = 0)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (upgrade < 0) throw new ArgumentOutOfRangeException(nameof(upgrade));
            return new EquippedArmor(itemId, def.Tier, upgrade, StrengthRequirement(def, upgrade),
                upgrade, def.Tier * (2 + upgrade), rules.DeficitPenalty, rules.DefenseDivisor);
        }

        /// <summary>강화 단계의 요구 힘 — 무기와 같은 규칙 (사용자 결정 2026-10-19): 요구 힘 − ⌈강화 / 2⌉.</summary>
        public static int StrengthRequirement(ArmorDef def, int upgrade) =>
            def.StrengthRequirement == 0 ? 0 : Math.Max(0, def.StrengthRequirement - (upgrade + 1) / 2);

        /// <summary>인벤토리 칸 index의 갑옷을 입는다 (틱 안 — EquipWeaponCommand가 부름). 갑옷이 아니면 false.</summary>
        public static bool Equip(World world, ItemBook items, Unit unit, int inventoryIndex)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (inventoryIndex < 0 || inventoryIndex >= unit.InventorySlots.Length) return false;
            var entry = unit.InventorySlots[inventoryIndex];
            if (entry.Kind != InventoryKind.Item || !items.TryGet(entry.Id, out var item) || item.Use != ItemUse.EquipArmor) return false;

            var previous = unit.Armor;
            Inventory.RemoveAt(world, unit, inventoryIndex);
            if (!previous.IsEmpty) Inventory.TryAdd(world, null, items, unit, InventoryRef.Item(previous.ItemId, previous.Upgrade));
            unit.Armor = Resolve(entry.Id, item.Armor, items.ArmorRules, entry.Upgrade);
            world.Events.Publish(new ArmorEquippedEvent(unit.Id, unit.Armor, previous.ItemId));
            return true;
        }

        /// <summary>인벤토리를 거치지 않고 입힌다 (판 시작의 천 갑옷). 갑옷이 아니면 false.</summary>
        public static bool Wear(World world, ItemBook items, Unit unit, string itemId, int upgrade = 0)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (!items.TryGet(itemId, out var item) || item.Use != ItemUse.EquipArmor) return false;
            unit.Armor = Resolve(itemId, item.Armor, items.ArmorRules, upgrade);
            world.Events.Publish(new ArmorEquippedEvent(unit.Id, unit.Armor, null));
            return true;
        }

        /// <summary>입은 갑옷 강화 + amount (강화 주문서). 맨몸이면 false.</summary>
        public static bool Upgrade(World world, ItemBook items, Unit unit, int amount)
        {
            var worn = unit.Armor;
            if (worn.IsEmpty || !items.TryGet(worn.ItemId, out var item) || item.Armor == null) return false;
            unit.Armor = Resolve(worn.ItemId, item.Armor, items.ArmorRules, worn.Upgrade + amount);
            world.Events.Publish(new ArmorUpgradedEvent(unit.Id, unit.Armor));
            return true;
        }

        /// <summary>모자란 힘 (요구 − 힘, 0 이상). 맨몸 0.</summary>
        public static int StrengthDeficit(Unit unit) =>
            unit.Armor.IsEmpty ? 0 : Math.Max(0, unit.Armor.StrengthRequirement - unit.Strength);

        /// <summary>회피 배수 (SPD): 갑옷 힘 모자람 1당 ÷ 1.5.</summary>
        public static float EvasionFactor(Unit unit)
        {
            int deficit = StrengthDeficit(unit);
            return deficit == 0 ? 1f : (float)Math.Pow(1.5, -deficit);
        }

        /// <summary>
        /// 갑옷 방어 굴림 (게임 크기): (DefenseMin~DefenseMax 굴림 − 모자람 × 벌점) ÷ 나누기 (반올림), 0 이상. 맨몸이면 0 · 난수 안 씀.
        /// </summary>
        public static int RollDefense(World world, Unit unit)
        {
            var a = unit.Armor;
            if (a.IsEmpty || a.DefenseMax <= 0) return 0;
            int raw = world.Rng.Get(RngStream.Combat).Range(a.DefenseMin, a.DefenseMax + 1) - StrengthDeficit(unit) * a.DeficitPenalty;
            return raw <= 0 ? 0 : EquippedArmor.Divide(raw, a.DefenseDivisor);
        }
    }

    /// <summary>갑옷을 입었다 (3-10a). Previous = 인벤토리로 돌아간 갑옷 ID (맨몸이었으면 null).</summary>
    public sealed class ArmorEquippedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public EquippedArmor Armor { get; }
        public string Previous { get; }

        public ArmorEquippedEvent(EntityId unit, EquippedArmor armor, string previous)
        {
            Unit = unit;
            Armor = armor;
            Previous = previous;
        }
    }

    /// <summary>입은 갑옷이 강화됐다 (3-10a 강화 주문서).</summary>
    public sealed class ArmorUpgradedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public EquippedArmor Armor { get; }

        public ArmorUpgradedEvent(EntityId unit, EquippedArmor armor)
        {
            Unit = unit;
            Armor = armor;
        }
    }
}
