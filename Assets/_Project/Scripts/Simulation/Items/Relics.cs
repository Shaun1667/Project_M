using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Items
{
    /// <summary>유물 끼기 결과 (3-28a).</summary>
    public enum RelicEquipResult
    {
        Equipped = 0,
        NotRelic = 1,
        /// <summary>같은 종류를 이미 끼고 있다 (결정 2026-11-05 — 같은 종류는 한 칸만).</summary>
        SameKind = 2,
    }

    /// <summary>
    /// 유물 장비 (3-28a, 결정 2026-11-05 — 사용자: 유물 칸 2칸 · 같은 종류는 하나만 · 효과는 특성 효과 표와 같은 데이터).
    /// 끼기는 무기 · 갑옷과 같은 EquipWeaponCommand(차례 1) → Weapons.Equip → Armors.Equip → 여기. 빈 칸이 있으면 그 칸, 없으면 첫 칸과 바꿈(빼낸 유물은 배낭으로).
    /// 효과 값은 Talents.Sum이 낀 유물까지 더한다 (강화 단계 n → Values[n], 3-28b 강화). 최대 체력 효과만 끼고 뺄 때 바로 반영.
    /// </summary>
    public static class Relics
    {
        public const int SlotCount = 2;

        public static bool IsRelic(ItemBook items, string itemId) =>
            items != null && itemId != null && items.TryGet(itemId, out var def) && def.Use == ItemUse.Relic;

        /// <summary>그 유물의 효과 (없으면 빈 목록).</summary>
        public static IReadOnlyList<TalentEffectDef> EffectsOf(ItemBook items, string itemId) =>
            items != null && itemId != null && items.TryGet(itemId, out var def) && def.Use == ItemUse.Relic ? def.RelicEffects : Array.Empty<TalentEffectDef>();

        /// <summary>효과 값을 읽는 점수 (강화 0 = 1점 = Values[0]).</summary>
        public static int PointsOf(InventoryRef relic) => 1 + Math.Max(0, relic.Upgrade);

        public static bool IsWearing(Unit unit, string itemId)
        {
            foreach (var r in unit.Relics)
                if (!r.IsEmpty && r.Id == itemId) return true;
            return false;
        }

        /// <summary>배낭 inventoryIndex의 유물을 낀다. 같은 종류면 SameKind(그대로) — RelicRejectedEvent.</summary>
        public static RelicEquipResult Equip(World world, ItemBook items, Unit unit, int inventoryIndex)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (inventoryIndex < 0 || inventoryIndex >= unit.InventorySlots.Length) return RelicEquipResult.NotRelic;
            var entry = unit.InventorySlots[inventoryIndex];
            if (entry.Kind != InventoryKind.Item || !IsRelic(items, entry.Id)) return RelicEquipResult.NotRelic;
            if (IsWearing(unit, entry.Id))
            {
                world.Events.Publish(new RelicRejectedEvent(unit.Id, entry.Id));
                return RelicEquipResult.SameKind;
            }

            int slot = Array.FindIndex(unit.Relics, r => r.IsEmpty);
            if (slot < 0) slot = 0;
            var previous = unit.Relics[slot];
            Inventory.RemoveAt(world, unit, inventoryIndex);
            if (!previous.IsEmpty)
            {
                ApplyMaxHp(items, unit, previous, -1);
                unit.Relics[slot] = InventoryRef.Empty;
                Inventory.TryAdd(world, null, items, unit, InventoryRef.Item(previous.Id, previous.Upgrade));
            }
            var worn = InventoryRef.Item(entry.Id, entry.Upgrade);
            unit.Relics[slot] = worn;
            ApplyMaxHp(items, unit, worn, +1);
            world.Events.Publish(new RelicEquippedEvent(unit.Id, slot, entry.Id, previous.IsEmpty ? null : previous.Id, unit.Hp, unit.MaxHp));
            return RelicEquipResult.Equipped;
        }

        /// <summary>낀 칸의 유물을 바꾼다 (3-28b 강화 — 같은 유물의 다른 단계). 최대 체력 효과는 차이만큼.</summary>
        public static void ReplaceWorn(ItemBook items, Unit unit, int slot, InventoryRef relic)
        {
            if (unit == null || slot < 0 || slot >= unit.Relics.Length) return;
            var old = unit.Relics[slot];
            if (!old.IsEmpty) ApplyMaxHp(items, unit, old, -1);
            unit.Relics[slot] = relic;
            if (!relic.IsEmpty) ApplyMaxHp(items, unit, relic, +1);
        }

        /// <summary>최대 체력 효과: 끼면 +값 (현재 체력도), 빼면 −값 (현재 체력은 최대를 넘지 않게, 최소 1).</summary>
        private static void ApplyMaxHp(ItemBook items, Unit unit, InventoryRef relic, int sign)
        {
            int points = PointsOf(relic);
            foreach (var e in EffectsOf(items, relic.Id))
            {
                if (e.Effect != TalentEffect.MaxHp) continue;
                int value = e.ValueAt(points) * sign;
                unit.MaxHp = Math.Max(1, unit.MaxHp + value);
                unit.Hp = sign > 0 ? Math.Min(unit.MaxHp, unit.Hp + value) : Math.Max(1, Math.Min(unit.Hp, unit.MaxHp));
            }
        }
    }

    /// <summary>유물을 꼈다 (3-28a — HUD 로그 · 배낭 장비 줄 · 체력 막대). Previous = 빼낸 유물(없으면 null).</summary>
    public sealed class RelicEquippedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public int Slot { get; }
        public string ItemId { get; }
        public string Previous { get; }
        public int Hp { get; }
        public int MaxHp { get; }
        /// <summary>로그 없이 화면(장비 줄 · 체력 막대)만 맞춤 (3-28b 강화).</summary>
        public bool Silent { get; }

        public RelicEquippedEvent(EntityId unit, int slot, string itemId, string previous, int hp, int maxHp, bool silent = false)
        {
            Silent = silent;
            Unit = unit;
            Slot = slot;
            ItemId = itemId;
            Previous = previous;
            Hp = hp;
            MaxHp = maxHp;
        }
    }

    /// <summary>같은 종류 유물을 이미 끼고 있어 못 꼈다 (3-28a — 안내 줄).</summary>
    public sealed class RelicRejectedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public string ItemId { get; }

        public RelicRejectedEvent(EntityId unit, string itemId)
        {
            Unit = unit;
            ItemId = itemId;
        }
    }
}
