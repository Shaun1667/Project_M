using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>
    /// 계단 사용 = 층 이동 (3-4a, 결정 2026-10-18). 계단 칸으로 걸어가는 것은 보통 이동이고, 계단 칸에 선 채로 한 번 더 누르면(입력 쪽) 여기를 부른다.
    /// 틱을 쓰지 않는다 — 틱 사이에만 부르고(빠른 칸 등록과 같은 틱 밖 변경), 부른 쪽이 이벤트를 비운다.
    /// ① 지금 층을 멈춘 채 저장(StoredFloors) ② 처음 가는 층이면 생성(맵 생성 난수 — 같은 시드 = 같은 층) · 적 배치, 떠났던 층이면 그대로 복원.
    ///    마지막 층은 보스 방 + 보스 (3-4b)
    /// ③ 플레이어를 도착 계단(아래로 = 그 층의 올라가는 계단, 위로 = 내려가는 계단)에 놓는다 — 다른 유닛이 서 있으면 가장 가까운 빈 칸
    /// ④ 화면용 이벤트(MapLoaded · 지형 · 바닥 아이템 · 유닛 · FloorChanged)와 시야 갱신. 처음 가는 일반 층이면 그다음 마법서를 놓는다 (3-5).
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
        private readonly UnitDef _bossDef;
        private readonly LootConfig _loot;
        private readonly FloorDecorConfig _decor;
        private readonly TerrainDef _water;
        private readonly TrapBook _traps;
        private readonly ShopConfig _shop;
        private readonly ItemBook _items;
        private readonly ZoneBook _zones;
        private readonly TerrainBook _terrains;
        private readonly UnitDef _ghostDef;
        private readonly GhostRecord _ghost;

        /// <param name="bossDef">마지막 층(보스 층)의 보스 (3-4b). null이면 마지막 층도 일반 층처럼 만든다 (테스트용).</param>
        /// <param name="loot">바닥 아이템 배치 수치 (3-5). null이면 새 층에 아이템을 놓지 않는다.</param>
        /// <param name="decor">층 꾸미기 수치 (3-11a — 풀 · 물 웅덩이). null이면 꾸미지 않는다. 보스 층은 꾸미지 않는다.</param>
        /// <param name="water">물 웅덩이에 쓸 물 지형 규칙. null이면 웅덩이 없이 풀만.</param>
        /// <param name="terrains">지형 규칙 표 (3-18). 있으면 웅덩이 지형을 구역 표(PuddleTerrain)에서 고른다 — 없으면 water.</param>
        public FloorTransition(World world, DungeonGenerator generator, int floorCount, UnitDef enemyDef, SpawnConfig spawn,
            Func<World, EntityId, ICommandSource> aiFactory, PlayerVisionSystem vision, UnitDef bossDef = null, LootConfig loot = null,
            FloorDecorConfig decor = null, TerrainDef water = null, TrapBook traps = null, ShopConfig shop = null, ItemBook items = null,
            ZoneBook zones = null, TerrainBook terrains = null, GhostRecord ghost = null, UnitDef ghostDef = null)
        {
            _ghost = ghost; // 3-23b: 있으면 마지막 보스 층(GhostFloor)의 보스 = 지난 회차 플레이어
            _ghostDef = ghostDef;
            _terrains = terrains;
            _zones = zones; // 3-17a: 있으면 보스 층 · 보스 · 적 종류를 구역 표에서
            _shop = shop;
            _items = items;
            _traps = traps;
            _decor = decor;
            _water = water;
            _bossDef = bossDef;
            _loot = loot;
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
            GridPos? newFloorStart = null;
            if (stored != null)
            {
                var map = GridMap.FromCells(stored.MapWidth, stored.MapHeight, stored.Cells);
                var occupied = new HashSet<GridPos>();
                foreach (var u in stored.Units) occupied.Add(new GridPos(u.X, u.Y));
                var arrival = NearestFree(map, map.Find(arrivalTile) ?? FirstWalkable(map), occupied);
                _world.Units.Teleport(player.Id, arrival);
                _world.RestoreStoredFloor(target, _aiFactory);
            }
            else if (_zones != null && _zones.IsAltarFloor(target))
            {
                // 3-23a 제단 층 (26층): 보스 방과 같은 큰 방 하나 · 적 없음 · 보스 자리(오른쪽 끝)에 유물. 주우면 승리 (ItemPickupSystem)
                var floor = _generator.GenerateBossRoom(sealedExit: false);
                _world.EnterNewFloor(target, floor.Map);
                _world.Units.Teleport(player.Id, floor.Start);
                _world.FloorItems.Add(floor.Exit, _zones.ZoneOf(target).AltarItemId); // Announce가 그린다
            }
            else if (BossOf(target) != null)
            {
                // 보스 층 (3-4b): 큰 방 하나 + 보스만 (일반 적 · 재생성 없음). 마지막 층 보스가 죽으면 승리,
                // 중간 보스 층(3-17a)은 보스 뒤 봉인된 계단이 열린다 (DeathCheckSystem)
                var floor = _generator.GenerateBossRoom(sealedExit: target < _floorCount);
                _world.EnterNewFloor(target, floor.Map);
                _world.Units.Teleport(player.Id, floor.Start);
                var boss = UnitSpawner.SpawnEnemy(_world, BossOf(target), floor.Exit, _aiFactory(_world, EntityId.None));
                if (IsGhostFloor(target)) GhostBoss.Equip(boss, _ghost); // 3-23b: 기록의 무기 · 갑옷
                _world.Run.SetBoss(boss.Id);
                if (target < _floorCount) newFloorStart = floor.Start; // 3-17a: 중간 보스 층에도 이 층 몫의 아이템 (계획이 빼지 않음)
            }
            else
            {
                bool last = target == _floorCount;
                var floor = _generator.Generate(_world.Rng.Get(RngStream.MapGen), stairsUp: target > 1, stairsDown: !last);
                _world.EnterNewFloor(target, floor.Map);
                if (_decor != null) FloorDecor.Apply(_world, floor, _decor, PuddleOf(target), _traps, _zones == null || _zones.ZoneOf(target).Grass); // 3-11a · 3-12a: 이벤트는 아래 Announce가 맵 · 지형 · 함정을 다시 보낸다
                Shop.Build(_world, floor, _shop, _items, _world.Rng.Get(RngStream.Decor), new IdleNeutralAi()); // 3-15c: 상점 층이면
                _world.Units.Teleport(player.Id, floor.Start);
                if (_zones != null)
                {
                    var kindRng = _world.Rng.Get(RngStream.EnemyKind);
                    FloorPopulator.PopulateEnemies(_world, floor.Start, () => _zones.PickEnemy(target, kindRng), _spawn, _aiFactory(_world, EntityId.None));
                }
                else FloorPopulator.PopulateEnemies(_world, floor.Start, _enemyDef, _spawn, _aiFactory(_world, EntityId.None));
                newFloorStart = floor.Start;
            }

            Announce(player, restored: stored != null);
            if (newFloorStart.HasValue && _loot != null)
            {
                Loot.PlaceFloorItems(_world, newFloorStart.Value, _loot); // 3-5: MapLoaded 뒤에 놓아야 화면이 지우지 않는다
                Loot.PlaceGold(_world, newFloorStart.Value, _loot); // 3-15a
                Loot.PlaceThrown(_world, newFloorStart.Value, _loot); // 3-16a
            }
            _world.Events.Publish(new FloorChangedEvent(from, target));
            _vision.Refresh();
        }

        /// <summary>그 층 웅덩이의 지형 규칙 (3-18): 구역 표의 PuddleTerrain, 규칙이 없으면 물.</summary>
        private TerrainDef PuddleOf(int floor)
        {
            if (_zones == null || _terrains == null) return _water;
            return _terrains.TryGet(_zones.ZoneOf(floor).PuddleTerrain, out var def) ? def : _water;
        }

        /// <summary>그 층의 보스 (구역 표가 있으면 구역 보스 층, 없으면 마지막 층의 bossDef). 보스 층이 아니면 null.</summary>
        private UnitDef BossOf(int floor)
        {
            var boss = _zones != null ? _zones.BossOf(floor) : floor == _floorCount ? _bossDef : null;
            return boss != null && IsGhostFloor(floor) ? _ghostDef : boss;
        }

        /// <summary>지난 회차 플레이어 보스가 나오는 층 (3-23b): 기록이 있으면 마지막 보스 층(구역 표의 가장 깊은 보스 층 — 지금 25층).</summary>
        public bool IsGhostFloor(int floor) => _ghost != null && _ghostDef != null && _zones != null && floor == _zones.LastBossFloor;

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
            foreach (var pair in _world.Traps.ToSortedList()) // 3-12a: 드러난 함정만
                if (!pair.Value.Hidden) events.Publish(new TrapChangedEvent(_world.Traps.PosOf(pair.Key), pair.Value.Type, true));
            foreach (var item in _world.FloorItems.All)
                events.Publish(new FloorItemPlacedEvent(item.Pos, item.ItemId, ProjectM.Items.Identification.VisualOf(_world, item.ItemId)));
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
