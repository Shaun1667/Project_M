using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Units;

namespace ProjectM.Items
{
    /// <summary>바닥 아이템 배치 수치 (3-5). 값은 LootConfigAsset — BALANCE.md 11번.</summary>
    public sealed class LootConfig
    {
        /// <summary>한 층에 놓는 마법서 최대 수 (층마다 1권부터 채우고 남는 책을 무작위 층에 하나씩 더).</summary>
        public int MaxBooksPerFloor { get; }
        /// <summary>층 시작 칸(도착 계단)에서 아이템까지 최소 거리 (칸, 대각선 포함).</summary>
        public int MinDistanceFromStart { get; }

        public LootConfig(int maxBooksPerFloor, int minDistanceFromStart)
        {
            if (maxBooksPerFloor < 1) throw new ArgumentOutOfRangeException(nameof(maxBooksPerFloor));
            if (minDistanceFromStart < 0) throw new ArgumentOutOfRangeException(nameof(minDistanceFromStart));
            MaxBooksPerFloor = maxBooksPerFloor;
            MinDistanceFromStart = minDistanceFromStart;
        }
    }

    /// <summary>바닥에 놓인 아이템 하나 (3-5).</summary>
    public readonly struct FloorItem
    {
        public readonly GridPos Pos;
        public readonly string ItemId;

        public FloorItem(GridPos pos, string itemId)
        {
            Pos = pos;
            ItemId = itemId;
        }
    }

    /// <summary>
    /// 지금 층 바닥에 놓인 아이템 (3-5). World.FloorItems — 층이 바뀌면 통째로 바뀌고 떠난 층에 저장된다.
    /// 순서는 칸(y → x) → 놓은 순서로 고정 (결정론 · 스냅샷 바이트 고정).
    /// </summary>
    public sealed class FloorItemLayer
    {
        private readonly List<FloorItem> _items = new List<FloorItem>();

        public int Count => _items.Count;
        public IReadOnlyList<FloorItem> All => _items;

        public void Add(GridPos pos, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) throw new ArgumentException("아이템 ID가 비어 있다.", nameof(itemId));
            int index = _items.Count;
            while (index > 0 && Compare(_items[index - 1].Pos, pos) > 0) index--;
            _items.Insert(index, new FloorItem(pos, itemId));
        }

        public bool Any(GridPos pos)
        {
            foreach (var item in _items)
                if (item.Pos == pos) return true;
            return false;
        }

        /// <summary>그 칸의 아이템 ID들 (놓은 순서).</summary>
        public List<string> At(GridPos pos)
        {
            var ids = new List<string>();
            foreach (var item in _items)
                if (item.Pos == pos) ids.Add(item.ItemId);
            return ids;
        }

        /// <summary>그 칸에서 그 아이템 하나를 뺀다. 없으면 false.</summary>
        public bool Remove(GridPos pos, string itemId)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Pos != pos || _items[i].ItemId != itemId) continue;
                _items.RemoveAt(i);
                return true;
            }
            return false;
        }

        public void Clear() => _items.Clear();

        private static int Compare(GridPos a, GridPos b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X);
    }

    /// <summary>
    /// 마법서 배치 (3-5, 결정 2026-10-19): 한 판에 속성별 마법서(ItemUse.LearnSpell)를 한 권씩, 마지막(보스) 층을 뺀 층에 나눠 놓는다.
    /// ① 판 시작에 Plan — 책 순서를 섞고(RngStream.Loot) 1층부터 한 권씩, 남는 책은 아직 MaxBooksPerFloor 미만인 층에 무작위로 더 → RunState 계획.
    ///    1층은 책이 하나라도 있으면 반드시 1권 이상 (빈손 시작 보장).
    /// ② 처음 가는 층마다 Place — 그 층 몫을 바닥 칸(계단 제외, 시작 칸에서 MinDistanceFromStart 이상)에 무작위로 놓는다.
    /// 난수는 Loot 수열만 쓴다 — 맵 · 적 배치(MapGen)는 바뀌지 않는다. 같은 시드 + 같은 진행 = 같은 책 위치.
    /// </summary>
    public static class Loot
    {
        /// <summary>마법서 배치 계획을 세운다 (판 시작에 한 번). floorCount = 전체 층 수 — 마지막 층(보스)은 빼되 1층뿐이면 1층에.</summary>
        public static void PlanSpellbooks(World world, ItemBook items, LootConfig config, int floorCount)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (config == null) throw new ArgumentNullException(nameof(config));

            var books = new List<string>();
            foreach (var id in items.Ids)
                if (items.TryGet(id, out var def) && def.Use == ItemUse.LearnSpell) books.Add(id);

            var rng = world.Rng.Get(RngStream.Loot);
            for (int i = 0; i < books.Count - 1; i++)
            {
                int j = rng.Range(i, books.Count);
                (books[i], books[j]) = (books[j], books[i]);
            }

            int floors = Math.Max(1, floorCount - 1);
            var counts = new int[floors];
            int remaining = books.Count;
            for (int f = 0; f < floors && remaining > 0; f++, remaining--) counts[f] = 1;
            var open = new List<int>();
            while (remaining > 0)
            {
                open.Clear();
                for (int f = 0; f < floors; f++)
                    if (counts[f] < config.MaxBooksPerFloor) open.Add(f);
                int pick = open.Count > 0 ? open[rng.Range(0, open.Count)] : floors - 1; // 칸이 모자라면 마지막 일반 층에 몰아 둔다
                counts[pick]++;
                remaining--;
            }

            var plan = new List<PendingFloorItem>(books.Count);
            int next = 0;
            for (int f = 0; f < floors; f++)
                for (int k = 0; k < counts[f]; k++)
                    plan.Add(new PendingFloorItem(f + 1, books[next++]));
            world.Run.SetPendingFloorItems(plan);
        }

        /// <summary>지금 층 몫을 바닥에 놓고 FloorItemPlacedEvent를 낸다 (처음 가는 층 — 맵 · 적을 놓고 화면에 알린 뒤).</summary>
        public static void PlaceFloorItems(World world, GridPos start, LootConfig config)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (config == null) throw new ArgumentNullException(nameof(config));
            var ids = world.Run.TakePendingFloorItems(world.Floor);
            if (ids.Count == 0) return;

            var map = world.Map;
            var candidates = new List<GridPos>();
            for (int distance = config.MinDistanceFromStart; distance >= 0 && candidates.Count < ids.Count; distance--)
            {
                candidates.Clear();
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                    {
                        var pos = new GridPos(x, y);
                        if (map.Get(pos) == TileType.Floor && pos != start && GridMovement.Chebyshev(pos, start) >= distance && !world.FloorItems.Any(pos))
                            candidates.Add(pos);
                    }
            }

            var rng = world.Rng.Get(RngStream.Loot);
            foreach (var id in ids)
            {
                if (candidates.Count == 0) break; // 놓을 칸이 없는 층 — 책은 사라진다 (맵 생성상 일어나지 않음)
                int i = rng.Range(0, candidates.Count);
                var pos = candidates[i];
                candidates.RemoveAt(i);
                world.FloorItems.Add(pos, id);
                world.Events.Publish(new FloorItemPlacedEvent(pos, id));
            }
        }
    }

    /// <summary>
    /// 바닥 아이템 줍기 (3-5, 결정 2026-10-19 — 걸어 들어가면 자동): 플레이어가 선 칸의 아이템을 인벤토리에 넣는다.
    /// 인벤토리가 꽉 차면 바닥에 남기고 ItemPickupFailedEvent (HUD가 같은 칸이면 한 번만 알림). 차례를 더 쓰지 않는다. 상태를 갖지 않는다.
    /// 등록: TickPhase.ManaRegen (⑤, 행동 다음).
    /// </summary>
    public sealed class ItemPickupSystem : ITickSystem
    {
        private readonly World _world;
        private readonly ItemBook _items;

        public ItemPickupSystem(World world, ItemBook items)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public void Run(in TickContext context) => PickUp(_world, _items);

        /// <summary>플레이어 발밑 아이템을 줍는다 (틱 시스템 · 층 도착 공용). 주운 개수.</summary>
        public static int PickUp(World world, ItemBook items)
        {
            if (world.FloorItems.Count == 0 || !PlayerActions.TryGetPlayer(world, out var player) || !player.IsAlive) return 0;
            int picked = 0;
            foreach (var id in world.FloorItems.At(player.Pos))
            {
                if (!items.TryGet(id, out _)) continue; // 데이터에서 빠진 아이템은 바닥에 남긴다
                if (Inventory.TryAdd(world, null, items, player, InventoryRef.Item(id)) < 0)
                {
                    world.Events.Publish(new ItemPickupFailedEvent(player.Id, player.Pos, id));
                    continue;
                }
                world.FloorItems.Remove(player.Pos, id);
                world.Events.Publish(new ItemPickedUpEvent(player.Id, player.Pos, id));
                picked++;
            }
            return picked;
        }
    }

    /// <summary>마법서 사용 결과 (3-5).</summary>
    public enum LearnResult
    {
        Learned = 0,
        /// <summary>이미 그 주문이 인벤토리에 있다 — 책은 그대로.</summary>
        AlreadyKnown = 1,
        /// <summary>그 칸이 마법서가 아니거나 비었다.</summary>
        NotASpellbook = 2,
        /// <summary>책이 가르치는 주문이 주문 데이터에 없다.</summary>
        UnknownSpell = 3,
    }

    /// <summary>
    /// 마법서 사용 = 주문 배우기 (3-5, 결정 2026-10-19): 책을 인벤토리에서 빼고 주문을 넣은 뒤, 빈 빠른 칸이 있으면 첫 빈 칸에 등록한다.
    /// 틱을 쓰지 않는 틱 밖 변경 (빠른 칸 등록과 같음) — 부른 쪽이 이벤트를 비운다.
    /// </summary>
    public static class Spellbooks
    {
        public static LearnResult TryLearn(World world, SpellBook spells, ItemBook items, Unit unit, int inventoryIndex)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (inventoryIndex < 0 || inventoryIndex >= unit.InventorySlots.Length) return LearnResult.NotASpellbook;
            var entry = unit.InventorySlots[inventoryIndex];
            if (entry.Kind != InventoryKind.Item || items == null || !items.TryGet(entry.Id, out var book) || book.Use != ItemUse.LearnSpell)
                return LearnResult.NotASpellbook;
            if (spells == null || !spells.TryGet(book.SpellId, out _)) return LearnResult.UnknownSpell;
            var spell = InventoryRef.Spell(book.SpellId);
            if (Array.IndexOf(unit.InventorySlots, spell) >= 0) return LearnResult.AlreadyKnown;

            Inventory.RemoveAt(world, unit, inventoryIndex);
            Inventory.TryAdd(world, spells, items, unit, spell); // 책이 빠진 칸이 있으니 늘 들어간다
            int quick = Array.IndexOf(unit.QuickSlots, InventoryRef.Empty);
            if (quick >= 0) Inventory.AssignQuickSlot(world, unit, quick, spell);
            world.Events.Publish(new SpellLearnedEvent(unit.Id, book.Id, book.SpellId, quick));
            return LearnResult.Learned;
        }
    }

    /// <summary>바닥에 아이템이 놓였다 (3-5 — 층 생성 · 돌아온 층 다시 그리기).</summary>
    public sealed class FloorItemPlacedEvent : ISimEvent
    {
        public GridPos Pos { get; }
        public string ItemId { get; }

        public FloorItemPlacedEvent(GridPos pos, string itemId)
        {
            Pos = pos;
            ItemId = itemId;
        }
    }

    /// <summary>바닥 아이템을 주웠다 (3-5).</summary>
    public sealed class ItemPickedUpEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public GridPos Pos { get; }
        public string ItemId { get; }

        public ItemPickedUpEvent(EntityId unit, GridPos pos, string itemId)
        {
            Unit = unit;
            Pos = pos;
            ItemId = itemId;
        }
    }

    /// <summary>인벤토리가 꽉 차 줍지 못했다 (3-5) — 그 칸에 서 있는 동안 틱마다 온다.</summary>
    public sealed class ItemPickupFailedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public GridPos Pos { get; }
        public string ItemId { get; }

        public ItemPickupFailedEvent(EntityId unit, GridPos pos, string itemId)
        {
            Unit = unit;
            Pos = pos;
            ItemId = itemId;
        }
    }

    /// <summary>마법서로 주문을 배웠다 (3-5). QuickSlot = 자동 등록한 빠른 칸, 빈 칸이 없었으면 −1.</summary>
    public sealed class SpellLearnedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public string ItemId { get; }
        public string SpellId { get; }
        public int QuickSlot { get; }

        public SpellLearnedEvent(EntityId unit, string itemId, string spellId, int quickSlot)
        {
            Unit = unit;
            ItemId = itemId;
            SpellId = spellId;
            QuickSlot = quickSlot;
        }
    }
}
