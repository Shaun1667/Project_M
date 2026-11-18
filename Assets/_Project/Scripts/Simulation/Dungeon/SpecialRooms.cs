using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;

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
        /// <summary>연구실 (3-16c): 가운데 연금 솥 · 씨앗.</summary>
        Laboratory = 5,
        /// <summary>창고 (3-41, SPD Storage): 입구 = 바리케이드(불로만 열림) · 물약 · 주문서. 같은 층 방 밖에 불꽃 물약 하나 보장.</summary>
        Storage = 6,
        /// <summary>마법 불 방 (3-42, SPD Magical Fire): 방을 가로지르는 꺼지지 않는 불 벽 · 그 뒤에 좋은 물건 하나(보물 방 목록). 얼음 · 물 · 냉기 물약으로 끔. 같은 층 방 밖에 냉기 물약 하나 보장.</summary>
        MagicalFire = 7,
        /// <summary>독가스 방 (3-43, SPD Toxic Gas): 방 전체 영구 독구름 · 금화 더미 2~3개 + 물건 하나(보물 방 목록). 정화 물약 · 번개(가스 폭발)로 없앰. 같은 층 방 밖에 정화 물약 하나 보장.</summary>
        ToxicGas = 8,
        /// <summary>보물 창고 (3-44, SPD Treasury): 잠긴 문 · 금화 더미 4~6개(층 금액). 열쇠는 같은 층 방 밖.</summary>
        Treasury = 9,
        /// <summary>함정 방 (3-45, SPD Traps): 입구부터 맨 안쪽 줄 앞까지 바닥 전체에 보이는 함정(방마다 한 종류) · 맨 안쪽 줄에 좋은 물건 하나(보물 방 목록). 같은 층 방 밖에 부양 물약 하나 보장.</summary>
        TrapRoom = 10,
        /// <summary>마법 우물 (3-46, SPD Magic Well): 가운데에 우물 하나(치유 · 지식 중 하나, 한 번 마시면 마름). 아이템 없음.</summary>
        MagicWell = 11,
        /// <summary>지하 묘지 (3-47, SPD Crypt): 잠긴 문 · 가운데 관 하나. 열면 강화된 갑옷 + 지킴이(원혼) — Crypts.Open. 열쇠는 같은 층 방 밖.</summary>
        Crypt = 12,
        /// <summary>석상 방 (3-48, SPD Statue): 가운데에 잠든 석상 하나(Statues.Place). 쓰러뜨리면 강화된 무기. 아이템 없음.</summary>
        Statue = 13,
        /// <summary>감시탑 (3-49, SPD Sentry): 맨 안쪽 줄 가운데에 무적 감시자(Sentries.Place) · 그 줄에 좋은 물건 하나(보물 방 목록). 감시자는 보이는 플레이어 칸에 예고 → 다음 차례 번개.</summary>
        Sentry = 14,
        /// <summary>피라냐 연못 (3-50, SPD Pool): 가운데 받침대만 빼고 영구 물 · 물속 피라냐(Pools.Fill) · 받침대에 좋은 물건 하나(보물 방 목록).</summary>
        Pool = 15,
        /// <summary>약한 바닥 (3-51, SPD Weak Floor): 입구 쪽 발판 한 줄만 남기고 구덩이 · 맨 안쪽 줄 가운데 돌기둥에 좋은 물건 하나(보물 방 목록). 같은 층 방 밖에 부양 물약 하나 보장. 떨어질 수 없는 층(다음 층이 보스)에는 안 생김.</summary>
        WeakFloor = 16,
    }

    /// <summary>특수 방 수치 · 아이템 목록 (3-15b). 값은 FloorDecorConfigAsset에서 — 기준은 BALANCE.md 16번.</summary>
    public sealed class SpecialRoomConfig
    {
        /// <summary>층마다 특수 방이 생길 확률 (%) — 생기면 하나.</summary>
        public int RoomPercent { get; }
        /// <summary>종류 무게 (보물 방 · 무기고 · 도서관 · 정원 · 연구실 · 창고 · 마법 불 · 독가스 · 보물 창고 · 함정 방 · 마법 우물 · 지하 묘지 · 석상 · 감시탑 · 피라냐 연못 · 약한 바닥 순 — 모자란 뒤쪽은 0).</summary>
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
        /// <summary>연구실 아이템 (3-16c 씨앗) · 개수.</summary>
        public IReadOnlyList<string> LabItems { get; }
        public int LabItemCount { get; }
        /// <summary>보물 방 열쇠 아이템 ID.</summary>
        public string KeyItem { get; }
        /// <summary>창고 (3-41): 아이템 목록(같은 ID를 여러 번 넣어 무게) · 개수 최소 ~ 최대 · 방 밖에 보장하는 불 아이템.</summary>
        public IReadOnlyList<string> StorageItems { get; }
        public int StorageItemMin { get; }
        public int StorageItemMax { get; }
        public string StorageFireItem { get; }
        /// <summary>마법 불 방 (3-42): 방 밖에 보장하는 불 끄는 아이템.</summary>
        public string MagicalFireItem { get; }
        /// <summary>독가스 방 (3-43): 방 밖에 보장하는 가스 없애는 아이템 · 금화 더미 수 최소 ~ 최대 · 금액(기본 + 층마다, 최소 ~ 최대 — 층 금화와 같은 식).</summary>
        public string ToxicGasItem { get; }
        public int ToxicGoldPilesMin { get; }
        public int ToxicGoldPilesMax { get; }
        public int ToxicGoldMinBase { get; }
        public int ToxicGoldMinPerFloor { get; }
        public int ToxicGoldMaxBase { get; }
        public int ToxicGoldMaxPerFloor { get; }
        /// <summary>보물 창고 (3-44): 금화 더미 수 최소 ~ 최대 — 금액은 독가스 방과 같은 식(ToxicGold*Base · PerFloor).</summary>
        public int TreasuryPilesMin { get; }
        public int TreasuryPilesMax { get; }
        /// <summary>함정 방 (3-45): 고를 함정 종류(방마다 하나) · 방 밖에 보장하는 아이템.</summary>
        public IReadOnlyList<TrapType> TrapRoomTypes { get; }
        public string TrapRoomItem { get; }
        /// <summary>마법 우물 (3-46): 고를 우물 종류 (같은 종류를 여러 번 넣으면 무게).</summary>
        public IReadOnlyList<TileType> WellTypes { get; }
        /// <summary>약한 바닥 (3-51): 방 밖에 보장하는 아이템 (부양 물약 — 떠서 건넘).</summary>
        public string WeakFloorItem { get; }

        public SpecialRoomConfig(int roomPercent, IReadOnlyList<int> weights, int minSize, int maxSize,
            IReadOnlyList<string> vaultItems, int vaultItemCount, IReadOnlyList<string> armoryItems, int armoryItemCount,
            IReadOnlyList<string> libraryItems, int libraryItemCount, IReadOnlyList<string> gardenItems, int gardenItemCount, string keyItem,
            IReadOnlyList<string> labItems = null, int labItemCount = 0,
            IReadOnlyList<string> storageItems = null, int storageItemMin = 0, int storageItemMax = 0, string storageFireItem = null,
            string magicalFireItem = null,
            string toxicGasItem = null, int toxicGoldPilesMin = 0, int toxicGoldPilesMax = 0,
            int toxicGoldMinBase = 0, int toxicGoldMinPerFloor = 0, int toxicGoldMaxBase = 0, int toxicGoldMaxPerFloor = 0,
            int treasuryPilesMin = 0, int treasuryPilesMax = 0,
            IReadOnlyList<TrapType> trapRoomTypes = null, string trapRoomItem = null,
            IReadOnlyList<TileType> wellTypes = null, string weakFloorItem = null)
        {
            WeakFloorItem = string.IsNullOrEmpty(weakFloorItem) ? null : weakFloorItem;
            StorageItems = storageItems ?? Array.Empty<string>();
            StorageItemMin = Math.Max(0, storageItemMin);
            StorageItemMax = Math.Max(StorageItemMin, storageItemMax);
            StorageFireItem = string.IsNullOrEmpty(storageFireItem) ? null : storageFireItem;
            MagicalFireItem = string.IsNullOrEmpty(magicalFireItem) ? null : magicalFireItem;
            ToxicGasItem = string.IsNullOrEmpty(toxicGasItem) ? null : toxicGasItem;
            ToxicGoldPilesMin = Math.Max(0, toxicGoldPilesMin);
            ToxicGoldPilesMax = Math.Max(ToxicGoldPilesMin, toxicGoldPilesMax);
            ToxicGoldMinBase = Math.Max(0, toxicGoldMinBase);
            ToxicGoldMinPerFloor = Math.Max(0, toxicGoldMinPerFloor);
            ToxicGoldMaxBase = Math.Max(ToxicGoldMinBase, toxicGoldMaxBase);
            ToxicGoldMaxPerFloor = Math.Max(ToxicGoldMinPerFloor, toxicGoldMaxPerFloor);
            TreasuryPilesMin = Math.Max(0, treasuryPilesMin);
            TreasuryPilesMax = Math.Max(TreasuryPilesMin, treasuryPilesMax);
            TrapRoomTypes = trapRoomTypes ?? Array.Empty<TrapType>();
            foreach (var t in TrapRoomTypes) if (t == TrapType.None) throw new ArgumentException("함정 방 종류에 None", nameof(trapRoomTypes));
            TrapRoomItem = string.IsNullOrEmpty(trapRoomItem) ? null : trapRoomItem;
            WellTypes = wellTypes ?? Array.Empty<TileType>();
            foreach (var t in WellTypes) if (t != TileType.WellHealth && t != TileType.WellAwareness) throw new ArgumentException("우물 종류는 WellHealth · WellAwareness", nameof(wellTypes));
            LabItems = labItems ?? Array.Empty<string>();
            LabItemCount = Math.Max(0, labItemCount);
            if (roomPercent < 0 || roomPercent > 100) throw new ArgumentOutOfRangeException(nameof(roomPercent));
            if (weights == null || weights.Count < 4 || weights.Count > 16) throw new ArgumentException("종류 무게는 4 ~ 16개 (보물 · 무기고 · 도서관 · 정원 · 연구실 · 창고 · 마법 불 · 독가스 · 보물 창고 · 함정 방 · 마법 우물 · 지하 묘지 · 석상 · 감시탑 · 피라냐 연못 · 약한 바닥)", nameof(weights));
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
            var type = PickType(config, rng, Chasms.CanFall(world)); // 3-51: 떨어질 수 없는 층에는 약한 바닥 없음
            var room = CarveEmpty(world, floor, rng, config.MinSize, config.MaxSize, EntranceTile(type));
            if (room == null) return null;
            room.Type = type;
            Furnish(world, floor, config, rng, type, room.Cells, room.Entrance);
            return room;
        }

        /// <summary>빈 방 하나를 판다 (3-15b 특수 방 · 3-15c 상점 공용): 입구 칸 = entranceTile. 못 팠으면 null.</summary>
        public static Result CarveEmpty(World world, DungeonFloor floor, IRng rng, int minSize, int maxSize, TileType entranceTile)
        {
            var map = world.Map;
            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                var anchor = new GridPos(rng.Range(1, map.Width - 1), rng.Range(1, map.Height - 1));
                var dir = Sides[rng.Range(0, Sides.Length)];
                int size = rng.Range(minSize, maxSize + 1);
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
                world.SetTile(entrance, entranceTile);
                return new Result { Type = SpecialRoomType.None, Cells = cells, Entrance = entrance };
            }
            return null;
        }

        /// <summary>입구 칸: 보물 방 · 보물 창고(3-44) · 지하 묘지(3-47) = 잠긴 문, 창고 = 바리케이드 (3-41), 나머지 = 닫힌 문.</summary>
        private static TileType EntranceTile(SpecialRoomType type) =>
            type == SpecialRoomType.Vault || type == SpecialRoomType.Treasury || type == SpecialRoomType.Crypt ? TileType.LockedDoor : type == SpecialRoomType.Storage ? TileType.Barricade : TileType.Door;

        private static SpecialRoomType PickType(SpecialRoomConfig config, IRng rng, bool allowChasm)
        {
            int Weight(int i) => !allowChasm && i + 1 == (int)SpecialRoomType.WeakFloor ? 0 : Math.Max(0, config.Weights[i]);
            int total = 0;
            for (int i = 0; i < config.Weights.Count; i++) total += Weight(i);
            if (total == 0) return SpecialRoomType.Armory;
            int roll = rng.Range(0, total);
            for (int i = 0; i < config.Weights.Count; i++)
            {
                int w = Weight(i);
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
                case SpecialRoomType.Laboratory: pool = config.LabItems; count = config.LabItemCount; break;
                case SpecialRoomType.MagicalFire:
                case SpecialRoomType.ToxicGas:
                case SpecialRoomType.TrapRoom:
                case SpecialRoomType.Sentry: // 3-49: 감시자 옆 맨 안쪽 줄에
                case SpecialRoomType.Pool: // 3-50: 가운데 받침대에
                case SpecialRoomType.WeakFloor: pool = config.VaultItems; count = 1; break; // 3-51: 구덩이 건너 돌기둥에
                case SpecialRoomType.Treasury: // 3-44: 금화만
                case SpecialRoomType.MagicWell: // 3-46: 우물만
                case SpecialRoomType.Crypt: // 3-47: 관만 (갑옷은 열 때)
                case SpecialRoomType.Statue: pool = Array.Empty<string>(); count = 0; break; // 3-48: 석상만 (무기는 쓰러질 때) // 3-42: 보물 방 목록에서 하나
                case SpecialRoomType.Storage: pool = config.StorageItems; count = config.StorageItemMin + rng.Range(0, config.StorageItemMax - config.StorageItemMin + 1); break;
                default: pool = config.GardenItems; count = config.GardenItemCount; break;
            }
            if (type == SpecialRoomType.Garden)
                foreach (var c in cells) world.SetTile(c, TileType.TallGrass);

            var spots = new List<GridPos>(cells);
            if (type == SpecialRoomType.Laboratory) // 3-16c: 가운데 칸에 연금 솥
            {
                var center = cells[cells.Count / 2];
                world.SetTile(center, TileType.AlchemyPot);
                spots.Remove(center);
            }
            if (type == SpecialRoomType.MagicalFire) spots = PlaceFireWall(world, cells);
            if (type == SpecialRoomType.TrapRoom) spots = PlaceTrapField(world, config, rng, cells);
            if (type == SpecialRoomType.Crypt) world.SetTile(cells[cells.Count / 2], TileType.Tomb); // 3-47: 가운데 칸에 관
            if (type == SpecialRoomType.Statue) Statues.Place(world, cells[cells.Count / 2]); // 3-48: 가운데 칸에 잠든 석상
            if (type == SpecialRoomType.Sentry) spots = PlaceSentry(world, cells); // 3-49: 맨 안쪽 줄 가운데에 감시자
            if (type == SpecialRoomType.Pool) spots = Pools.Fill(world, rng, cells); // 3-50: 물 · 피라냐, 가운데 받침대
            if (type == SpecialRoomType.WeakFloor) spots = Chasms.PlaceWeakFloor(world, cells); // 3-51: 발판 한 줄 · 구덩이 · 돌기둥
            if (type == SpecialRoomType.MagicWell && config.WellTypes.Count > 0) // 3-46: 가운데 칸에 우물
                world.SetTile(cells[cells.Count / 2], config.WellTypes[rng.Range(0, config.WellTypes.Count)]);
            if (type == SpecialRoomType.ToxicGas) // 3-43: 방 전체 영구 독구름 · 금화 더미 먼저
            {
                foreach (var c in cells) TerrainEffects.Place(world, c, TerrainEffectType.PoisonCloud, TerrainLayer.Permanent);
                PlaceGold(world, config, rng, spots, config.ToxicGoldPilesMin, config.ToxicGoldPilesMax, keepOne: true);
            }
            var choices = new List<string>(pool);
            for (int i = 0; i < count && spots.Count > 0 && choices.Count > 0; i++)
            {
                int s = rng.Range(0, spots.Count);
                int k = rng.Range(0, choices.Count);
                world.FloorItems.Add(spots[s], choices[k]);
                world.Events.Publish(new ProjectM.Items.FloorItemPlacedEvent(spots[s], choices[k], ProjectM.Items.Identification.VisualOf(world, choices[k]))); // 3-41: 물약 · 주문서는 겉모습으로 (정체 숨김)
                spots.RemoveAt(s);
                if (type != SpecialRoomType.Garden && type != SpecialRoomType.Laboratory && type != SpecialRoomType.Storage) choices.RemoveAt(k); // 음식 · 씨앗 · 창고 소모품은 겹쳐도 됨
            }

            if ((type == SpecialRoomType.Vault || type == SpecialRoomType.Treasury || type == SpecialRoomType.Crypt) && config.KeyItem != null) PlaceOutside(world, floor, config.KeyItem, rng, new HashSet<GridPos>(cells));
            if (type == SpecialRoomType.Storage && config.StorageFireItem != null) PlaceOutside(world, floor, config.StorageFireItem, rng, new HashSet<GridPos>(cells)); // 3-41: 불을 못 써도 열 수 있게
            if (type == SpecialRoomType.WeakFloor && config.WeakFloorItem != null) PlaceOutside(world, floor, config.WeakFloorItem, rng, new HashSet<GridPos>(cells)); // 3-51: 부양으로 건넘
            if (type == SpecialRoomType.TrapRoom && config.TrapRoomItem != null) PlaceOutside(world, floor, config.TrapRoomItem, rng, new HashSet<GridPos>(cells)); // 3-45: 부양 = 함정 위를 떠서 지나감
            if (type == SpecialRoomType.Treasury) PlaceGold(world, config, rng, spots, config.TreasuryPilesMin, config.TreasuryPilesMax, keepOne: false);
            if (type == SpecialRoomType.ToxicGas && config.ToxicGasItem != null) PlaceOutside(world, floor, config.ToxicGasItem, rng, new HashSet<GridPos>(cells)); // 3-43: 번개가 없어도 없앨 수 있게
            if (type == SpecialRoomType.MagicalFire && config.MagicalFireItem != null) PlaceOutside(world, floor, config.MagicalFireItem, rng, new HashSet<GridPos>(cells)); // 3-42: 얼음을 못 써도 끌 수 있게
        }

        /// <summary>특수 방 금화 (3-43 독가스 방 · 3-44 보물 창고): 더미 수 최소 ~ 최대, 금액 = 기본 + 층마다 × 층 (최소 ~ 최대). 놓은 칸은 spots에서 뺀다. keepOne = 물건 자리 한 칸을 남김.</summary>
        private static void PlaceGold(World world, SpecialRoomConfig config, IRng rng, List<GridPos> spots, int pilesMin, int pilesMax, bool keepOne)
        {
            int piles = pilesMin + rng.Range(0, pilesMax - pilesMin + 1);
            int min = config.ToxicGoldMinBase + config.ToxicGoldMinPerFloor * world.Floor;
            int max = Math.Max(min, config.ToxicGoldMaxBase + config.ToxicGoldMaxPerFloor * world.Floor);
            for (int i = 0; i < piles && spots.Count > (keepOne ? 1 : 0) && min > 0; i++)
            {
                int s = rng.Range(0, spots.Count);
                world.FloorItems.Add(spots[s], ProjectM.Items.Loot.GoldItemId, rng.Range(min, max + 1));
                world.Events.Publish(new ProjectM.Items.FloorItemPlacedEvent(spots[s], ProjectM.Items.Loot.GoldItemId, ProjectM.Items.Loot.GoldItemId));
                spots.RemoveAt(s);
            }
        }

        /// <summary>
        /// 함정 밭 (3-45): 방 칸은 입구에서 깊이 순(한 줄 = size칸). 맨 안쪽 줄 앞까지 모두 보이는 함정 — 종류는 TrapRoomTypes에서 방마다 하나.
        /// 돌려주는 칸 = 맨 안쪽 줄 (보상 자리). 종류 목록이 비면 함정 없이 맨 안쪽 줄만.
        /// </summary>
        public static List<GridPos> PlaceTrapField(World world, SpecialRoomConfig config, IRng rng, List<GridPos> cells)
        {
            int size = (int)Math.Round(Math.Sqrt(cells.Count));
            var trap = config.TrapRoomTypes.Count > 0 ? config.TrapRoomTypes[rng.Range(0, config.TrapRoomTypes.Count)] : TrapType.None;
            var back = new List<GridPos>();
            for (int a = 0; a < size; a++)
                for (int b = 0; b < size; b++)
                {
                    var c = cells[a * size + b];
                    if (a == size - 1) back.Add(c);
                    else if (trap != TrapType.None) Traps.Place(world, c, trap, hidden: false);
                }
            return back;
        }

        /// <summary>
        /// 감시탑 (3-49): 방 칸은 입구에서 깊이 순(한 줄 = size칸). 맨 안쪽 줄 가운데에 감시자를 세우고, 돌려주는 칸 = 맨 안쪽 줄의 나머지 (보상 자리).
        /// </summary>
        public static List<GridPos> PlaceSentry(World world, List<GridPos> cells)
        {
            int size = (int)Math.Round(Math.Sqrt(cells.Count));
            var post = cells[(size - 1) * size + size / 2];
            Sentries.Place(world, post);
            var back = new List<GridPos>();
            for (int b = 0; b < size; b++)
            {
                var c = cells[(size - 1) * size + b];
                if (c != post) back.Add(c);
            }
            return back;
        }

        /// <summary>
        /// 마법 불 벽 (3-42): 방 칸은 입구에서 깊이 순(한 줄 = size칸, CarveEmpty 순서). 깊이 size/2부터 한 줄(한 변 5 이상이면 두 줄)을
        /// 영구 화염 지대로 — 방 너비 전체를 막는다. 깊이 1 이상이라 입구 문과 붙지 않아 문이 타지 않는다. 돌려주는 칸 = 불 뒤 칸 (보상 자리).
        /// </summary>
        public static List<GridPos> PlaceFireWall(World world, List<GridPos> cells)
        {
            int size = (int)Math.Round(Math.Sqrt(cells.Count));
            int start = size / 2;
            int thickness = size >= 5 ? 2 : 1;
            var behind = new List<GridPos>();
            for (int a = 0; a < size; a++)
                for (int b = 0; b < size; b++)
                {
                    var c = cells[a * size + b];
                    if (a >= start && a < start + thickness) TerrainEffects.Place(world, c, TerrainEffectType.FireZone, TerrainLayer.Permanent);
                    else if (a >= start + thickness) behind.Add(c);
                }
            return behind;
        }

        /// <summary>열쇠 · 창고 불 아이템: 방 밖 맨땅, 시작 칸에서 3칸 이상, 아이템 없는 칸 (y → x 순 목록에서).</summary>
        private static void PlaceOutside(World world, DungeonFloor floor, string keyItem, IRng rng, HashSet<GridPos> room)
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
            world.Events.Publish(new ProjectM.Items.FloorItemPlacedEvent(at, keyItem, ProjectM.Items.Identification.VisualOf(world, keyItem)));
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
