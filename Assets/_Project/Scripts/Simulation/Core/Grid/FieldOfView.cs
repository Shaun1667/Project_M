using System;

namespace ProjectM.Core
{
    /// <summary>
    /// 대칭 그림자 투사 시야 (Symmetric Shadowcasting, Albert Ford 방식 — 결정 2026-10-16).
    /// - 대칭: a에서 b가 보이면 b에서도 a가 보인다 (바닥 칸끼리).
    /// - 시야를 막는 칸: 벽 · 빈칸 · 맵 밖 (GridMap.BlocksSight). 막는 칸 자체는 보인다 (방 윤곽).
    /// - 범위: 원형, dx² + dy² ≤ r² + r (반지름 r).
    /// 기울기는 정수 분수(분자/분모)로만 계산해 실수 오차가 없고 결정론적이다. 그리드 계산만 쓴다 (규칙 2).
    /// </summary>
    public static class FieldOfView
    {
        /// <summary>반지름 r 원 안인지 (원점 기준 상대 좌표).</summary>
        public static bool InRadius(int dx, int dy, int radius) => dx * dx + dy * dy <= radius * radius + radius;

        /// <summary>
        /// origin에서 보이는 칸을 visible(인덱스 = y * Width + x)에 true로 표시한다. visible은 먼저 비운다.
        /// </summary>
        public static void Compute(GridMap map, GridPos origin, int radius, bool[] visible)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (visible == null) throw new ArgumentNullException(nameof(visible));
            if (visible.Length != map.Width * map.Height) throw new ArgumentException("visible 크기가 맵과 다르다.", nameof(visible));
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius), radius, "반지름은 0 이상이어야 한다.");

            Array.Clear(visible, 0, visible.Length);
            if (!map.InBounds(origin)) return;
            visible[origin.Y * map.Width + origin.X] = true;

            for (int quadrant = 0; quadrant < 4; quadrant++)
            {
                var scan = new Scanner(map, origin, radius, quadrant, visible);
                scan.Scan(1, -1, 1, 1, 1);
            }
        }

        /// <summary>from에서 to가 보이는지. 대칭이므로 CanSee(a, b) == CanSee(b, a) (바닥 칸끼리).</summary>
        public static bool CanSee(GridMap map, GridPos from, GridPos to, int radius)
        {
            if (!InRadius(to.X - from.X, to.Y - from.Y, radius)) return false;
            if (from == to) return true;
            var visible = new bool[map.Width * map.Height];
            Compute(map, from, radius, visible);
            return map.InBounds(to) && visible[to.Y * map.Width + to.X];
        }

        /// <summary>한 사분면(북 · 동 · 남 · 서) 스캔. depth = 원점에서 멀어지는 줄, col = 줄 안의 가로 위치.</summary>
        private readonly struct Scanner
        {
            private readonly GridMap _map;
            private readonly GridPos _origin;
            private readonly int _radius;
            private readonly int _quadrant;
            private readonly bool[] _visible;

            public Scanner(GridMap map, GridPos origin, int radius, int quadrant, bool[] visible)
            {
                _map = map;
                _origin = origin;
                _radius = radius;
                _quadrant = quadrant;
                _visible = visible;
            }

            /// <summary>
            /// depth 줄을 [start, end] 기울기 범위로 훑는다. 기울기 = 분자/분모 (분모 &gt; 0).
            /// </summary>
            public void Scan(int depth, int startNum, int startDen, int endNum, int endDen)
            {
                if (depth > _radius) return;

                int minCol = RoundTiesUp(depth * startNum, startDen);
                int maxCol = RoundTiesDown(depth * endNum, endDen);

                bool hasPrev = false;
                bool prevWall = false;
                for (int col = minCol; col <= maxCol; col++)
                {
                    var pos = Transform(depth, col);
                    bool wall = _map.BlocksSight(pos);

                    // 벽은 범위 안이면 보이고, 바닥은 대칭 조건(줄 안의 기울기 범위)을 만족할 때만 보인다
                    if (wall || IsSymmetric(depth, col, startNum, startDen, endNum, endDen))
                        Reveal(pos, depth, col);

                    if (hasPrev && prevWall && !wall)
                    {
                        // 새 시작 기울기 = (2·col − 1) / (2·depth)
                        startNum = 2 * col - 1;
                        startDen = 2 * depth;
                    }
                    if (hasPrev && !prevWall && wall)
                    {
                        // 벽을 만나기 전까지의 구간을 다음 줄에서 계속
                        Scan(depth + 1, startNum, startDen, 2 * col - 1, 2 * depth);
                    }

                    hasPrev = true;
                    prevWall = wall;
                }

                if (hasPrev && !prevWall)
                    Scan(depth + 1, startNum, startDen, endNum, endDen);
            }

            private void Reveal(GridPos pos, int depth, int col)
            {
                if (!_map.InBounds(pos)) return;
                if (!InRadius(depth, col, _radius)) return;
                _visible[pos.Y * _map.Width + pos.X] = true;
            }

            private GridPos Transform(int depth, int col)
            {
                switch (_quadrant)
                {
                    case 0: return new GridPos(_origin.X + col, _origin.Y + depth);   // 북
                    case 1: return new GridPos(_origin.X + depth, _origin.Y - col);   // 동
                    case 2: return new GridPos(_origin.X - col, _origin.Y - depth);   // 남
                    default: return new GridPos(_origin.X - depth, _origin.Y + col);  // 서
                }
            }

            // col이 [depth·start, depth·end] 안인지 (분모 > 0이므로 곱해서 비교)
            private static bool IsSymmetric(int depth, int col, int startNum, int startDen, int endNum, int endDen)
            {
                return (long)col * startDen >= (long)depth * startNum && (long)col * endDen <= (long)depth * endNum;
            }

            // floor(n/d + 1/2) = floor((2n + d) / 2d)
            private static int RoundTiesUp(int n, int d) => FloorDiv(2 * n + d, 2 * d);

            // ceil(n/d − 1/2) = ceil((2n − d) / 2d)
            private static int RoundTiesDown(int n, int d) => -FloorDiv(-(2 * n - d), 2 * d);

            private static int FloorDiv(int a, int b)
            {
                int q = a / b;
                if ((a % b != 0) && ((a < 0) != (b < 0))) q--;
                return q;
            }
        }
    }
}
