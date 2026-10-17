using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>
    /// 계단 사용 = 층 이동 (3-4a, 결정 2026-10-18). 계단 칸으로 걸어가는 것은 보통 이동이고, 계단 칸에 선 채로 한 번 더 누르면(입력 쪽) 여기를 부른다.
    /// 틱을 쓰지 않는다 — 틱 사이에만 부르고(빠른 칸 등록과 같은 틱 밖 변경), 부른 쪽이 이벤트를 비운다.
    /// ① 지금 층을 멈춘 채 저장(StoredFloors) ② 처음 가는 층이면 생성(맵 생성 난수 — 같은 시드 = 같은 층) · 적 배치, 떠났던 층이면 그대로 복원
    /// ③ 플레이어를 도착 계단(아래로 = 그 층의 올라가는 계단, 위로 = 내려가는 계단)에 놓는다 — 다른 유닛이 서 있으면 가장 가까운 빈 칸
    /// ④ 화면용 이벤트(MapLoaded · 지형 · 유닛 · FloorChanged)와 시야 갱신.
    /// 플레이어는 체력 · 마나 · 인벤토리 · 상태 이상을 그대로 가져간다. 상태를 갖지 않는다.
    /// </summary>
    public sealed class FloorTransition
    {
        private readonly World _world;
        private readonly DungeonGenerator _generator;
        private readonly int _floorCount;
        private readonly UnitDef _enemyDef;
        private readonly SpawnConfig _spawn;
        private readonly Func<World, EntityId, ICommandSource> _aiFactory;
        private readonly PlayerVisionSystem _vision;

        public FloorTransition(World world, DungeonGenerator generator, int floorCount, UnitDef enemyDef, SpawnConfig spawn,
            Func<World, EntityId, ICommandSource> aiFactory, PlayerVisionSystem vision)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            if (floorCount < 1) throw new ArgumentOutOfRangeException(nameof(floorCount));
            _floorCount = floorCount;
            _enemyDef = enemyDef ?? throw new ArgumentNullException(nameof(enemyDef));
            _spawn = spawn ?? throw new ArgumentNullException(nameof(spawn));
            _aiFactory = aiFactory ?? throw new ArgumentNullException(nameof(aiFactory));
            _vision = vision ?? throw new ArgumentNullException(nameof(vision));
        }

        /// <summary>플레이어가 서 있는 칸의 계단으로 갈 수 있는 방향 (+1 아래, −1 위), 계단이 아니거나 갈 층이 없으면 0.</summary>
        public static int StairsDirection(World world, int floorCount)
        {
            if (!PlayerActions.TryGetPlayer(world, out var player) || !player.IsAlive) return 0;
            int delta = world.Map.Get(player.Pos) == TileType.StairsDown ? 1 : world.Map.Get(player.Pos) == TileType.StairsUp ? -1 : 0;
            int target = world.Floor + delta;
            return delta != 0 && target >= 1 && target <= floorCount ? delta : 0;
        }

        public int StairsDirection() => StairsDirection(_world, _floorCount);

        /// <summary>서 있는 계단을 쓴다. 계단 위가 아니면 false. 플레이어 입력이 처리되지 않고 남아 있으면 쓰지 않는다.</summary>
        public bool TryUse()
        {
            int delta = StairsDirection();
            if (delta == 0 || _world.PlayerInput.HasCommand) return false;
            PlayerActions.TryGetPlayer(_world, out var player);
            Change(player, _world.Floor + delta, delta);
            return true;
        }

        private void Change(Unit player, int target, int delta)
        {
            int from = _world.Floor;
            _world.StoreCurrentFloor(player.Id);

            // 도착 계단: 아래로 가면 그 층의 올라가는 계단, 위로 가면 내려가는 계단
            var arrivalTile = delta > 0 ? TileType.StairsUp : TileType.StairsDown;
            var stored = _world.FindStoredFloor(target);
            if (stored != null)
            {
                var map = GridMap.FromCells(stored.MapWidth, stored.MapHeight, stored.Cells);
                var occupied = new HashSet<GridPos>();
                foreach (var u in stored.Units) occupied.Add(new GridPos(u.X, u.Y));
                var arrival = NearestFree(map, map.Find(arrivalTile) ?? FirstWalkable(map), occupied);
                _world.Units.Teleport(player.Id, arrival);
                _world.RestoreStoredFloor(target, _aiFactory);
            }
            else
            {
                bool last = target == _floorCount;
                var floor = _generator.Generate(_world.Rng.Get(RngStream.MapGen), stairsUp: target > 1, stairsDown: !last);
                _world.EnterNewFloor(target, floor.Map);
                _world.Units.Teleport(player.Id, floor.Start);
                FloorPopulator.PopulateEnemies(_world, floor.Start, _enemyDef, _spawn, _aiFactory(_world, EntityId.None));
            }

            Announce(player, restored: stored != null);
            _world.Events.Publish(new FloorChangedEvent(from, target));
            _vision.Refresh();
        }

        /// <summary>
        /// 화면을 새 층으로: 맵 → 지형 → (돌아온 층이면) 유닛 생성 · 상태 → 플레이어 위치.
        /// 처음 가는 층의 적은 UnitSpawner가 이미 생성 이벤트를 냈다.
        /// </summary>
        private void Announce(Unit player, bool restored)
        {
            var events = _world.Events;
            events.Publish(new MapLoadedEvent(_world.Map));
            foreach (var pair in _world.Terrain.ToSortedList())
                foreach (var t in pair.Value)
                    events.Publish(new TerrainChangedEvent(_world.Terrain.PosOf(pair.Key), t.Type, t.Remaining));
            foreach (var unit in _world.Units.ToSortedList())
            {
                if (!restored || unit.Id == player.Id) continue;
                events.Publish(new EntitySpawnedEvent(unit.Id, unit.Pos, unit.VisualId));
                foreach (var s in unit.Statuses)
                    events.Publish(new StatusChangedEvent(unit.Id, s.Type, s.Remaining, true));
            }
            events.Publish(new EntityMovedEvent(player.Id, player.Pos, player.Pos));
        }

        /// <summary>start가 비어 있으면 start, 아니면 8방향 너비 우선으로 가장 가까운 걸을 수 있는 빈 칸.</summary>
        public static GridPos NearestFree(GridMap map, GridPos start, ICollection<GridPos> occupied)
        {
            var queue = new List<GridPos> { start };
            var seen = new HashSet<GridPos> { start };
            for (int i = 0; i < queue.Count; i++)
            {
                var p = queue[i];
                if (map.IsWalkable(p) && !occupied.Contains(p)) return p;
                foreach (var dir in GridMovement.Directions)
                {
                    var n = p + dir;
                    if (map.InBounds(n) && seen.Add(n)) queue.Add(n);
                }
            }
            throw new InvalidOperationException("빈 칸이 없다.");
        }

        private static GridPos FirstWalkable(GridMap map)
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map.IsWalkable(new GridPos(x, y))) return new GridPos(x, y);
            throw new InvalidOperationException("걸을 수 있는 칸이 없다.");
        }
    }

    /// <summary>층이 바뀌었다 (3-4a) — HUD · 로그 · 입력용.</summary>
    public sealed class FloorChangedEvent : ISimEvent
    {
        public int From { get; }
        public int To { get; }

        public FloorChangedEvent(int from, int to)
        {
            From = from;
            To = to;
        }
    }
}
