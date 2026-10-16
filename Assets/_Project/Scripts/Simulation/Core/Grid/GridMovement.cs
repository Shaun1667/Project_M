using System;
using System.Collections.Generic;

namespace ProjectM.Core
{
    /// <summary>
    /// 8방향 한 칸 이동 규칙 (결정 2026-10-16). 거리는 체비셰프 거리(대각선 한 칸 = 1).
    /// 대각선 이동은 양옆 두 칸 중 하나라도 걸을 수 있으면 된다 (변경 2026-10-16). 양옆이 모두 벽인 틈만 막힌다.
    /// 유닛이 칸을 막는지는 여기서 보지 않는다 (지형만).
    /// </summary>
    public static class GridMovement
    {
        /// <summary>8방향. 순서 고정: 위 → 오른쪽 위 → 오른쪽 → … → 왼쪽 위 (시계 방향). 경로 탐색 동점 처리에 쓰인다.</summary>
        public static readonly IReadOnlyList<GridPos> Directions = new[]
        {
            new GridPos(0, 1), new GridPos(1, 1), new GridPos(1, 0), new GridPos(1, -1),
            new GridPos(0, -1), new GridPos(-1, -1), new GridPos(-1, 0), new GridPos(-1, 1),
        };

        public static int Chebyshev(GridPos a, GridPos b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            return dx > dy ? dx : dy;
        }

        public static bool IsDiagonal(GridPos dir) => dir.X != 0 && dir.Y != 0;

        /// <summary>from에서 to로 한 걸음에 갈 수 있는지 (지형만). 인접 칸 · 걸을 수 있음 · 대각선 모서리 규칙.</summary>
        public static bool CanStep(GridMap map, GridPos from, GridPos to)
        {
            if (Chebyshev(from, to) != 1) return false;
            if (!map.IsWalkable(to)) return false;

            var dir = to - from;
            if (!IsDiagonal(dir)) return true;
            return map.IsWalkable(new GridPos(from.X + dir.X, from.Y)) || map.IsWalkable(new GridPos(from.X, from.Y + dir.Y));
        }
    }
}
