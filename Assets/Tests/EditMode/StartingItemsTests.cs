using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Items;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests.EditMode
{
    /// <summary>시작 배낭 (2026-11-05 지스타 시연 — 마법 융합기 · 물 마법서 · 치유 물약 + 무작위 마법서 하나).</summary>
    public class StartingItemsTests
    {
        private static readonly string[] Pool = { "spellbook_fire", "spellbook_ice", "spellbook_lightning", "spellbook_poison" };

        private static UnitDef Def() => new UnitDef("player", 20, 3, 8, 10, null,
            startingItems: new[] { "fusion_device", "spellbook_water", "", null }, startingRandomItems: Pool);

        private static World NewWorld(ulong seed) => new World(GridMovementTests.Parse("#####", "#...#", "#####"), seed);

        [Test]
        public void UnitDef_CopiesKeepStartingItems_EmptyIdsDropped()
        {
            var def = Def();
            Assert.That(def.StartingItems, Is.EqualTo(new[] { "fusion_device", "spellbook_water" }));
            foreach (var copy in new[] { def.WithVisualId("hero_black"), def.WithExtraStartingSpells(new[] { "sia_leap" }), def.Scaled(150) })
            {
                Assert.That(copy.StartingItems, Is.EqualTo(def.StartingItems));
                Assert.That(copy.StartingRandomItems, Is.EqualTo(Pool));
            }
            Assert.That(new UnitDef("enemy_melee", 8, 2, 6).StartingItems, Is.Empty);
        }

        [Test]
        public void Roll_FixedItemsPlusOneFromPool_SameSeedSamePick_LootStreamUntouched()
        {
            var a = NewWorld(7UL);
            var b = NewWorld(7UL);
            var lootBefore = b.Rng.Get(RngStream.Loot).Range(0, 1000000);
            var kitA = StartingKit.Roll(a, Def());
            var kitB = StartingKit.Roll(NewWorld(7UL), Def());
            Assert.That(kitA.Take(2), Is.EqualTo(new[] { "fusion_device", "spellbook_water" }));
            Assert.That(kitA.Count, Is.EqualTo(3));
            Assert.That(Pool, Does.Contain(kitA[2]));
            Assert.That(kitB, Is.EqualTo(kitA), "같은 시드 = 같은 책");
            Assert.That(a.Rng.Get(RngStream.Loot).Range(0, 1000000), Is.EqualTo(lootBefore), "StartKit 난수만 씀 — 아이템 배치 난수는 그대로");

            var seen = new HashSet<string>();
            for (ulong seed = 1; seed <= 60; seed++) seen.Add(StartingKit.Roll(NewWorld(seed), Def())[2]);
            Assert.That(seen, Is.EquivalentTo(Pool), "시드에 따라 네 권 모두 나옴");
        }

        [Test]
        public void PlanSpellbooks_ExcludesStartingBooks()
        {
            var items = new ItemBook(new[]
            {
                new ItemDef("spellbook_fire", ItemUse.LearnSpell, "fire_basic"),
                new ItemDef("spellbook_water", ItemUse.LearnSpell, "water_basic"),
                new ItemDef("spellbook_ice", ItemUse.LearnSpell, "ice_basic"),
            });
            var world = NewWorld(3UL);
            Loot.PlanSpellbooks(world, items, new LootConfig(2, 3), 5, new[] { "spellbook_water", "spellbook_fire" });
            Assert.That(world.Run.PendingFloorItems.Select(p => p.ItemId), Is.EqualTo(new[] { "spellbook_ice" }));
        }

        [Test]
        public void PlayerAsset_StartKit()
        {
            var player = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/Player.asset").ToDef();
            Assert.That(player.StartingItems, Is.EqualTo(new[] { "fusion_device", "spellbook_water", "potion_healing" }));
            Assert.That(player.StartingRandomItems, Is.EqualTo(Pool));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            foreach (var id in player.StartingItems.Concat(player.StartingRandomItems)) Assert.That(items.TryGet(id, out _), Is.True, $"ItemCatalog에 {id}");
        }
    }
}
