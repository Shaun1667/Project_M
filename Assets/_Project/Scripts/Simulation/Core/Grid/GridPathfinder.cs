using System;
using System.Collections.Generic;

namespace ProjectM.Core
{
    /// <summary>
    /// 8방향 A* (GridMovement.CanStep 규칙). 한 걸음 = 1턴이므로 걸음 수가 가장 적은 경로를 찾고,
    /// 걸음 수가 같으면 대각선이 적은 경로를 고른다 (곧은 길 선호). 동점은 고정 순서로 처리해 결정론적이다. 클릭 이동이 쓴다.
    /// 추적용 한 걸음은 ChaseStep — 걸음 수가 같으면 목표와의 직선 거리가 가장 가까워지는 칸 (대각선으로 따라붙음).
    /// </summary>
    public static class GridPathfinder
    {
        // 걸음 비용 = Step, 대각선이면 +1. 대각선 가산이 Step보다 훨씬 작아 걸음 수가 항상 우선한다.
        private const int Step = 1000;
        private const int DiagonalExtra = 1;

        /// <summary>
        /// 추적용 한 걸음 (2026-10-16): from에서 갈 수 있는 이웃 중 goal까지 남은 걸음 수가 가장 적은 칸.
        /// 같으면 goal과의 직선 거리(dx² + dy²)가 가장 짧은 칸, 그래도 같으면 GridMovement.Directions 순서.
        /// 예: 바로 옆 플레이어가 대각선으로 물러나면 적도 대각선으로 따라간다.
        /// isBlocked인 칸은 지나지 않는다 (goal 칸은 예외). 길이 없으면 null.
        /// </summary>
        public static GridPos? ChaseStep(GridMap map, GridPos from, GridPos goal, Func<GridPos, bool> isBlocked = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (from == goal || !map.IsWalkable(goal)) return null;

            // goal에서 거꾸로 걸음 수 (CanStep은 걸을 수 있는 두 칸 사이에서 대칭)
            int w = map.Width;
            var dist = new int[w * map.Height];
            for (int i = 0; i < dist.Length; i++) dist[i] = int.MaxValue;
            var queue = new Queue<GridPos>();
            dist[goal.Y * w + goal.X] = 0;
            queue.Enqueue(goal);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                int pd = dist[p.Y * w + p.X];
                for (int d = 0; d < GridMovement.Directions.Count; d++)
                {
                    var n = p + GridMovement.Directions[d];
                    if (!GridMovement.CanStep(map, p, n)) continue;
                    int ni = n.Y * w + n.X;
                    if (dist[ni] != int.MaxValue) continue;
                    if (n != from && isBlocked != null && isBlocked(n)) continue;
                    dist[ni] = pd + 1;
                    if (n != from) queue.Enqueue(n);
                }
            }

            GridPos? best = null;
            int bestDist = int.MaxValue, bestEuclid = int.MaxValue;
            for (int d = 0; d < GridMovement.Directions.Count; d++)
            {
                var n = from + GridMovement.Directions[d];
                if (!GridMovement.CanStep(map, from, n)) continue;
                if (n != goal && isBlocked != null && isBlocked(n)) continue;
                int nd = dist[n.Y * w + n.X];
                if (nd == int.MaxValue) continue;
                int ex = n.X - goal.X, ey = n.Y - goal.Y;
                int euclid = ex * ex + ey * ey;
                if (nd < bestDist || (nd == bestDist && euclid < bestEuclid))
                {
                    best = n;
                    bestDist = nd;
                    bestEuclid = euclid;
                }
            }
            return best;
        }

        /// <summary>
        /// from → goal 경로 (from 제외, goal 포함). 못 가면 null, from == goal이면 빈 목록.
        /// isBlocked는 중간 칸(유닛 등)을 막는다 — goal 칸에는 적용하지 않는다 (공격 대상 칸까지의 경로를 찾을 수 있게).
        /// </summary>
        public static List<GridPos> FindPath(GridMap map, GridPos from, GridPos goal, Func<GridPos, bool> isBlocked = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (!map.InBounds(from) || !map.IsWalkable(goal)) return null;
            if (from == goal) return new List<GridPos>();

            int count = map.Width * map.Height;
            var g = new int[count];
            var parent = new int[count];
            var closed = new bool[count];
            for (int i = 0; i < count; i++)
            {
                g[i] = int.MaxValue;
                parent[i] = -1;
            }

            int start = Index(map, from);
            int goalIndex = Index(map, goal);
            g[start] = 0;
            var open = new MinHeap();
            long sequence = 0;
            open.Push(new Node(start, Heuristic(from, goal), Heuristic(from, goal), sequence++));

            while (open.Count > 0)
            {
                var node = open.Pop();
                int current = node.Index;
                if (closed[current]) continue;
                closed[current] = true;

                if (current == goalIndex) return BuildPath(map, parent, start, goalIndex);

                var currentPos = new GridPos(current % map.Width, current / map.Width);
                for (int d = 0; d < GridMovement.Directions.Count; d++)
                {
                    var dir = GridMovement.Directions[d];
                    var next = currentPos + dir;
                    if (!GridMovement.CanStep(map, currentPos, next)) continue;
                    int ni = Index(map, next);
                    if (closed[ni]) continue;
                    if (ni != goalIndex && isBlocked != null && isBlocked(next)) continue;

                    int cost = g[current] + Step + (GridMovement.IsDiagonal(dir) ? DiagonalExtra : 0);
                    if (cost >= g[ni]) continue;
                    g[ni] = cost;
                    parent[ni] = current;
                    int h = Heuristic(next, goal);
                    open.Push(new Node(ni, cost + h, h, sequence++));
                }
            }

            return null;
        }

        private static int Index(GridMap map, GridPos pos) => pos.Y * map.Width + pos.X;

        // 체비셰프 거리 × Step — 대각선 가산을 무시하므로 과대평가하지 않는다.
        private static int Heuristic(GridPos a, GridPos b) => GridMovement.Chebyshev(a, b) * Step;

        private static List<GridPos> BuildPath(GridMap map, int[] parent, int start, int goal)
        {
            var path = new List<GridPos>();
            for (int i = goal; i != start; i = parent[i])
                path.Add(new GridPos(i % map.Width, i / map.Width));
            path.Reverse();
            return path;
        }

        private readonly struct Node
        {
            public readonly int Index;
            public readonly int F;
            public readonly int H;
            public readonly long Sequence;

            public Node(int index, int f, int h, long sequence)
            {
                Index = index;
                F = f;
                H = h;
                Sequence = sequence;
            }

            // F 최소 → H 최소 → 먼저 넣은 것
            public bool IsBefore(in Node other)
            {
                if (F != other.F) return F < other.F;
                if (H != other.H) return H < other.H;
                return Sequence < other.Sequence;
            }
        }

        private sealed class MinHeap
        {
            private readonly List<Node> _items = new List<Node>();

            public int Count => _items.Count;

            public void Push(Node node)
            {
                _items.Add(node);
                int i = _items.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (!_items[i].IsBefore(_items[p])) break;
                    (_items[i], _items[p]) = (_items[p], _items[i]);
                    i = p;
                }
            }

            public Node Pop()
            {
                var top = _items[0];
                int last = _items.Count - 1;
                _items[0] = _items[last];
                _items.RemoveAt(last);

                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, best = i;
                    if (l < _items.Count && _items[l].IsBefore(_items[best])) best = l;
                    if (r < _items.Count && _items[r].IsBefore(_items[best])) best = r;
                    if (best == i) break;
                    (_items[i], _items[best]) = (_items[best], _items[i]);
                    i = best;
                }
                return top;
            }
        }
    }
}
