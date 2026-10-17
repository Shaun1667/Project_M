using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 층에 플레이어와 적을 놓는다. 적 칸은 맵 생성 난수(RngStream.MapGen)로 고른다 — 같은 시드 = 같은 배치.
    /// 적 후보: 바닥 칸(계단 제외) 중 시작 칸에서 최소 거리 이상.
    /// </summary>
    public static class FloorPopulator
    {
        public static Unit Populate(World world, GridPos start, UnitDef playerDef, UnitDef enemyDef, SpawnConfig spawn, ICommandSource enemyAi,
            int quickSlotCount = 0, int inventorySize = ProjectM.Magic.MagicConfig.DefaultInventorySlotCount)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (spawn == null) throw new ArgumentNullException(nameof(spawn));

            var player = UnitSpawner.SpawnPlayer(world, playerDef, start, quickSlotCount, inventorySize);
            var cells = PickEnemyCells(world.Map, start, spawn.EnemyCount, spawn.EnemyMinDistanceFromStart, world.Rng.Get(RngStream.MapGen));
            foreach (var cell in cells)
                UnitSpawner.SpawnEnemy(world, enemyDef, cell, enemyAi);
            return player;
        }

        /// <summary>후보 칸에서 count개를 겹치지 않게 뽑는다 (부분 Fisher-Yates, 후보는 y → x 순).</summary>
        public static List<GridPos> PickEnemyCells(GridMap map, GridPos start, int count, int minDistance, IRng rng)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            var candidates = new List<GridPos>();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var pos = new GridPos(x, y);
                    if (map.Get(pos) == TileType.Floor && GridMovement.Chebyshev(pos, start) >= minDistance)
                        candidates.Add(pos);
                }
            }

            int take = Math.Min(count, candidates.Count);
            for (int i = 0; i < take; i++)
            {
                int j = rng.Range(i, candidates.Count);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }
            return candidates.GetRange(0, take);
        }
    }
}
