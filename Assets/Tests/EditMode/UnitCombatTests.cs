using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    public class UnitCombatTests
    {
        // BALANCE.md 3번과 같은 값 (Q3 임시값)
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8);
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 8, 2, 6);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        // BALANCE.md 4번과 같은 값
        private static readonly ProjectM.Magic.MagicConfig Magic = new ProjectM.Magic.MagicConfig(3, 1, 4);

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;
            public readonly List<ISimEvent> Events = new List<ISimEvent>();

            public void Submit(ICommand command) => Clock.SubmitPlayerCommand(command);

            public void Step(int dx, int dy)
            {
                Assert.That(PlayerActions.TryStep(World, Config, new GridPos(dx, dy), out var command), Is.True, $"({dx},{dy}) 행동 없음");
                Submit(command);
            }
        }

        private static ICommandSource Ai(World world) => new MeleeChaserAi(world, Config, new ShadowcastSightRule());

        private static Session Start(GridMap map, GridPos playerPos, params GridPos[] enemies)
        {
            var world = new World(map, 42UL);
            var vision = world.InstallCoreSystems(new ShadowcastSightRule(), Magic, new ProjectM.Status.StatusBook(new ProjectM.Status.StatusDef[0]), ProjectM.Status.TerrainBook.Empty);
            var session = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            world.Events.Subscribe<UnitDamagedEvent>(session.Events.Add);
            world.Events.Subscribe<UnitDiedEvent>(session.Events.Add);
            world.Events.Subscribe<EntityRemovedEvent>(session.Events.Add);
            world.Events.Subscribe<VisibilityChangedEvent>(session.Events.Add);

            session.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, playerPos);
            var ai = Ai(world);
            foreach (var pos in enemies)
                UnitSpawner.SpawnEnemy(world, EnemyDef, pos, ai);
            vision.Refresh();
            world.Events.Flush();
            return session;
        }

        private static GridMap Room() => GridMovementTests.Parse(
            "############",
            "#..........#",
            "#..........#",
            "#..........#",
            "#..........#",
            "############");

        [Test]
        public void PlayerActions_MoveAttackOrNothing()
        {
            var s = Start(GridMovementTests.Parse(
                "#####",
                "#...#",
                "#.#.#",
                "#####"), new GridPos(1, 2), new GridPos(3, 2));

            Assert.That(PlayerActions.TryStep(s.World, Config, new GridPos(1, 0), out var move), Is.True);
            Assert.That(move, Is.InstanceOf<MoveCommand>());
            Assert.That(PlayerActions.TryStep(s.World, Config, new GridPos(0, 1), out _), Is.False, "벽");
            Assert.That(PlayerActions.TryStep(s.World, Config, new GridPos(1, -1), out _), Is.False, "대각선 벽");

            s.Step(1, 0);
            Assert.That(s.Player.Pos, Is.EqualTo(new GridPos(2, 2)));
            Assert.That(PlayerActions.TryStep(s.World, Config, new GridPos(1, 0), out var attack), Is.True);
            Assert.That(attack, Is.InstanceOf<AttackCommand>(), "적 칸으로 가면 공격");
        }

        [Test]
        public void PlayerKillsEnemyInThreeHits_EnemyRemoved()
        {
            // 적은 플레이어 오른쪽에 붙어 있다. 플레이어 3 × 3 = 9 ≥ 8 → 세 번째에 죽는다.
            var s = Start(Room(), new GridPos(2, 2), new GridPos(3, 2));
            var enemyId = s.World.Units.ToSortedList()[1].Id;

            s.Step(1, 0);
            s.Step(1, 0);
            Assert.That(s.World.Units.Get(enemyId).Hp, Is.EqualTo(2));
            s.Step(1, 0);

            Assert.That(s.World.Units.TryGet(enemyId, out _), Is.False);
            Assert.That(s.World.Scheduler.IsRegistered(enemyId), Is.False);
            Assert.That(s.Events.Exists(e => e is UnitDiedEvent d && d.Id == enemyId && d.Faction == Faction.Enemy), Is.True);
            Assert.That(s.Events.Exists(e => e is EntityRemovedEvent r && r.Id == enemyId), Is.True);
            // 적은 첫 두 틱에 반격했고, 죽은 틱에는 행동하지 않았다 (2 × 2 = 4)
            Assert.That(s.Player.Hp, Is.EqualTo(20 - 4));
        }

        [Test]
        public void EnemyChasesAndAttacks()
        {
            var s = Start(Room(), new GridPos(1, 1), new GridPos(5, 1));
            var enemy = s.World.Units.ToSortedList()[1];

            s.Submit(PlayerActions.Wait(Config));
            Assert.That(enemy.Pos, Is.EqualTo(new GridPos(4, 1)));
            s.Submit(PlayerActions.Wait(Config));
            s.Submit(PlayerActions.Wait(Config));
            Assert.That(enemy.Pos, Is.EqualTo(new GridPos(2, 1)), "붙으면 멈춘다");
            Assert.That(s.Player.Hp, Is.EqualTo(20));

            s.Submit(PlayerActions.Wait(Config));
            Assert.That(s.Player.Hp, Is.EqualTo(18));
            Assert.That(s.Events.Exists(e => e is UnitDamagedEvent d && d.Target == s.Player.Id && d.Amount == 2 && d.Hp == 18), Is.True);
        }

        // 2026-10-16: 붙어 있던 적은 플레이어가 떠난 칸(발자국)을 그대로 밟고 따라온다
        [Test]
        public void AdjacentEnemy_FollowsPlayersExactTrail()
        {
            var s = Start(Room(), new GridPos(3, 1), new GridPos(2, 1));
            var enemy = s.World.Units.ToSortedList()[1];
            var trail = new List<GridPos>();

            void Step(int dx, int dy)
            {
                trail.Add(s.Player.Pos);
                s.Step(dx, dy);
            }

            Step(1, 1);   // 플레이어 (4,2)
            Assert.That(enemy.Pos, Is.EqualTo(trail[0]), "플레이어가 떠난 (3,1)로");
            Step(1, 0);   // (5,2)
            Assert.That(enemy.Pos, Is.EqualTo(trail[1]), "(4,2)로 — 대각선 발자국도 그대로");
            Step(1, 1);   // (6,3)
            Assert.That(enemy.Pos, Is.EqualTo(trail[2]));
            Step(0, 1);   // (6,4)
            Assert.That(enemy.Pos, Is.EqualTo(trail[3]));
            Assert.That(s.Player.Hp, Is.EqualTo(20), "계속 물러나는 동안은 맞지 않는다 (따라붙기만 함)");
        }

        [Test]
        public void ChaseStep_PrefersDiagonalWhenCloser_StraightWhenStraightIsCloser()
        {
            var map = Room();
            Assert.That(GridPathfinder.ChaseStep(map, new GridPos(2, 1), new GridPos(4, 2)), Is.EqualTo(new GridPos(3, 2)), "대각선이 더 가까움");
            Assert.That(GridPathfinder.ChaseStep(map, new GridPos(5, 1), new GridPos(1, 1)), Is.EqualTo(new GridPos(4, 1)), "일직선이면 곧게");
            Assert.That(GridPathfinder.ChaseStep(map, new GridPos(1, 1), new GridPos(1, 1)), Is.Null);
        }

        // 2026-10-16: 플레이어를 못 본 적은 맵을 배회한다 (목적지 = Ai 난수, 같은 시드 = 같은 움직임)
        [Test]
        public void EnemyOutOfSight_WandersDeterministically()
        {
            var a = Start(Room(), new GridPos(1, 1), new GridPos(10, 4)); // 거리 9 > 시야 6
            var b = Start(Room(), new GridPos(1, 1), new GridPos(10, 4));
            var enemyA = a.World.Units.ToSortedList()[1];
            var enemyB = b.World.Units.ToSortedList()[1];

            a.Submit(PlayerActions.Wait(Config));
            b.Submit(PlayerActions.Wait(Config));
            Assert.That(enemyA.Pos, Is.Not.EqualTo(new GridPos(10, 4)), "가만히 있지 않는다");
            Assert.That(GridMovement.Chebyshev(enemyA.Pos, new GridPos(10, 4)), Is.EqualTo(1), "한 턴에 한 칸");
            Assert.That(enemyA.WanderTarget.HasValue, Is.True);
            Assert.That(enemyA.LastSeenTargetPos.HasValue, Is.False, "아직 플레이어를 못 봤다");

            for (int i = 0; i < 4; i++)
            {
                a.Submit(PlayerActions.Wait(Config));
                b.Submit(PlayerActions.Wait(Config));
                Assert.That(enemyB.Pos, Is.EqualTo(enemyA.Pos), $"같은 시드 = 같은 배회 ({i + 2}턴)");
            }
        }

        [Test]
        public void Wander_PicksNewTargetOnArrival()
        {
            var s = Start(Room(), new GridPos(1, 1), new GridPos(10, 4));
            var enemy = s.World.Units.ToSortedList()[1];
            enemy.WanderTarget = new GridPos(9, 4); // 한 칸 옆을 목적지로

            s.Submit(PlayerActions.Wait(Config));
            Assert.That(enemy.Pos, Is.EqualTo(new GridPos(9, 4)));
            s.Submit(PlayerActions.Wait(Config));
            Assert.That(enemy.WanderTarget.HasValue && enemy.WanderTarget.Value != new GridPos(9, 4), Is.True, "도착하면 새 목적지");
            Assert.That(enemy.Pos, Is.Not.EqualTo(new GridPos(9, 4)), "바로 다시 걷는다");
        }

        [Test]
        public void Snapshot_RoundTripsTrailAndWanderTarget()
        {
            var s = Start(Room(), new GridPos(1, 1), new GridPos(10, 4));
            s.Step(1, 0);
            var enemy = s.World.Units.ToSortedList()[1];
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(s.World.Capture())), (w, id) => Ai(w));

            Assert.That(restored.Units.Get(s.Player.Id).PreviousPos, Is.EqualTo(new GridPos(1, 1)));
            Assert.That(restored.Units.Get(enemy.Id).WanderTarget, Is.EqualTo(enemy.WanderTarget));
            Assert.That(restored.Units.Get(enemy.Id).PreviousPos, Is.EqualTo(enemy.PreviousPos));
        }

        [Test]
        public void PlayerDiesAfterTenHits_ClockStops()
        {
            var s = Start(Room(), new GridPos(1, 1), new GridPos(2, 1));
            for (int i = 0; i < 9; i++)
                s.Submit(PlayerActions.Wait(Config));
            Assert.That(s.Player.Hp, Is.EqualTo(2));

            s.Submit(PlayerActions.Wait(Config));
            Assert.That(s.Player.Hp, Is.EqualTo(0));
            Assert.That(s.World.Scheduler.PlayerId.IsValid, Is.False);
            Assert.That(s.World.Units.TryGet(s.Player.Id, out _), Is.False);
            Assert.That(s.Events.Exists(e => e is UnitDiedEvent d && d.Faction == Faction.Player), Is.True);
            Assert.That(PlayerActions.TryStep(s.World, Config, new GridPos(0, 1), out _), Is.False, "죽은 뒤에는 행동 없음");
        }

        [Test]
        public void EnemiesDoNotStack_AndRouteAroundEachOther()
        {
            var s = Start(Room(), new GridPos(1, 2), new GridPos(5, 2), new GridPos(6, 2), new GridPos(7, 2));
            for (int i = 0; i < 6; i++)
                s.Submit(PlayerActions.Wait(Config));

            var positions = new HashSet<GridPos>();
            foreach (var u in s.World.Units.ToSortedList())
                Assert.That(positions.Add(u.Pos), Is.True, "한 칸에 유닛 둘: " + u.Pos);
            int adjacent = 0;
            foreach (var u in s.World.Units.ToSortedList())
                if (u.Faction == Faction.Enemy && GridMovement.Chebyshev(u.Pos, s.Player.Pos) == 1) adjacent++;
            Assert.That(adjacent, Is.EqualTo(3), "세 적 모두 플레이어를 둘러쌌다");
        }

        // 1-4 왕복 테스트를 실제 유닛 · AI · 전투로: 저장 → 불러와 진행 = 저장 없이 진행
        [Test]
        public void SaveLoadThenPlay_WithUnits_EqualsPlayWithoutSave()
        {
            var original = Start(Room(), new GridPos(1, 1), new GridPos(6, 3), new GridPos(8, 1));
            // 길게 하면 플레이어가 죽는다 (두 적 × 2피해) — 죽기 전까지만
            var moves = new[] { (1, 0), (1, 0), (0, 0), (1, 1), (0, 0), (1, 0) };
            const int split = 3;

            void Play(Session s, int from, int to)
            {
                for (int i = from; i < to; i++)
                {
                    var (dx, dy) = moves[i];
                    if ((dx == 0 && dy == 0) || !PlayerActions.TryStep(s.World, Config, new GridPos(dx, dy), out var cmd))
                        cmd = PlayerActions.Wait(Config);
                    s.Submit(cmd);
                }
            }

            Play(original, 0, split);
            byte[] saved = WorldSnapshotCodec.Encode(original.World.Capture());
            Play(original, split, moves.Length);
            byte[] withoutSave = WorldSnapshotCodec.Encode(original.World.Capture());

            var restoredWorld = World.Restore(WorldSnapshotCodec.Decode(saved), (w, id) => Ai(w));
            restoredWorld.InstallCoreSystems(new ShadowcastSightRule(), Magic, new ProjectM.Status.StatusBook(new ProjectM.Status.StatusDef[0]), ProjectM.Status.TerrainBook.Empty);
            var restored = new Session { World = restoredWorld, Clock = new TurnClock(restoredWorld.Scheduler, restoredWorld.PlayerInput) };
            Play(restored, split, moves.Length);
            byte[] afterLoad = WorldSnapshotCodec.Encode(restoredWorld.Capture());

            Assert.That(original.Player.Hp, Is.LessThan(20), "시험 중 전투가 실제로 일어나야 의미가 있다");
            Assert.That(afterLoad, Is.EqualTo(withoutSave));
        }

        // ── 1-7 시야 · 탐험 기록 · 적 기억 ──

        // ㄱ자 복도: 위 줄 x1~7, 오른쪽 아래로 x7 두 칸
        private static GridMap Corner() => GridMovementTests.Parse(
            "#########",
            "#.......#",
            "#######.#",
            "#######.#",
            "#########");

        [Test]
        public void Enemy_LosesSight_GoesToLastSeen_ThenFindsPlayer()
        {
            var map = Corner();
            var s = Start(map, new GridPos(5, 3), new GridPos(1, 3));
            var enemy = s.World.Units.ToSortedList()[1];

            s.Step(1, 0);   // (6,3)
            s.Step(1, -1);  // (7,2) — 모퉁이
            s.Step(0, -1);  // (7,1) — 벽 뒤
            Assert.That(FieldOfView.CanSee(map, enemy.Pos, s.Player.Pos, enemy.SightRange), Is.False, "전제: 적이 플레이어를 놓쳤다");
            Assert.That(enemy.LastSeenTargetPos.HasValue, Is.True, "마지막으로 본 칸을 기억");

            for (int i = 0; i < 8 && s.Player.Hp == 20; i++)
                s.Submit(PlayerActions.Wait(Config));
            Assert.That(s.Player.Hp, Is.LessThan(20), "기억한 칸으로 와서 다시 찾아 공격했다");
        }

        [Test]
        public void Enemy_ForgetsAfterReachingLastSeen()
        {
            var s = Start(Room(), new GridPos(1, 1), new GridPos(10, 4));
            var enemy = s.World.Units.ToSortedList()[1];
            enemy.LastSeenTargetPos = new GridPos(8, 4); // 시야 밖(거리 9)에서 기억만 남은 상태

            s.Submit(PlayerActions.Wait(Config));
            s.Submit(PlayerActions.Wait(Config));
            Assert.That(enemy.Pos, Is.EqualTo(new GridPos(8, 4)));
            s.Submit(PlayerActions.Wait(Config));
            Assert.That(enemy.LastSeenTargetPos.HasValue, Is.False, "도착하면 잊는다");
            Assert.That(enemy.Pos, Is.EqualTo(new GridPos(8, 4)), "잊은 뒤에는 대기");
        }

        [Test]
        public void Exploration_GrowsAndPersists()
        {
            var map = Corner();
            var s = Start(map, new GridPos(1, 3));
            int Index(int x, int y) => y * map.Width + x;

            Assert.That(s.World.Explored[Index(1, 3)], Is.True, "시작 칸");
            Assert.That(s.World.Explored[Index(7, 1)], Is.False, "모퉁이 너머는 아직 안 봄");

            for (int i = 0; i < 6; i++) s.Step(1, 0);   // (7,3)
            s.Step(0, -1);                               // (7,2)
            Assert.That(s.World.Explored[Index(7, 1)], Is.True);
            Assert.That(s.World.Explored[Index(1, 3)], Is.True, "지금은 안 보여도 기록은 남는다");

            var last = (VisibilityChangedEvent)s.Events.FindLast(e => e is VisibilityChangedEvent);
            Assert.That(last.IsVisible(s.Player.Pos), Is.True);
            Assert.That(last.Explored[Index(1, 3)], Is.True);
        }

        [Test]
        public void Snapshot_RoundTripsExploredAndMemory()
        {
            var s = Start(Corner(), new GridPos(1, 3), new GridPos(5, 3));
            s.Submit(PlayerActions.Wait(Config));
            var enemy = s.World.Units.ToSortedList()[1];
            Assert.That(enemy.LastSeenTargetPos.HasValue, Is.True);

            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(s.World.Capture())), (w, id) => Ai(w));
            Assert.That(restored.Explored, Is.EqualTo(s.World.Explored));
            Assert.That(restored.Units.Get(enemy.Id).LastSeenTargetPos, Is.EqualTo(enemy.LastSeenTargetPos));
        }

        [Test]
        public void Snapshot_RoundTripsUnits()
        {
            var s = Start(Room(), new GridPos(1, 1), new GridPos(2, 1));
            s.Submit(PlayerActions.Wait(Config));
            var snapshot = WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(s.World.Capture()));

            Assert.That(snapshot.Units.Length, Is.EqualTo(2));
            var restored = World.Restore(snapshot, (w, id) => Ai(w));
            var player = restored.Units.Get(s.Player.Id);
            Assert.That(player.Hp, Is.EqualTo(18));
            Assert.That(player.Faction, Is.EqualTo(Faction.Player));
            Assert.That(player.VisualId, Is.EqualTo("player"));
            Assert.That(restored.Units.TryGetAt(new GridPos(2, 1), out var enemy) && enemy.Faction == Faction.Enemy, Is.True);
        }

        [Test]
        public void PickEnemyCells_SameSeedSameCells_RespectsDistance()
        {
            var map = Room();
            var start = new GridPos(1, 1);
            var a = FloorPopulator.PickEnemyCells(map, start, 5, 4, new RngSet(7).Get(RngStream.MapGen));
            var b = FloorPopulator.PickEnemyCells(map, start, 5, 4, new RngSet(7).Get(RngStream.MapGen));

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Count, Is.EqualTo(5));
            Assert.That(new HashSet<GridPos>(a).Count, Is.EqualTo(5), "겹치지 않음");
            foreach (var p in a)
            {
                Assert.That(map.Get(p), Is.EqualTo(TileType.Floor));
                Assert.That(GridMovement.Chebyshev(p, start), Is.GreaterThanOrEqualTo(4));
            }

            var few = FloorPopulator.PickEnemyCells(map, start, 100, 9, new RngSet(7).Get(RngStream.MapGen));
            Assert.That(few.Count, Is.EqualTo(4), "후보(x=10, y=1~4)가 모자라면 있는 만큼");
        }

        [Test]
        public void Defs_RejectInvalidValues()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnitDef("x", 0, 1, 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnitDef("x", 1, -1, 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnitDef("x", 1, 1, 0));
            Assert.Throws<System.ArgumentException>(() => new UnitDef("", 1, 1, 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SpawnConfig(-1, 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SpawnConfig(1, 0));
        }

        // 검산: 프로젝트 에셋이 BALANCE.md 3번 표와 같은 값인지. BALANCE.md를 고치면 같이 고친다.
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            const string hint = " 없음 — 메뉴 ProjectM/Setup/Main Scene 실행";
            var player = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/Player.asset");
            var enemy = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/MeleeEnemy.asset");
            var spawn = AssetDatabase.LoadAssetAtPath<SpawnConfigAsset>("Assets/_Project/Data/Units/SpawnConfig.asset");
            Assert.That(player, Is.Not.Null, "Player.asset" + hint);
            Assert.That(enemy, Is.Not.Null, "MeleeEnemy.asset" + hint);
            Assert.That(spawn, Is.Not.Null, "SpawnConfig.asset" + hint);

            AssertDef(player.ToDef(), PlayerDef);
            AssertDef(enemy.ToDef(), EnemyDef);
            var s = spawn.ToConfig();
            Assert.That(s.EnemyCount, Is.EqualTo(5));
            Assert.That(s.EnemyMinDistanceFromStart, Is.EqualTo(8));
            Assert.That(s.RespawnIntervalTicks, Is.EqualTo(30), "3-4a 재생성 주기");
            Assert.That(s.RespawnMaxEnemies, Is.EqualTo(5), "3-4a 재생성 상한");
        }

        private static void AssertDef(UnitDef actual, UnitDef expected)
        {
            Assert.That(actual.VisualId, Is.EqualTo(expected.VisualId));
            Assert.That(actual.MaxHp, Is.EqualTo(expected.MaxHp));
            Assert.That(actual.AttackDamage, Is.EqualTo(expected.AttackDamage));
            Assert.That(actual.SightRange, Is.EqualTo(expected.SightRange));
        }
    }
}
