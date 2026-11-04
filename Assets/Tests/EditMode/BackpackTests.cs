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
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(19), "3-10b 겹치기 (정확한 값은 HungerTests — 20)");
            var (world, player) = Start();
            for (int i = 0; i < 3; i++) Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("potion_healing"));
            world.Events.Flush();
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => null);
            restored.Units.TryGet(player.Id, out var p);
            Assert.That(p.InventorySlots[0].Count, Is.EqualTo(3));
        }

        // 대상 고르기 (3-10b): 조건에 맞는 칸만 고를 수 있고, 고르면 닫히며 (주문서, 대상)을 넘긴다
        [Test]
        public void ItemPick_OnlyFilteredEntries()
        {
            var (world, player) = Start();
            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("fusion_device"));
            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("potion_healing"));
            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("scroll_rage"));
            var menu = new InventoryMenu();
            menu.OpenForPick(player.InventorySlots[2], e => e.Id == "potion_healing", player.InventorySlots);
            Assert.That(menu.State, Is.EqualTo(InventoryMenuState.ItemPick));
            Assert.That(menu.Selected, Is.EqualTo(1), "고를 수 있는 첫 칸");
            menu.Select(0, player.InventorySlots.Length);
            Assert.That(menu.Confirm(world, player).Kind, Is.EqualTo(InventoryActionKind.None), "조건 밖 칸");
            menu.Select(1, player.InventorySlots.Length);
            var action = menu.Confirm(world, player);
            Assert.That(action.Kind, Is.EqualTo(InventoryActionKind.PickedItem));
            Assert.That((action.Entry.Id, action.Second.Id), Is.EqualTo(("scroll_rage", "potion_healing")));
            Assert.That(menu.IsOpen, Is.False);
        }

        // 식별 주문서의 고르기 조건 = 아직 모르는 아이템 (주문서 자신 제외)
        [Test]
        public void IdentifyPickFilter_UnknownItemsOnly()
        {
            var items = new ItemBook(new[]
            {
                new ItemDef("scroll_identify", ItemUse.Scroll, effect: ConsumableEffect.Identify, weight: 1),
                new ItemDef("potion_healing", ItemUse.Potion, effect: ConsumableEffect.Healing, weight: 3),
            });
            var (world, _) = Start();
            Identification.AssignLooks(world, items, ConsumableTests.BalanceConfig());
            var filter = Consumables.PickFilter(world, items, InventoryRef.Item("scroll_identify"));
            Assert.That(filter, Is.Not.Null);
            Assert.That(filter(InventoryRef.Item("potion_healing")), Is.True);
            Assert.That(filter(InventoryRef.Item("scroll_identify")), Is.False, "자기 자신");
            Assert.That(Consumables.PickFilter(world, items, InventoryRef.Item("potion_healing")), Is.Null, "고르기 없는 물약");
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
