using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.UI;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-15c 상점 (결정 2026-11-04 — 3층 · 사기 + 팔기): 가격표 물건 사기 · 금화 모자람 · 팔기(값의 절반) · 상인 중립 · 스냅샷 v22 (BALANCE.md 16번).</summary>
    public class ShopTests
    {
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        private static readonly ItemBook Items = new ItemBook(new[]
        {
            new ItemDef("gold", ItemUse.Gold),
            new ItemDef("potion_healing", ItemUse.Potion, effect: ConsumableEffect.Healing, weight: 3, value: 30),
            new ItemDef("shortsword", ItemUse.EquipWeapon, weapon: new WeaponDef(15, 3, 100, 1, 1, 12), value: 60),
            new ItemDef("key_iron", ItemUse.Key),
        });

        private static ShopConfig Config() => new ShopConfig(3, new[] { "potion_healing", "shortsword" }, 2, 100, 50, "shopkeeper", 999);

        private static (World World, Unit Player, TurnClock Clock) Start()
        {
            var world = new World(GridMovementTests.Parse("#######", "#.....#", "#######"), 5UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            world.Scheduler.AddSystem(TickPhase.ManaRegen, new ItemPickupSystem(world, Items));
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 20, 3, 8, 10), new GridPos(1, 1), Magic.QuickSlotCount);
            world.Events.Flush();
            return (world, player, new TurnClock(world.Scheduler, world.PlayerInput));
        }

        private static void Step(World world, TurnClock clock)
        {
            Assert.That(PlayerActions.TryStep(world, StepConfig, new GridPos(1, 0), out var step), Is.True);
            clock.SubmitPlayerCommand(step);
            world.Events.Flush();
        }

        // 가격표 물건: 금화가 모자라면 안 집히고, 있으면 금화가 빠지며 배낭으로
        [Test]
        public void PricedItem_BoughtOnlyWithEnoughGold()
        {
            var (world, player, clock) = Start();
            world.FloorItems.Add(new GridPos(2, 1), Loot.GoldItemId, 20); // 금화 20 먼저 줍고
            world.FloorItems.Add(new GridPos(2, 1), "potion_healing", 1, 30); // 30짜리는 모자람
            Step(world, clock);
            Assert.That(world.FloorItems.Count, Is.EqualTo(1), "모자람 — 그대로");
            Assert.That(world.Run.Gold, Is.EqualTo(20));

            world.FloorItems.Add(new GridPos(2, 1), Loot.GoldItemId, 30);
            for (int i = 0; i < 2; i++) clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig)); // 그 칸에 선 채: 금화 → 다음 틱에 물건
            world.Events.Flush();
            Assert.That(world.FloorItems.Count, Is.EqualTo(0));
            Assert.That(world.Run.Gold, Is.EqualTo(20));
            Assert.That(System.Array.IndexOf(player.InventorySlots, InventoryRef.Item("potion_healing")), Is.GreaterThanOrEqualTo(0));
        }

        // 팔기: 값 × 50%, 한 개씩, 값 0(열쇠 등)은 못 팜
        [Test]
        public void Sell_HalfValue()
        {
            var (world, player, _) = Start();
            int index = Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("shortsword"));
            Assert.That(Shop.Sell(world, Items, Config(), player, index), Is.EqualTo(30));
            Assert.That(world.Run.Gold, Is.EqualTo(30));
            Assert.That(player.InventorySlots[index].IsEmpty, Is.True);
            int key = Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("key_iron"));
            Assert.That(Shop.Sell(world, Items, Config(), player, key), Is.EqualTo(0));
            Assert.That(Shop.BuyPrice(Items, Config(), "shortsword"), Is.EqualTo(60));
        }

        // 상인은 중립 — 플레이어 · 적 누구와도 적대 아님, 부딪혀도 공격하지 않음
        [Test]
        public void Shopkeeper_IsNeutral()
        {
            Assert.That(Factions.AreHostile(Faction.Neutral, Faction.Player), Is.False);
            Assert.That(Factions.AreHostile(Faction.Neutral, Faction.Enemy), Is.False);
            var (world, _, _) = Start();
            UnitSpawner.SpawnNeutral(world, new UnitDef("shopkeeper", 999, 0, 6), new GridPos(2, 1), new IdleNeutralAi());
            Assert.That(PlayerActions.TryStep(world, StepConfig, new GridPos(1, 0), out _), Is.False);
        }

        // 상점 짓기: 상점 층에서만, 상인 + 가격표 물건
        [Test]
        public void Build_OnShopFloorOnly()
        {
            var gen = new DungeonGenConfig(width: 50, height: 50, roomCount: 10, roomSizeRoll: 20, roomMinSize: 7,
                wallBlockCount: 20, wallBlockSizeRoll: 10, wallBlockMinSize: 3, wallTunnelCostMultiplier: 5, placementAttemptLimit: 1000, deadEndLoopMinWalkDistance: 10);
            DungeonFloor floor = null;
            var world = World.Create(4UL, rng => (floor = new DungeonGenerator(gen).Generate(rng)).Map);
            Assert.That(Shop.Build(world, floor, Config(), Items, world.Rng.Get(RngStream.Decor), new IdleNeutralAi()), Is.False, "1층");
            world.EnterNewFloor(3, floor.Map);
            Assert.That(Shop.Build(world, floor, Config(), Items, world.Rng.Get(RngStream.Decor), new IdleNeutralAi()), Is.True);
            Assert.That(world.Units.ToSortedList().Exists(u => u.Faction == Faction.Neutral), Is.True);
            int priced = 0;
            foreach (var it in world.FloorItems.All) if (it.Price > 0) priced++;
            Assert.That(priced, Is.EqualTo(2));
        }

        // 스냅샷 v22: 가격표 왕복
        [Test]
        public void Snapshot_RoundTripsPrice()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(22), "3-15c 상점");
            var (world, _, _) = Start();
            world.FloorItems.Add(new GridPos(4, 1), "shortsword", 1, 60);
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => null);
            Assert.That(restored.FloorItems.ItemsAt(new GridPos(4, 1))[0].Price, Is.EqualTo(60));
        }

        [Test]
        public void Texts()
        {
            var text = new HudText();
            Assert.That(text.CannotAfford("x", 30), Does.Contain("금화 30개가 필요하다"));
        }

        // 검산: 상점 값 = BALANCE.md 16번, 물건 목록은 ItemCatalog에 있고 값이 0이 아니다
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var c = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToShop();
            Assert.That((c.Floor, c.StockCount, c.BuyPercent, c.SellPercent, c.KeeperHp), Is.EqualTo((3, 6, 100, 50, 999)));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            foreach (var id in c.Stock)
            {
                Assert.That(items.TryGet(id, out var d), Is.True, id);
                Assert.That(d.Value, Is.GreaterThan(0), id);
            }
            items.TryGet("shortsword", out var sword);
            Assert.That(sword.Value, Is.EqualTo(60));
        }
    }
}
