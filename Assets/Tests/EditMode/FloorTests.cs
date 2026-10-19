using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Dungeon;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Tests
{
    /// <summary>
    /// 3-4a 층 이동: 계단 칸에 들어가면 다음 · 이전 층, 떠난 층은 멈춘 채 보존, 플레이어 상태는 전부 유지,
    /// 같은 시드 = 같은 층, 스냅샷 v9 왕복, 마지막 층 계단 없음, 적 재생성 (결정 2026-10-18).
    /// </summary>
    public class FloorTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 8, 2, 6);
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        // BALANCE 2번 값 + 층 수
        private static DungeonGenConfig Gen(int floorCount) => new DungeonGenConfig(50, 50, 10, 20, 7, 20, 10, 3, 5, 1000, 10, floorCount);

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static readonly ICommandSource Idle = new IdleAi();

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;
            public DungeonFloor Floor1;
            public FloorTransition Stairs;
            public readonly List<FloorChangedEvent> Changes = new List<FloorChangedEvent>();
        }

        private static Session Start(ulong seed, int floorCount = 5, SpawnConfig spawn = null)
        {
            spawn = spawn ?? new SpawnConfig(5, 8);
            var generator = new DungeonGenerator(Gen(floorCount));
            DungeonFloor floor = null;
            var world = World.Create(seed, rng =>
            {
                floor = generator.Generate(rng, stairsUp: false, stairsDown: floorCount > 1);
                return floor.Map;
            });
            var statuses = SpellTests.BalanceStatuses();
            var vision = world.InstallCoreSystems(Sight, Magic, statuses, SpellTests.BalanceTerrains());
            world.Scheduler.AddSystem(TickPhase.ManaRegen, new EnemyRespawnSystem(world, spawn, EnemyDef, Idle, Sight, floorCount));
            var s = new Session
            {
                World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput), Floor1 = floor,
                Stairs = new FloorTransition(world, generator, floorCount, EnemyDef, spawn, (w, id) => Idle, vision),
            };
            world.Events.Subscribe<FloorChangedEvent>(s.Changes.Add);
            s.Player = FloorPopulator.Populate(world, floor.Start, PlayerDef, EnemyDef, spawn, Idle, Magic.QuickSlotCount, Magic.InventorySlotCount);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        /// <summary>계단 칸으로 걸어간 뒤 계단을 쓴다 (3-4a: 걸어가기 + 한 번 더).</summary>
        private static void StepOnto(Session s, TileType stairs)
        {
            WalkOnto(s, stairs);
            Assert.That(s.Stairs.TryUse(), Is.True);
            s.World.Events.Flush();
        }

        /// <summary>플레이어를 그 계단 옆 빈 칸에 놓고 계단 칸으로 한 걸음 (층은 안 바뀜).</summary>
        private static void WalkOnto(Session s, TileType stairs)
        {
            var target = s.World.Map.Find(stairs);
            Assert.That(target.HasValue, Is.True, stairs + " 없음");
            var map = s.World.Map;
            foreach (var dir in GridMovement.Directions)
            {
                var from = target.Value + dir;
                if (from != s.Player.Pos && (!map.IsWalkable(from) || s.World.Units.IsOccupied(from) || map.Get(from) != TileType.Floor)) continue;
                if (!GridMovement.CanStep(map, from, target.Value)) continue;
                s.World.Units.Teleport(s.Player.Id, from);
                Assert.That(PlayerActions.TryStepTo(s.World, Config, s.Player, target.Value, out var command), Is.True);
                s.Clock.SubmitPlayerCommand(command);
                s.World.Events.Flush();
                return;
            }
            Assert.Fail("계단 옆 빈 칸이 없다");
        }

        private static void Wait(Session s, int turns)
        {
            for (int i = 0; i < turns; i++) s.Clock.SubmitPlayerCommand(PlayerActions.Wait(Config));
            s.World.Events.Flush();
        }

        // 결정 2026-10-18: 계단 칸으로는 걸어가기만, 그 위에서 한 번 더 = 이동. 이동은 틱을 쓰지 않는다
        [Test]
        public void WalkOntoStairs_DoesNotChangeFloor_UseCostsNoTick()
        {
            var s = Start(13);
            WalkOnto(s, TileType.StairsDown);
            Assert.That(s.World.Floor, Is.EqualTo(1), "걸어 들어가기만");
            Assert.That(s.Stairs.StairsDirection(), Is.EqualTo(1));
            long tick = s.World.Scheduler.CurrentTick;
            long ready = s.World.Scheduler.GetReadyTick(s.Player.Id);

            Assert.That(s.Stairs.TryUse(), Is.True);
            Assert.That(s.World.Floor, Is.EqualTo(2));
            Assert.That(s.World.Scheduler.CurrentTick, Is.EqualTo(tick), "틱 안 씀");
            Assert.That(s.World.Scheduler.GetReadyTick(s.Player.Id), Is.EqualTo(ready));
            Assert.That(s.World.Scheduler.IsWaitingForPlayer, Is.True, "바로 다음 입력을 기다림");
        }

        [Test]
        public void TryUse_NotOnStairs_Nothing()
        {
            var s = Start(13);
            Assert.That(s.Stairs.StairsDirection(), Is.EqualTo(0));
            Assert.That(s.Stairs.TryUse(), Is.False);
            Assert.That(s.World.Floor, Is.EqualTo(1));
        }

        [Test]
        public void Descend_NewFloor_PlayerOnStairsUp_KeepsEverything()
        {
            var s = Start(20261018);
            var floor1Enemies = s.World.Units.Count - 1;
            Combat.ApplyDamage(s.World, EntityId.None, s.Player, 5);
            Mana.TrySpend(s.World, s.Player, 4);
            SpellTests.BalanceStatuses().TryGet(StatusType.Poison, out var poison);
            StatusEffects.Apply(s.World, s.Player, poison);

            StepOnto(s, TileType.StairsDown);

            Assert.That(s.World.Floor, Is.EqualTo(2));
            Assert.That(s.Changes.Count, Is.EqualTo(1));
            Assert.That((s.Changes[0].From, s.Changes[0].To), Is.EqualTo((1, 2)));
            Assert.That(s.World.Map.Get(s.Player.Pos), Is.EqualTo(TileType.StairsUp), "도착 = 올라가는 계단");
            Assert.That(s.Player.Hp, Is.LessThanOrEqualTo(15), "체력 그대로 (중독 피해만 더 줄 수 있음)");
            Assert.That(s.Player.Mana, Is.EqualTo(6), "마나 그대로 (회복 주기 전)");
            Assert.That(s.Player.Statuses.Exists(x => x.Type == StatusType.Poison), Is.True, "상태 이상도 가져감");
            Assert.That(s.World.StoredFloors.Count, Is.EqualTo(1));
            Assert.That(s.World.StoredFloors[0].Units.Length, Is.EqualTo(floor1Enemies), "1층 적은 저장됨");
            Assert.That(s.World.Units.Count, Is.EqualTo(1 + 5), "플레이어 + 2층 적 5");
        }

        // 위로 갔다가 다시: 떠난 층은 멈춘 채 그대로 (적 ID · 위치 · 지형 남은 틱), 2층은 다시 만들지 않음
        [Test]
        public void Ascend_RestoresFrozenFloor_DescendAgain_SameFloor2()
        {
            var s = Start(7);
            SpellTests.BalanceTerrains().TryGet(TerrainEffectType.PoisonCloud, out var cloud);
            var cloudCell = FindFloorCell(s.World.Map, s.Floor1.Start, 3);
            TerrainEffects.Place(s.World, cloudCell, cloud);

            StepOnto(s, TileType.StairsDown);
            var stored1 = s.World.FindStoredFloor(1);
            var floor2Cells = s.World.Map.CopyCells();
            int cloudLeft = 0;
            foreach (var t in stored1.Terrain) if (t.Type == TerrainEffectType.PoisonCloud) cloudLeft = t.Remaining;
            Assert.That(cloudLeft, Is.GreaterThan(0));
            Wait(s, 2); // 2층에서 시간이 흘러도 1층은 멈춤

            StepOff(s);
            StepOnto(s, TileType.StairsUp);
            Assert.That(s.World.Floor, Is.EqualTo(1));
            Assert.That(s.World.Map.Get(s.Player.Pos), Is.EqualTo(TileType.StairsDown).Or.EqualTo(TileType.Floor), "내려가는 계단(막혔으면 가장 가까운 칸)");
            foreach (var u in stored1.Units)
            {
                Assert.That(s.World.Units.TryGet(u.Id, out var unit), Is.True, u.Id.ToString());
                Assert.That(unit.Pos, Is.EqualTo(new GridPos(u.X, u.Y)), "멈춘 자리 그대로");
            }
            Assert.That(RemainingAt(s.World, cloudCell, TerrainEffectType.PoisonCloud), Is.EqualTo(cloudLeft), "지형 남은 틱 그대로");
            Assert.That(s.World.HasStoredFloor(2), Is.True);

            StepOff(s);
            StepOnto(s, TileType.StairsDown);
            Assert.That(s.World.Floor, Is.EqualTo(2));
            Assert.That(s.World.Map.CopyCells(), Is.EqualTo(floor2Cells), "2층은 다시 만들지 않음");
        }

        [Test]
        public void SameSeed_SameFloor2()
        {
            var a = Start(42);
            var b = Start(42);
            StepOnto(a, TileType.StairsDown);
            StepOnto(b, TileType.StairsDown);
            Assert.That(a.World.Map.CopyCells(), Is.EqualTo(b.World.Map.CopyCells()));
            var ea = a.World.Units.ToSortedList().ConvertAll(u => u.Pos);
            var eb = b.World.Units.ToSortedList().ConvertAll(u => u.Pos);
            Assert.That(ea, Is.EqualTo(eb));
        }

        // 1층 맵은 3-4a 전과 같다 (계단 유무는 난수를 쓰지 않음)
        [Test]
        public void Floor1_SameAsBeforeStairsOptions()
        {
            var generator = new DungeonGenerator(Gen(5));
            var a = generator.Generate(new RngSet(20261016).Get(RngStream.MapGen));
            var b = generator.Generate(new RngSet(20261016).Get(RngStream.MapGen), stairsUp: true, stairsDown: false);
            for (int y = 0; y < a.Map.Height; y++)
                for (int x = 0; x < a.Map.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (p == a.Start || p == a.Exit) continue;
                    Assert.That(b.Map.Get(p), Is.EqualTo(a.Map.Get(p)), p.ToString());
                }
            Assert.That(b.Map.Get(b.Start), Is.EqualTo(TileType.StairsUp));
            Assert.That(b.Map.Get(b.Exit), Is.EqualTo(TileType.Floor));
        }

        [Test]
        public void LastFloor_HasNoStairsDown()
        {
            var s = Start(3, floorCount: 2);
            StepOnto(s, TileType.StairsDown);
            Assert.That(s.World.Floor, Is.EqualTo(2));
            Assert.That(s.World.Map.Find(TileType.StairsDown).HasValue, Is.False);
            Assert.That(s.World.Map.Find(TileType.StairsUp).HasValue, Is.True);
        }

        // 스냅샷 v9: 떠난 층까지 저장 → 복원 → 같은 바이트
        [Test]
        public void Snapshot_RoundTripsStoredFloors()
        {
            var s = Start(11);
            StepOnto(s, TileType.StairsDown);
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => Idle);
            Assert.That(restored.Floor, Is.EqualTo(2));
            Assert.That(restored.StoredFloors.Count, Is.EqualTo(1));
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(15), "3-4b에서 10, 3-5에서 11, 3-6에서 12, 3-7a에서 13, 3-7b에서 14, 3-7c에서 15");
        }

        [Test]
        public void FloorLog_Text()
        {
            Assert.That(ProjectM.UI.HudText.FloorChanged(1, 2), Is.EqualTo("2층으로 내려왔다"));
            Assert.That(ProjectM.UI.HudText.FloorChanged(3, 2), Is.EqualTo("2층으로 올라왔다"));
        }

        [Test]
        public void NearestFree_SkipsOccupied()
        {
            var map = GridMovementTests.Parse(
                "#####",
                "#...#",
                "#...#",
                "#####");
            var start = new GridPos(1, 1);
            Assert.That(FloorTransition.NearestFree(map, start, new HashSet<GridPos>()), Is.EqualTo(start));
            var near = FloorTransition.NearestFree(map, start, new HashSet<GridPos> { start });
            Assert.That(GridMovement.Chebyshev(near, start), Is.EqualTo(1));
        }

        // 적 재생성: 주기마다 1마리, 시야 밖 바닥 칸, 상한까지. 마지막(보스) 층은 없음
        [Test]
        public void Respawn_EveryInterval_OutOfSight_UpToMax()
        {
            var s = Start(5, spawn: new SpawnConfig(0, 8, respawnIntervalTicks: 3, respawnMaxEnemies: 2));
            Assert.That(s.World.Units.Count, Is.EqualTo(1), "처음엔 적 0");
            var spawned = new List<EntitySpawnedEvent>();
            s.World.Events.Subscribe<EntitySpawnedEvent>(spawned.Add);

            Wait(s, 3); // 틱 0 · 1 · 2 → 틱 2에 1마리
            Assert.That(spawned.Count, Is.EqualTo(1));
            var visible = new bool[s.World.Map.Width * s.World.Map.Height];
            Sight.ComputeVisible(s.World.Map, s.Player.Pos, s.Player.SightRange, visible);
            Assert.That(visible[spawned[0].Pos.Y * s.World.Map.Width + spawned[0].Pos.X], Is.False, "시야 밖");
            Assert.That(s.World.Map.Get(spawned[0].Pos), Is.EqualTo(TileType.Floor));

            Wait(s, 9);
            Assert.That(spawned.Count, Is.EqualTo(2), "상한 2");
        }

        [Test]
        public void Respawn_NotOnBossFloor_AndSeeded()
        {
            var boss = Start(5, floorCount: 1, spawn: new SpawnConfig(0, 8, 3, 2));
            Wait(boss, 6);
            Assert.That(boss.World.Units.Count, Is.EqualTo(1), "1층이 마지막(보스) 층 → 재생성 없음");

            var a = Start(9, spawn: new SpawnConfig(0, 8, 3, 2));
            var b = Start(9, spawn: new SpawnConfig(0, 8, 3, 2));
            Wait(a, 3);
            Wait(b, 3);
            Assert.That(a.World.Units.ToSortedList().ConvertAll(u => u.Pos), Is.EqualTo(b.World.Units.ToSortedList().ConvertAll(u => u.Pos)), "같은 시드 = 같은 자리");
        }

        private static void StepOff(Session s)
        {
            var map = s.World.Map;
            foreach (var dir in GridMovement.Directions)
            {
                var to = s.Player.Pos + dir;
                if (map.Get(to) != TileType.Floor || s.World.Units.IsOccupied(to) || !GridMovement.CanStep(map, s.Player.Pos, to)) continue;
                Assert.That(PlayerActions.TryStepTo(s.World, Config, s.Player, to, out var command), Is.True);
                s.Clock.SubmitPlayerCommand(command);
                s.World.Events.Flush();
                return;
            }
            Assert.Fail("계단에서 내려설 칸이 없다");
        }

        private static GridPos FindFloorCell(GridMap map, GridPos near, int minDistance)
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (map.Get(p) == TileType.Floor && GridMovement.Chebyshev(p, near) >= minDistance) return p;
                }
            throw new System.InvalidOperationException();
        }

        private static int RemainingAt(World world, GridPos pos, TerrainEffectType type)
        {
            foreach (var t in world.Terrain.At(pos))
                if (t.Type == type) return t.Remaining;
            return 0;
        }
    }
}
