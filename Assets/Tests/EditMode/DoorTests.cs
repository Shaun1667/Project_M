using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Dungeon;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Tests
{
    /// <summary>3-11c 문 (SPD): 방 입구에 놓기 · 닫히면 시야 가림 · 서면 열림 · 비면 닫힘 · 불에 탐.</summary>
    public class DoorTests
    {
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;

            public void Step(int dx, int dy)
            {
                Assert.That(PlayerActions.TryStep(World, StepConfig, new GridPos(dx, dy), out var step), Is.True);
                Clock.SubmitPlayerCommand(step);
                World.Events.Flush();
            }

            public TileType Tile(int x, int y) => World.Map.Get(new GridPos(x, y));
        }

        /// <summary>
        /// 복도(1,1)~(3,1) — 문 (4,1) — 방 (5..7, 0..2 중 가운데 줄). 좌우가 아니라 위아래가 벽인 길목.
        /// </summary>
        private static Session Start(FireSpreadConfig fire = null)
        {
            var map = GridMovementTests.Parse(
                "#########",
                "#...#...#",
                "#.......#",
                "#...#...#",
                "#########");
            map.Set(new GridPos(4, 2), TileType.Door);
            var world = new World(map, 5UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains(), fireSpread: fire);
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(3, 2), Magic.QuickSlotCount);
            world.Events.Flush();
            return s;
        }

        [Test]
        public void ClosedDoor_BlocksSight_OpenDoorDoesNot()
        {
            var s = Start();
            var map = s.World.Map;
            Assert.That(Sight.CanSee(map, new GridPos(3, 2), new GridPos(4, 2), 8), Is.True, "문 자체는 보인다");
            Assert.That(Sight.CanSee(map, new GridPos(3, 2), new GridPos(6, 2), 8), Is.False, "닫힌 문 너머는 안 보인다");
            Assert.That(map.IsWalkable(new GridPos(4, 2)), Is.True);
            map.Set(new GridPos(4, 2), TileType.OpenDoor);
            Assert.That(Sight.CanSee(map, new GridPos(3, 2), new GridPos(6, 2), 8), Is.True, "열린 문은 안 가림");
        }

        // 들어서면 열리고(같은 틱에 시야가 열림), 나가면 닫힌다
        [Test]
        public void Door_OpensWhileOccupied_ClosesWhenLeft()
        {
            var s = Start();
            s.Step(1, 0);
            Assert.That(s.Player.Pos, Is.EqualTo(new GridPos(4, 2)));
            Assert.That(s.Tile(4, 2), Is.EqualTo(TileType.OpenDoor));
            Assert.That(s.World.Explored[2 * s.World.Map.Width + 6], Is.True, "문 너머 방이 보였다");
            s.Step(1, 0);
            Assert.That(s.Tile(4, 2), Is.EqualTo(TileType.Door), "아무도 없으면 닫힘");
        }

        // 불을 맞거나 번진 불이 닿으면 탄다 → 바닥
        [Test]
        public void Door_Burns()
        {
            var s = Start(new FireSpreadConfig(3, 8));
            s.World.Map.Set(new GridPos(5, 2), TileType.Grass);
            TerrainEffects.Place(s.World, new GridPos(5, 2), TerrainEffectType.FireZone, 3);
            s.Clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
            s.World.Events.Flush();
            Assert.That(s.Tile(4, 2), Is.EqualTo(TileType.Floor), "번진 불에 문이 탐");
            Assert.That(s.World.Terrain.Has(new GridPos(4, 2), TerrainEffectType.FireZone), Is.True);
            var reactions = ReactionTests.BalanceReactions(SpellTests.BalanceStatuses());
            Assert.That(reactions.FindTile(TileType.Door, Element.Fire), Is.Not.Null, "불 주문으로도 점화");
            Assert.That(reactions.FindTile(TileType.OpenDoor, Element.Fire), Is.Not.Null);
        }

        // 문 자리 = 복도가 방에 들어가는 한 칸 폭 길목 (방 안 · 넓은 곳 · 겹문은 아님)
        [Test]
        public void DoorSpots_AreRoomEntrances()
        {
            var map = GridMovementTests.Parse(
                "###########",
                "#####...###",
                "#.....#...#",
                "#####...###",
                "###########");
            var room = new System.Collections.Generic.List<GridPos>();
            for (int x = 5; x <= 7; x++)
            for (int y = 1; y <= 3; y++)
                if (map.Get(new GridPos(x, y)) == TileType.Floor) room.Add(new GridPos(x, y));
            var floor = new DungeonFloor(map, new GridPos(1, 2), new GridPos(9, 2), new[] { (System.Collections.Generic.IReadOnlyList<GridPos>)room });
            var spots = FloorDecor.DoorSpots(map, floor);
            Assert.That(spots, Is.EqualTo(new[] { new GridPos(4, 2), new GridPos(8, 2) }),
                "왼쪽 · 오른쪽 복도가 방에 들어가는 칸만 — 복도 중간 (3,2)과 방 안 칸은 아님");
        }

        // 생성된 층: 문은 모두 좌우 또는 상하가 벽인 칸, 서로 붙지 않음, 걸을 수 있는 칸은 그대로
        [Test]
        public void GeneratedDoors_AreChokepoints()
        {
            var gen = new DungeonGenConfig(width: 50, height: 50, roomCount: 10, roomSizeRoll: 20, roomMinSize: 7,
                wallBlockCount: 20, wallBlockSizeRoll: 10, wallBlockMinSize: 3, wallTunnelCostMultiplier: 5, placementAttemptLimit: 1000,
                deadEndLoopMinWalkDistance: 10);
            int total = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                DungeonFloor floor = null;
                var world = World.Create(seed, rng => (floor = new DungeonGenerator(gen).Generate(rng)).Map);
                FloorDecor.Apply(world, floor, new FloorDecorConfig(0, 1, 1, 0, 0, 1, 1, doorPercent: 100), null);
                var map = world.Map;
                for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (map.Get(p) != TileType.Door) continue;
                    total++;
                    bool horizontal = !map.IsWalkable(p + new GridPos(1, 0)) && !map.IsWalkable(p + new GridPos(-1, 0));
                    bool vertical = !map.IsWalkable(p + new GridPos(0, 1)) && !map.IsWalkable(p + new GridPos(0, -1));
                    Assert.That(horizontal || vertical, Is.True, $"시드 {seed} {p}: 길목");
                    foreach (var dir in GridMovement.Directions)
                        Assert.That(map.Get(p + dir), Is.Not.EqualTo(TileType.Door), $"시드 {seed} {p}: 겹문");
                }
            }
            Assert.That(total, Is.GreaterThan(0), "시드 20개에 문이 하나는 있다");
        }
    }
}
