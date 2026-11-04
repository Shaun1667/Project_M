using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Dungeon
{
    /// <summary>특수 방 종류 (3-15b, SPD). 끝에만 추가한다.</summary>
    public enum SpecialRoomType
    {
        None = 0,
        /// <summary>보물 방: 잠긴 문 · 좋은 아이템. 열쇠는 같은 층 어딘가.</summary>
        Vault = 1,
        /// <summary>무기고: 무기 · 갑옷.</summary>
        Armory = 2,
        /// <summary>도서관: 주문서.</summary>
        Library = 3,
        /// <summary>정원: 높은 풀 가득 · 음식.</summary>
        Garden = 4,
    }

    /// <summary>특수 방 수치 · 아이템 목록 (3-15b). 값은 FloorDecorConfigAsset에서 — 기준은 BALANCE.md 16번.</summary>
    public sealed class SpecialRoomConfig
    {
        /// <summary>층마다 특수 방이 생길 확률 (%) — 생기면 하나.</summary>
        public int RoomPercent { get; }
        /// <summary>종류 무게 (보물 방 · 무기고 · 도서관 · 정원 순).</summary>
        public IReadOnlyList<int> Weights { get; }
        /// <summary>방 안쪽 크기 (정사각 한 변 최소 ~ 최대).</summary>
        public int MinSize { get; }
        public int MaxSize { get; }
        /// <summary>보물 방 아이템 목록 · 개수 (목록에서 겹치지 않게).</summary>
        public IReadOnlyList<string> VaultItems { get; }
        public int VaultItemCount { get; }
        public IReadOnlyList<string> ArmoryItems { get; }
        public int ArmoryItemCount { get; }
        public IReadOnlyList<string> LibraryItems { get; }
        public int LibraryItemCount { get; }
        public IReadOnlyList<string> GardenItems { get; }
        public int GardenItemCount { get; }
        /// <summary>보물 방 열쇠 아이템 ID.</summary>
        public string KeyItem { get; }

        public SpecialRoomConfig(int roomPercent, IReadOnlyList<int> weights, int minSize, int maxSize,
            IReadOnlyList<string> vaultItems, int vaultItemCount, IReadOnlyList<string> armoryItems, int armoryItemCount,
            IReadOnlyList<string> libraryItems, int libraryItemCount, IReadOnlyList<string> gardenItems, int gardenItemCount, string keyItem)
        {
            if (roomPercent < 0 || roomPercent > 100) throw new ArgumentOutOfRangeException(nameof(roomPercent));
            if (weights == null || weights.Count != 4) throw new ArgumentException("종류 무게는 4개 (보물 · 무기고 · 도서관 · 정원)", nameof(weights));
            if (minSize < 2 || maxSize < minSize) throw new ArgumentOutOfRangeException(nameof(maxSize), "2 ≤ 최소 ≤ 최대");
            RoomPercent = roomPercent;
            Weights = weights;
            MinSize = minSize;
            MaxSize = maxSize;
            VaultItems = vaultItems ?? Array.Empty<string>();
            VaultItemCount = Math.Max(0, vaultItemCount);
            ArmoryItems = armoryItems ?? Array.Empty<string>();
            ArmoryItemCount = Math.Max(0, armoryItemCount);
            LibraryItems = libraryItems ?? Array.Empty<string>();
            LibraryItemCount = Math.Max(0, libraryItemCount);
            GardenItems = gardenItems ?? Array.Empty<string>();
            GardenItemCount = Math.Max(0, gardenItemCount);
            KeyItem = string.IsNullOrEmpty(keyItem) ? null : keyItem;
        }
    }

    /// <summary>
    /// 특수 방 (3-15b, 결정 2026-11-04 — SPD): 벽 덩어리 속에 방 하나를 파고 입구 한 칸에 문(보물 방은 잠긴 문)을 둔다.
    /// 입구 = 맨땅 칸 옆의 벽 칸, 방은 그 바깥쪽으로 — 방 · 둘레 한 줄이 모두 벽(테두리 밖 아님)이어야 판다. 난수 RngStream.Decor (꾸미기 안).
    /// 보물 방이면 열쇠를 방 밖 맨땅(시작 칸에서 3칸 이상)에 놓는다. 아이템은 바닥에 (MapLoaded 뒤에 불러야 화면이 지우지 않는다).
    /// </summary>
    public static class SpecialRooms
    {
        private static readonly GridPos[] Sides = { new GridPos(0, 1), new GridPos(1, 0), new GridPos(0, -1), new GridPos(-1, 0) };
        private const int Attempts = 200;

        /// <summary>파낸 방 (종류 · 안쪽 칸 · 입구). 못 팠으면 null.</summary>
        public sealed class Result
        {
            public SpecialRoomType Type;
            public List<GridPos> Cells;
            public GridPos Entrance;
        }

        public static Result Carve(World world, DungeonFloor floor, SpecialRoomConfig config, IRng rng)
        {
            if (config == null || rng.Range(0, 100) >= config.RoomPercent) return null;
            var type = PickType(config, rng);
            var map = world.Map;
            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                var anchor = new GridPos(rng.Range(1, map.Width - 1), rng.Range(1, map.Height - 1));
                var dir = Sides[rng.Range(0, Sides.Length)];
                int size = rng.Range(config.MinSize, config.MaxSize + 1);
                if (!GridMap.IsGroundType(map.Get(anchor)) || anchor == floor.Start) continue;
                var entrance = anchor + dir;
                if (!map.InBounds(entrance) || map.Get(entrance) != TileType.Wall) continue;
                // 방 안쪽: 입구 너머로 size × size, 입구가 한 변 가운데쯤에 오게
                var across = new GridPos(dir.Y, dir.X); // 입구 방향에 수직
                int offset = rng.Range(0, size);
                var origin = entrance + dir - new GridPos(across.X * offset, across.Y * offset);
                var cells = new List<GridPos>();
                for (int a = 0; a < size; a++)
                    for (int b = 0; b < size; b++)
                        cells.Add(origin + new GridPos(dir.X * a + across.X * b, dir.Y * a + across.Y * b));
                if (!AllSolid(map, cells, entrance)) continue;

                foreach (var c in cells) world.SetTile(c, TileType.Floor);
                world.SetTile(entrance, type == SpecialRoomType.Vault ? TileType.LockedDoor : TileType.Door);
                Furnish(world, floor, config, rng, type, cells, entrance);
                return new Result { Type = type, Cells = cells, Entrance = entrance };
            }
            return null;
        }

        private static SpecialRoomType PickType(SpecialRoomConfig config, IRng rng)
        {
            int total = 0;
            foreach (var w in config.Weights) total += Math.Max(0, w);
            if (total == 0) return SpecialRoomType.Armory;
            int roll = rng.Range(0, total);
            for (int i = 0; i < config.Weights.Count; i++)
            {
                int w = Math.Max(0, config.Weights[i]);
                if (roll < w) return (SpecialRoomType)(i + 1);
                roll -= w;
            }
            return SpecialRoomType.Armory;
        }

        /// <summary>방 칸 · 둘레 한 줄이 모두 벽이고 맵 테두리 안인지 (입구 칸은 둘레에서 뺀다).</summary>
        private static bool AllSolid(GridMap map, List<GridPos> cells, GridPos entrance)
        {
            var set = new HashSet<GridPos>(cells);
            foreach (var c in cells)
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        var p = new GridPos(c.X + dx, c.Y + dy);
                        if (p == entrance) continue;
                        if (!map.InBounds(p) || map.IsBorder(p) && set.Contains(p)) return false;
                        if (map.Get(p) != TileType.Wall) return false;
                    }
            }
            return true;
        }

        private static void Furnish(World world, DungeonFloor floor, SpecialRoomConfig config, IRng rng, SpecialRoomType type, List<GridPos> cells, GridPos entrance)
        {
            IReadOnlyList<string> pool;
            int count;
            switch (type)
            {
                case SpecialRoomType.Vault: pool = config.VaultItems; count = config.VaultItemCount; break;
                case SpecialRoomType.Armory: pool = config.ArmoryItems; count = config.ArmoryItemCount; break;
                case SpecialRoomType.Library: pool = config.LibraryItems; count = config.LibraryItemCount; break;
                default: pool = config.GardenItems; count = config.GardenItemCount; break;
            }
            if (type == SpecialRoomType.Garden)
                foreach (var c in cells) world.SetTile(c, TileType.TallGrass);

            var spots = new List<GridPos>(cells);
            var choices = new List<string>(pool);
            for (int i = 0; i < count && spots.Count > 0 && choices.Count > 0; i++)
            {
                int s = rng.Range(0, spots.Count);
                int k = rng.Range(0, choices.Count);
                world.FloorItems.Add(spots[s], choices[k]);
                world.Events.Publish(new ProjectM.Items.FloorItemPlacedEvent(spots[s], choices[k], choices[k]));
                spots.RemoveAt(s);
                if (type != SpecialRoomType.Garden) choices.RemoveAt(k); // 음식은 겹쳐도 됨
            }

            if (type == SpecialRoomType.Vault && config.KeyItem != null) PlaceKey(world, floor, config.KeyItem, rng, new HashSet<GridPos>(cells));
        }

        /// <summary>열쇠: 방 밖 맨땅, 시작 칸에서 3칸 이상, 아이템 없는 칸 (y → x 순 목록에서).</summary>
        private static void PlaceKey(World world, DungeonFloor floor, string keyItem, IRng rng, HashSet<GridPos> room)
        {
            var map = world.Map;
            var candidates = new List<GridPos>();
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (GridMap.IsGroundType(map.Get(p)) && !room.Contains(p) && GridMovement.Chebyshev(p, floor.Start) >= 3 && !world.FloorItems.Any(p))
                        candidates.Add(p);
                }
            if (candidates.Count == 0) return;
            var at = candidates[rng.Range(0, candidates.Count)];
            world.FloorItems.Add(at, keyItem);
            world.Events.Publish(new ProjectM.Items.FloorItemPlacedEvent(at, keyItem, keyItem));
        }
    }

    /// <summary>
    /// 잠긴 문 열기 (3-15b, 차례 1): 옆(상하좌우) 잠긴 문에 배낭의 열쇠(ItemUse.Key) 하나를 쓰고 닫힌 문으로. 열쇠가 없거나 잠긴 문이 아니면 차례만 쓴다.
    /// </summary>
    public sealed class UnlockCommand : ProjectM.Core.ICommand
    {
        private readonly World _world;
        private readonly ProjectM.Items.ItemBook _items;

        public GridPos Door { get; }
        public int CostTicks { get; }

        public UnlockCommand(World world, ProjectM.Items.ItemBook items, GridPos door, int costTicks)
        {
            if (costTicks < 1) throw new ArgumentOutOfRangeException(nameof(costTicks));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            Door = door;
            CostTicks = costTicks;
        }

        /// <summary>열쇠가 든 인벤토리 칸, 없으면 -1.</summary>
        public static int KeySlot(ProjectM.Items.ItemBook items, ProjectM.Units.Unit unit)
        {
            for (int i = 0; i < unit.InventorySlots.Length; i++)
            {
                var e = unit.InventorySlots[i];
                if (!e.IsEmpty && e.Kind == ProjectM.Items.InventoryKind.Item && items.TryGet(e.Id, out var def) && def.Use == ProjectM.Items.ItemUse.Key) return i;
            }
            return -1;
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var unit) || !unit.IsAlive) return;
            if (!_world.Map.InBounds(Door) || _world.Map.Get(Door) != TileType.LockedDoor || GridMovement.Chebyshev(unit.Pos, Door) != 1) return;
            int slot = KeySlot(_items, unit);
            if (slot < 0) return;
            ProjectM.Items.Inventory.RemoveAt(_world, unit, slot);
            _world.SetTile(Door, TileType.Door);
            _world.Events.Publish(new DoorUnlockedEvent(Door));
        }
    }

    /// <summary>잠긴 문을 열었다 (로그).</summary>
    public sealed class DoorUnlockedEvent : ProjectM.Core.ISimEvent
    {
        public GridPos Pos { get; }

        public DoorUnlockedEvent(GridPos pos)
        {
            Pos = pos;
        }
    }
}
