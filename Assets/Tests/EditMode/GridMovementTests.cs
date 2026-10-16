using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;

namespace ProjectM.Tests
{
    public class GridMovementTests
    {
        /// <summary>문자열 맵: 첫 줄이 맨 위(y 최대). '#' 벽, '.' 바닥.</summary>
        internal static GridMap Parse(params string[] rows)
        {
            var map = new GridMap(rows[0].Length, rows.Length);
            for (int r = 0; r < rows.Length; r++)
            {
                int y = rows.Length - 1 - r;
                for (int x = 0; x < rows[r].Length; x++)
                    map.Set(new GridPos(x, y), rows[r][x] == '#' ? TileType.Wall : TileType.Floor);
            }
            return map;
        }

        [Test]
        public void Chebyshev_DiagonalCountsAsOne()
        {
            Assert.That(GridMovement.Chebyshev(new GridPos(0, 0), new GridPos(3, 2)), Is.EqualTo(3));
            Assert.That(GridMovement.Chebyshev(new GridPos(1, 1), new GridPos(2, 2)), Is.EqualTo(1));
            Assert.That(GridMovement.Directions.Count, Is.EqualTo(8));
        }

        [Test]
        public void CanStep_EightDirectionsOnOpenFloor()
        {
            var map = Parse(
                "#####",
                "#...#",
                "#...#",
                "#...#",
                "#####");
            var center = new GridPos(2, 2);
            foreach (var dir in GridMovement.Directions)
                Assert.That(GridMovement.CanStep(map, center, center + dir), Is.True, dir.ToString());

            Assert.That(GridMovement.CanStep(map, center, center), Is.False, "제자리");
            Assert.That(GridMovement.CanStep(map, center, new GridPos(2, 4)), Is.False, "두 칸");
        }

        [Test]
        public void CanStep_DiagonalBlockedByWallCorner()
        {
            // (1,1)에서 (2,2)로: 오른쪽 (2,1)이 벽 → 모서리 통과 금지
            var map = Parse(
                "####",
                "#..#",
                "#.##",
                "####");
            Assert.That(GridMovement.CanStep(map, new GridPos(1, 1), new GridPos(2, 2)), Is.False);
            Assert.That(GridMovement.CanStep(map, new GridPos(1, 1), new GridPos(1, 2)), Is.True);
            Assert.That(GridMovement.CanStep(map, new GridPos(1, 1), new GridPos(2, 1)), Is.False, "벽");
        }

        [Test]
        public void FindPath_UsesDiagonalsAndPrefersStraight()
        {
            var map = Parse(
                "#######",
                "#.....#",
                "#.....#",
                "#.....#",
                "#######");
            var diagonal = GridPathfinder.FindPath(map, new GridPos(1, 1), new GridPos(3, 3));
            Assert.That(diagonal, Is.EqualTo(new List<GridPos> { new GridPos(2, 2), new GridPos(3, 3) }));

            var straight = GridPathfinder.FindPath(map, new GridPos(1, 2), new GridPos(5, 2));
            Assert.That(straight, Is.EqualTo(new List<GridPos> { new GridPos(2, 2), new GridPos(3, 2), new GridPos(4, 2), new GridPos(5, 2) }),
                "걸음 수가 같으면 대각선 없는 곧은 길");
        }

        [Test]
        public void FindPath_GoesAroundWallsAndBlockers()
        {
            var map = Parse(
                "#######",
                "#.....#",
                "#.###.#",
                "#.....#",
                "#######");
            var from = new GridPos(1, 2);
            var to = new GridPos(5, 2);
            var path = GridPathfinder.FindPath(map, from, to);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.Count, Is.EqualTo(6), "벽 모서리를 비스듬히 못 지나므로 위 또는 아래 줄로 6걸음");
            AssertValidPath(map, from, path);

            // 위쪽 줄을 막으면 아래로 돈다
            var blocked = new HashSet<GridPos> { new GridPos(3, 3) };
            var around = GridPathfinder.FindPath(map, from, to, blocked.Contains);
            Assert.That(around, Is.Not.Null);
            Assert.That(around, Has.No.Member(new GridPos(3, 3)));
            AssertValidPath(map, from, around);
        }

        [Test]
        public void FindPath_GoalCellIgnoresBlocker_UnreachableReturnsNull()
        {
            var map = Parse(
                "#####",
                "#...#",
                "#####");
            var goal = new GridPos(3, 1);
            var path = GridPathfinder.FindPath(map, new GridPos(1, 1), goal, p => p == goal);
            Assert.That(path, Is.EqualTo(new List<GridPos> { new GridPos(2, 1), goal }), "목표 칸의 유닛은 막지 않음 (공격 대상)");

            Assert.That(GridPathfinder.FindPath(map, new GridPos(1, 1), goal, p => p == new GridPos(2, 1)), Is.Null);
            Assert.That(GridPathfinder.FindPath(map, new GridPos(1, 1), new GridPos(0, 0)), Is.Null, "벽 목표");
            Assert.That(GridPathfinder.FindPath(map, new GridPos(1, 1), new GridPos(1, 1)), Is.Empty);
        }

        private static void AssertValidPath(GridMap map, GridPos from, List<GridPos> path)
        {
            var prev = from;
            foreach (var p in path)
            {
                Assert.That(GridMovement.CanStep(map, prev, p), Is.True, $"{prev} → {p}");
                prev = p;
            }
        }
    }
}
