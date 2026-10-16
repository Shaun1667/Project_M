using System;
using System.Collections.Generic;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Core
{
    /// <summary>
    /// 한 판의 시뮬레이션 상태 묶음. 저장 대상 상태는 모두 여기(또는 여기서 닿는 객체)에 있어야 하며,
    /// AI(ICommandSource)와 단계 시스템(ITickSystem)은 상태를 갖지 않고 World의 데이터만 읽고 쓴다.
    /// 그래야 Capture → Restore 후 같은 결과가 나온다 (스냅샷 왕복 테스트로 검사).
    /// </summary>
    public sealed class World
    {
        public GridMap Map { get; }
        public EntityIdAllocator Ids { get; }
        public RngSet Rng { get; }
        public TickScheduler Scheduler { get; }
        public PlayerCommandSource PlayerInput { get; }
        public UnitRegistry Units { get; }

        /// <summary>플레이어가 한 번이라도 본 칸 (탐험 기록, 1-7). 인덱스 = y * Map.Width + x. PlayerVisionSystem이 채운다.</summary>
        public bool[] Explored { get; }

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
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Ids = ids;
            Rng = rng;
            Scheduler = scheduler;
            PlayerInput = new PlayerCommandSource();
            Units = new UnitRegistry();
            Explored = new bool[map.Width * map.Height];
            Events = new SimEventBus();
        }

        /// <summary>
        /// 기본 단계 시스템을 등록한다: ④ 상태 처리 → ⑤ 마나 회복 → ⑥ 사망 판정 → ⑥ 끝 플레이어 시야 갱신 → ⑦ 이벤트 발행.
        /// 행동 규칙(마비 = 행동 불가, 둔화 = 비용 추가)도 스케줄러에 연결한다.
        /// 새 World · 복원한 World 모두 한 번 부른다. 돌려준 시야 시스템의 Refresh()로 층 시작 · 불러오기 직후 시야를 한 번 계산한다.
        /// </summary>
        public PlayerVisionSystem InstallCoreSystems(ISightRule sight, MagicConfig magic, StatusBook statuses)
        {
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            var vision = new PlayerVisionSystem(this, sight);
            Scheduler.SetActionRules(id => StatusEffects.CanAct(this, statuses, id),
                (id, cost) => StatusEffects.AdjustCost(this, statuses, id, cost));
            Scheduler.AddSystem(TickPhase.Status, new StatusSystem(this, statuses));
            Scheduler.AddSystem(TickPhase.ManaRegen, new ManaRegenSystem(this, magic));
            Scheduler.AddSystem(TickPhase.DeathCheck, new DeathCheckSystem(this));
            Scheduler.AddSystem(TickPhase.DeathCheck, vision);
            Scheduler.AddSystem(TickPhase.EventFlush, new EventFlushSystem(Events));
            return vision;
        }

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

            return new WorldSnapshot
            {
                CurrentTick = Scheduler.CurrentTick,
                MapWidth = Map.Width,
                MapHeight = Map.Height,
                Cells = Map.CopyCells(),
                Explored = (bool[])Explored.Clone(),
                NextEntityId = Ids.NextValue,
                RngSeed = Rng.Seed,
                RngStreams = streams.ToArray(),
                Actors = Scheduler.CaptureActors().ToArray(),
                Units = CaptureUnits(),
            };
        }

        private UnitState[] CaptureUnits()
        {
            var units = Units.ToSortedList();
            var states = new UnitState[units.Count];
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                var seen = u.LastSeenTargetPos;
                states[i] = new UnitState(u.Id, u.Faction, u.VisualId, u.MaxHp, u.AttackDamage, u.SightRange, u.Pos.X, u.Pos.Y, u.Hp,
                    seen.HasValue, seen?.X ?? 0, seen?.Y ?? 0, u.MaxMana, u.Mana, (string[])u.SpellSlots.Clone(),
                    u.PreviousPos, u.WanderTarget, u.Statuses.ConvertAll(s => new UnitStatusState(s.Type, s.Remaining)).ToArray());
            }
            return states;
        }

        /// <summary>
        /// 스냅샷에서 World를 다시 만든다. AI 행위자의 ICommandSource는 aiFactory가 새 World 기준으로 만들어 준다.
        /// 단계 시스템(ITickSystem)은 저장 대상이 아니므로 호출한 쪽이 다시 등록한다.
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
            if (snapshot.Explored.Length != world.Explored.Length)
                throw new InvalidOperationException($"탐험 기록 크기 {snapshot.Explored.Length}가 맵과 맞지 않는다.");
            Array.Copy(snapshot.Explored, world.Explored, snapshot.Explored.Length);

            foreach (var u in snapshot.Units)
            {
                GridPos? seen = u.HasLastSeen ? new GridPos(u.LastSeenX, u.LastSeenY) : (GridPos?)null;
                var unit = new Unit(u.Id, u.Faction, u.VisualId, u.MaxHp, u.AttackDamage, u.SightRange, new GridPos(u.X, u.Y), u.Hp, seen,
                    u.MaxMana, u.Mana, (string[])u.SpellSlots.Clone(), u.PreviousPos, u.WanderTarget);
                foreach (var st in u.Statuses)
                    unit.Statuses.Add(new StatusInstance(st.Type, st.Remaining));
                world.Units.Add(unit);
            }

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
