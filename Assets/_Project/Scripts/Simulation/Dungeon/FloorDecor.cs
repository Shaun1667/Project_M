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

        public FloorDecorConfig(int grassPatchCount, int grassPatchMinSize, int grassPatchMaxSize, int tallGrassPercent,
            int puddleCount, int puddleMinSize, int puddleMaxSize)
        {
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
    /// 층 꾸미기 (3-11a, 결정 2026-11-04): 방 안에 물 웅덩이(영구 물 지형) → 풀 덩어리(짧은 · 높은 풀 칸)를 덩어리로 놓는다.
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
        public static void Apply(World world, DungeonFloor floor, FloorDecorConfig config, TerrainDef water)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (floor == null) throw new ArgumentNullException(nameof(floor));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (floor.Rooms.Count == 0) return;
            var rng = world.Rng.Get(RngStream.Decor);
            var map = world.Map;
            var used = new HashSet<GridPos> { floor.Start };

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
