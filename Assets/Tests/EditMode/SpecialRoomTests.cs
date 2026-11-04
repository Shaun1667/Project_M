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
    /// <summary>3-15b 특수 방 · 열쇠 (SPD — 결정 2026-11-04): 벽 속에 방 하나 · 입구 한 칸 · 보물 방은 잠긴 문 + 같은 층 열쇠 (BALANCE.md 16번).</summary>
    public class SpecialRoomTests
    {
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ItemBook Items = new ItemBook(new[] { new ItemDef("key_iron", ItemUse.Key), new ItemDef("scroll_upgrade", ItemUse.Scroll, amount: 1, effect: ConsumableEffect.Upgrade) });

        private static DungeonGenConfig GenConfig() => new DungeonGenConfig(width: 50, height: 50, roomCount: 10, roomSizeRoll: 20, roomMinSize: 7,
            wallBlockCount: 20, wallBlockSizeRoll: 10, wallBlockMinSize: 3, wallTunnelCostMultiplier: 5, placementAttemptLimit: 1000, deadEndLoopMinWalkDistance: 10);

        private static SpecialRoomConfig Only(SpecialRoomType type)
        {
            var w = new int[4];
            w[(int)type - 1] = 1;
            return new SpecialRoomConfig(100, w, 3, 5, new[] { "scroll_upgrade" }, 1, new[] { "a" }, 1, new[] { "b" }, 1, new[] { "c" }, 1, "key_iron");
        }

        private static (World World, DungeonFloor Floor, SpecialRooms.Result Room) Carve(ulong seed, SpecialRoomType type)
        {
            DungeonFloor floor = null;
            var world = World.Create(seed, rng => (floor = new DungeonGenerator(GenConfig()).Generate(rng)).Map);
            var room = SpecialRooms.Carve(world, floor, Only(type), world.Rng.Get(RngStream.Decor));
            return (world, floor, room);
        }

        // 방은 입구 한 칸으로만 이어진다 (둘레는 모두 벽)
        [Test]
        public void Room_HasSingleEntrance()
        {
            for (ulong seed = 1; seed <= 15; seed++)
            {
                var (world, _, room) = Carve(seed, SpecialRoomType.Armory);
                if (room == null) continue;
                var cells = new System.Collections.Generic.HashSet<GridPos>(room.Cells);
                foreach (var c in room.Cells)
                    foreach (var d in GridMovement.Directions)
                    {
                        var n = c + d;
                        if (cells.Contains(n) || n == room.Entrance) continue;
                        Assert.That(world.Map.IsWalkable(n), Is.False, $"시드 {seed} {n}: 방 둘레는 벽");
                    }
                Assert.That(world.Map.Get(room.Entrance), Is.EqualTo(TileType.Door));
            }
        }

        // 보물 방: 잠긴 문 · 안에 아이템 · 방 밖에 열쇠
        [Test]
        public void Vault_LockedWithKeyOutside()
        {
            int vaults = 0;
            for (ulong seed = 1; seed <= 15; seed++)
            {
                var (world, _, room) = Carve(seed, SpecialRoomType.Vault);
                if (room == null) continue;
                vaults++;
                Assert.That(world.Map.Get(room.Entrance), Is.EqualTo(TileType.LockedDoor));
                Assert.That(world.Map.IsWalkable(room.Entrance), Is.False);
                bool itemInside = false, keyOutside = false;
                var cells = new System.Collections.Generic.HashSet<GridPos>(room.Cells);
                foreach (var it in world.FloorItems.All)
                {
                    if (it.ItemId == "scroll_upgrade" && cells.Contains(it.Pos)) itemInside = true;
                    if (it.ItemId == "key_iron" && !cells.Contains(it.Pos)) keyOutside = true;
                }
                Assert.That(itemInside && keyOutside, Is.True, $"시드 {seed}");
            }
            Assert.That(vaults, Is.GreaterThan(0));
        }

        [Test]
        public void Garden_FilledWithTallGrass()
        {
            for (ulong seed = 1; seed <= 15; seed++)
            {
                var (world, _, room) = Carve(seed, SpecialRoomType.Garden);
                if (room == null) continue;
                foreach (var c in room.Cells) Assert.That(world.Map.Get(c), Is.EqualTo(TileType.TallGrass));
                return;
            }
            Assert.Fail("정원이 하나도 안 생김");
        }

        // 열쇠를 가지고 부딪히면(UnlockCommand) 문이 열리고 열쇠가 하나 사라진다. 없으면 그대로
        [Test]
        public void Unlock_UsesKey()
        {
            var map = GridMovementTests.Parse("#######", "#..#..#", "#######");
            map.Set(new GridPos(3, 1), TileType.LockedDoor);
            var world = new World(map, 3UL);
            world.InstallCoreSystems(new ShadowcastSightRule(), Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 20, 3, 8, 10), new GridPos(2, 1), Magic.QuickSlotCount);
            var clock = new TurnClock(world.Scheduler, world.PlayerInput);
            clock.SubmitPlayerCommand(new UnlockCommand(world, Items, new GridPos(3, 1), 1));
            Assert.That(world.Map.Get(new GridPos(3, 1)), Is.EqualTo(TileType.LockedDoor), "열쇠 없음");

            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("key_iron"));
            clock.SubmitPlayerCommand(new UnlockCommand(world, Items, new GridPos(3, 1), 1));
            Assert.That(world.Map.Get(new GridPos(3, 1)), Is.EqualTo(TileType.Door));
            Assert.That(UnlockCommand.KeySlot(Items, player), Is.EqualTo(-1), "열쇠를 씀");
        }

        // 검산: FloorDecorConfig 특수 방 값 = BALANCE.md 16번, 목록의 아이템은 ItemCatalog에 있다
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var c = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToConfig().SpecialRooms;
            Assert.That((c.RoomPercent, c.MinSize, c.MaxSize, c.VaultItemCount, c.ArmoryItemCount, c.LibraryItemCount, c.GardenItemCount, c.KeyItem),
                Is.EqualTo((70, 3, 5, 2, 2, 3, 2, "key_iron")));
            Assert.That(c.Weights, Is.EqualTo(new[] { 3, 2, 2, 2 }));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            foreach (var list in new[] { c.VaultItems, c.ArmoryItems, c.LibraryItems, c.GardenItems })
                foreach (var id in list) Assert.That(items.TryGet(id, out _), Is.True, id);
            Assert.That(items.TryGet("key_iron", out var key) && key.Use == ItemUse.Key, Is.True);
        }
    }
}
