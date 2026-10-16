using System;
using System.Collections.Generic;

namespace ProjectM.Core
{
    /// <summary>
    /// 사거리 · 조준선 판정 (1-7, CLAUDE.md: 바뀔 수 있는 판정은 인터페이스 뒤에). 원거리 공격 · 마법(Phase 2)이 쓴다.
    /// 상태를 갖지 않는다. 그리드 계산만 쓴다 (규칙 2).
    /// </summary>
    public interface ITargetingRule
    {
        /// <summary>to가 사거리 안인지 (지형 무시).</summary>
        bool InRange(GridPos from, GridPos to, int range);

        /// <summary>
        /// from → to 방향으로 날아가는 칸 목록 (from 제외). to에 닿거나, 사거리를 넘거나, 막히면 멈춘다.
        /// - 벽 칸 직전에서 멈춘다 (벽 칸은 포함 안 함).
        /// - isOccupied가 true인 칸(유닛)에서 멈춘다 (그 칸 포함 — 맞는 대상).
        /// </summary>
        List<GridPos> Trace(GridMap map, GridPos from, GridPos to, int range, Func<GridPos, bool> isOccupied);
    }

    /// <summary>
    /// 1-7 판정 (결정 2026-10-16): 사거리는 체비셰프 거리(이동 걸음 수와 같음), 조준선은 브레젠험 직선.
    /// 대각선 한 칸이 양옆 모두 벽인 틈을 지나면 막힌다 (이동 규칙 GridMovement.CanStep과 같은 모서리 규칙).
    /// 조준선과 시야(FieldOfView)는 다른 계산이라 드물게 "보이지만 못 맞히는" 칸이 있을 수 있다.
    /// </summary>
    public sealed class BresenhamTargetingRule : ITargetingRule
    {
        public bool InRange(GridPos from, GridPos to, int range) => GridMovement.Chebyshev(from, to) <= range;

        public List<GridPos> Trace(GridMap map, GridPos from, GridPos to, int range, Func<GridPos, bool> isOccupied)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            var cells = new List<GridPos>();
            if (from == to) return cells;

            int dx = Math.Abs(to.X - from.X), sx = from.X < to.X ? 1 : -1;
            int dy = -Math.Abs(to.Y - from.Y), sy = from.Y < to.Y ? 1 : -1;
            int err = dx + dy;
            int x = from.X, y = from.Y;

            while (true)
            {
                var prev = new GridPos(x, y);
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x += sx; }
                if (e2 <= dx) { err += dx; y += sy; }
                var cell = new GridPos(x, y);

                if (GridMovement.Chebyshev(from, cell) > range) return cells;
                if (map.BlocksSight(cell)) return cells;
                if (cell.X != prev.X && cell.Y != prev.Y
                    && map.BlocksSight(new GridPos(cell.X, prev.Y)) && map.BlocksSight(new GridPos(prev.X, cell.Y)))
                    return cells; // 양옆이 벽인 대각선 틈

                cells.Add(cell);
                if (cell == to) return cells;
                if (isOccupied != null && isOccupied(cell)) return cells;
            }
        }
    }
}
