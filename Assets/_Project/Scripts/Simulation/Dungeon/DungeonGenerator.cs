using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Dungeon
{
    /// <summary>
    /// 층 생성 (Reference/MapGenerator_75121da.cs 이식, TASKS 1-5).
    /// 순서: ① 벽 덩어리 ② 방(벽 아닌 칸만 바닥) ③ 테두리 벽 ④ 방마다 기준점 ⑤ 기준점끼리 차례로 복도
    ///       ⑥ 시작점과 끊긴 바닥 조각을 시작점까지 잇기 ⑦ 남은 빈칸 → 벽 ⑦-2 막다른 길을 벽을 뚫어 다른 길과 잇기 ⑧ 도착점에 계단.
    /// 방 겹침과 크기 분포는 의도된 것이다 (2026-10-16 사용자 확인). 결과는 항상 모든 걸을 수 있는 칸이 시작점과 이어진다.
    /// 난수는 넘겨받은 IRng(RngStream.MapGen)만 쓴다.
    /// </summary>
    public sealed class DungeonGenerator
    {
        private static readonly GridPos[] Directions =
        {
            new GridPos(0, 1), new GridPos(1, 0), new GridPos(0, -1), new GridPos(-1, 0),
        };

        private readonly DungeonGenConfig _config;

        public DungeonGenerator(DungeonGenConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public DungeonFloor Generate(IRng rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            var c = _config;
            var map = new GridMap(c.Width, c.Height);

            // ① 벽 덩어리: 시작점이 이미 벽이면 다시 뽑는다
            for (int i = 0; i < c.WallBlockCount; i++)
                PlaceRect(map, rng, c.WallBlockSizeRoll, c.WallBlockMinSize, null);

            // ② 방: 벽이 아닌 칸만 바닥으로, 칸 목록을 모은다
            var rooms = new List<List<GridPos>>(c.RoomCount);
            for (int i = 0; i < c.RoomCount; i++)
            {
                var cells = new List<GridPos>();
                PlaceRect(map, rng, c.RoomSizeRoll, c.RoomMinSize, cells);
                rooms.Add(cells);
            }

            // ③ 테두리 한 줄은 항상 벽 — 복도보다 먼저 쳐야 가장자리 복도가 잘리지 않는다
            for (int x = 0; x < map.Width; x++)
            {
                map.Set(new GridPos(x, 0), TileType.Wall);
                map.Set(new GridPos(x, map.Height - 1), TileType.Wall);
            }
            for (int y = 0; y < map.Height; y++)
            {
                map.Set(new GridPos(0, y), TileType.Wall);
                map.Set(new GridPos(map.Width - 1, y), TileType.Wall);
            }

            // ④ 방마다 테두리 안쪽 바닥 칸 하나를 기준점으로
            var waypoints = new List<GridPos>();
            var waypointRooms = new List<List<GridPos>>();
            foreach (var cells in rooms)
            {
                var usable = cells.FindAll(p => !map.IsBorder(p) && map.Get(p) == TileType.Floor);
                if (usable.Count == 0) continue;
                waypoints.Add(usable[rng.Range(0, usable.Count)]);
                waypointRooms.Add(usable);
            }
            if (waypoints.Count < 2)
                throw new InvalidOperationException($"기준점이 {waypoints.Count}개뿐이라 시작 · 도착을 정할 수 없다. 수치(BALANCE 2번)를 확인한다.");

            var start = waypoints[0];
            var exit = PickExit(waypoints, waypointRooms, start);

            // ⑤ 기준점 i → i+1 복도
            for (int i = 0; i < waypoints.Count - 1; i++)
                Carve(map, CorridorPathfinder.FindPath(map, waypoints[i], waypoints[i + 1], c.WallTunnelCostMultiplier));

            // ⑥ 시작점과 이어지지 않은 바닥 조각을 시작점까지 잇는다
            ConnectStrayRegions(map, start);

            // ⑦ 남은 빈칸은 벽
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (map.Get(p) == TileType.Empty) map.Set(p, TileType.Wall);
                }
            }

            // ⑦-2 막다른 길 잇기 (2026-10-16): 벽 덩어리와 겹친 방이 한 칸 폭 띠로 남아 생기는 막다른 길을 없앤다
            ConnectDeadEnds(map);

            // ⑧ 도착점
            map.Set(exit, TileType.StairsDown);
            return new DungeonFloor(map, start, exit);
        }

        /// <summary>크기 뽑기: [0, roll)에서 뽑고 min 미만이면 min을 더한다 (이전 MapGenerator 규칙).</summary>
        public static int RollSize(IRng rng, int roll, int min)
        {
            int v = rng.Range(0, roll);
            return v < min ? v + min : v;
        }

        /// <summary>
        /// 사각형 하나 배치. 시작점은 x ∈ [0, Width − min), y ∈ [0, Height − min). 시작점이 벽이면 다시 뽑는다.
        /// roomCells == null → 벽 덩어리(전부 벽). 아니면 방(벽 아닌 칸만 바닥, 칸 목록에 추가).
        /// </summary>
        private void PlaceRect(GridMap map, IRng rng, int roll, int min, List<GridPos> roomCells)
        {
            for (int attempt = 0; attempt < _config.PlacementAttemptLimit; attempt++)
            {
                int px = rng.Range(0, map.Width - min);
                int py = rng.Range(0, map.Height - min);
                int sx = RollSize(rng, roll, min);
                int sy = RollSize(rng, roll, min);

                if (map.Get(new GridPos(px, py)) == TileType.Wall) continue;

                for (int x = px; x < px + sx && x < map.Width; x++)
                {
                    for (int y = py; y < py + sy && y < map.Height; y++)
                    {
                        var p = new GridPos(x, y);
                        if (roomCells == null)
                        {
                            map.Set(p, TileType.Wall);
                        }
                        else if (map.Get(p) != TileType.Wall)
                        {
                            map.Set(p, TileType.Floor);
                            roomCells.Add(p);
                        }
                    }
                }
                return;
            }
        }

        /// <summary>마지막 기준점. 시작점과 같은 칸이면 그 방의 다른 칸, 그래도 없으면 앞 방의 기준점을 쓴다.</summary>
        private static GridPos PickExit(List<GridPos> waypoints, List<List<GridPos>> rooms, GridPos start)
        {
            int last = waypoints.Count - 1;
            if (waypoints[last] != start) return waypoints[last];

            foreach (var p in rooms[last])
            {
                if (p != start) return p;
            }
            for (int i = last - 1; i >= 0; i--)
            {
                if (waypoints[i] != start) return waypoints[i];
            }
            throw new InvalidOperationException("시작점과 다른 도착점을 찾지 못했다.");
        }

        private static void Carve(GridMap map, List<GridPos> path)
        {
            if (path == null) return;
            foreach (var p in path)
            {
                if (!GridMap.IsWalkableType(map.Get(p))) map.Set(p, TileType.Floor);
            }
        }

        private void ConnectStrayRegions(GridMap map, GridPos start)
        {
            while (true)
            {
                var reached = FloodWalkable(map, start);
                GridPos? stray = null;
                for (int y = 0; y < map.Height && stray == null; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        var p = new GridPos(x, y);
                        if (GridMap.IsWalkableType(map.Get(p)) && !reached[y * map.Width + x])
                        {
                            stray = p;
                            break;
                        }
                    }
                }
                if (stray == null) return;

                var path = CorridorPathfinder.FindPath(map, stray.Value, start, _config.WallTunnelCostMultiplier);
                if (path == null)
                    throw new InvalidOperationException($"끊긴 바닥 {stray.Value}을 시작점과 잇지 못했다.");
                Carve(map, path);
            }
        }

        /// <summary>
        /// 막다른 칸 = 걸을 수 있고, 한 걸음(8방향, GridMovement.CanStep)에 갈 수 있는 이웃이 1개 이하인 칸.
        /// </summary>
        public static bool IsDeadEnd(GridMap map, GridPos p)
        {
            if (!map.IsWalkable(p)) return false;
            int exits = 0;
            foreach (var dir in GridMovement.Directions)
            {
                if (GridMovement.CanStep(map, p, p + dir) && ++exits > 1) return false;
            }
            return true;
        }

        /// <summary>막다른 칸이 없어질 때까지, 칸 순서(y → x)대로 하나씩 잇는다.</summary>
        private void ConnectDeadEnds(GridMap map)
        {
            int passLimit = map.Width * map.Height;
            for (int pass = 0; pass < passLimit; pass++)
            {
                bool carved = false;
                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        var p = new GridPos(x, y);
                        if (IsDeadEnd(map, p) && TryLoopDeadEnd(map, p)) carved = true;
                    }
                }
                if (!carved) return;
            }
        }

        /// <summary>
        /// 막다른 칸 d에서 벽을 가장 적게 뚫어(4방향 BFS), 걸어서 DeadEndLoopMinWalkDistance 이상 떨어진 칸과 잇는다.
        /// 먼저 자기 길 옆에 붙어 가지 않는 굴만 찾고, 없으면 그 제한 없이 찾는다. 테두리는 뚫지 않는다.
        /// </summary>
        private bool TryLoopDeadEnd(GridMap map, GridPos d)
        {
            var near = WalkDistanceWithin(map, d, _config.DeadEndLoopMinWalkDistance - 1);
            return TryDig(map, d, near, avoidNear: true) || TryDig(map, d, near, avoidNear: false);
        }

        private static bool TryDig(GridMap map, GridPos d, bool[] near, bool avoidNear)
        {
            int w = map.Width;
            var parent = new int[w * map.Height];
            for (int i = 0; i < parent.Length; i++) parent[i] = -2; // -2 미방문, -1 시작(d 옆)

            var queue = new Queue<GridPos>();
            foreach (var dir in Directions)
            {
                var n = d + dir;
                if (!CanDig(map, n, d, near, avoidNear)) continue;
                parent[n.Y * w + n.X] = -1;
                queue.Enqueue(n);
            }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var dir in Directions)
                {
                    var n = cell + dir;
                    if (!map.InBounds(n)) continue;
                    int ni = n.Y * w + n.X;
                    if (map.IsWalkable(n))
                    {
                        if (near[ni]) continue;
                        // 찾음: cell부터 d 옆까지 거슬러 올라가며 뚫는다
                        for (int i = cell.Y * w + cell.X; i >= 0; i = parent[i])
                            map.Set(new GridPos(i % w, i / w), TileType.Floor);
                        return true;
                    }
                    if (parent[ni] != -2 || !CanDig(map, n, d, near, avoidNear)) continue;
                    parent[ni] = cell.Y * w + cell.X;
                    queue.Enqueue(n);
                }
            }
            return false;
        }

        /// <summary>뚫을 수 있는 칸: 맵 안쪽의 걸을 수 없는 칸. avoidNear면 d 말고 가까운 자기 길 칸에 4방향으로 붙은 칸은 제외.</summary>
        private static bool CanDig(GridMap map, GridPos p, GridPos d, bool[] near, bool avoidNear)
        {
            if (!map.InBounds(p) || map.IsBorder(p) || map.IsWalkable(p)) return false;
            if (!avoidNear) return true;
            foreach (var dir in Directions)
            {
                var n = p + dir;
                if (n == d || !map.InBounds(n)) continue;
                if (near[n.Y * map.Width + n.X]) return false;
            }
            return true;
        }

        /// <summary>start에서 8방향으로 maxSteps걸음 안에 닿는 칸.</summary>
        private static bool[] WalkDistanceWithin(GridMap map, GridPos start, int maxSteps)
        {
            var dist = new int[map.Width * map.Height];
            for (int i = 0; i < dist.Length; i++) dist[i] = -1;
            var within = new bool[dist.Length];

            var queue = new Queue<GridPos>();
            dist[start.Y * map.Width + start.X] = 0;
            within[start.Y * map.Width + start.X] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                int pd = dist[p.Y * map.Width + p.X];
                if (pd >= maxSteps) continue;
                foreach (var dir in GridMovement.Directions)
                {
                    var n = p + dir;
                    if (!GridMovement.CanStep(map, p, n)) continue;
                    int ni = n.Y * map.Width + n.X;
                    if (dist[ni] >= 0) continue;
                    dist[ni] = pd + 1;
                    within[ni] = true;
                    queue.Enqueue(n);
                }
            }
            return within;
        }

        /// <summary>start에서 4방향으로 걸어서 닿는 칸.</summary>
        public static bool[] FloodWalkable(GridMap map, GridPos start)
        {
            var reached = new bool[map.Width * map.Height];
            if (!map.IsWalkable(start)) return reached;

            var queue = new Queue<GridPos>();
            queue.Enqueue(start);
            reached[start.Y * map.Width + start.X] = true;
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var dir in Directions)
                {
                    var n = p + dir;
                    if (!map.IsWalkable(n)) continue;
                    int i = n.Y * map.Width + n.X;
                    if (reached[i]) continue;
                    reached[i] = true;
                    queue.Enqueue(n);
                }
            }
            return reached;
        }
    }
}
