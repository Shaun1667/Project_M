using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Dungeon
{
    /// <summary>
    /// 복도용 A* (4방향, 위 → 오른쪽 → 아래 → 왼쪽 순 — 이전 MapGenerator와 같은 순서).
    /// 테두리 줄은 지나지 않는다. 벽은 지날 수 있지만 비용이 WallCostMultiplier배라 보통은 돌아간다.
    /// 탐색마다 새 배열을 써서 이전 탐색 값이 남지 않는다 (이전 코드의 G · H · Parent 재사용 버그 수정).
    /// </summary>
    public static class CorridorPathfinder
    {
        private const int StepCost = 10;

        private static readonly GridPos[] Directions =
        {
            new GridPos(0, 1), new GridPos(1, 0), new GridPos(0, -1), new GridPos(-1, 0),
        };

        /// <summary>from → to 경로 (양 끝 포함). 테두리 밖 끝점이면 null.</summary>
        public static List<GridPos> FindPath(GridMap map, GridPos from, GridPos to, int wallCostMultiplier)
        {
            if (!IsInterior(map, from) || !IsInterior(map, to)) return null;

            int count = map.Width * map.Height;
            var g = new int[count];
            var parent = new int[count];
            var state = new byte[count]; // 0 미방문, 1 열림, 2 닫힘
            for (int i = 0; i < count; i++)
            {
                g[i] = int.MaxValue;
                parent[i] = -1;
            }

            int start = Index(map, from);
            int goal = Index(map, to);
            g[start] = 0;
            state[start] = 1;
            var open = new List<int> { start };

            while (open.Count > 0)
            {
                // F 최소, 같으면 H 최소, 같으면 먼저 열린 것 — 결정론적 선택
                int bestSlot = 0;
                int bestF = int.MaxValue, bestH = int.MaxValue;
                for (int s = 0; s < open.Count; s++)
                {
                    int idx = open[s];
                    int h = Heuristic(map, idx, to);
                    int f = g[idx] + h;
                    if (f < bestF || (f == bestF && h < bestH))
                    {
                        bestF = f;
                        bestH = h;
                        bestSlot = s;
                    }
                }

                int current = open[bestSlot];
                open.RemoveAt(bestSlot);
                state[current] = 2;

                if (current == goal) return BuildPath(map, parent, goal);

                var currentPos = new GridPos(current % map.Width, current / map.Width);
                foreach (var dir in Directions)
                {
                    var next = currentPos + dir;
                    if (!IsInterior(map, next)) continue;
                    int ni = Index(map, next);
                    if (state[ni] == 2) continue;

                    int stepCost = map.Get(next) == TileType.Wall ? StepCost * wallCostMultiplier : StepCost;
                    int cost = g[current] + stepCost;
                    if (cost < g[ni])
                    {
                        g[ni] = cost;
                        parent[ni] = current;
                        if (state[ni] == 0)
                        {
                            state[ni] = 1;
                            open.Add(ni);
                        }
                    }
                }
            }

            return null; // 테두리 안쪽은 모두 지날 수 있으므로 정상적으로는 오지 않는다
        }

        private static bool IsInterior(GridMap map, GridPos pos) => map.InBounds(pos) && !map.IsBorder(pos);

        private static int Index(GridMap map, GridPos pos) => pos.Y * map.Width + pos.X;

        private static int Heuristic(GridMap map, int index, GridPos to)
        {
            int x = index % map.Width, y = index / map.Width;
            int dx = x > to.X ? x - to.X : to.X - x;
            int dy = y > to.Y ? y - to.Y : to.Y - y;
            return (dx + dy) * StepCost;
        }

        private static List<GridPos> BuildPath(GridMap map, int[] parent, int goal)
        {
            var path = new List<GridPos>();
            for (int i = goal; i != -1; i = parent[i])
                path.Add(new GridPos(i % map.Width, i / map.Width));
            path.Reverse();
            return path;
        }
    }
}
