using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>상점 수치 (3-15c). 값은 FloorDecorConfigAsset에서 — 기준은 BALANCE.md 16번.</summary>
    public sealed class ShopConfig
    {
        /// <summary>상점이 있는 층 (결정 2026-11-04: 3층).</summary>
        public int Floor { get; }
        /// <summary>파는 물건 목록 · 놓는 수 (목록에서 겹치지 않게).</summary>
        public IReadOnlyList<string> Stock { get; }
        public int StockCount { get; }
        /// <summary>사는 값 = 아이템 값(ItemDef.Value) × 이 % · 파는 값 = 값 × SellPercent %.</summary>
        public int BuyPercent { get; }
        public int SellPercent { get; }
        /// <summary>상인 모습 · 체력.</summary>
        public string KeeperVisualId { get; }
        public int KeeperHp { get; }

        /// <summary>3-29 화난 상인: 체력 = 그 구역 보스 체력 × % (공격 · 명중 · 회피는 보스 값) · 차례마다 물건을 쓸 확률 % · 체력이 이 % 아래면 치유 물약.</summary>
        public int AngryHpPercent { get; }
        public int ItemUsePercent { get; }
        public int HealBelowPercent { get; }

        public ShopConfig(int floor, IReadOnlyList<string> stock, int stockCount, int buyPercent, int sellPercent, string keeperVisualId, int keeperHp,
            int angryHpPercent = 150, int itemUsePercent = 35, int healBelowPercent = 50)
        {
            if (angryHpPercent < 1 || itemUsePercent < 0 || itemUsePercent > 100 || healBelowPercent < 0 || healBelowPercent > 100) throw new ArgumentOutOfRangeException(nameof(angryHpPercent));
            AngryHpPercent = angryHpPercent;
            ItemUsePercent = itemUsePercent;
            HealBelowPercent = healBelowPercent;
            if (floor < 1 || stockCount < 0 || buyPercent < 1 || sellPercent < 0 || keeperHp < 1) throw new ArgumentOutOfRangeException(nameof(floor));
            Floor = floor;
            Stock = stock ?? Array.Empty<string>();
            StockCount = stockCount;
            BuyPercent = buyPercent;
            SellPercent = sellPercent;
            KeeperVisualId = string.IsNullOrEmpty(keeperVisualId) ? "shopkeeper" : keeperVisualId;
            KeeperHp = keeperHp;
        }
    }

    /// <summary>
    /// 상점 (3-15c, 결정 2026-11-04 — 3층 · 사기 + 팔기): 상점 방(특수 방처럼 벽 속에 파고 보통 문)에 상인(Faction.Neutral) + 가격표 물건.
    /// 사기 = 가격표 물건 칸에 걸어 들어가면 금화가 충분할 때 줍는다(ItemPickupSystem). 팔기 = 상인에게 부딪혀 배낭에서 고름 — 값 × SellPercent.
    /// </summary>
    public static class Shop
    {
        public static int BuyPrice(ItemBook items, ShopConfig config, string itemId) =>
            items.TryGet(itemId, out var def) ? Math.Max(1, def.Value * config.BuyPercent / 100) : 0;

        public static int SellPrice(ItemBook items, ShopConfig config, InventoryRef entry) =>
            entry.Kind == InventoryKind.Item && items.TryGet(entry.Id, out var def) ? def.Value * config.SellPercent / 100 : 0;

        /// <summary>
        /// 상점 층이면 상점 방을 파서 채운다 (꾸미기 안, Decor 난수 — SpecialRooms와 같은 파기). 못 팠으면 false.
        /// </summary>
        public static bool Build(World world, DungeonFloor floor, ShopConfig config, ItemBook items, IRng rng, ICommandSource keeperAi)
        {
            if (config == null || items == null || keeperAi == null || world.Floor != config.Floor) return false;
            var room = SpecialRooms.CarveEmpty(world, floor, rng, 4, 5, TileType.Door);
            if (room == null) return false;
            var cells = new List<GridPos>(room.Cells);
            // 상인은 입구에서 가장 먼 칸 (막지 않게), 물건은 나머지 칸에
            cells.Sort((a, b) => GridMovement.Chebyshev(b, room.Entrance).CompareTo(GridMovement.Chebyshev(a, room.Entrance)));
            var keeperDef = new UnitDef(config.KeeperVisualId, config.KeeperHp, 0, 6);
            UnitSpawner.SpawnNeutral(world, keeperDef, cells[0], keeperAi, Math.Max(1, config.StockCount)); // 3-29: 화나면 물건을 챙길 칸
            cells.RemoveAt(0);
            var stock = new List<string>();
            foreach (var id in config.Stock) if (items.TryGet(id, out _)) stock.Add(id);
            for (int i = 0; i < config.StockCount && stock.Count > 0 && cells.Count > 0; i++)
            {
                int k = rng.Range(0, stock.Count);
                int s = rng.Range(0, cells.Count);
                world.FloorItems.Add(cells[s], stock[k], 1, BuyPrice(items, config, stock[k]));
                world.Events.Publish(new FloorItemPlacedEvent(cells[s], stock[k], Identification.VisualOf(world, stock[k])));
                stock.RemoveAt(k);
                cells.RemoveAt(s);
            }
            return true;
        }

        /// <summary>팔기 (틱 밖 — 입력이 부르고 이벤트를 비운다): 배낭 칸 하나를 빼고 금화 + SellPrice. 판 값 (0이면 못 팜).</summary>
        public static int Sell(World world, ItemBook items, ShopConfig config, Unit seller, int inventoryIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= seller.InventorySlots.Length) return 0;
            var entry = seller.InventorySlots[inventoryIndex];
            int price = SellPrice(items, config, entry);
            if (price <= 0) return 0;
            Inventory.RemoveAt(world, seller, inventoryIndex);
            world.Run.Gold += price;
            world.Events.Publish(new GoldChangedEvent(world.Run.Gold, price, fromSale: true));
            world.Events.Publish(new ItemSoldEvent(entry.Id, price));
            return price;
        }
    }

    /// <summary>상인 · 중립 유닛 AI (3-15c): 늘 기다린다.</summary>
    public sealed class IdleNeutralAi : ICommandSource
    {
        public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
    }

    /// <summary>물건을 팔았다 (로그).</summary>
    public sealed class ItemSoldEvent : ISimEvent
    {
        public string ItemId { get; }
        public int Price { get; }

        public ItemSoldEvent(string itemId, int price)
        {
            ItemId = itemId;
            Price = price;
        }
    }

    /// <summary>물건을 샀다 (로그).</summary>
    public sealed class ItemBoughtEvent : ISimEvent
    {
        public string ItemId { get; }
        public int Price { get; }

        public ItemBoughtEvent(string itemId, int price)
        {
            ItemId = itemId;
            Price = price;
        }
    }

    /// <summary>금화가 모자라 못 샀다 (안내).</summary>
    public sealed class PurchaseFailedEvent : ISimEvent
    {
        public string ItemId { get; }
        public int Price { get; }

        public PurchaseFailedEvent(string itemId, int price)
        {
            ItemId = itemId;
            Price = price;
        }
    }
}
