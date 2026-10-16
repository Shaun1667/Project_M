using System;
using System.Linq;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using UnityEditor;

namespace ProjectM.Tests
{
    public class DungeonGeneratorTests
    {
        private const string AssetPath = "Assets/_Project/Data/Dungeon/DungeonGenConfig.asset";

        /// <summary>BALANCE.md 2번 표와 같은 값.</summary>
        private static DungeonGenConfig BalanceConfig() => new DungeonGenConfig(
            width: 50, height: 50,
            roomCount: 10, roomSizeRoll: 20, roomMinSize: 7,
            wallBlockCount: 20, wallBlockSizeRoll: 10, wallBlockMinSize: 3,
            wallTunnelCostMultiplier: 5, placementAttemptLimit: 1000,
            deadEndLoopMinWalkDistance: 10);

        private static DungeonFloor Generate(ulong seed) =>
            new DungeonGenerator(BalanceConfig()).Generate(new RngSet(seed).Get(RngStream.MapGen));

        [Test]
        public void SameSeed_SameFloor()
        {
            var a = Generate(42UL);
            var b = Generate(42UL);
            Assert.That(b.Map.CopyCells(), Is.EqualTo(a.Map.CopyCells()));
            Assert.That(b.Start, Is.EqualTo(a.Start));
            Assert.That(b.Exit, Is.EqualTo(a.Exit));
        }

        [Test]
        public void DifferentSeeds_DifferentFloors()
        {
            Assert.That(Generate(2UL).Map.CopyCells(), Is.Not.EqualTo(Generate(1UL).Map.CopyCells()));
        }

        // 1-5 핵심: 모든 걸을 수 있는 칸이 시작점과 이어진다 (이전 코드의 "연결 끊김" 수정)
        [Test]
        public void ManySeeds_AllWalkableCellsConnectedToStart()
        {
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var floor = Generate(seed);
                var map = floor.Map;
                var reached = DungeonGenerator.FloodWalkable(map, floor.Start);

                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        if (map.IsWalkable(new GridPos(x, y)) && !reached[y * map.Width + x])
                            Assert.Fail($"시드 {seed}: ({x}, {y})가 시작점 {floor.Start}과 끊겨 있다");
                    }
                }
            }
        }

        // 2026-10-16: 막다른 길이 없다 — 모든 칸이 한 걸음에 갈 수 있는 이웃을 2개 이상 가진다
        [Test]
        public void ManySeeds_NoDeadEnds()
        {
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var map = Generate(seed).Map;
                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        if (DungeonGenerator.IsDeadEnd(map, new GridPos(x, y)))
                            Assert.Fail($"시드 {seed}: ({x}, {y})가 막다른 길이다");
                    }
                }
            }
        }

        [Test]
        public void IsDeadEnd_CorridorTipOnly()
        {
            var map = GridMovementTests.Parse(
                "#######",
                "#.....#",
                "#.....#",
                "###.###",
                "###.###",
                "#######");
            Assert.That(DungeonGenerator.IsDeadEnd(map, new GridPos(3, 1)), Is.True, "복도 끝");
            Assert.That(DungeonGenerator.IsDeadEnd(map, new GridPos(3, 2)), Is.False, "복도 중간");
            Assert.That(DungeonGenerator.IsDeadEnd(map, new GridPos(1, 4)), Is.False, "방 모서리");
            Assert.That(DungeonGenerator.IsDeadEnd(map, new GridPos(0, 0)), Is.False, "벽");
        }

        [Test]
        public void ManySeeds_BorderWall_StartFloor_SingleStairsAtExit()
        {
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var floor = Generate(seed);
                var map = floor.Map;
                var cells = map.CopyCells();

                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        var p = new GridPos(x, y);
                        if (map.IsBorder(p))
                            Assert.That(map.Get(p), Is.EqualTo(TileType.Wall), $"시드 {seed}: 테두리 {p}");
                    }
                }

                Assert.That(cells.Contains(TileType.Empty), Is.False, $"시드 {seed}: 빈칸이 남음");
                Assert.That(map.Get(floor.Start), Is.EqualTo(TileType.Floor), $"시드 {seed}: 시작점");
                Assert.That(map.Get(floor.Exit), Is.EqualTo(TileType.StairsDown), $"시드 {seed}: 도착점");
                Assert.That(cells.Count(t => t == TileType.StairsDown), Is.EqualTo(1), $"시드 {seed}: 계단 수");
                Assert.That(floor.Exit, Is.Not.EqualTo(floor.Start), $"시드 {seed}: 시작 = 도착");
            }
        }

        // 의도된 분포 유지: [min, roll) 범위, [min, 2·min)이 [2·min, roll)의 칸당 두 배
        [Test]
        public void RollSize_KeepsIntendedDistribution()
        {
            var rng = new Pcg32(5UL, 1UL);
            var counts = new int[20];
            const int samples = 40000;
            for (int i = 0; i < samples; i++)
                counts[DungeonGenerator.RollSize(rng, 20, 7)]++;

            for (int v = 0; v < 7; v++)
                Assert.That(counts[v], Is.EqualTo(0), "최소 7 미만은 나오지 않음");

            double low = counts.Skip(7).Take(7).Average();   // 7~13
            double high = counts.Skip(14).Take(6).Average(); // 14~19
            Assert.That(low / high, Is.InRange(1.8, 2.2));
        }

        [Test]
        public void Pathfinder_GoesAroundWallWhenCheaper()
        {
            var map = new GridMap(7, 5);
            map.Set(new GridPos(3, 1), TileType.Wall);
            map.Set(new GridPos(3, 2), TileType.Wall); // (3, 3)은 열림

            var path = CorridorPathfinder.FindPath(map, new GridPos(1, 2), new GridPos(5, 2), 5);

            Assert.That(path.First(), Is.EqualTo(new GridPos(1, 2)));
            Assert.That(path.Last(), Is.EqualTo(new GridPos(5, 2)));
            Assert.That(path.Any(p => map.Get(p) == TileType.Wall), Is.False);
            Assert.That(path.Any(map.IsBorder), Is.False, "테두리를 지나지 않음");
        }

        [Test]
        public void Pathfinder_TunnelsWhenBlocked()
        {
            var map = new GridMap(7, 5);
            for (int y = 1; y <= 3; y++) map.Set(new GridPos(3, y), TileType.Wall);

            var path = CorridorPathfinder.FindPath(map, new GridPos(1, 2), new GridPos(5, 2), 5);

            Assert.That(path, Is.Not.Null);
            Assert.That(path.Count(p => map.Get(p) == TileType.Wall), Is.EqualTo(1), "벽은 한 칸만 뚫는다");
            Assert.That(path.Any(map.IsBorder), Is.False);
        }

        [Test]
        public void Config_RejectsInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DungeonGenConfig(50, 50, 10, 7, 7, 20, 10, 3, 5, 1000, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DungeonGenConfig(50, 50, 1, 20, 7, 20, 10, 3, 5, 1000, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DungeonGenConfig(8, 8, 10, 20, 7, 20, 10, 3, 5, 1000, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DungeonGenConfig(50, 50, 10, 20, 7, 20, 10, 3, 0, 1000, 10));
        }

        [Test]
        public void WorldCreate_ConsumesMapGenStream()
        {
            // 맵 생성에 쓴 난수 소비가 World의 MapGen 수열에 남아야 다음 층이 같은 맵을 반복하지 않는다
            var generator = new DungeonGenerator(BalanceConfig());
            var world = World.Create(9UL, rng => generator.Generate(rng).Map);
            var fresh = new RngSet(9UL).Get(RngStream.MapGen);
            Assert.That(world.Rng.Get(RngStream.MapGen).NextUInt(), Is.Not.EqualTo(fresh.NextUInt()));
        }

        // 검산: 프로젝트의 DungeonGenConfig 에셋이 BALANCE.md 2번 표와 같은 값인지
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DungeonGenConfigAsset>(AssetPath);
            Assert.That(asset, Is.Not.Null, AssetPath + " 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");

            var c = asset.ToConfig();
            var expected = BalanceConfig();
            Assert.That(c.Width, Is.EqualTo(expected.Width));
            Assert.That(c.Height, Is.EqualTo(expected.Height));
            Assert.That(c.RoomCount, Is.EqualTo(expected.RoomCount));
            Assert.That(c.RoomSizeRoll, Is.EqualTo(expected.RoomSizeRoll));
            Assert.That(c.RoomMinSize, Is.EqualTo(expected.RoomMinSize));
            Assert.That(c.WallBlockCount, Is.EqualTo(expected.WallBlockCount));
            Assert.That(c.WallBlockSizeRoll, Is.EqualTo(expected.WallBlockSizeRoll));
            Assert.That(c.WallBlockMinSize, Is.EqualTo(expected.WallBlockMinSize));
            Assert.That(c.WallTunnelCostMultiplier, Is.EqualTo(expected.WallTunnelCostMultiplier));
            Assert.That(c.PlacementAttemptLimit, Is.EqualTo(expected.PlacementAttemptLimit));
            Assert.That(c.DeadEndLoopMinWalkDistance, Is.EqualTo(expected.DeadEndLoopMinWalkDistance));
        }
    }
}
