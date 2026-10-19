using System;
using System.Collections.Generic;

namespace ProjectM.Core
{
    /// <summary>
    /// 틱 파이프라인 (CLAUDE.md 핵심 설계 규칙 3 · 4). Tick() 한 번 = 1틱 진행이며, 누가 언제 Tick()을 부르는지는 시계(ITickClock)가 정한다.
    /// 한 틱: ① 준비된 행위자 행동(플레이어 → 적 EntityId 순) → ②~⑦ 단계 시스템(TickPhase 순서) → 틱 번호 증가.
    /// </summary>
    public sealed class TickScheduler
    {
        private sealed class ActorEntry
        {
            public readonly EntityId Id;
            public readonly ICommandSource Source;
            public long ReadyTick;

            public ActorEntry(EntityId id, ICommandSource source, long readyTick)
            {
                Id = id;
                Source = source;
                ReadyTick = readyTick;
            }
        }

        private readonly Dictionary<EntityId, ActorEntry> _actors = new Dictionary<EntityId, ActorEntry>();
        private readonly List<ITickSystem>[] _systems;
        private readonly List<ActorEntry> _turnOrder = new List<ActorEntry>();

        private EntityId _playerId = EntityId.None;
        private PlayerCommandSource _playerSource;
        private bool _ticking;

        // 행동 규칙 (2-3, 상태 없음 — 복원 후 다시 연결): 행동 가능 여부(마비) · 비용 조정(둔화)
        private Func<EntityId, bool> _canAct;
        private Func<EntityId, int, int> _adjustCost;

        /// <summary>
        /// 행동 규칙을 연결한다. canAct가 false면 차례가 와도 커맨드를 받지 않고 1틱 뒤로 미룬다 (플레이어 입력도 기다리지 않음).
        /// adjustCost는 커맨드 비용을 고친다 (결과 1 이상). 둘 다 상태를 갖지 않는 규칙이어야 한다.
        /// </summary>
        public void SetActionRules(Func<EntityId, bool> canAct, Func<EntityId, int, int> adjustCost)
        {
            _canAct = canAct;
            _adjustCost = adjustCost;
        }

        /// <summary>다음에 처리할 틱 번호.</summary>
        public long CurrentTick { get; private set; }

        public EntityId PlayerId => _playerId;

        public TickScheduler(long startTick = 0)
        {
            if (startTick < 0) throw new ArgumentOutOfRangeException(nameof(startTick));
            CurrentTick = startTick;

            int phaseCount = Enum.GetValues(typeof(TickPhase)).Length;
            _systems = new List<ITickSystem>[phaseCount];
            for (int i = 0; i < phaseCount; i++)
                _systems[i] = new List<ITickSystem>();
        }

        /// <summary>플레이어를 등록한다. 플레이어는 하나뿐이다.</summary>
        public void RegisterPlayer(EntityId id, PlayerCommandSource source)
        {
            RegisterPlayer(id, source, DefaultReadyTick());
        }

        /// <summary>스냅샷 복원용: 다음 행동 틱을 지정해 플레이어를 등록한다.</summary>
        public void RegisterPlayer(EntityId id, PlayerCommandSource source, long readyTick)
        {
            if (_playerId.IsValid) throw new InvalidOperationException("플레이어가 이미 등록돼 있다: " + _playerId);
            AddActor(id, source, readyTick);
            _playerId = id;
            _playerSource = source;
        }

        /// <summary>
        /// 적 등 AI 행위자를 등록한다. 틱 처리 도중에 등록되면 그 틱에는 행동하지 않고 다음 틱부터 행동한다.
        /// </summary>
        public void RegisterActor(EntityId id, ICommandSource source)
        {
            AddActor(id, source, DefaultReadyTick());
        }

        /// <summary>스냅샷 복원용: 다음 행동 틱을 지정해 행위자를 등록한다.</summary>
        public void RegisterActor(EntityId id, ICommandSource source, long readyTick)
        {
            AddActor(id, source, readyTick);
        }

        /// <summary>스냅샷용: 등록된 행위자 상태를 EntityId 오름차순으로 돌려준다.</summary>
        public List<ActorState> CaptureActors()
        {
            if (_ticking) throw new InvalidOperationException("틱 처리 중에는 상태를 저장할 수 없다.");
            var list = new List<ActorState>(_actors.Count);
            foreach (var entry in _actors.Values)
                list.Add(new ActorState(entry.Id, entry.ReadyTick, entry.Id == _playerId));
            list.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));
            return list;
        }

        /// <summary>행위자를 뺀다 (사망 등). 틱 처리 도중에 빠지면 그 틱의 남은 차례에서 제외된다.</summary>
        public bool Unregister(EntityId id)
        {
            if (!_actors.Remove(id)) return false;
            if (id == _playerId)
            {
                _playerId = EntityId.None;
                _playerSource = null;
            }
            return true;
        }

        public bool IsRegistered(EntityId id) => _actors.ContainsKey(id);

        /// <summary>행동 규칙(SetActionRules)으로 지금 행동할 수 있는지 — 마비 · 빙결이면 false. 규칙이 없으면 true. 회피 0 판정(3-7)이 쓴다.</summary>
        public bool CanAct(EntityId id) => _canAct == null || _canAct(id);

        /// <summary>그 행위자가 다음으로 행동할 수 있는 틱.</summary>
        public long GetReadyTick(EntityId id) => GetEntry(id).ReadyTick;

        /// <summary>틱 단계에 시스템을 등록한다. 같은 단계 안에서는 등록 순서대로 실행된다.</summary>
        public void AddSystem(TickPhase phase, ITickSystem system)
        {
            if (phase == TickPhase.Actions) throw new ArgumentException("① 행동 단계는 TickScheduler가 직접 처리한다.", nameof(phase));
            if (system == null) throw new ArgumentNullException(nameof(system));
            _systems[(int)phase].Add(system);
        }

        /// <summary>플레이어가 이번 틱에 행동해야 하는데 입력이 없다 → 턴제에서는 여기서 멈춘다.</summary>
        public bool IsWaitingForPlayer =>
            _playerId.IsValid && _actors[_playerId].ReadyTick <= CurrentTick && !_playerSource.HasCommand
            && (_canAct == null || _canAct(_playerId));

        /// <summary>1틱 진행.</summary>
        public void Tick()
        {
            if (_ticking) throw new InvalidOperationException("틱 처리 중에 Tick()을 다시 부를 수 없다.");
            if (IsWaitingForPlayer) throw new InvalidOperationException("플레이어 입력 대기 중이다.");

            _ticking = true;
            try
            {
                long tick = CurrentTick;
                RunActions(tick);

                var context = new TickContext(tick, EntityId.None);
                for (int phase = (int)TickPhase.Actions + 1; phase < _systems.Length; phase++)
                {
                    var systems = _systems[phase];
                    for (int i = 0; i < systems.Count; i++)
                        systems[i].Run(context);
                }

                CurrentTick = tick + 1;
            }
            finally
            {
                _ticking = false;
            }
        }

        private void RunActions(long tick)
        {
            _turnOrder.Clear();
            foreach (var entry in _actors.Values)
            {
                if (entry.ReadyTick <= tick)
                    _turnOrder.Add(entry);
            }
            _turnOrder.Sort(CompareTurnOrder);

            for (int i = 0; i < _turnOrder.Count; i++)
            {
                var entry = _turnOrder[i];
                // 앞선 행동으로 빠진(사망한) 행위자는 건너뛴다.
                if (!_actors.TryGetValue(entry.Id, out var current) || current != entry) continue;

                // 행동 불가(마비): 커맨드를 받지 않고 다음 틱으로 미룬다
                if (_canAct != null && !_canAct(entry.Id))
                {
                    entry.ReadyTick = tick + 1;
                    continue;
                }

                var context = new TickContext(tick, entry.Id);
                var command = entry.Source.NextCommand(context);
                if (command == null) throw new InvalidOperationException(entry.Id + "의 커맨드가 null이다.");
                if (command.CostTicks < 1) throw new InvalidOperationException(entry.Id + "의 행동 비용이 1틱 미만이다: " + command.CostTicks);

                command.Execute(context);
                int cost = _adjustCost != null ? _adjustCost(entry.Id, command.CostTicks) : command.CostTicks;
                if (cost < 1) throw new InvalidOperationException(entry.Id + "의 조정된 행동 비용이 1틱 미만이다: " + cost);
                entry.ReadyTick = tick + cost;
            }
        }

        private int CompareTurnOrder(ActorEntry a, ActorEntry b)
        {
            bool aPlayer = a.Id == _playerId;
            bool bPlayer = b.Id == _playerId;
            if (aPlayer != bPlayer) return aPlayer ? -1 : 1;
            return a.Id.Value.CompareTo(b.Id.Value);
        }

        private long DefaultReadyTick() => _ticking ? CurrentTick + 1 : CurrentTick;

        private void AddActor(EntityId id, ICommandSource source, long readyTick)
        {
            if (!id.IsValid) throw new ArgumentException("유효하지 않은 EntityId.", nameof(id));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (readyTick < 0) throw new ArgumentOutOfRangeException(nameof(readyTick), readyTick, "행동 틱은 0 이상이어야 한다.");
            if (_actors.ContainsKey(id)) throw new InvalidOperationException("이미 등록된 행위자: " + id);

            _actors.Add(id, new ActorEntry(id, source, readyTick));
        }

        private ActorEntry GetEntry(EntityId id)
        {
            if (!_actors.TryGetValue(id, out var entry)) throw new KeyNotFoundException("등록되지 않은 행위자: " + id);
            return entry;
        }
    }
}
