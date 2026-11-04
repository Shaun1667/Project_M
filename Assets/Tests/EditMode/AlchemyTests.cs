using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-16c 연금 솥 (결정 2026-11-04 — SPD): 씨앗 3개 → 가장 많은 씨앗의 물약 · 식별 · 연구실 방 (BALANCE.md 16 · 19번).</summary>
    public class AlchemyTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);

        private static readonly ItemBook Items = new ItemBook(new[]
        {
            new ItemDef("seed_firebloom", ItemUse.Seed, plantTrap: TrapType.Firebloom, brewsInto: "potion_liquid_flame"),
            new ItemDef("seed_icecap", ItemUse.Seed, plantTrap: TrapType.Icecap, brewsInto: "potion_frost"),
            new ItemDef("potion_liquid_flame", ItemUse.Potion, effect: ConsumableEffect.LiquidFlame),
            new ItemDef("potion_frost", ItemUse.Potion, effect: ConsumableEffect.Frost),
        });

        private static (World World, Unit Player) Start()
        {
            var world = new World(GridMovementTests.Parse("#####", "#...#", "#####"), 3UL);
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 20, 3, 8, 10), new GridPos(1, 1), Magic.QuickSlotCount);
            world.Events.Flush();
            return (world, player);
        }

        private static void Give(World world, Unit player, string id, int count)
        {
            for (int i = 0; i < count; i++) Inventory.TryAdd(world, null, Items, player, InventoryRef.Item(id));
        }

        private static int CountOf(Unit unit, string id)
        {
            int n = 0;
            foreach (var e in unit.InventorySlots) if (!e.IsEmpty && e.Id == id) n += e.Count;
            return n;
        }

        // 불꽃꽃 2 + 얼음버섯 1 → 액체 화염 (많은 쪽), 씨앗 3개가 빠지고 물약은 식별
        [Test]
        public void Brew_MostCommonSeedWins_AndIdentifies()
        {
            var (world, player) = Start();
            Give(world, player, "seed_firebloom", 2);
            Give(world, player, "seed_icecap", 1);
            var fire = InventoryRef.Item("seed_firebloom");
            var ice = InventoryRef.Item("seed_icecap");
            var made = Alchemy.Brew(world, Items, player, new List<InventoryRef> { ice, fire, fire });
            Assert.That(made, Is.EqualTo("potion_liquid_flame"));
            Assert.That(CountOf(player, "seed_firebloom") + CountOf(player, "seed_icecap"), Is.EqualTo(0));
            Assert.That(CountOf(player, "potion_liquid_flame"), Is.EqualTo(1));
            Assert.That(world.Run.IsKnown("potion_liquid_flame"), Is.True);
        }

        // 셋이 모두 다르면 먼저 고른 씨앗
        [Test]
        public void Brew_TieGoesToFirstPicked()
        {
            var (world, player) = Start();
            Give(world, player, "seed_firebloom", 1);
            Give(world, player, "seed_icecap", 2);
            var fire = InventoryRef.Item("seed_firebloom");
            var ice = InventoryRef.Item("seed_icecap");
            Assert.That(Alchemy.Brew(world, Items, player, new List<InventoryRef> { ice, fire }), Is.Null, "2개는 안 됨");
            Assert.That(Alchemy.Brew(world, Items, player, new List<InventoryRef> { fire, fire, ice }), Is.Null, "불꽃꽃이 1개뿐");
            Assert.That(CountOf(player, "seed_icecap"), Is.EqualTo(2), "실패하면 아무것도 안 빠짐");
            Give(world, player, "seed_firebloom", 1);
            Assert.That(Alchemy.Brew(world, Items, player, new List<InventoryRef> { fire, ice, ice }), Is.EqualTo("potion_frost"));
        }

        // 연구실: 입구 하나 · 가운데 연금 솥(걸을 수 없음, 시야 안 가림) · 씨앗
        [Test]
        public void Laboratory_HasPot()
        {
            var gen = new DungeonGenConfig(width: 50, height: 50, roomCount: 10, roomSizeRoll: 20, roomMinSize: 7,
                wallBlockCount: 20, wallBlockSizeRoll: 10, wallBlockMinSize: 3, wallTunnelCostMultiplier: 5, placementAttemptLimit: 1000, deadEndLoopMinWalkDistance: 10);
            var config = new SpecialRoomConfig(100, new[] { 0, 0, 0, 0, 1 }, 3, 5, new string[0], 0, new string[0], 0, new string[0], 0, new string[0], 0, "key_iron",
                new[] { "seed_firebloom" }, 2);
            int carved = 0;
            for (ulong seed = 1; seed <= 10; seed++)
            {
                DungeonFloor floor = null;
                var world = World.Create(seed, rng => (floor = new DungeonGenerator(gen).Generate(rng)).Map);
                var room = SpecialRooms.Carve(world, floor, config, world.Rng.Get(RngStream.Decor));
                if (room == null) continue;
                carved++;
                Assert.That(room.Type, Is.EqualTo(SpecialRoomType.Laboratory));
                int pots = 0;
                foreach (var c in room.Cells) if (world.Map.Get(c) == TileType.AlchemyPot) pots++;
                Assert.That(pots, Is.EqualTo(1), $"시드 {seed}");
                var pot = room.Cells.Find(c => world.Map.Get(c) == TileType.AlchemyPot);
                Assert.That(world.Map.IsWalkable(pot), Is.False);
                Assert.That(world.Map.BlocksSight(pot), Is.False);
            }
            Assert.That(carved, Is.GreaterThan(0));
        }

        [Test]
        public void Texts()
        {
            Assert.That(HudTextBrew(), Does.Contain("1/3"));
        }

        private static string HudTextBrew() => ProjectM.UI.HudText.BrewTitle(0);

        // 검산: 씨앗 5종의 연금 결과 = BALANCE.md 19번, 결과 물약은 ItemCatalog에 있다
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            var expected = new Dictionary<string, string>
            {
                { "seed_firebloom", "potion_liquid_flame" }, { "seed_icecap", "potion_frost" }, { "seed_sorrowmoss", "potion_toxic_gas" },
                { "seed_sungrass", "potion_healing" }, { "seed_fadeleaf", "potion_invisibility" },
            };
            foreach (var pair in expected)
            {
                Assert.That(items.TryGet(pair.Key, out var seed), Is.True, pair.Key);
                Assert.That(seed.BrewsInto, Is.EqualTo(pair.Value), pair.Key);
                Assert.That(items.TryGet(pair.Value, out var potion) && potion.Use == ItemUse.Potion, Is.True, pair.Value);
            }
        }
    }
}
