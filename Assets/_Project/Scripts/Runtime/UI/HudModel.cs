using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.UI
{
    /// <summary>
    /// HUD가 보일 값과 전투 로그를 이벤트로만 모은다 (2-5). 엔진 타입을 쓰지 않아 테스트에서 그대로 돌린다.
    /// 화면 그리기는 HudPresenter. 로그는 최근 N줄, 같은 상태의 지속 갱신(이미 걸려 있는데 다시 걸림)은 남기지 않는다.
    /// </summary>
    public sealed class HudModel
    {
        private readonly int _maxLogLines;
        private readonly List<string> _log = new List<string>();
        private readonly SortedDictionary<StatusType, int> _playerStatuses = new SortedDictionary<StatusType, int>();
        private readonly Dictionary<EntityId, HashSet<StatusType>> _activeStatuses = new Dictionary<EntityId, HashSet<StatusType>>();
        private SimEventBus _bus;
        private EntityId _playerId;
        private string[] _spellSlots = new string[0];
        private int[] _slotCosts = new int[0];
        private SpellBook _spellBook;

        public HudModel(int maxLogLines, HudText text = null)
        {
            if (maxLogLines < 1) throw new ArgumentOutOfRangeException(nameof(maxLogLines));
            _maxLogLines = maxLogLines;
            Text = text ?? new HudText();
        }

        public HudText Text { get; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public int Mana { get; private set; }
        public int MaxMana { get; private set; }
        public bool Dead { get; private set; }
        public IReadOnlyList<string> SpellSlots => _spellSlots;
        /// <summary>칸마다 마나 비용, 주문이 없으면 -1.</summary>
        public IReadOnlyList<int> SlotCosts => _slotCosts;
        /// <summary>플레이어 상태 → 남은 틱 (종류 순).</summary>
        public IReadOnlyDictionary<StatusType, int> PlayerStatuses => _playerStatuses;
        /// <summary>오래된 줄이 앞, 새 줄이 뒤.</summary>
        public IReadOnlyList<string> Log => _log;

        /// <summary>값이 바뀌면 (이벤트 하나마다) 알린다.</summary>
        public event Action Changed;

        public void Bind(SimEventBus bus, Unit player, SpellBook spellBook = null)
        {
            Unbind();
            _bus = bus;
            _playerId = player.Id;
            Hp = player.Hp;
            MaxHp = player.MaxHp;
            Mana = player.Mana;
            MaxMana = player.MaxMana;
            Dead = false;
            _spellBook = spellBook;
            SetSlots(player.SpellSlots);
            _log.Clear();
            _playerStatuses.Clear();
            _activeStatuses.Clear();
            foreach (var s in player.Statuses)
            {
                _playerStatuses[s.Type] = s.Remaining;
                Active(_playerId).Add(s.Type);
            }

            _bus.Subscribe<UnitDamagedEvent>(OnDamaged);
            _bus.Subscribe<UnitDiedEvent>(OnDied);
            _bus.Subscribe<ManaChangedEvent>(OnManaChanged);
            _bus.Subscribe<SpellCastEvent>(OnSpellCast);
            _bus.Subscribe<StatusChangedEvent>(OnStatusChanged);
            _bus.Subscribe<SpellSlotsChangedEvent>(OnSlotsChanged);
            _bus.Subscribe<ReactionTriggeredEvent>(OnReaction);
            Changed?.Invoke();
        }

        private void SetSlots(string[] slots)
        {
            _spellSlots = (string[])slots.Clone();
            _slotCosts = new int[_spellSlots.Length];
            for (int i = 0; i < _spellSlots.Length; i++)
                _slotCosts[i] = _spellBook != null && _spellSlots[i] != null && _spellBook.TryGet(_spellSlots[i], out var spell) ? spell.ManaCost : -1;
        }

        private void OnSlotsChanged(SpellSlotsChangedEvent e)
        {
            if (e.Id != _playerId) return;
            SetSlots(e.Slots);
            Changed?.Invoke();
        }

        // 반응 로그 (3-1): "독화! 적 #3" · 빈 칸의 지형 반응은 이름만
        private void OnReaction(ReactionTriggeredEvent e)
        {
            AddLog(e.Target.IsValid ? $"{Text.Reaction(e.ReactionId)}! {Who(e.Target)}" : $"{Text.Reaction(e.ReactionId)}!");
            Changed?.Invoke();
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<UnitDamagedEvent>(OnDamaged);
            _bus.Unsubscribe<UnitDiedEvent>(OnDied);
            _bus.Unsubscribe<ManaChangedEvent>(OnManaChanged);
            _bus.Unsubscribe<SpellCastEvent>(OnSpellCast);
            _bus.Unsubscribe<StatusChangedEvent>(OnStatusChanged);
            _bus.Unsubscribe<SpellSlotsChangedEvent>(OnSlotsChanged);
            _bus.Unsubscribe<ReactionTriggeredEvent>(OnReaction);
            _bus = null;
        }

        private HashSet<StatusType> Active(EntityId id)
        {
            if (!_activeStatuses.TryGetValue(id, out var set))
            {
                set = new HashSet<StatusType>();
                _activeStatuses[id] = set;
            }
            return set;
        }

        private string Who(EntityId id) => id == _playerId ? "나" : HudText.Enemy(id);

        private void OnDamaged(UnitDamagedEvent e)
        {
            bool fromStatus = !e.Attacker.IsValid; // 상태 이상(화상 · 중독) · 지형 피해
            if (e.Target == _playerId)
            {
                Hp = e.Hp;
                AddLog(fromStatus
                    ? $"나: 지속 피해 {e.Amount} (HP {e.Hp}/{e.MaxHp})"
                    : $"{HudText.Enemy(e.Attacker)}의 공격: 피해 {e.Amount} (HP {e.Hp}/{e.MaxHp})");
            }
            else if (e.Attacker == _playerId)
            {
                AddLog($"{HudText.Enemy(e.Target)}에게 피해 {e.Amount} ({e.Hp}/{e.MaxHp})");
            }
            else if (fromStatus)
            {
                AddLog($"{HudText.Enemy(e.Target)}: 지속 피해 {e.Amount} ({e.Hp}/{e.MaxHp})");
            }
            Changed?.Invoke();
        }

        private void OnStatusChanged(StatusChangedEvent e)
        {
            var active = Active(e.Id);
            bool wasActive = active.Contains(e.Type);
            if (e.Remaining > 0) active.Add(e.Type);
            else active.Remove(e.Type);

            if (e.Id == _playerId)
            {
                if (e.Remaining > 0) _playerStatuses[e.Type] = e.Remaining;
                else _playerStatuses.Remove(e.Type);
            }

            if (e.Applied && !wasActive) AddLog($"{Who(e.Id)}: {HudText.Status(e.Type)} {e.Remaining}턴");
            else if (!e.Applied && e.Remaining <= 0) AddLog($"{Who(e.Id)}: {HudText.Status(e.Type)} 풀림");
            Changed?.Invoke();
        }

        private void OnSpellCast(SpellCastEvent e)
        {
            if (e.Caster != _playerId) return;
            AddLog($"{Text.Spell(e.SpellId)} 시전");
            Changed?.Invoke();
        }

        private void OnManaChanged(ManaChangedEvent e)
        {
            if (e.Id != _playerId) return;
            Mana = e.Mana;
            MaxMana = e.MaxMana;
            Changed?.Invoke();
        }

        private void OnDied(UnitDiedEvent e)
        {
            _activeStatuses.Remove(e.Id);
            if (e.Id == _playerId)
            {
                Dead = true;
                Hp = 0;
                _playerStatuses.Clear();
                AddLog("쓰러졌다.");
            }
            else
            {
                AddLog($"{HudText.Enemy(e.Id)} 쓰러짐");
            }
            Changed?.Invoke();
        }

        private void AddLog(string line)
        {
            _log.Add(line);
            if (_log.Count > _maxLogLines) _log.RemoveRange(0, _log.Count - _maxLogLines);
        }
    }
}
