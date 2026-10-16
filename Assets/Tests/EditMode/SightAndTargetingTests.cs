using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;

namespace ProjectM.Tests
{
    public class SightAndTargetingTests
    {
        private static bool[] Fov(GridMap map, GridPos origin, int radius)
        {
            var visible = new bool[map.Width * map.Height];
            FieldOfView.Compute(map, origin, radius, visible);
            return visible;
        }

        private static bool At(GridMap map, bool[] cells, int x, int y) => cells[y * map.Width + x];

        private static GridMap OpenRoom(int size)
        {
            var rows = new string[size];
            for (int r = 0; r < size; r++)
                rows[r] = (r == 0 || r == size - 1) ? new string('#', size) : "#" + new string('.', size - 2) + "#";
            return GridMovementTests.Parse(rows);
        }

        [Test]
        public void OpenRoom_VisibleExactlyInsideCircle()
        {
            var map = OpenRoom(13);
            var origin = new GridPos(6, 6);
            var visible = Fov(map, origin, 3);

            for (int y = 1; y < 12; y++)
            {
                for (int x = 1; x < 12; x++)
                {
                    bool expected = FieldOfView.InRadius(x - 6, y - 6, 3);
                    Assert.That(At(map, visible, x, y), Is.EqualTo(expected), $"({x}, {y})");
                }
            }
        }

        [Test]
        public void Pillar_HidesCellBehind_WallItselfVisible()
        {
            var map = GridMovementTests.Parse(
                "#########",
                "#.......#",
                "#.......#",
                "#..#....#",
                "#.......#",
                "#.......#",
                "#########");
            // 원점 (1,3), 기둥 (3,3)
            var visible = Fov(map, new GridPos(1, 3), 8);
            Assert.That(At(map, visible, 3, 3), Is.True, "기둥 자체는 보인다");
            Assert.That(At(map, visible, 4, 3), Is.False, "기둥 바로 뒤");
            Assert.That(At(map, visible, 6, 3), Is.False, "기둥 뒤 일직선");
            Assert.That(At(map, visible, 6, 5), Is.True, "비스듬히 비켜난 칸은 보인다");
            Assert.That(At(map, visible, 0, 3), Is.True, "방 벽(윤곽)도 보인다");
            Assert.That(At(map, visible, 1, 3), Is.True, "원점");
        }

        [Test]
        public void ClosedDoorlessWall_BlocksNextRoom()
        {
            var map = GridMovementTests.Parse(
                "#########",
                "#...#...#",
                "#...#...#",
                "#...#...#",
                "#########");
            var visible = Fov(map, new GridPos(2, 2), 8);
            Assert.That(At(map, visible, 4, 2), Is.True, "칸막이 벽");
            for (int y = 1; y <= 3; y++)
                for (int x = 5; x <= 7; x++)
                    Assert.That(At(map, visible, x, y), Is.False, $"옆방 ({x}, {y})");
        }

        // 대칭: a에서 b가 보이면 b에서도 a가 보인다 (바닥 칸끼리) — 시드 고정 무작위 맵 3장
        [Test]
        public void RandomMaps_SightIsSymmetric()
        {
            const int size = 20, radius = 8;
            for (ulong seed = 1; seed <= 3; seed++)
            {
                var rng = new RngSet(seed).Get(RngStream.MapGen);
                var map = new GridMap(size, size);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        bool border = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                        map.Set(new GridPos(x, y), border || rng.Range(0, 100) < 30 ? TileType.Wall : TileType.Floor);
                    }

                var floors = new List<GridPos>();
                var fov = new Dictionary<GridPos, bool[]>();
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        var p = new GridPos(x, y);
                        if (!map.IsWalkable(p)) continue;
                        floors.Add(p);
                        fov[p] = Fov(map, p, radius);
                    }

                foreach (var a in floors)
                    foreach (var b in floors)
                    {
                        bool ab = fov[a][b.Y * size + b.X];
                        bool ba = fov[b][a.Y * size + a.X];
                        if (ab != ba) Assert.Fail($"시드 {seed}: {a} → {b} = {ab}, 반대 = {ba}");
                    }
            }
        }

        [Test]
        public void CanSee_MatchesCompute_AndRespectsRadius()
        {
            var map = OpenRoom(13);
            Assert.That(FieldOfView.CanSee(map, new GridPos(1, 1), new GridPos(4, 1), 3), Is.True);
            Assert.That(FieldOfView.CanSee(map, new GridPos(1, 1), new GridPos(5, 1), 3), Is.False, "반지름 밖");
            Assert.That(FieldOfView.CanSee(map, new GridPos(1, 1), new GridPos(1, 1), 0), Is.True, "자기 칸");
        }

        // ── 사거리 · 조준선 ──

        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();

        [Test]
        public void InRange_UsesChebyshev()
        {
            Assert.That(Targeting.InRange(new GridPos(0, 0), new GridPos(3, 3), 3), Is.True);
            Assert.That(Targeting.InRange(new GridPos(0, 0), new GridPos(4, 1), 3), Is.False);
        }

        [Test]
        public void Trace_ReachesTargetInOpenRoom()
        {
            var map = OpenRoom(13);
            var path = Targeting.Trace(map, new GridPos(1, 1), new GridPos(7, 4), 10, null);
            Assert.That(path[path.Count - 1], Is.EqualTo(new GridPos(7, 4)));
            Assert.That(path.Count, Is.EqualTo(6), "체비셰프 거리만큼 칸을 지난다");
        }

        [Test]
        public void Trace_StopsBeforeWall_AtUnit_AndAtRange()
        {
            var map = GridMovementTests.Parse(
                "#########",
                "#.......#",
                "#....#..#",
                "#.......#",
                "#########");
            var from = new GridPos(1, 2);

            var wallPath = Targeting.Trace(map, from, new GridPos(7, 2), 10, null);
            Assert.That(wallPath, Is.EqualTo(new List<GridPos> { new GridPos(2, 2), new GridPos(3, 2), new GridPos(4, 2) }), "벽 (5,2) 직전에서 멈춤");

            var unitPath = Targeting.Trace(map, from, new GridPos(4, 2), 10, p => p == new GridPos(3, 2));
            Assert.That(unitPath[unitPath.Count - 1], Is.EqualTo(new GridPos(3, 2)), "유닛 칸에서 멈춤 (맞는 대상)");

            var shortPath = Targeting.Trace(map, from, new GridPos(4, 2), 2, null);
            Assert.That(shortPath.Count, Is.EqualTo(2), "사거리 2");
        }

        [Test]
        public void Trace_BlockedByDiagonalGapBetweenTwoWalls()
        {
            var map = GridMovementTests.Parse(
                "#####",
                "#.#.#",
                "##..#",
                "#...#",
                "#####");
            // (1,3)에서 첫 칸 (2,2)로: 양옆 (2,3) · (1,2)가 모두 벽인 틈 → 막힘
            var path = Targeting.Trace(map, new GridPos(1, 3), new GridPos(3, 1), 10, null);
            Assert.That(path, Is.Empty);

            // 한쪽만 벽이면 지나간다: (3,3) → (2,2)는 (2,3) 벽, (3,2) 바닥
            var ok = Targeting.Trace(map, new GridPos(3, 3), new GridPos(1, 1), 10, null);
            Assert.That(ok[ok.Count - 1], Is.EqualTo(new GridPos(1, 1)));
        }
    }
}
