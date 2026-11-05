using NUnit.Framework;
using ProjectM.Data;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests.EditMode
{
    /// <summary>시작 배낭 아이템 (2026-11-05 지스타 시연 — 마법 융합기).</summary>
    public class StartingItemsTests
    {
        [Test]
        public void UnitDef_CopiesKeepStartingItems_EmptyIdsDropped()
        {
            var def = new UnitDef("player", 20, 3, 8, 10, null, startingItems: new[] { "fusion_device", "", null });
            Assert.That(def.StartingItems, Is.EqualTo(new[] { "fusion_device" }));
            Assert.That(def.WithVisualId("hero_black").StartingItems, Is.EqualTo(new[] { "fusion_device" }));
            Assert.That(def.WithExtraStartingSpells(new[] { "sia_leap" }).StartingItems, Is.EqualTo(new[] { "fusion_device" }));
            Assert.That(def.Scaled(150).StartingItems, Is.EqualTo(new[] { "fusion_device" }));
            Assert.That(new UnitDef("enemy_melee", 8, 2, 6).StartingItems, Is.Empty);
        }

        [Test]
        public void PlayerAsset_StartsWithFusionDevice()
        {
            var player = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/Player.asset").ToDef();
            Assert.That(player.StartingItems, Does.Contain("fusion_device"));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            foreach (var id in player.StartingItems) Assert.That(items.TryGet(id, out _), Is.True, $"ItemCatalog에 {id}");
        }
    }
}
