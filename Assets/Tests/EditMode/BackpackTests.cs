using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.UI;
using ProjectM.Units;

namespace ProjectM.Tests
{
    /// <summary>3-10b 배낭 (SPD — 결정 2026-10-20): 같은 물약 · 주문서는 한 칸에 겹침 · 장비 5칸 이름 · 설명 줄 · 스냅샷 v19.</summary>
    public class BackpackTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);

        private static readonly ItemBook Items = new ItemBook(new[]
        {
            new ItemDef("potion_healing", ItemUse.Potion, effect: ConsumableEffect.Healing, weight: 3),
            new ItemDef("scroll_rage", ItemUse.Scroll, effect: ConsumableEffect.Rage, weight: 1),
            new ItemDef("fusion_device", ItemUse.Fusion),
        });

        private static (World World, Unit Player) Start()
        {
            var world = new World(GridMovementTests.Parse("#####", "#...#", "#####"), 3UL);
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount, 6);
            world.Events.Flush();
            return (world, player);
        }

        [Test]
        public void SameConsumables_StackInOneSlot_OthersDoNot()
        {
            var (world, player) = Start();
            Assert.That(Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("potion_healing")), Is.EqualTo(0));
            Assert.That(Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("fusion_device")), Is.EqualTo(1));
            Assert.That(Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("potion_healing")), Is.EqualTo(0), "같은 칸에 겹침");
            Assert.That(player.InventorySlots[0].Count, Is.EqualTo(2));
            Assert.That(Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("fusion_device")), Is.EqualTo(2), "물약 · 주문서가 아니면 칸마다");
        }

        // 겹친 칸이 꽉 찬 인벤토리에서도 하나 더 들어간다
        [Test]
        public void Stack_FitsWhenFull()
        {
            var (world, player) = Start();
            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("scroll_rage"));
            for (int i = 1; i < player.InventorySlots.Length; i++) Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("fusion_device"));
            Assert.That(Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("fusion_device")), Is.EqualTo(-1), "꽉 참");
            Assert.That(Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("scroll_rage")), Is.EqualTo(0));
            Assert.That(player.InventorySlots[0].Count, Is.EqualTo(2));
        }

        // 쓰면 하나씩 — 마지막 하나를 쓰면 칸 · 빠른 칸이 빈다. 빠른 칸은 개수와 상관없이 같은 항목
        [Test]
        public void RemoveAt_TakesOne_QuickSlotKeepsUntilLast()
        {
            var (world, player) = Start();
            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("potion_healing"));
            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("potion_healing"));
            Inventory.AssignQuickSlot(world, player, 0, player.InventorySlots[0]);
            Assert.That(Inventory.QuickSlotOf(player, InventoryRef.Item("potion_healing")), Is.EqualTo(0), "개수는 항목 판정에 안 씀");

            Inventory.RemoveAt(world, player, 0);
            Assert.That(player.InventorySlots[0].Count, Is.EqualTo(1));
            Assert.That(player.QuickSlots[0].IsEmpty, Is.False);
            Inventory.RemoveAt(world, player, 0);
            Assert.That(player.InventorySlots[0].IsEmpty, Is.True);
            Assert.That(player.QuickSlots[0].IsEmpty, Is.True);
        }

        // 스냅샷 v19: 개수가 왕복
        [Test]
        public void Snapshot_RoundTripsCounts()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(19), "3-10b 겹치기");
            var (world, player) = Start();
            for (int i = 0; i < 3; i++) Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("potion_healing"));
            world.Events.Flush();
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => null);
            restored.Units.TryGet(player.Id, out var p);
            Assert.That(p.InventorySlots[0].Count, Is.EqualTo(3));
        }

        [Test]
        public void Texts()
        {
            Assert.That(HudText.EquipSlotNames, Is.EqualTo(new[] { "무기", "갑옷", "유물", "기타", "반지" }));
            var text = new HudText();
            Assert.That(HudText.ItemInfo(text, Items, InventoryRef.Item("potion_healing").WithCount(2)), Does.Contain("(2개) — 물약. 마시기 · 던지기"));
            Assert.That(HudText.ItemInfo(text, Items, InventoryRef.Empty), Is.Empty);
        }
    }
}
