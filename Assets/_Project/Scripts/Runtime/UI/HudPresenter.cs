using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Units;
using TMPro;
using UnityEngine;
using EntityId = ProjectM.Core.EntityId; // Unity 6의 UnityEngine.EntityId와 구분

namespace ProjectM.UI
{
    /// <summary>
    /// 그레이박스 HUD: 플레이어 체력 + 최근 전투 로그 몇 줄. 이벤트만 보고 그린다.
    /// 기본 TMP 폰트(LiberationSans)에 한글이 없어 영어로 쓴다 — 한글 폰트는 UI 단계에서.
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
        private bool _dead;

        public string StatusText => _statusText != null ? _statusText.text : null;
        public string LogText => _logText != null ? _logText.text : null;

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(TMP_Text statusText, TMP_Text logText)
        {
            _statusText = statusText;
            _logText = logText;
        }

        public void Bind(SimEventBus bus, EntityId playerId, int hp, int maxHp)
        {
            Unbind();
            _bus = bus;
            _playerId = playerId;
            _hp = hp;
            _maxHp = maxHp;
            _dead = false;
            _log.Clear();
            _bus.Subscribe<UnitDamagedEvent>(OnDamaged);
            _bus.Subscribe<UnitDiedEvent>(OnDied);
            Redraw();
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<UnitDamagedEvent>(OnDamaged);
            _bus.Unsubscribe<UnitDiedEvent>(OnDied);
            _bus = null;
        }

        private void OnDestroy() => Unbind();

        private void OnDamaged(UnitDamagedEvent e)
        {
            if (e.Target == _playerId)
            {
                _hp = e.Hp;
                AddLog($"Enemy {e.Attacker} hits you for {e.Amount}.");
            }
            else if (e.Attacker == _playerId)
            {
                AddLog($"You hit enemy {e.Target} for {e.Amount} ({e.Hp}/{e.MaxHp}).");
            }
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
                _statusText.text = _dead ? $"HP 0/{_maxHp}   DEAD - press R to restart" : $"HP {_hp}/{_maxHp}";
            if (_logText != null)
                _logText.text = string.Join("\n", _log);
        }
    }
}
