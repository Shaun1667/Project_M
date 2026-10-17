using System;
using System.Collections.Generic;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Core
{
    /// <summary>
    /// 한 판의 시뮬레이션 상태 묶음. 저장 대상 상태는 모두 여기(또는 여기서 닿는 객체)에 있어야 하며,
    /// AI(ICommandSource)와 단계 시스템(ITickSystem)은 상태를 갖지 않고 World의 데이터만 읽고 쓴다.
    /// 그래야 Capture → Restore 후 같은 결과가 나온다 (스냅샷 왕복 테스트로 검사).
    /// 층 (3-4a): Map · Explored · Terrain · 유닛은 지금 층의 것이고, 떠난 층은 StoredFloors에 멈춘 채 보존된다.
    /// ID 발급기 · 난수 · 스케줄러(틱)는 한 판 전체가 같이 쓴다 — 층이 바뀌어도 ID가 겹치지 않고, 같은 시드 = 같은 층.
    /// </summary>
    public sealed class World
    {
        public GridMap Map { get; private set; }
        public EntityIdAllocator Ids { get; }
        public RngSet Rng { get; }
        public TickScheduler Scheduler { get; }
        public PlayerCommandSource PlayerInput { get; }
        public UnitRegistry Units { get; }

        /// <summary>플레이어가 한 번이라도 본 칸 (탐험 기록, 1-7). 인덱스 = y * Map.Width + x. PlayerVisionSystem이 채운다. 지금 층의 것.</summary>
        public bool[] Explored { get; private set; }

        /// <summary>지형 효과 레이어 (2-4): 기본 지형 위에 놓인 화염 지대 · 독구름 등. TerrainEffects로만 놓는다. 지금 층의 것.</summary>
        public TerrainLayer Terrain { get; private set; }

        /// <summary>지금 층 번호 (1부터, 3-4a).</summary>
        public int Floor { get; private set; } = 1;

        /// <summary>한 판 기록 (3-4b): 처치 수 · 결과 · 보스 · 사망 원인.</summary>
        public RunState Run { get; } = new RunState();

        /// <summary>지금까지 가 본 가장 깊은 층 (3-4b 결과 화면).</summary>
        public int DeepestFloor
        {
            get
            {
                int deepest = Floor;
                foreach (var f in _storedFloors) deepest = Math.Max(deepest, f.Floor);
                return deepest;
            }
        }

        private readonly List<StoredFloorState> _storedFloors = new List<StoredFloorState>();
        /// <summary>떠난 층들, 층 번호 오름차순 (3-4a).</summary>
        public IReadOnlyList<StoredFloorState> StoredFloors => _storedFloors;

        /// <summary>표현 레이어로 가는 이벤트. 저장하지 않는다 (복원 후 화면은 MapLoaded 등으로 다시 그린다).</summary>
        public SimEventBus Events { get; }

        public World(GridMap map, ulong seed)
            : this(map, new EntityIdAllocator(), new RngSet(seed), new TickScheduler())
        {
        }

        /// <summary>
        /// 새 판: 시드에서 난수 묶음을 만들고, 맵 생성 난수(RngStream.MapGen)로 맵을 만든 뒤 World를 만든다.
        /// 맵 생성에 쓴 난수 소비가 World의 MapGen 수열에 그대로 남아, 다음 층 생성이 같은 맵을 반복하지 않는다.
        /// </summary>
        public static World Create(ulong seed, Func<IRng, GridMap> buildMap)
        {
            if (buildMap == null) throw new ArgumentNullException(nameof(buildMap));
            var rng = new RngSet(seed);
            var map = buildMap(rng.Get(RngStream.MapGen));
            return new World(map, new EntityIdAllocator(), rng, new TickScheduler());
        }

        private World(GridMap map, EntityIdAllocator ids, RngSet rng, TickScheduler scheduler)
        {
            Ids = ids;
            Rng = rng;
            Scheduler = scheduler;
            PlayerInput = new PlayerCommandSource();
            Units = new UnitRegistry();
            Events = new SimEventBus();
            SetFloorMap(map);
        }

        private void SetFloorMap(GridMap map)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Explored = new bool[map.Width * map.Height];
            Terrain = new TerrainLayer(map.Width, map.Height);
        }

        /// <summary>
        /// 기본 단계 시스템을 등록한다: ③ 지형 처리 → ④ 상태 처리 → ⑤ 마나 회복 → ⑥ 사망 판정 → ⑥ 끝 플레이어 시야 갱신 → ⑦ 이벤트 발행.
        /// 행동 규칙(마비 = 행동 불가, 둔화 = 비용 추가)도 스케줄러에 연결한다.
        /// 새 World · 복원한 World 모두 한 번 부른다. 돌려준 시야 시스템의 Refresh()로 층 시작 · 불러오기 직후 시야를 한 번 계산한다.
        /// 적 재생성(EnemyRespawnSystem)은 적 데이터가 필요해 부른 쪽이 따로 등록한다. 층 이동(FloorTransition)은 틱 밖 동작이라 시스템이 아니다 (3-4a).
        /// </summary>
        public PlayerVisionSystem InstallCoreSystems(ISightRule sight, MagicConfig magic, StatusBook statuses, TerrainBook terrains)
        {
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            if (terrains == null) throw new ArgumentNullException(nameof(terrains));
            var vision = new PlayerVisionSystem(this, sight);
            Scheduler.SetActionRules(id => StatusEffects.CanAct(this, statuses, id),
                (id, cost) => StatusEffects.AdjustCost(this, statuses, id, cost));
            Scheduler.AddSystem(TickPhase.Terrain, new TerrainSystem(this, terrains, statuses));
            Scheduler.AddSystem(TickPhase.Status, new StatusSystem(this, statuses));
            Scheduler.AddSystem(TickPhase.ManaRegen, new ManaRegenSystem(this, magic));
            Scheduler.AddSystem(TickPhase.ManaRegen, new HpRegenSystem(this, magic));
            Scheduler.AddSystem(TickPhase.DeathCheck, new DeathCheckSystem(this));
            Scheduler.AddSystem(TickPhase.DeathCheck, vision);
            Scheduler.AddSystem(TickPhase.EventFlush, new EventFlushSystem(Events));
            return vision;
        }

        // ── 층 (3-4a) ──

        public bool HasStoredFloor(int floor) => _storedFloors.Exists(f => f.Floor == floor);

        /// <summary>떠난 층 기록 (도착 칸 계산용, 바꾸지 않는다). 없으면 null.</summary>
        public StoredFloorState FindStoredFloor(int floor) => _storedFloors.Find(f => f.Floor == floor);

        /// <summary>
        /// 지금 층을 StoredFloors에 넣고 비운다: keep(플레이어) 말고 모든 유닛 · 행위자를 빼고 EntityRemovedEvent를 낸다.
        /// 행위자 ReadyTick은 지금 틱과의 차이로 저장한다 (돌아오면 그만큼 뒤에 다시 행동 — "멈춤"). 틱 사이에만 부른다.
        /// </summary>
        public void StoreCurrentFloor(EntityId keep)
        {
            var units = new List<UnitState>();
            var actors = new List<ActorState>();
            foreach (var u in Units.ToSortedList())
            {
                if (u.Id == keep) continue;
                units.Add(ToState(u));
                if (Scheduler.IsRegistered(u.Id))
                    actors.Add(new ActorState(u.Id, Scheduler.GetReadyTick(u.Id) - Scheduler.CurrentTick, false));
            }
            _storedFloors.RemoveAll(f => f.Floor == Floor);
            _storedFloors.Add(new StoredFloorState
            {
                Floor = Floor,
                MapWidth = Map.Width,
                MapHeight = Map.Height,
                Cells = Map.CopyCells(),
                Explored = (bool[])Explored.Clone(),
                Terrain = CaptureTerrain(),
                Units = units.ToArray(),
                Actors = actors.ToArray(),
            });
            _storedFloors.Sort((a, b) => a.Floor.CompareTo(b.Floor));

            foreach (var u in units)
            {
                Scheduler.Unregister(u.Id);
                Units.Remove(u.Id);
                Events.Publish(new EntityRemovedEvent(u.Id));
            }
        }

        /// <summary>처음 가는 층: 새 맵으로 바꾼다 (탐험 기록 · 지형 효과는 비어 있음). 유닛은 부른 쪽이 놓는다.</summary>
        public void EnterNewFloor(int floor, GridMap map)
        {
            if (HasStoredFloor(floor)) throw new InvalidOperationException($"{floor}층은 이미 있다 — RestoreStoredFloor를 쓴다.");
            Floor = floor;
            SetFloorMap(map);
        }

        /// <summary>
        /// 떠났던 층으로 돌아간다: 맵 · 탐험 기록 · 지형 · 유닛 · 행위자를 떠날 때 그대로 되돌리고 StoredFloors에서 뺀다.
        /// 표현용 이벤트(MapLoaded · 유닛 생성 등)는 부른 쪽이 낸다.
        /// </summary>
        public void RestoreStoredFloor(int floor, Func<World, EntityId, ICommandSource> aiFactory)
        {
            if (aiFactory == null) throw new ArgumentNullException(nameof(aiFactory));
            var stored = _storedFloors.Find(f => f.Floor == floor) ?? throw new InvalidOperationException($"{floor}층 기록이 없다.");
            _storedFloors.Remove(stored);
            Floor = floor;
            LoadFloorData(this, stored.MapWidth, stored.MapHeight, stored.Cells, stored.Explored, stored.Terrain, stored.Units);
            foreach (var a in stored.Actors)
                Scheduler.RegisterActor(a.Id, aiFactory(this, a.Id), Scheduler.CurrentTick + a.ReadyTick);
        }

        // ── 저장 ──

        /// <summary>
        /// 지금 상태를 스냅샷으로 만든다. 틱 사이, 처리되지 않은 플레이어 입력이 없을 때만 가능하다.
        /// </summary>
        public WorldSnapshot Capture()
        {
            if (PlayerInput.HasCommand) throw new InvalidOperationException("처리되지 않은 플레이어 입력이 있을 때는 저장할 수 없다.");

            var rngStates = Rng.CaptureStates();
            var streams = new List<RngStreamState>(rngStates.Count);
            foreach (var pair in rngStates)
                streams.Add(new RngStreamState(pair.Key, pair.Value));
            streams.Sort((a, b) => ((int)a.Stream).CompareTo((int)b.Stream));

            var units = Units.ToSortedList();
            return new WorldSnapshot
            {
                CurrentTick = Scheduler.CurrentTick,
                MapWidth = Map.Width,
                MapHeight = Map.Height,
                Cells = Map.CopyCells(),
                Explored = (bool[])Explored.Clone(),
                Terrain = CaptureTerrain(),
                NextEntityId = Ids.NextValue,
                RngSeed = Rng.Seed,
                RngStreams = streams.ToArray(),
                Actors = Scheduler.CaptureActors().ToArray(),
                Units = units.ConvertAll(ToState).ToArray(),
                Floor = Floor,
                StoredFloors = _storedFloors.ToArray(),
                Kills = Run.Kills,
                Outcome = Run.Outcome,
                BossId = Run.BossId,
                LastPlayerDamage = Run.LastPlayerDamage,
                LastPlayerAttacker = Run.LastPlayerAttacker,
            };
        }

        private TerrainCellState[] CaptureTerrain()
        {
            var list = new List<TerrainCellState>();
            foreach (var pair in Terrain.ToSortedList())
                foreach (var t in pair.Value)
                    list.Add(new TerrainCellState(pair.Key, t.Type, t.Remaining));
            return list.ToArray();
        }

        private static UnitState ToState(Unit u)
        {
            var seen = u.LastSeenTargetPos;
            return new UnitState(u.Id, u.Faction, u.VisualId, u.MaxHp, u.AttackDamage, u.SightRange, u.Pos.X, u.Pos.Y, u.Hp,
                seen.HasValue, seen?.X ?? 0, seen?.Y ?? 0, u.MaxMana, u.Mana, (InventoryRef[])u.InventorySlots.Clone(), (InventoryRef[])u.QuickSlots.Clone(),
                u.PreviousPos, u.WanderTarget, u.Statuses.ConvertAll(s => new UnitStatusState(s.Type, s.Remaining)).ToArray());
        }

        private static Unit ToUnit(UnitState u)
        {
            GridPos? seen = u.HasLastSeen ? new GridPos(u.LastSeenX, u.LastSeenY) : (GridPos?)null;
            var unit = new Unit(u.Id, u.Faction, u.VisualId, u.MaxHp, u.AttackDamage, u.SightRange, new GridPos(u.X, u.Y), u.Hp, seen,
                u.MaxMana, u.Mana, (InventoryRef[])u.InventorySlots.Clone(), (InventoryRef[])u.QuickSlots.Clone(), u.PreviousPos, u.WanderTarget);
            foreach (var st in u.Statuses)
                unit.Statuses.Add(new StatusInstance(st.Type, st.Remaining));
            return unit;
        }

        /// <summary>맵 · 탐험 기록 · 지형 · 유닛을 world의 지금 층으로 넣는다 (복원 · 돌아가기 공용).</summary>
        private static void LoadFloorData(World world, int width, int height, TileType[] cells, bool[] explored, TerrainCellState[] terrain, UnitState[] units)
        {
            world.SetFloorMap(GridMap.FromCells(width, height, cells));
            if (explored.Length != world.Explored.Length)
                throw new InvalidOperationException($"탐험 기록 크기 {explored.Length}가 맵과 맞지 않는다.");
            Array.Copy(explored, world.Explored, explored.Length);
            foreach (var t in terrain)
                world.Terrain.Place(world.Terrain.PosOf(t.CellIndex), t.Type, t.Remaining);
            foreach (var u in units)
                world.Units.Add(ToUnit(u));
        }

        /// <summary>
        /// 스냅샷에서 World를 다시 만든다. AI 행위자의 ICommandSource는 aiFactory가 새 World 기준으로 만들어 준다.
        /// 단계 시스템(ITickSystem)은 저장 대상이 아니므로 호출한 쪽이 다시 등록한다. 떠난 층은 데이터 그대로 보존된다.
        /// </summary>
        public static World Restore(WorldSnapshot snapshot, Func<World, EntityId, ICommandSource> aiFactory)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (aiFactory == null) throw new ArgumentNullException(nameof(aiFactory));
            if (snapshot.FormatVersion != WorldSnapshot.CurrentFormatVersion)
                throw new InvalidOperationException($"지원하지 않는 스냅샷 버전 {snapshot.FormatVersion}.");

            var map = GridMap.FromCells(snapshot.MapWidth, snapshot.MapHeight, snapshot.Cells);
            var ids = new EntityIdAllocator(snapshot.NextEntityId);

            var rng = new RngSet(snapshot.RngSeed);
            var states = new Dictionary<RngStream, Pcg32State>(snapshot.RngStreams.Length);
            foreach (var s in snapshot.RngStreams)
                states.Add(s.Stream, s.State);
            rng.RestoreStates(states);

            var scheduler = new TickScheduler(snapshot.CurrentTick);
            var world = new World(map, ids, rng, scheduler);
            LoadFloorData(world, snapshot.MapWidth, snapshot.MapHeight, snapshot.Cells, snapshot.Explored, snapshot.Terrain, snapshot.Units);
            world.Floor = snapshot.Floor;
            world._storedFloors.AddRange(snapshot.StoredFloors);
            world.Run.Kills = snapshot.Kills;
            world.Run.Outcome = snapshot.Outcome;
            world.Run.BossId = snapshot.BossId;
            world.Run.LastPlayerDamage = snapshot.LastPlayerDamage;
            world.Run.LastPlayerAttacker = snapshot.LastPlayerAttacker;

            foreach (var actor in snapshot.Actors)
            {
                if (actor.IsPlayer)
                    scheduler.RegisterPlayer(actor.Id, world.PlayerInput, actor.ReadyTick);
                else
                    scheduler.RegisterActor(actor.Id, aiFactory(world, actor.Id), actor.ReadyTick);
            }

            return world;
        }
    }
}
