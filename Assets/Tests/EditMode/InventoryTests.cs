using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.UI;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>테스트용: 주문을 인벤토리에 넣고(이미 있으면 그대로) 빠른 칸에 등록한다.</summary>
    internal static class TestInventory
    {
        public static void Equip(World world, SpellBook book, Unit unit, int slot, string spellId)
        {
            var entry = InventoryRef.Spell(spellId);
            if (!unit.HasSpell(spellId)) Assert.That(Inventory.TryAdd(world, book, ItemBook.Empty, unit, entry), Is.GreaterThanOrEqualTo(0));
            Inventory.AssignQuickSlot(world, unit, slot, entry);
            world.Events.Flush();
        }
    }

    /// <summary>3-2a 인벤토리 · 빠른 칸: 넣기 · 빼기 · 등록 · 저장, 인벤토리에서 바로 시전, 인벤토리 창 상태.</summary>
    public class InventoryTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6, inventorySlotCount: 10);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10, new[] { "fire_basic" });
        private static readonly ItemBook Items = new ItemBook(new[] { new ItemDef("fusion_device", ItemUse.Fusion) });
        private static readonly InventoryRef Fire = InventoryRef.Spell("fire_basic");
        private static readonly InventoryRef Ice = InventoryRef.Spell("ice_basic");
        private static readonly InventoryRef Device = InventoryRef.Item("fusion_device");

        private sealed class Session
        {
            public World World;
            public SpellBook Book;
            public Unit Player;
            public readonly List<InventoryChangedEvent> InventoryEvents = new List<InventoryChangedEvent>();
            public readonly List<QuickSlotsChangedEvent> QuickEvents = new List<QuickSlotsChangedEvent>();

            public int Add(InventoryRef entry) => Inventory.TryAdd(World, Book, Items, Player, entry);
        }

        private static Session Start()
        {
            var map = GridMovementTests.Parse(
                "########",
                "#......#",
                "#......#",
                "########");
            var world = new World(map, 3UL);
            var s = new Session { World = world, Book = SpellTests.BalanceBookFull() };
            world.InstallCoreSystems(new ShadowcastSightRule(), Magic, s.Book.Statuses, s.Book.Terrains);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount, Magic.InventorySlotCount);
            world.Events.Subscribe<InventoryChangedEvent>(s.InventoryEvents.Add);
            world.Events.Subscribe<QuickSlotsChangedEvent>(s.QuickEvents.Add);
            world.Events.Flush();
            return s;
        }

        [Test]
        public void StartingSpell_InInventoryAndFirstQuickSlot()
        {
            var s = Start();
            Assert.That(s.Player.InventorySlots.Length, Is.EqualTo(10));
            Assert.That(s.Player.QuickSlots.Length, Is.EqualTo(6));
            Assert.That(s.Player.InventorySlots[0], Is.EqualTo(Fire));
            Assert.That(s.Player.QuickSlots[0], Is.EqualTo(Fire));
            Assert.That(s.Player.QuickSlots[1].IsEmpty, Is.True);
        }

        [Test]
        public void TryAdd_FillsFrontEmptySlot_SpellOnce_ItemsMany_FullRejects()
        {
            var s = Start();
            Assert.That(s.Add(Ice), Is.EqualTo(1));
            Assert.That(s.Add(Ice), Is.EqualTo(-1), "같은 주문은 한 번만");
            Assert.That(s.Add(Device), Is.EqualTo(2));
            Assert.That(s.Add(Device), Is.EqualTo(3), "아이템은 여러 개");
            while (s.Add(Device) >= 0) { }
            Assert.That(Array.IndexOf(s.Player.InventorySlots, InventoryRef.Empty), Is.EqualTo(-1), "꽉 참");
            Assert.That(s.Add(InventoryRef.Spell("poison_basic")), Is.EqualTo(-1), "꽉 차면 못 넣음");
            Assert.Throws<ArgumentException>(() => s.Add(InventoryRef.Spell("no_such_spell")));
            Assert.Throws<ArgumentException>(() => s.Add(InventoryRef.Item("no_such_item")));
            s.World.Events.Flush();
            Assert.That(s.InventoryEvents.Count, Is.EqualTo(9), "넣을 때마다 한 번 (실패는 없음)");
        }

        [Test]
        public void AssignQuickSlot_OneSlotPerEntry_MustBeInInventory()
        {
            var s = Start();
            s.Add(Ice);
            Inventory.AssignQuickSlot(s.World, s.Player, 3, Fire);
            Assert.That(s.Player.QuickSlots[0].IsEmpty, Is.True, "다른 칸으로 옮겨짐");
            Assert.That(s.Player.QuickSlots[3], Is.EqualTo(Fire));
            Inventory.AssignQuickSlot(s.World, s.Player, 3, Ice);
            Assert.That(s.Player.QuickSlots[3], Is.EqualTo(Ice), "덮어씀");
            Assert.Throws<ArgumentException>(() => Inventory.AssignQuickSlot(s.World, s.Player, 1, Device), "인벤토리에 없음");
            Assert.Throws<ArgumentOutOfRangeException>(() => Inventory.AssignQuickSlot(s.World, s.Player, 6, Fire));
            s.World.Events.Flush();
            Assert.That(s.QuickEvents.Count, Is.EqualTo(2));
            Assert.That(s.QuickEvents[1].Slots[3], Is.EqualTo(Ice));
        }

        [Test]
        public void RemoveAt_ClearsQuickSlot_UnlessAnotherCopyRemains()
        {
            var s = Start();
            s.Add(Device); // 칸 1
            s.Add(Device); // 칸 2
            Inventory.AssignQuickSlot(s.World, s.Player, 2, Device);
            Inventory.RemoveAt(s.World, s.Player, 1);
            Assert.That(s.Player.QuickSlots[2], Is.EqualTo(Device), "같은 아이템이 남아 있음");
            Inventory.RemoveAt(s.World, s.Player, 2);
            Assert.That(s.Player.QuickSlots[2].IsEmpty, Is.True);
            Inventory.RemoveAt(s.World, s.Player, 0);
            Assert.That(s.Player.QuickSlots[0].IsEmpty, Is.True, "불 주문 칸도 비워짐");
            Assert.That(s.Player.HasSpell("fire_basic"), Is.False);
        }

        [Test]
        public void Snapshot_RoundTripsInventoryAndQuickSlots()
        {
            var s = Start();
            s.Add(Ice);
            s.Add(Device);
            Inventory.AssignQuickSlot(s.World, s.Player, 5, Device);
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => null);
            var p = restored.Units.Get(s.Player.Id);
            Assert.That(p.InventorySlots, Is.EqualTo(s.Player.InventorySlots));
            Assert.That(p.QuickSlots, Is.EqualTo(s.Player.QuickSlots));
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(11), "3-4b: 한 판 기록 · 3-5: 바닥 아이템");
        }

        // 결정 2026-10-17: 빠른 칸에 없는 주문도 인벤토리에서 바로 쓴다. 빠른 칸에 아이템이 있으면 주문 시전은 막힘
        [Test]
        public void Cast_FromInventory_And_QuickSlotItemIsNotASpell()
        {
            var s = Start();
            var enemyDef = new UnitDef("enemy_melee", 8, 2, 6);
            var enemy = UnitSpawner.SpawnEnemy(s.World, enemyDef, new GridPos(4, 1), new WaitAi());
            s.Add(Ice);
            s.Add(Device);
            Inventory.AssignQuickSlot(s.World, s.Player, 1, Device);
            var targeting = new BresenhamTargetingRule();
            var sight = new ShadowcastSightRule();

            Assert.That(SpellCasting.TryCreateCastSpell(s.World, Config, s.Book, targeting, sight, "ice_basic", enemy.Pos, out var cast), Is.EqualTo(CastBlock.None));
            Assert.That(cast, Is.Not.Null);
            Assert.That(SpellCasting.TryCreateCastSpell(s.World, Config, s.Book, targeting, sight, "poison_basic", enemy.Pos, out _), Is.EqualTo(CastBlock.NotOwned));
            Assert.That(SpellCasting.TryCreateCast(s.World, Config, s.Book, targeting, sight, 1, enemy.Pos, out _), Is.EqualTo(CastBlock.NotASpell));
            Assert.That(SpellCasting.TryCreateCast(s.World, Config, s.Book, targeting, sight, 2, enemy.Pos, out _), Is.EqualTo(CastBlock.EmptySlot));
        }

        private sealed class WaitAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        // ── 인벤토리 창 (InventoryMenu) ──

        [Test]
        public void Menu_GridMove_Clamped()
        {
            var s = Start();
            var menu = new InventoryMenu();
            menu.Open();
            menu.Move(1, 0, 10);
            menu.Move(0, -1, 10); // 아래 줄
            Assert.That(menu.Selected, Is.EqualTo(InventoryMenu.Columns + 1));
            menu.Move(0, -1, 10);
            Assert.That(menu.Selected, Is.EqualTo(InventoryMenu.Columns + 1), "2줄 밖으로 안 나감");
            menu.Move(-9, 1, 10);
            Assert.That(menu.Selected, Is.EqualTo(0));
        }

        [Test]
        public void Menu_UseSpell_ClosesAndRequestsCast()
        {
            var s = Start();
            var menu = new InventoryMenu();
            menu.Open();
            Assert.That(menu.Confirm(s.World, s.Player).Kind, Is.EqualTo(InventoryActionKind.None));
            Assert.That(menu.State, Is.EqualTo(InventoryMenuState.Menu));
            Assert.That(menu.Options, Is.EqualTo(new[] { InventoryMenuOption.Use, InventoryMenuOption.Assign, InventoryMenuOption.Unassign, InventoryMenuOption.Close }),
                "불은 빠른 칸 1에 등록돼 있어 '등록 해제'가 보임");
            var action = menu.Confirm(s.World, s.Player); // 사용
            Assert.That(action.Kind, Is.EqualTo(InventoryActionKind.CastSpell));
            Assert.That(action.Entry, Is.EqualTo(Fire));
            Assert.That(menu.IsOpen, Is.False);
        }

        [Test]
        public void Menu_AssignThenPressSlot_Registers()
        {
            var s = Start();
            s.Add(Device);
            var menu = new InventoryMenu();
            menu.Open();
            menu.Move(1, 0, 10); // 칸 1 = 융합기
            menu.Confirm(s.World, s.Player);
            Assert.That(menu.Options, Is.EqualTo(new[] { InventoryMenuOption.Use, InventoryMenuOption.Assign, InventoryMenuOption.Close }));
            menu.Move(0, -1, 10); // 등록하기
            menu.Confirm(s.World, s.Player);
            Assert.That(menu.State, Is.EqualTo(InventoryMenuState.ChooseSlot));
            var action = menu.PressQuickSlot(s.World, s.Player, 4);
            Assert.That(action.Kind, Is.EqualTo(InventoryActionKind.Assigned));
            Assert.That(s.Player.QuickSlots[4], Is.EqualTo(Device));
            Assert.That(menu.State, Is.EqualTo(InventoryMenuState.Browse), "창은 열린 채");

            // 숫자 키는 칸 고르기에서도 바로 등록
            menu.Move(-1, 0, 10);
            menu.PressQuickSlot(s.World, s.Player, 2);
            Assert.That(s.Player.QuickSlots[2], Is.EqualTo(Fire));
            Assert.That(s.Player.QuickSlots[0].IsEmpty, Is.True, "원래 칸 1은 비워짐");
        }

        // 빈 빠른 칸을 누르면 인벤토리가 열리고, 고른 항목이 그 칸에 바로 등록되고 닫힌다
        [Test]
        public void Menu_OpenForAssign_ConfirmRegistersAndCloses()
        {
            var s = Start();
            s.Add(Ice);
            var menu = new InventoryMenu();
            menu.OpenForAssign(3);
            Assert.That(menu.AssignTarget, Is.EqualTo(3));
            menu.Select(5, 10);
            Assert.That(menu.Confirm(s.World, s.Player).Kind, Is.EqualTo(InventoryActionKind.None), "빈 칸은 아무 일 없음");
            menu.Select(1, 10);
            var action = menu.Confirm(s.World, s.Player);
            Assert.That(action.Kind, Is.EqualTo(InventoryActionKind.Assigned));
            Assert.That(action.Slot, Is.EqualTo(3));
            Assert.That(s.Player.QuickSlots[3], Is.EqualTo(Ice));
            Assert.That(menu.IsOpen, Is.False);
        }

        [Test]
        public void Menu_UseItem_And_CancelStepsBack()
        {
            var s = Start();
            s.Add(Device);
            var menu = new InventoryMenu();
            menu.Open();
            menu.Select(1, 10);
            menu.Confirm(s.World, s.Player);
            menu.Cancel();
            Assert.That(menu.State, Is.EqualTo(InventoryMenuState.Browse), "메뉴 → 칸 고르기");
            menu.Confirm(s.World, s.Player);
            Assert.That(menu.Confirm(s.World, s.Player).Kind, Is.EqualTo(InventoryActionKind.UseItem));
            menu.Open();
            menu.Cancel();
            Assert.That(menu.IsOpen, Is.False, "칸 고르기 → 닫기");
        }

        // 검산: ItemCatalog 에셋이 BALANCE.md 9번과 같은지
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset");
            Assert.That(asset, Is.Not.Null, "ItemCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var book = asset.ToBook();
            Assert.That(book.Count, Is.EqualTo(6), "융합기 + 마법서 5 (3-5)");
            Assert.That(book.TryGet("fusion_device", out var device), Is.True);
            Assert.That(device.Use, Is.EqualTo(ItemUse.Fusion));
            Assert.That(asset.DisplayNames()["fusion_device"], Is.EqualTo("마법 융합기"));
        }
    }
}
