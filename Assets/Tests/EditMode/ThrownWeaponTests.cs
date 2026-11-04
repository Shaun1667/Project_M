using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-16a 던지는 무기 (SPD — 결정 2026-11-04 다트 · 투창): 명중 · 피해 · 떨어진 칸에 남음 · 부러짐 · 묶음 줍기 (BALANCE.md 11 · 13번).</summary>
    public class ThrownWeaponTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        private static readonly ItemBook Items = new ItemBook(new[]
        {
            new ItemDef("dart", ItemUse.Thrown, throwMinDamage: 1, throwMaxDamage: 3, breakPercent: 0),
            new ItemDef("javelin_always_breaks", ItemUse.Thrown, throwMinDamage: 3, throwMaxDamage: 8, breakPercent: 100),
        });

        private static (World World, Unit Player, Unit Enemy, ConsumableRules Rules) Start()
        {
            var world = new World(GridMovementTests.Parse("##########", "#........#", "##########"), 5UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 20, 3, 8, 10), new GridPos(1, 1), Magic.QuickSlotCount);
            var enemy = UnitSpawner.SpawnEnemy(world, new UnitDef("enemy_melee", 30, 1, 6), new GridPos(5, 1), new WaitAi());
            world.Events.Flush();
            return (world, player, enemy, new ConsumableRules(Items));
        }

        // 다트: 회피 0인 적은 맞음 · 피해 1~3 · 다트는 그 칸 바닥에 남음
        [Test]
        public void Dart_HitsAndStaysOnFloor()
        {
            var (world, player, enemy, rules) = Start();
            int index = Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("dart").WithCount(3));
            Assert.That(Consumables.Throw(world, rules, new BresenhamTargetingRule(), player, index, enemy.Pos), Is.EqualTo(ConsumeResult.Used));
            Assert.That(enemy.Hp, Is.InRange(30 - 3, 30 - 1));
            Assert.That(world.FloorItems.At(enemy.Pos), Is.EqualTo(new[] { "dart" }));
            Assert.That(player.InventorySlots[index].Count, Is.EqualTo(2), "한 개만 던짐");
        }

        // 부러짐 100%면 바닥에 안 남는다
        [Test]
        public void Javelin_CanBreak()
        {
            var (world, player, enemy, rules) = Start();
            int index = Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("javelin_always_breaks"));
            Consumables.Throw(world, rules, new BresenhamTargetingRule(), player, index, enemy.Pos);
            Assert.That(world.FloorItems.Count, Is.EqualTo(0));
        }

        // 빈 칸에 던지면 그 칸에 떨어진다
        [Test]
        public void ThrowAtEmptyCell_Lands()
        {
            var (world, player, _, rules) = Start();
            int index = Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("dart"));
            Consumables.Throw(world, rules, new BresenhamTargetingRule(), player, index, new GridPos(3, 1));
            Assert.That(world.FloorItems.At(new GridPos(3, 1)), Is.EqualTo(new[] { "dart" }));
        }

        // 묶음 줍기: 바닥 다트 5개 = 배낭 한 칸 ×5
        [Test]
        public void Bundle_PickedUpWithCount()
        {
            var (world, player, _, _) = Start();
            world.FloorItems.Add(player.Pos, "dart", 5);
            ItemPickupSystem.PickUp(world, Items);
            Assert.That(player.InventorySlots[0].Count, Is.EqualTo(5));
            Assert.That(Consumables.CanThrow(Items, InventoryRef.Item("dart")), Is.True);
        }

        // 검산: ItemCatalog 다트 · 투창 = BALANCE.md 13번, LootConfig 묶음 = 11번
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            Assert.That(items.TryGet("dart", out var dart), Is.True);
            Assert.That((dart.Use, dart.ThrowMinDamage, dart.ThrowMaxDamage, dart.BreakPercent), Is.EqualTo((ItemUse.Thrown, 1, 3, 0)));
            Assert.That(items.TryGet("javelin", out var jav), Is.True);
            Assert.That((jav.ThrowMinDamage, jav.ThrowMaxDamage, jav.BreakPercent), Is.EqualTo((3, 8, 33)));
            var loot = AssetDatabase.LoadAssetAtPath<LootConfigAsset>("Assets/_Project/Data/Items/LootConfig.asset").ToConfig();
            Assert.That(loot.ThrownPerFloor, Is.EqualTo(1));
            Assert.That(loot.ThrownItems, Is.EqualTo(new[] { "dart", "javelin" }));
            Assert.That(loot.ThrownBundles, Is.EqualTo(new[] { 5, 2 }));
        }

        private sealed class WaitAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }
    }
}
