using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;

namespace ProjectM.Dungeon
{
    /// <summary>층 꾸미기 수치 (3-11a). 값은 FloorDecorConfigAsset에서 온다 — 기준은 BALANCE.md 16번.</summary>
    public sealed class FloorDecorConfig
    {
        /// <summary>층마다 풀 덩어리 수.</summary>
        public int GrassPatchCount { get; }
        public int GrassPatchMinSize { get; }
        public int GrassPatchMaxSize { get; }
        /// <summary>풀 칸 중 높은 풀의 확률 (%) — 나머지는 짧은 풀.</summary>
        public int TallGrassPercent { get; }
        /// <summary>층마다 물 웅덩이 수.</summary>
        public int PuddleCount { get; }
        public int PuddleMinSize { get; }
        public int PuddleMaxSize { get; }
        /// <summary>방 입구(문 자리)마다 문을 놓을 확률 (%) — 3-11c.</summary>
        public int DoorPercent { get; }
        /// <summary>놓은 문이 숨겨진 문일 확률 (%) — 3-12b. 막으면 층이 끊기는 자리는 보통 문.</summary>
        public int HiddenDoorPercent { get; }

        public FloorDecorConfig(int grassPatchCount, int grassPatchMinSize, int grassPatchMaxSize, int tallGrassPercent,
            int puddleCount, int puddleMinSize, int puddleMaxSize, int doorPercent = 0, int hiddenDoorPercent = 0)
        {
            if (hiddenDoorPercent < 0 || hiddenDoorPercent > 100) throw new ArgumentOutOfRangeException(nameof(hiddenDoorPercent));
            HiddenDoorPercent = hiddenDoorPercent;
            if (doorPercent < 0 || doorPercent > 100) throw new ArgumentOutOfRangeException(nameof(doorPercent));
            DoorPercent = doorPercent;
            if (grassPatchCount < 0) throw new ArgumentOutOfRangeException(nameof(grassPatchCount));
            if (grassPatchMinSize < 1 || grassPatchMaxSize < grassPatchMinSize) throw new ArgumentOutOfRangeException(nameof(grassPatchMaxSize), "풀 덩어리 크기는 1 ≤ 최소 ≤ 최대");
            if (tallGrassPercent < 0 || tallGrassPercent > 100) throw new ArgumentOutOfRangeException(nameof(tallGrassPercent));
            if (puddleCount < 0) throw new ArgumentOutOfRangeException(nameof(puddleCount));
            if (puddleMinSize < 1 || puddleMaxSize < puddleMinSize) throw new ArgumentOutOfRangeException(nameof(puddleMaxSize), "웅덩이 크기는 1 ≤ 최소 ≤ 최대");
            GrassPatchCount = grassPatchCount;
            GrassPatchMinSize = grassPatchMinSize;
            GrassPatchMaxSize = grassPatchMaxSize;
            TallGrassPercent = tallGrassPercent;
            PuddleCount = puddleCount;
            PuddleMinSize = puddleMinSize;
            PuddleMaxSize = puddleMaxSize;
        }
    }

    /// <summary>
    /// 층 꾸미기 (3-11a · 3-11c, 결정 2026-11-04): 방 입구에 문 → 방 안에 물 웅덩이(영구 물 지형) → 풀 덩어리(짧은 · 높은 풀 칸)를 덩어리로 놓는다.
    /// 난수는 RngStream.Decor만 — 맵 모양 · 적 배치는 꾸미기와 상관없이 같다. 바닥(Floor) 칸만 바꾸고 시작 칸은 비운다.
    /// 처음 가는 층에만 부르고(떠난 층은 저장된 그대로), 맵 이벤트(MapLoaded) 뒤에 불러야 화면이 지우지 않는다.
    /// </summary>
    public static class FloorDecor
    {
        private static readonly GridPos[] Directions =
        {
            new GridPos(0, 1), new GridPos(1, 0), new GridPos(0, -1), new GridPos(-1, 0),
        };

        /// <param name="water">물 지형 규칙 (TerrainBook의 Water). null이면 웅덩이를 놓지 않는다.</param>
        /// <param name="traps">함정 표 (3-12a). null이면 함정을 놓지 않는다. 꾸미기 맨 끝 (같은 Decor 난수).</param>
        public static void Apply(World world, DungeonFloor floor, FloorDecorConfig config, TerrainDef water, TrapBook traps = null)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (floor == null) throw new ArgumentNullException(nameof(floor));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (floor.Rooms.Count == 0) return;
            var rng = world.Rng.Get(RngStream.Decor);
            var map = world.Map;
            var used = new HashSet<GridPos> { floor.Start };

            if (config.DoorPercent > 0)
            {
                foreach (var pos in DoorSpots(map, floor))
                {
                    if (rng.Range(0, 100) >= config.DoorPercent) continue;
                    bool hidden = config.HiddenDoorPercent > 0 && rng.Range(0, 100) < config.HiddenDoorPercent && StaysConnected(map, floor.Start, pos);
                    world.SetTile(pos, hidden ? TileType.HiddenDoor : TileType.Door);
                }
            }
            if (water != null)
            {
                for (int i = 0; i < config.PuddleCount; i++)
                {
                    foreach (var pos in GrowPatch(map, floor, rng, used, config.PuddleMinSize, config.PuddleMaxSize))
                        TerrainEffects.Place(world, pos, water, TerrainLayer.Permanent);
                }
            }
            for (int i = 0; i < config.GrassPatchCount; i++)
            {
                foreach (var pos in GrowPatch(map, floor, rng, used, config.GrassPatchMinSize, config.GrassPatchMaxSize))
                    world.SetTile(pos, rng.Range(0, 100) < config.TallGrassPercent ? TileType.TallGrass : TileType.Grass);
            }
            if (traps != null) Traps.PlaceForFloor(world, floor.Start, traps, rng);
        }

        /// <summary>
        /// 문 자리 (3-11c): 방 칸이 아닌 바닥 칸 중 한 축(좌우 또는 상하)이 모두 벽이고, 다른 축 두 칸이 걸을 수 있고 그중 하나가 방 칸인 칸 —
        /// 복도가 방에 들어가는 한 칸 폭 길목. 이미 고른 자리와 8방향으로 붙은 칸은 뺀다 (겹문 방지). 칸 순서(y → x)로 고정.
        /// </summary>
        public static List<GridPos> DoorSpots(GridMap map, DungeonFloor floor)
        {
            var roomCells = new HashSet<GridPos>();
            foreach (var room in floor.Rooms)
                foreach (var p in room) roomCells.Add(p);
            var spots = new List<GridPos>();
            for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new GridPos(x, y);
                if (map.Get(p) != TileType.Floor || roomCells.Contains(p) || p == floor.Start || p == floor.Exit) continue;
                if (!IsChokepoint(map, roomCells, p, new GridPos(1, 0), new GridPos(0, 1))
                    && !IsChokepoint(map, roomCells, p, new GridPos(0, 1), new GridPos(1, 0))) continue;
                if (spots.Exists(s => GridMovement.Chebyshev(s, p) <= 1)) continue;
                spots.Add(p);
            }
            return spots;
        }

        /// <summary>pos를 막아도(숨겨진 문) 시작 칸에서 걸을 수 있는 모든 칸에 닿는지 — 숨겨진 문은 우회로가 있는 자리만 (결정 2026-11-04).</summary>
        private static bool StaysConnected(GridMap map, GridPos start, GridPos pos)
        {
            var before = map.Get(pos);
            map.Set(pos, TileType.Wall);
            try
            {
                var reached = DungeonGenerator.FloodWalkable(map, start);
                for (int i = 0; i < reached.Length; i++)
                    if (!reached[i] && map.IsWalkable(new GridPos(i % map.Width, i / map.Width))) return false;
                return true;
            }
            finally
            {
                map.Set(pos, before);
            }
        }

        /// <summary>wallAxis 양쪽이 벽이고 openAxis 양쪽이 걸을 수 있으며 그중 하나가 방 칸인지.</summary>
        private static bool IsChokepoint(GridMap map, HashSet<GridPos> roomCells, GridPos p, GridPos wallAxis, GridPos openAxis)
        {
            if (map.IsWalkable(p + wallAxis) || map.IsWalkable(p - wallAxis)) return false;
            var a = p + openAxis;
            var b = p - openAxis;
            if (!map.IsWalkable(a) || !map.IsWalkable(b)) return false;
            return roomCells.Contains(a) != roomCells.Contains(b); // 한쪽만 방 = 방 입구 (방 안의 기둥 틈은 아님)
        }

        /// <summary>무작위 방의 무작위 바닥 칸에서 시작해 상하좌우로 넓힌 덩어리 (바닥 · 아직 안 쓴 칸만). 시작 칸을 못 찾으면 빈 목록.</summary>
        private static List<GridPos> GrowPatch(GridMap map, DungeonFloor floor, IRng rng, HashSet<GridPos> used, int minSize, int maxSize)
        {
            var patch = new List<GridPos>();
            var room = floor.Rooms[rng.Range(0, floor.Rooms.Count)];
            if (room.Count == 0) return patch;
            var seed = room[rng.Range(0, room.Count)];
            int size = rng.Range(minSize, maxSize + 1);
            if (!IsFree(map, used, seed)) return patch;

            patch.Add(seed);
            used.Add(seed);
            var frontier = new List<GridPos>();
            AddNeighbors(map, used, seed, frontier);
            while (patch.Count < size && frontier.Count > 0)
            {
                int pick = rng.Range(0, frontier.Count);
                var next = frontier[pick];
                frontier.RemoveAt(pick);
                if (!IsFree(map, used, next)) continue;
                patch.Add(next);
                used.Add(next);
                AddNeighbors(map, used, next, frontier);
            }
            return patch;
        }

        private static void AddNeighbors(GridMap map, HashSet<GridPos> used, GridPos pos, List<GridPos> frontier)
        {
            foreach (var dir in Directions)
            {
                var n = pos + dir;
                if (IsFree(map, used, n) && !frontier.Contains(n)) frontier.Add(n);
            }
        }

        private static bool IsFree(GridMap map, HashSet<GridPos> used, GridPos pos) =>
            map.InBounds(pos) && map.Get(pos) == TileType.Floor && !used.Contains(pos);
    }
}
