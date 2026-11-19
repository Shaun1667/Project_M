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
    /// 3-52 수정 방 (SPD Crystal Choice — 결정 2026-11-18): 앞쪽 빈 칸 · 안쪽 작은 방 둘(투명 수정 문), 앞쪽 가운데에 수정 열쇠 하나 —
    /// 한 방 = 보물 방 목록 1개, 다른 방 = 금화(보물 창고 더미 수만큼 굴린 합). 수정 열쇠는 수정 문만, 쇠 열쇠는 잠긴 문만 연다 (BALANCE.md 16번).
    /// </summary>
    public class CrystalTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ItemBook Items = new ItemBook(new[] { new ItemDef("key_iron", ItemUse.Key), new ItemDef("key_crystal", ItemUse.CrystalKey) });

        // 수정 문 칸: 걸을 수 없음 · 시야는 통과(안이 보임) · 주문 경로는 막음
        [Test]
        public void CrystalDoorTile_Rules()
        {
            var map = GridMovementTests.Parse("#####", "#...#", "#####");
            map.Set(new GridPos(2, 1), TileType.CrystalDoor);
            Assert.That(map.IsWalkable(new GridPos(2, 1)), Is.False);
            Assert.That(map.BlocksSight(new GridPos(2, 1)), Is.False);
            Assert.That(map.BlocksProjectile(new GridPos(2, 1)), Is.True);
        }

        // 수정 열쇠로만 수정 문이 열리고(바닥이 됨 · 열쇠 하나 씀), 쇠 열쇠로는 안 열린다. 수정 열쇠로 잠긴 문은 못 엶
        [Test]
        public void Unlock_KeyMatchesDoor()
        {
            var map = GridMovementTests.Parse("#######", "#..#..#", "#######");
            map.Set(new GridPos(3, 1), TileType.CrystalDoor);
            var world = new World(map, 3UL);
            world.InstallCoreSystems(new ShadowcastSightRule(), Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 20, 3, 8, 10), new GridPos(2, 1), Magic.QuickSlotCount);
            var clock = new TurnClock(world.Scheduler, world.PlayerInput);

            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("key_iron"));
            clock.SubmitPlayerCommand(new UnlockCommand(world, Items, new GridPos(3, 1), 1));
            Assert.That(world.Map.Get(new GridPos(3, 1)), Is.EqualTo(TileType.CrystalDoor), "쇠 열쇠로는 안 열림");

            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("key_crystal"));
            clock.SubmitPlayerCommand(new UnlockCommand(world, Items, new GridPos(3, 1), 1));
            Assert.That(world.Map.Get(new GridPos(3, 1)), Is.EqualTo(TileType.Floor), "수정 문은 사라짐");
            Assert.That(UnlockCommand.KeySlot(Items, player, ItemUse.CrystalKey), Is.EqualTo(-1), "수정 열쇠를 씀");
            Assert.That(UnlockCommand.KeySlot(Items, player, ItemUse.Key), Is.GreaterThanOrEqualTo(0), "쇠 열쇠는 남음");

            world.SetTile(new GridPos(3, 1), TileType.LockedDoor);
            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("key_crystal"));
            Assert.That(UnlockCommand.KeyFor(TileType.LockedDoor), Is.EqualTo(ItemUse.Key));
            Assert.That(UnlockCommand.KeyFor(TileType.CrystalDoor), Is.EqualTo(ItemUse.CrystalKey));
            Assert.That(UnlockCommand.KeyFor(TileType.Door), Is.EqualTo(ItemUse.None));
        }

        // 수정 방: 입구 = 문, 맨 안쪽 앞 줄 = 벽 + 수정 문 2개, 맨 안쪽 줄 가운데 = 벽(두 방 나눔), 앞쪽 가운데 = 수정 열쇠, 한 방 = 보물 1개 · 다른 방 = 금화
        [Test]
        public void Room_TwoChambersOneKey()
        {
            int carved = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var (world, _, room) = SpecialRoomTests.CarveFor(seed, SpecialRoomType.Crystal);
                if (room == null) continue;
                carved++;
                int size = (int)System.Math.Round(System.Math.Sqrt(room.Cells.Count));
                int mid = size / 2;
                Assert.That(world.Map.Get(room.Entrance), Is.EqualTo(TileType.Door), $"시드 {seed}");
                int doors = 0;
                for (int b = 0; b < size; b++)
                {
                    var t = world.Map.Get(room.Cells[(size - 2) * size + b]);
                    Assert.That(t == TileType.CrystalDoor || t == TileType.Wall, Is.True, $"시드 {seed}");
                    if (t == TileType.CrystalDoor) doors++;
                }
                Assert.That(doors, Is.EqualTo(2), $"시드 {seed}");
                Assert.That(world.Map.Get(room.Cells[(size - 1) * size + mid]), Is.EqualTo(TileType.Wall), $"시드 {seed}: 두 방 사이");
                var key = world.FloorItems.ItemsAt(room.Cells[mid]);
                Assert.That(key.Count == 1 && key[0].ItemId == "key_crystal", Is.True, $"시드 {seed}: 열쇠");
                int prize = 0, gold = 0;
                for (int b = 0; b < size; b++)
                    foreach (var item in world.FloorItems.ItemsAt(room.Cells[(size - 1) * size + b]))
                    {
                        if (item.ItemId == Loot.GoldItemId) { gold++; Assert.That(item.Quantity, Is.GreaterThan(0)); }
                        else prize++;
                    }
                Assert.That((prize, gold), Is.EqualTo((1, 1)), $"시드 {seed}");
            }
            Assert.That(carved, Is.GreaterThan(0));
        }

        // 검산: FloorDecorConfig 수정 방 값 = BALANCE.md 16번, 수정 열쇠 아이템 줄
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var c = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToConfig().SpecialRooms;
            Assert.That(c.CrystalKeyItem, Is.EqualTo("key_crystal"));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            Assert.That(items.TryGet("key_crystal", out var key) && key.Use == ItemUse.CrystalKey, Is.True);
        }
    }
}
