using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Units;

namespace ProjectM.Items
{
    /// <summary>인벤토리 칸에 든 것의 종류 (3-2a). 끝에만 추가한다 (스냅샷이 정수로 저장). None = 빈 칸.</summary>
    public enum InventoryKind
    {
        None = 0,
        Spell = 1,
        Item = 2,
    }

    /// <summary>인벤토리 칸 · 빠른 칸 하나의 내용: 종류 + ID (주문 ID 또는 아이템 ID). 빈 칸 = Empty.</summary>
    public readonly struct InventoryRef : IEquatable<InventoryRef>
    {
        public InventoryKind Kind { get; }
        public string Id { get; }

        public InventoryRef(InventoryKind kind, string id)
        {
            if (kind == InventoryKind.None ? !string.IsNullOrEmpty(id) : string.IsNullOrEmpty(id))
                throw new ArgumentException("빈 칸은 ID가 없고, 빈 칸이 아니면 ID가 있어야 한다.", nameof(id));
            Kind = kind;
            Id = kind == InventoryKind.None ? null : id;
        }

        public static InventoryRef Empty => default;
        public static InventoryRef Spell(string id) => new InventoryRef(InventoryKind.Spell, id);
        public static InventoryRef Item(string id) => new InventoryRef(InventoryKind.Item, id);

        public bool IsEmpty => Kind == InventoryKind.None;

        public bool Equals(InventoryRef other) => Kind == other.Kind && Id == other.Id;
        public override bool Equals(object obj) => obj is InventoryRef other && Equals(other);
        public override int GetHashCode() => ((int)Kind * 397) ^ (Id != null ? Id.GetHashCode() : 0);
        public static bool operator ==(InventoryRef a, InventoryRef b) => a.Equals(b);
        public static bool operator !=(InventoryRef a, InventoryRef b) => !a.Equals(b);
        public override string ToString() => IsEmpty ? "(빈 칸)" : $"{Kind}:{Id}";
    }

    /// <summary>아이템을 "사용"하면 하는 일 (3-2a). 끝에만 추가한다.</summary>
    public enum ItemUse
    {
        None = 0,
        /// <summary>마법 융합기 (3-2b): 주문 두 개를 골라 한 번에 시전.</summary>
        Fusion = 1,
    }

    /// <summary>아이템 하나의 규칙 데이터 (3-2a). 값은 ItemCatalogAsset에서 온다 — BALANCE.md 9번.</summary>
    public sealed class ItemDef
    {
        public string Id { get; }
        public ItemUse Use { get; }

        public ItemDef(string id, ItemUse use)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("아이템 ID가 비어 있다.", nameof(id));
            Id = id;
            Use = use;
        }
    }

    /// <summary>아이템 ID → 규칙. 같은 ID가 두 번 나오면 예외.</summary>
    public sealed class ItemBook
    {
        private readonly Dictionary<string, ItemDef> _defs = new Dictionary<string, ItemDef>();
        private readonly List<string> _ids = new List<string>();

        public ItemBook(IEnumerable<ItemDef> defs)
        {
            if (defs == null) throw new ArgumentNullException(nameof(defs));
            foreach (var def in defs)
            {
                if (def == null) throw new ArgumentException("null 아이템", nameof(defs));
                if (_defs.ContainsKey(def.Id)) throw new ArgumentException("같은 아이템 ID가 두 번: " + def.Id, nameof(defs));
                _defs.Add(def.Id, def);
                _ids.Add(def.Id);
            }
        }

        public static ItemBook Empty => new ItemBook(Array.Empty<ItemDef>());

        public int Count => _defs.Count;
        /// <summary>넣은 순서의 ID 목록 (디버그 창 고르기용).</summary>
        public IReadOnlyList<string> Ids => _ids;

        public bool TryGet(string id, out ItemDef def)
        {
            def = null;
            return id != null && _defs.TryGetValue(id, out def);
        }
    }

    /// <summary>
    /// 인벤토리 · 빠른 칸을 바꾸는 곳은 여기 하나 (3-2a). 칸 수는 고정(MagicConfig.InventorySlotCount · QuickSlotCount).
    /// - 인벤토리: 앞쪽 빈 칸부터 채운다. 같은 주문은 한 번만 (아이템은 여러 개 가능, 겹치지 않음). 꽉 차면 못 넣는다.
    /// - 빠른 칸: 인벤토리에 있는 것만 등록. 한 항목은 빠른 칸 하나에만 — 다른 칸에 등록하면 원래 칸은 비워진다.
    /// - 인벤토리에서 빼면 그것을 가리키던 빠른 칸도 비운다 (같은 것이 인벤토리에 더 남아 있으면 그대로).
    /// 바뀌면 InventoryChangedEvent · QuickSlotsChangedEvent.
    /// </summary>
    public static class Inventory
    {
        /// <summary>넣는다. 넣은 칸 번호, 꽉 찼거나 이미 있는 주문이면 -1. 모르는 주문 · 아이템이면 예외.</summary>
        public static int TryAdd(World world, SpellBook spells, ItemBook items, Unit unit, InventoryRef entry)
        {
            Validate(world, unit);
            if (entry.IsEmpty) throw new ArgumentException("빈 칸은 넣을 수 없다.", nameof(entry));
            if (entry.Kind == InventoryKind.Spell && (spells == null || !spells.TryGet(entry.Id, out _)))
                throw new ArgumentException("모르는 주문: " + entry.Id, nameof(entry));
            if (entry.Kind == InventoryKind.Item && (items == null || !items.TryGet(entry.Id, out _)))
                throw new ArgumentException("모르는 아이템: " + entry.Id, nameof(entry));

            var slots = unit.InventorySlots;
            if (entry.Kind == InventoryKind.Spell && Array.IndexOf(slots, entry) >= 0) return -1;
            int index = Array.IndexOf(slots, InventoryRef.Empty);
            if (index < 0) return -1;
            slots[index] = entry;
            PublishInventory(world, unit);
            return index;
        }

        /// <summary>인벤토리 칸 하나를 비운다. 그 항목이 인벤토리에 더 없으면 가리키던 빠른 칸도 비운다.</summary>
        public static void RemoveAt(World world, Unit unit, int index)
        {
            Validate(world, unit);
            var slots = unit.InventorySlots;
            if (index < 0 || index >= slots.Length) throw new ArgumentOutOfRangeException(nameof(index), index, "인벤토리 칸 밖");
            var removed = slots[index];
            if (removed.IsEmpty) return;
            slots[index] = InventoryRef.Empty;
            PublishInventory(world, unit);

            if (Array.IndexOf(slots, removed) >= 0) return;
            bool quickChanged = false;
            for (int i = 0; i < unit.QuickSlots.Length; i++)
            {
                if (unit.QuickSlots[i] != removed) continue;
                unit.QuickSlots[i] = InventoryRef.Empty;
                quickChanged = true;
            }
            if (quickChanged) PublishQuickSlots(world, unit);
        }

        /// <summary>
        /// 빠른 칸 slot에 등록 (Empty = 해제). 인벤토리에 없는 항목이면 예외. 이미 다른 칸에 있으면 그 칸은 비운다.
        /// </summary>
        public static void AssignQuickSlot(World world, Unit unit, int slot, InventoryRef entry)
        {
            Validate(world, unit);
            if (slot < 0 || slot >= unit.QuickSlots.Length) throw new ArgumentOutOfRangeException(nameof(slot), slot, "빠른 칸 밖");
            if (!entry.IsEmpty && Array.IndexOf(unit.InventorySlots, entry) < 0)
                throw new ArgumentException("인벤토리에 없는 항목은 등록할 수 없다: " + entry, nameof(entry));

            if (!entry.IsEmpty)
                for (int i = 0; i < unit.QuickSlots.Length; i++)
                    if (i != slot && unit.QuickSlots[i] == entry) unit.QuickSlots[i] = InventoryRef.Empty;
            unit.QuickSlots[slot] = entry;
            PublishQuickSlots(world, unit);
        }

        /// <summary>그 항목이 등록된 빠른 칸 번호, 없으면 -1.</summary>
        public static int QuickSlotOf(Unit unit, InventoryRef entry) => entry.IsEmpty ? -1 : Array.IndexOf(unit.QuickSlots, entry);

        private static void Validate(World world, Unit unit)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
        }

        private static void PublishInventory(World world, Unit unit) =>
            world.Events.Publish(new InventoryChangedEvent(unit.Id, (InventoryRef[])unit.InventorySlots.Clone()));

        private static void PublishQuickSlots(World world, Unit unit) =>
            world.Events.Publish(new QuickSlotsChangedEvent(unit.Id, (InventoryRef[])unit.QuickSlots.Clone()));
    }

    /// <summary>인벤토리 칸이 바뀌었다. Slots는 바뀐 뒤 전체 (복사본).</summary>
    public sealed class InventoryChangedEvent : ISimEvent
    {
        public EntityId Id { get; }
        public InventoryRef[] Slots { get; }

        public InventoryChangedEvent(EntityId id, InventoryRef[] slots)
        {
            Id = id;
            Slots = slots;
        }
    }

    /// <summary>빠른 칸이 바뀌었다 (3-2a, 2-1 주문 장착 칸을 대신함). Slots는 바뀐 뒤 전체 (복사본).</summary>
    public sealed class QuickSlotsChangedEvent : ISimEvent
    {
        public EntityId Id { get; }
        public InventoryRef[] Slots { get; }

        public QuickSlotsChangedEvent(EntityId id, InventoryRef[] slots)
        {
            Id = id;
            Slots = slots;
        }
    }
}
