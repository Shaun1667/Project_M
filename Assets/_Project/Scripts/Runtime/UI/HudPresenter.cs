using System.Collections.Generic;
using System.Text;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using TMPro;
using UnityEngine;
using EntityId = ProjectM.Core.EntityId; // Unity 6의 UnityEngine.EntityId와 구분

namespace ProjectM.UI
{
    /// <summary>
    /// 그레이박스 HUD: 체력 · 마나 · 주문 장착 칸 + 최근 전투 로그 몇 줄. 이벤트만 보고 그린다.
    /// 기본 TMP 폰트(LiberationSans)에 한글이 없어 영어로 쓴다 — 한글 폰트 · 막대 · 아이콘은 2-5 HUD에서.
    /// </summary>
    public sealed class HudPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _logText;
        [Min(1)] [SerializeField] private int _logLines = 6;

        private readonly Queue<string> _log = new Queue<string>();
        private SimEventBus _bus;
        private EntityId _playerId;
        private int _hp;
        private int _maxHp;
        private int _mana;
        private int _maxMana;
        private string[] _spellSlots = new string[0];
        private bool _dead;
        private SpellBook _spellBook;
        private string _hint = string.Empty;
        private readonly SortedDictionary<StatusType, int> _playerStatuses = new SortedDictionary<StatusType, int>();

        public string StatusText => _statusText != null ? _statusText.text : null;
        public string LogText => _logText != null ? _logText.text : null;

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(TMP_Text statusText, TMP_Text logText)
        {
            _statusText = statusText;
            _logText = logText;
        }

        public void Bind(SimEventBus bus, Unit player, SpellBook spellBook = null)
        {
            Unbind();
            _bus = bus;
            _playerId = player.Id;
            _hp = player.Hp;
            _maxHp = player.MaxHp;
            _mana = player.Mana;
            _maxMana = player.MaxMana;
            _spellSlots = (string[])player.SpellSlots.Clone();
            _spellBook = spellBook;
            _hint = string.Empty;
            _dead = false;
            _log.Clear();
            _playerStatuses.Clear();
            foreach (var s in player.Statuses) _playerStatuses[s.Type] = s.Remaining;
            _bus.Subscribe<UnitDamagedEvent>(OnDamaged);
            _bus.Subscribe<UnitDiedEvent>(OnDied);
            _bus.Subscribe<ManaChangedEvent>(OnManaChanged);
            _bus.Subscribe<SpellCastEvent>(OnSpellCast);
            _bus.Subscribe<StatusChangedEvent>(OnStatusChanged);
            Redraw();
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<UnitDamagedEvent>(OnDamaged);
            _bus.Unsubscribe<UnitDiedEvent>(OnDied);
            _bus.Unsubscribe<ManaChangedEvent>(OnManaChanged);
            _bus.Unsubscribe<SpellCastEvent>(OnSpellCast);
            _bus.Unsubscribe<StatusChangedEvent>(OnStatusChanged);
            _bus = null;
        }

        private void OnDestroy() => Unbind();

        private void OnDamaged(UnitDamagedEvent e)
        {
            bool fromStatus = !e.Attacker.IsValid; // 상태 이상(화상 · 중독) 피해
            if (e.Target == _playerId)
            {
                _hp = e.Hp;
                AddLog(fromStatus ? $"You take {e.Amount} damage." : $"Enemy {e.Attacker} hits you for {e.Amount}.");
            }
            else if (e.Attacker == _playerId)
            {
                AddLog($"You hit enemy {e.Target} for {e.Amount} ({e.Hp}/{e.MaxHp}).");
            }
            else if (fromStatus)
            {
                AddLog($"Enemy {e.Target} takes {e.Amount} ({e.Hp}/{e.MaxHp}).");
            }
            Redraw();
        }

        private void OnStatusChanged(StatusChangedEvent e)
        {
            bool isPlayer = e.Id == _playerId;
            if (isPlayer)
            {
                if (e.Remaining > 0) _playerStatuses[e.Type] = e.Remaining;
                else _playerStatuses.Remove(e.Type);
            }

            string who = isPlayer ? "You" : $"Enemy {e.Id}";
            if (e.Applied) AddLog($"{who}: {e.Type} ({e.Remaining}).");
            else if (e.Remaining <= 0) AddLog($"{who}: {e.Type} wears off.");
            Redraw();
        }

        /// <summary>조준 안내 · 시전 불가 이유 한 줄 (로그 위, 노란색). 빈 문자열 = 지움.</summary>
        public void SetHint(string hint)
        {
            _hint = hint ?? string.Empty;
            Redraw();
        }

        private void OnSpellCast(SpellCastEvent e)
        {
            if (e.Caster != _playerId) return;
            AddLog($"You cast {e.SpellId}.");
            Redraw();
        }

        private void OnManaChanged(ManaChangedEvent e)
        {
            if (e.Id != _playerId) return;
            _mana = e.Mana;
            _maxMana = e.MaxMana;
            Redraw();
        }

        private void OnDied(UnitDiedEvent e)
        {
            if (e.Id == _playerId)
            {
                _dead = true;
                AddLog("You died.");
            }
            else
            {
                AddLog($"Enemy {e.Id} dies.");
            }
            Redraw();
        }

        private void AddLog(string line)
        {
            _log.Enqueue(line);
            while (_log.Count > _logLines) _log.Dequeue();
        }

        private void Redraw()
        {
            if (_statusText != null)
            {
                var sb = new StringBuilder();
                sb.Append(_dead ? $"HP 0/{_maxHp}" : $"HP {_hp}/{_maxHp}");
                if (_maxMana > 0) sb.Append($"   MP {_mana}/{_maxMana}");
                foreach (var pair in _playerStatuses) sb.Append($"   {pair.Key} {pair.Value}");
                if (_dead) sb.Append("   DEAD - press R to restart");
                if (_spellSlots.Length > 0)
                {
                    sb.Append('\n');
                    for (int i = 0; i < _spellSlots.Length; i++)
                    {
                        if (i > 0) sb.Append("  ");
                        sb.Append($"[{i + 1}] {_spellSlots[i] ?? "-"}");
                        if (_spellBook != null && _spellBook.TryGet(_spellSlots[i], out var spell)) sb.Append($" {spell.ManaCost}MP");
                    }
                }
                _statusText.text = sb.ToString();
            }
            if (_logText != null)
            {
                string hint = _hint.Length > 0 ? $"<color=#FFD54F>{_hint}</color>\n" : string.Empty;
                _logText.text = hint + string.Join("\n", _log);
            }
        }
    }
}
