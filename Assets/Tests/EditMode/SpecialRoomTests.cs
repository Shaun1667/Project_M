using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
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
            var w = new int[7];
            w[(int)type - 1] = 1;
            return new SpecialRoomConfig(100, w, 3, 5, new[] { "scroll_upgrade" }, 1, new[] { "a" }, 1, new[] { "b" }, 1, new[] { "c" }, 1, "key_iron", new[] { "seed_x" }, 2,
                new[] { "potion_x", "scroll_y" }, 3, 4, "potion_liquid_flame", "potion_frost");
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

        // 3-41 창고: 입구 = 바리케이드(걸을 수 없음 · 시야를 가림 · 불에 탐), 안에 소모품 3~4개(겹쳐도 됨), 방 밖에 불꽃 물약 하나
        [Test]
        public void Storage_BarricadeEntrance_ConsumablesInside_FireItemOutside()
        {
            int carved = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var (world, floor, room) = Carve(seed, SpecialRoomType.Storage);
                if (room == null) continue;
                carved++;
                var map = world.Map;
                Assert.That(room.Type, Is.EqualTo(SpecialRoomType.Storage));
                Assert.That(map.Get(room.Entrance), Is.EqualTo(TileType.Barricade), $"시드 {seed}");
                Assert.That(map.IsWalkable(room.Entrance), Is.False);
                Assert.That(map.BlocksSight(room.Entrance), Is.True);
                Assert.That(map.BlocksProjectile(room.Entrance), Is.True, "던진 것은 앞에 떨어진다");
                Assert.That(GridMap.IsFlammableType(TileType.Barricade), Is.True);
                int inside = 0;
                foreach (var c in room.Cells) inside += world.FloorItems.ItemsAt(c).Count;
                Assert.That(inside, Is.InRange(3, 4), $"시드 {seed}");
                var cells = new System.Collections.Generic.HashSet<GridPos>(room.Cells);
                int fireOutside = 0;
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                    {
                        var p = new GridPos(x, y);
                        if (cells.Contains(p)) continue;
                        foreach (var item in world.FloorItems.ItemsAt(p)) if (item.ItemId == "potion_liquid_flame") fireOutside++;
                    }
                Assert.That(fireOutside, Is.EqualTo(1), $"시드 {seed} — 불을 못 써도 열 수 있게");
            }
            Assert.That(carved, Is.GreaterThan(0));
        }

        // 3-41: 바리케이드 옆에 화염 지대가 있으면 번져 타서 바닥이 된다 (불꽃 물약을 앞에 던진 경우)
        [Test]
        public void Barricade_BurnsFromAdjacentFire()
        {
            var map = GridMovementTests.Parse("#######", "#.....#", "#######");
            map.Set(new GridPos(4, 1), TileType.Barricade);
            var world = new World(map, 3UL);
            world.InstallCoreSystems(new ShadowcastSightRule(), Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains(), fireSpread: new FireSpreadConfig(3, 8));
            UnitSpawner.SpawnPlayer(world, new UnitDef("player", 20, 3, 8, 10), new GridPos(1, 1), Magic.QuickSlotCount);
            var clock = new TurnClock(world.Scheduler, world.PlayerInput);
            world.Events.Flush();
            TerrainEffects.Place(world, new GridPos(3, 1), TerrainEffectType.FireZone, 3);
            clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
            world.Events.Flush();
            Assert.That(world.Map.Get(new GridPos(4, 1)), Is.EqualTo(TileType.Floor));
            Assert.That(world.Terrain.Has(new GridPos(4, 1), TerrainEffectType.FireZone), Is.True);
        }

        // 3-41: 바리케이드를 겨눈 화살 주문은 그 칸에 닿는다 (칸 반응 칸 = 바리케이드), 불 × 바리케이드 반응이 있다
        [Test]
        public void Spell_AimedAtBarricade_HitsIt()
        {
            var map = GridMovementTests.Parse("#######", "#.....#", "#######");
            map.Set(new GridPos(4, 1), TileType.Barricade);
            var world = new World(map, 3UL);
            var bolt = new SpellDef("fire_basic", Element.Fire, SpellShape.Bolt, 6, 5, 3);
            var preview = SpellCasting.Preview(world, new BresenhamTargetingRule(), new GridPos(1, 1), bolt, new GridPos(4, 1));
            Assert.That(preview.Path[preview.Path.Count - 1], Is.EqualTo(new GridPos(4, 1)));
            Assert.That(CastSpellCommand.TerrainCells(bolt, preview), Is.EqualTo(new[] { new GridPos(4, 1) }));
            var past = SpellCasting.Preview(world, new BresenhamTargetingRule(), new GridPos(1, 1), bolt, new GridPos(5, 1));
            Assert.That(past.Path, Has.No.Member(new GridPos(4, 1)), "너머를 겨누면 앞에서 멈춘다");
            var reactions = ReactionTests.BalanceReactions(SpellTests.BalanceStatuses());
            Assert.That(reactions.FindTile(TileType.Barricade, Element.Fire), Is.Not.Null);
            Assert.That(reactions.FindTile(TileType.Barricade, Element.Ice), Is.Null, "불로만");
        }

        // 3-42 마법 불 방: 입구 = 문, 방 너비 전체를 막는 영구 화염 지대 벽, 보상 1개는 불 뒤에, 방 밖에 냉기 물약 하나
        [Test]
        public void MagicalFire_FireWallAcrossRoom_PrizeBehind_FrostOutside()
        {
            int carved = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var (world, floor, room) = Carve(seed, SpecialRoomType.MagicalFire);
                if (room == null) continue;
                carved++;
                var map = world.Map;
                Assert.That(room.Type, Is.EqualTo(SpecialRoomType.MagicalFire));
                Assert.That(map.Get(room.Entrance), Is.EqualTo(TileType.Door), $"시드 {seed}");
                int size = (int)System.Math.Round(System.Math.Sqrt(room.Cells.Count));
                int start = size / 2, thickness = size >= 5 ? 2 : 1;
                int prize = 0;
                for (int a = 0; a < size; a++)
                    for (int b = 0; b < size; b++)
                    {
                        var c = room.Cells[a * size + b];
                        bool fire = a >= start && a < start + thickness;
                        Assert.That(world.Terrain.Has(c, TerrainEffectType.FireZone), Is.EqualTo(fire), $"시드 {seed} {c} 깊이 {a}");
                        if (fire) Assert.That(System.Linq.Enumerable.First(world.Terrain.At(c), t => t.Type == TerrainEffectType.FireZone).Remaining, Is.EqualTo(TerrainLayer.Permanent), "꺼지지 않는 불");
                        int n = world.FloorItems.ItemsAt(c).Count;
                        if (n > 0) Assert.That(a, Is.GreaterThanOrEqualTo(start + thickness), $"시드 {seed}: 보상은 불 뒤");
                        prize += n;
                    }
                Assert.That(prize, Is.EqualTo(1), $"시드 {seed}");
                Assert.That(GridMovement.Chebyshev(room.Cells[start * size], room.Entrance), Is.GreaterThanOrEqualTo(2), "불이 문에 닿지 않는다");
                var cells = new System.Collections.Generic.HashSet<GridPos>(room.Cells);
                int frostOutside = 0;
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                    {
                        var p = new GridPos(x, y);
                        if (cells.Contains(p)) continue;
                        foreach (var item in world.FloorItems.ItemsAt(p)) if (item.ItemId == "potion_frost") frostOutside++;
                    }
                Assert.That(frostOutside, Is.EqualTo(1), $"시드 {seed} — 얼음을 못 써도 끌 수 있게");
            }
            Assert.That(carved, Is.GreaterThan(0));
        }

        // 3-42: 꺼지지 않는 불도 물이 닿으면 꺼진다 (FireSpreadSystem 그대로), 저절로는 줄지 않는다
        [Test]
        public void MagicalFire_PermanentFire_OutByWaterOnly()
        {
            var map = GridMovementTests.Parse("#######", "#.....#", "#######");
            var world = new World(map, 3UL);
            world.InstallCoreSystems(new ShadowcastSightRule(), Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains(), fireSpread: new FireSpreadConfig(3, 8));
            UnitSpawner.SpawnPlayer(world, new UnitDef("player", 20, 3, 8, 10), new GridPos(1, 1), Magic.QuickSlotCount);
            var clock = new TurnClock(world.Scheduler, world.PlayerInput);
            world.Events.Flush();
            var fire = new GridPos(4, 1);
            TerrainEffects.Place(world, fire, TerrainEffectType.FireZone, TerrainLayer.Permanent);
            for (int i = 0; i < 10; i++) clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
            Assert.That(world.Terrain.Has(fire, TerrainEffectType.FireZone), Is.True, "10틱 뒤에도 탄다");
            TerrainEffects.Place(world, fire, TerrainEffectType.Water, 5);
            clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
            Assert.That(world.Terrain.Has(fire, TerrainEffectType.FireZone), Is.False, "물이 닿으면 꺼진다");
        }

        // 검산: FloorDecorConfig 특수 방 값 = BALANCE.md 16번, 목록의 아이템은 ItemCatalog에 있다
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var c = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToConfig().SpecialRooms;
            Assert.That((c.RoomPercent, c.MinSize, c.MaxSize, c.VaultItemCount, c.ArmoryItemCount, c.LibraryItemCount, c.GardenItemCount, c.KeyItem),
                Is.EqualTo((70, 3, 5, 2, 2, 3, 2, "key_iron")));
            Assert.That(c.Weights, Is.EqualTo(new[] { 3, 2, 2, 2, 2, 2, 2 }), "3-16c 연구실 2 · 3-41 창고 2 · 3-42 마법 불 2");
            Assert.That(c.MagicalFireItem, Is.EqualTo("potion_frost"));
            Assert.That((c.StorageItemMin, c.StorageItemMax, c.StorageFireItem), Is.EqualTo((3, 4, "potion_liquid_flame")));
            Assert.That(c.LabItemCount, Is.EqualTo(2));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            foreach (var list in new[] { c.VaultItems, c.ArmoryItems, c.LibraryItems, c.GardenItems, c.LabItems, c.StorageItems })
                foreach (var id in list) Assert.That(items.TryGet(id, out _), Is.True, id);
            Assert.That(items.TryGet(c.MagicalFireItem, out _), Is.True);
            Assert.That(items.TryGet("key_iron", out var key) && key.Use == ItemUse.Key, Is.True);
        }
    }
}
