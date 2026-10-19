using System.Collections.Generic;
using System.Text;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Presentation;
using ProjectM.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectM.UI
{
    /// <summary>
    /// 그레이박스 HUD (2-5): 왼쪽 위 HP · MP 막대 + 숫자, 그 오른쪽 내 상태 아이콘(색 칸 + 이름 + 남은 턴),
    /// 아래로 빠른 칸 6개(3-2a — 주문 · 아이템, 클릭 가능) · 조준 안내 줄, 왼쪽 아래 전투 로그. 값과 로그는 HudModel이 이벤트로 모으고 여기서는 그리기만 한다.
    /// 상태 아이콘 색은 유닛 틴트와 같은 표(EntityPresenter.StatusTint).
    /// </summary>
    public sealed class HudPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform _hpFill;
        [SerializeField] private TMP_Text _hpLabel;
        [SerializeField] private RectTransform _mpFill;
        [SerializeField] private TMP_Text _mpLabel;
        [Tooltip("경험치 막대 (3-6) — 글자 = 레벨 · 경험치")] [SerializeField] private RectTransform _xpFill;
        [SerializeField] private TMP_Text _xpLabel;
        [Tooltip("든 무기 한 줄 (3-7a)")] [SerializeField] private TMP_Text _weaponLabel;
        [SerializeField] private RectTransform _statusIcons;
        [Tooltip("빠른 칸 상자들이 들어갈 줄 (3-2a)")] [SerializeField] private RectTransform _quickSlots;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private TMP_Text _logText;
        [Min(1)] [SerializeField] private int _logLines = 8;
        [Tooltip("상태 아이콘 한 개 크기")] [SerializeField] private Vector2 _iconSize = new Vector2(110, 30);
        [Tooltip("빠른 칸 상자 한 개 크기")] [SerializeField] private Vector2 _quickSlotSize = new Vector2(230, 40);

        private static readonly Color HintColor = new Color(1f, 0.84f, 0.31f);
        private static readonly Color DeadColor = new Color(1f, 0.4f, 0.35f);

        private HudModel _model;
        private string _hint = string.Empty;
        private readonly List<(Image box, TMP_Text label)> _icons = new List<(Image, TMP_Text)>();
        private readonly List<(Image box, TMP_Text label)> _slotBoxes = new List<(Image, TMP_Text)>();
        private int _highlightSlot = -1;

        private static readonly Color SlotColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        private static readonly Color SlotHighlightColor = new Color(0.55f, 0.45f, 0.12f, 0.95f);

        public HudModel Model => _model;
        public string LogText => _logText != null ? _logText.text : null;

        public void Bind(SimEventBus bus, Unit player, SpellBook spellBook = null, HudText text = null, ProgressionConfig progression = null)
        {
            Unbind();
            _hint = string.Empty;
            _model = new HudModel(_logLines, text);
            _model.Changed += Redraw;
            _model.Bind(bus, player, spellBook, progression);
        }

        public void Unbind()
        {
            if (_model == null) return;
            _model.Changed -= Redraw;
            _model.Unbind();
            _model = null;
        }

        private void OnDestroy() => Unbind();

        /// <summary>조준 안내 · 시전 불가 이유 한 줄 (노란색). 빈 문자열 = 지움.</summary>
        public void SetHint(string hint)
        {
            _hint = hint ?? string.Empty;
            Redraw();
        }

        /// <summary>강조할 빠른 칸 (조준 중인 칸 · 등록할 칸). -1 = 없음.</summary>
        public void SetHighlightSlot(int slot)
        {
            if (_highlightSlot == slot) return;
            _highlightSlot = slot;
            Redraw();
        }

        /// <summary>화면 좌표가 가리키는 빠른 칸 번호, 없으면 -1 (클릭 판정 — EventSystem 없이).</summary>
        public int QuickSlotAt(Vector2 screenPos)
        {
            for (int i = 0; i < _slotBoxes.Count; i++)
            {
                var box = _slotBoxes[i].box;
                if (box.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)box.transform, screenPos, null))
                    return i;
            }
            return -1;
        }

        private void Redraw()
        {
            if (_model == null) return;
            SetBar(_hpFill, _hpLabel, "HP", _model.Hp, _model.MaxHp);
            SetBar(_mpFill, _mpLabel, "MP", _model.Mana, _model.MaxMana);
            SetBar(_xpFill, _xpLabel, null, _model.ExpToNext > 0 ? _model.Exp : 1, _model.ExpToNext > 0 ? _model.ExpToNext : 1);
            if (_xpLabel != null) _xpLabel.text = HudText.ExpLabel(_model.Level, _model.Exp, _model.ExpToNext);
            if (_weaponLabel != null) _weaponLabel.text = _model.Text.WeaponLabel(_model.Weapon, _model.UnarmedDamage, _model.Strength);
            DrawStatusIcons();
            DrawQuickSlots();

            if (_hintText != null)
            {
                _hintText.text = _model.Dead ? HudText.DeadHint : _hint;
                _hintText.color = _model.Dead ? DeadColor : HintColor;
            }

            if (_logText != null)
            {
                // 오래된 줄일수록 흐리게 (맨 아래 = 가장 새 줄)
                var log = _model.Log;
                var sb = new StringBuilder();
                for (int i = 0; i < log.Count; i++)
                {
                    int age = log.Count - 1 - i;
                    int alpha = Mathf.Clamp(255 - age * 22, 90, 255);
                    if (i > 0) sb.Append('\n');
                    sb.Append($"<alpha=#{alpha:X2}>{log[i]}");
                }
                _logText.text = sb.ToString();
            }
        }

        private static void SetBar(RectTransform fill, TMP_Text label, string name, int value, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)value / max) : 0f;
            if (fill != null)
            {
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(ratio, 1f);
                fill.offsetMin = fill.offsetMax = Vector2.zero;
            }
            if (label != null) label.text = $"{name} {value}/{max}";
        }

        private void DrawStatusIcons()
        {
            if (_statusIcons == null) return;
            int index = 0;
            foreach (var pair in _model.PlayerStatuses)
            {
                if (index >= _icons.Count) _icons.Add(CreateIcon());
                var (box, label) = _icons[index];
                box.gameObject.SetActive(true);
                box.color = EntityPresenter.StatusTint(pair.Key);
                label.text = $"{HudText.Status(pair.Key)} {pair.Value}";
                index++;
            }
            for (int i = index; i < _icons.Count; i++) _icons[i].box.gameObject.SetActive(false);
        }

        // 빠른 칸 (3-2a): "1 불꽃 화살 3MP" · 빈 칸은 "1 -" (누르면 인벤토리가 열려 등록). 마나가 모자라면 비용이 빨강
        private void DrawQuickSlots()
        {
            if (_quickSlots == null) return;
            var slots = _model.QuickSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                if (i >= _slotBoxes.Count) _slotBoxes.Add(CreateBox(_quickSlots, "QuickSlot", _quickSlotSize, TextAlignmentOptions.Left, Color.white));
                var (box, label) = _slotBoxes[i];
                box.gameObject.SetActive(true);
                box.color = i == _highlightSlot ? SlotHighlightColor : SlotColor;
                var sb = new StringBuilder($" {i + 1} ");
                if (slots[i].IsEmpty) sb.Append("<alpha=#80>-");
                else sb.Append(_model.Text.Entry(slots[i]));
                int cost = _model.SlotCosts[i];
                if (cost >= 0) sb.Append(_model.Mana >= cost ? $" <alpha=#B0>{cost}MP" : $" <color=#FF6659>{cost}MP</color>");
                label.text = sb.ToString();
            }
            for (int i = slots.Count; i < _slotBoxes.Count; i++) _slotBoxes[i].box.gameObject.SetActive(false);
        }

        private (Image, TMP_Text) CreateIcon() =>
            CreateBox(_statusIcons, "StatusIcon", _iconSize, TextAlignmentOptions.Center, new Color(0.08f, 0.08f, 0.08f));

        private (Image, TMP_Text) CreateBox(RectTransform parent, string name, Vector2 size, TextAlignmentOptions alignment, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            var box = go.GetComponent<Image>();
            box.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var rect = (RectTransform)labelGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            if (_hpLabel != null) label.font = _hpLabel.font; // HUD 폰트(Galmuri11)를 그대로
            label.fontSize = size.y * 0.7f;
            label.alignment = alignment;
            label.color = textColor;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return (box, label);
        }
    }
}
