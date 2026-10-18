using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// 3-5 마법서 (결정 2026-10-19): 한 판에 속성별 한 권씩 1~4층(보스 층 제외)에 층당 1~2권 · 1층 최소 1권,
    /// 걸어 들어가면 자동으로 줍기, 쓰면 책이 사라지고 주문을 배워 빈 빠른 칸에 등록. 같은 시드 = 같은 배치, 스냅샷 v11 · 떠난 층 보존.
    /// </summary>
    public class SpellbookTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 8, 2, 6);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly LootConfig Loot1 = new LootConfig(2, 3);

        private static readonly string[] BookIds = { "spellbook_fire", "spellbook_ice", "spellbook_lightning", "spellbook_poison", "spellbook_water" };

        private static ItemBook Books() => new ItemBook(new[]
        {
            new ItemDef("fusion_device", ItemUse.Fusion),
            new ItemDef("spellbook_fire", ItemUse.LearnSpell, "fire_basic"),
            new ItemDef("spellbook_ice", ItemUse.LearnSpell, "ice_basic"),
            new ItemDef("spellbook_lightning", ItemUse.LearnSpell, "lightning_basic"),
            new ItemDef("spellbook_poison", ItemUse.LearnSpell, "poison_basic"),
            new ItemDef("spellbook_water", ItemUse.LearnSpell, "water_basic"),
        });

        private static DungeonGenConfig Gen(int floorCount) => new DungeonGenConfig(50, 50, 10, 20, 7, 20, 10, 3, 5, 1000, 10, floorCount);

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static readonly ICommandSource Idle = new IdleAi();

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;
            public DungeonFloor Floor1;
            public FloorTransition Stairs;
            public ItemBook Items;
            public SpellBook Spells;
            public readonly List<ISimEvent> Events = new List<ISimEvent>();
        }

        private static Session Start(ulong seed, int floorCount = 5)
        {
            var spawn = new SpawnConfig(0, 8); // 적 없이 — 줍기 · 이동만 본다
            var generator = new DungeonGenerator(Gen(floorCount));
            DungeonFloor floor = null;
            var world = World.Create(seed, rng =>
            {
                floor = generator.Generate(rng, stairsUp: false, stairsDown: floorCount > 1);
                return floor.Map;
            });
            var vision = world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var s = new Session
            {
                World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput), Floor1 = floor, Items = Books(), Spells = SpellTests.BalanceBookFull(),
            };
            world.Scheduler.AddSystem(TickPhase.ManaRegen, new ItemPickupSystem(world, s.Items));
            s.Stairs = new FloorTransition(world, generator, floorCount, EnemyDef, spawn, (w, id) => Idle, vision, null, Loot1);
            world.Events.Subscribe<FloorItemPlacedEvent>(s.Events.Add);
            world.Events.Subscribe<ItemPickedUpEvent>(s.Events.Add);
            world.Events.Subscribe<ItemPickupFailedEvent>(s.Events.Add);
            world.Events.Subscribe<SpellLearnedEvent>(s.Events.Add);
            s.Player = FloorPopulator.Populate(world, floor.Start, PlayerDef, EnemyDef, spawn, Idle, Magic.QuickSlotCount, Magic.InventorySlotCount);
            Loot.PlanSpellbooks(world, s.Items, Loot1, floorCount);
            Loot.PlaceFloorItems(world, floor.Start, Loot1);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static void Wait(Session s, int turns = 1)
        {
            for (int i = 0; i < turns; i++) s.Clock.SubmitPlayerCommand(PlayerActions.Wait(Config));
            s.World.Events.Flush();
        }

        /// <summary>플레이어를 아이템 칸 옆에 두고 한 걸음 들어간다.</summary>
        private static void WalkOnto(Session s, GridPos target)
        {
            var map = s.World.Map;
            foreach (var dir in GridMovement.Directions)
            {
                var from = target + dir;
                if (!map.IsWalkable(from) || s.World.Units.IsOccupied(from) || !GridMovement.CanStep(map, from, target)) continue;
                s.World.Units.Teleport(s.Player.Id, from);
                Assert.That(PlayerActions.TryStepTo(s.World, Config, s.Player, target, out var command), Is.True);
                s.Clock.SubmitPlayerCommand(command);
                s.World.Events.Flush();
                return;
            }
            Assert.Fail("아이템 옆 빈 칸이 없다");
        }

        // ── 배치 계획 ──

        [Test]
        public void Plan_FiveBooks_OnePerSpell_FloorsOneToFour_OneOrTwoEach()
        {
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var s = Start(seed);
                var all = new List<PendingFloorItem>(s.World.Run.PendingFloorItems);
                foreach (var item in s.World.FloorItems.All) all.Add(new PendingFloorItem(1, item.ItemId));
                Assert.That(all.Select(p => p.ItemId).OrderBy(x => x), Is.EqualTo(BookIds.OrderBy(x => x)), $"시드 {seed}: 속성별 한 권씩");
                var perFloor = all.GroupBy(p => p.Floor).ToDictionary(g => g.Key, g => g.Count());
                Assert.That(perFloor.Keys.All(f => f >= 1 && f <= 4), Is.True, $"시드 {seed}: 보스 층(5)에는 없음");
                Assert.That(perFloor.Values.All(c => c >= 1 && c <= 2), Is.True, $"시드 {seed}: 층당 1~2권");
                Assert.That(s.World.FloorItems.Count, Is.GreaterThanOrEqualTo(1), $"시드 {seed}: 1층 최소 1권 (빈손 시작 보장)");
                Assert.That(perFloor.Count, Is.EqualTo(4), "4개 층 모두 1권 이상");
            }
        }

        [Test]
        public void SameSeed_SamePlanAndPositions_DifferentSeedDiffers()
        {
            var a = Start(42);
            var b = Start(42);
            Assert.That(b.World.Run.PendingFloorItems, Is.EqualTo(a.World.Run.PendingFloorItems));
            Assert.That(b.World.FloorItems.All.Select(i => (i.Pos, i.ItemId)), Is.EqualTo(a.World.FloorItems.All.Select(i => (i.Pos, i.ItemId))));
            Assert.That(b.World.Map.CopyCells(), Is.EqualTo(a.World.Map.CopyCells()), "Loot 난수는 맵을 바꾸지 않는다");

            bool anyDifferent = false;
            for (ulong seed = 43; seed < 53 && !anyDifferent; seed++)
            {
                var c = Start(seed);
                anyDifferent = !c.World.FloorItems.All.Select(i => (i.Pos, i.ItemId)).SequenceEqual(a.World.FloorItems.All.Select(i => (i.Pos, i.ItemId)));
            }
            Assert.That(anyDifferent, Is.True);
        }

        [Test]
        public void Placement_OnFloorTiles_AwayFromStart_PublishesEvents()
        {
            var s = Start(7);
            foreach (var item in s.World.FloorItems.All)
            {
                Assert.That(s.World.Map.Get(item.Pos), Is.EqualTo(TileType.Floor), "계단 · 벽이 아닌 바닥");
                Assert.That(GridMovement.Chebyshev(item.Pos, s.Floor1.Start), Is.GreaterThanOrEqualTo(3), "BALANCE 11번 시작 칸에서 3칸 이상");
            }
            Assert.That(s.Events.OfType<FloorItemPlacedEvent>().Count(), Is.EqualTo(s.World.FloorItems.Count));
        }

        // ── 줍기 ──

        [Test]
        public void WalkingOntoBook_PicksItUp_NoExtraTick()
        {
            var s = Start(9);
            var book = s.World.FloorItems.All[0];
            WalkOnto(s, book.Pos);
            Assert.That(s.World.FloorItems.Any(book.Pos), Is.False, "바닥에서 사라짐");
            Assert.That(s.Player.InventorySlots, Does.Contain(InventoryRef.Item(book.ItemId)));
            Assert.That(s.Events.OfType<ItemPickedUpEvent>().Single().ItemId, Is.EqualTo(book.ItemId));
            Assert.That(s.World.Scheduler.CurrentTick, Is.EqualTo(1), "한 걸음 = 1틱 (줍기는 차례를 더 쓰지 않음)");
        }

        [Test]
        public void FullInventory_LeavesBookOnFloor_AndReportsFailure()
        {
            var s = Start(9);
            for (int i = 0; i < s.Player.InventorySlots.Length; i++) s.Player.InventorySlots[i] = InventoryRef.Item("fusion_device");
            var book = s.World.FloorItems.All[0];
            WalkOnto(s, book.Pos);
            Assert.That(s.World.FloorItems.Any(book.Pos), Is.True);
            Assert.That(s.Events.OfType<ItemPickupFailedEvent>().Count(), Is.EqualTo(1));
            Wait(s); // 서 있는 동안 틱마다 다시 시도 (HUD가 한 번만 알림)
            Assert.That(s.Events.OfType<ItemPickupFailedEvent>().Count(), Is.EqualTo(2));
            s.Player.InventorySlots[0] = InventoryRef.Empty;
            Wait(s);
            Assert.That(s.World.FloorItems.Any(book.Pos), Is.False, "칸이 비면 그 자리에서 줍는다");
        }

        // ── 사용 = 배우기 ──

        [Test]
        public void UsingBook_RemovesIt_AddsSpell_AutoAssignsFirstEmptyQuickSlot()
        {
            var s = Start(3);
            Inventory.TryAdd(s.World, s.Spells, s.Items, s.Player, InventoryRef.Item("fusion_device"));
            Inventory.AssignQuickSlot(s.World, s.Player, 0, InventoryRef.Item("fusion_device"));
            int index = Inventory.TryAdd(s.World, s.Spells, s.Items, s.Player, InventoryRef.Item("spellbook_fire"));
            long tick = s.World.Scheduler.CurrentTick;

            Assert.That(Spellbooks.TryLearn(s.World, s.Spells, s.Items, s.Player, index), Is.EqualTo(LearnResult.Learned));
            s.World.Events.Flush();
            Assert.That(s.Player.InventorySlots, Has.No.Member(InventoryRef.Item("spellbook_fire")), "책 소모");
            Assert.That(s.Player.InventorySlots, Does.Contain(InventoryRef.Spell("fire_basic")));
            Assert.That(s.Player.QuickSlots[1], Is.EqualTo(InventoryRef.Spell("fire_basic")), "첫 빈 빠른 칸(0은 융합기)");
            var learned = s.Events.OfType<SpellLearnedEvent>().Single();
            Assert.That(learned.SpellId, Is.EqualTo("fire_basic"));
            Assert.That(learned.QuickSlot, Is.EqualTo(1));
            Assert.That(s.World.Scheduler.CurrentTick, Is.EqualTo(tick), "차례를 쓰지 않음");
        }

        [Test]
        public void UsingBook_FullQuickSlots_StillLearns_NoAssign()
        {
            var s = Start(3);
            for (int i = 0; i < s.Player.QuickSlots.Length; i++) s.Player.QuickSlots[i] = InventoryRef.Item("fusion_device");
            int index = Inventory.TryAdd(s.World, s.Spells, s.Items, s.Player, InventoryRef.Item("spellbook_ice"));
            Assert.That(Spellbooks.TryLearn(s.World, s.Spells, s.Items, s.Player, index), Is.EqualTo(LearnResult.Learned));
            s.World.Events.Flush();
            Assert.That(s.Player.InventorySlots, Does.Contain(InventoryRef.Spell("ice_basic")));
            Assert.That(s.Events.OfType<SpellLearnedEvent>().Single().QuickSlot, Is.EqualTo(-1));
        }

        [Test]
        public void AlreadyKnown_KeepsBook_NotABook_Refused()
        {
            var s = Start(3);
            Inventory.TryAdd(s.World, s.Spells, s.Items, s.Player, InventoryRef.Spell("fire_basic"));
            int index = Inventory.TryAdd(s.World, s.Spells, s.Items, s.Player, InventoryRef.Item("spellbook_fire"));
            Assert.That(Spellbooks.TryLearn(s.World, s.Spells, s.Items, s.Player, index), Is.EqualTo(LearnResult.AlreadyKnown));
            Assert.That(s.Player.InventorySlots[index], Is.EqualTo(InventoryRef.Item("spellbook_fire")), "책은 그대로");

            int device = Inventory.TryAdd(s.World, s.Spells, s.Items, s.Player, InventoryRef.Item("fusion_device"));
            Assert.That(Spellbooks.TryLearn(s.World, s.Spells, s.Items, s.Player, device), Is.EqualTo(LearnResult.NotASpellbook));
            Assert.That(Spellbooks.TryLearn(s.World, s.Spells, s.Items, s.Player, 19), Is.EqualTo(LearnResult.NotASpellbook), "빈 칸");
        }

        [Test]
        public void ItemDef_LearnSpellNeedsSpellId()
        {
            Assert.Throws<System.ArgumentException>(() => new ItemDef("spellbook_x", ItemUse.LearnSpell));
            Assert.That(new ItemDef("fusion_device", ItemUse.Fusion, "fire_basic").SpellId, Is.Null, "마법서가 아니면 주문 없음");
        }

        // ── 층 · 저장 ──

        [Test]
        public void NewFloor_GetsItsBooks_LeftFloorKeepsUnpickedBooks()
        {
            var s = Start(21);
            var floor1Items = s.World.FloorItems.All.Select(i => (i.Pos, i.ItemId)).ToList();
            var down = s.World.Map.Find(TileType.StairsDown).Value;
            s.World.Units.Teleport(s.Player.Id, down);
            Assert.That(s.Stairs.TryUse(), Is.True);
            s.World.Events.Flush();
            Assert.That(s.World.Floor, Is.EqualTo(2));
            int floor2Planned = s.World.FloorItems.Count;
            Assert.That(floor2Planned, Is.InRange(1, 2), "2층 몫이 놓임");
            Assert.That(s.World.Run.PendingFloorItems.Any(p => p.Floor == 2), Is.False, "계획에서 빠짐");

            var up = s.World.Map.Find(TileType.StairsUp).Value;
            s.World.Units.Teleport(s.Player.Id, up);
            Assert.That(s.Stairs.TryUse(), Is.True);
            s.World.Events.Flush();
            Assert.That(s.World.Floor, Is.EqualTo(1));
            Assert.That(s.World.FloorItems.All.Select(i => (i.Pos, i.ItemId)), Is.EqualTo(floor1Items), "떠난 층 책은 그대로");
            Assert.That(s.Events.OfType<FloorItemPlacedEvent>().Count(), Is.EqualTo(floor1Items.Count * 2 + floor2Planned), "돌아오면 다시 그리기용으로 한 번 더 알림");
        }

        [Test]
        public void Snapshot_RoundTrip_KeepsFloorItemsAndPlan_ByteIdentical()
        {
            var s = Start(5);
            var down = s.World.Map.Find(TileType.StairsDown).Value;
            s.World.Units.Teleport(s.Player.Id, down);
            s.Stairs.TryUse();
            s.World.Events.Flush();

            var snapshot = s.World.Capture();
            Assert.That(snapshot.FloorItems.Length, Is.EqualTo(s.World.FloorItems.Count));
            Assert.That(snapshot.StoredFloors.Single().FloorItems.Length, Is.GreaterThanOrEqualTo(1), "1층 책은 떠난 층에");
            Assert.That(snapshot.PendingFloorItems.Length, Is.EqualTo(s.World.Run.PendingFloorItems.Count));
            var bytes = WorldSnapshotCodec.Encode(snapshot);
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => Idle);
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
            Assert.That(restored.FloorItems.All.Select(i => (i.Pos, i.ItemId)), Is.EqualTo(s.World.FloorItems.All.Select(i => (i.Pos, i.ItemId))));
        }

        // ── 데이터 (BALANCE 9 · 11번) ──

        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var loot = AssetDatabase.LoadAssetAtPath<LootConfigAsset>("Assets/_Project/Data/Items/LootConfig.asset");
            Assert.That(loot, Is.Not.Null, "LootConfig.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var config = loot.ToConfig();
            Assert.That(config.MaxBooksPerFloor, Is.EqualTo(2));
            Assert.That(config.MinDistanceFromStart, Is.EqualTo(3));

            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            var expected = new Dictionary<string, string>
            {
                { "spellbook_fire", "fire_basic" }, { "spellbook_ice", "ice_basic" }, { "spellbook_lightning", "lightning_basic" },
                { "spellbook_poison", "poison_basic" }, { "spellbook_water", "water_basic" },
            };
            foreach (var pair in expected)
            {
                Assert.That(items.TryGet(pair.Key, out var def), Is.True, pair.Key);
                Assert.That(def.Use, Is.EqualTo(ItemUse.LearnSpell));
                Assert.That(def.SpellId, Is.EqualTo(pair.Value));
            }
        }
    }
}
