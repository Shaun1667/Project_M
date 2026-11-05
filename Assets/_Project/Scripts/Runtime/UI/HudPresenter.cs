using System.Collections.Generic;
using System.Text;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Presentation;
using ProjectM.Units;
using ProjectM.Items;
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
        [Tooltip("입은 갑옷 한 줄 (3-10a)")] [SerializeField] private TMP_Text _armorLabel;
        [SerializeField] private RectTransform _statusIcons;
        [Tooltip("빠른 칸 상자들이 들어갈 줄 (3-2a)")] [SerializeField] private RectTransform _quickSlots;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private TMP_Text _logText;
        [Min(1)] [SerializeField] private int _logLines = 8;
        [Tooltip("상태 아이콘 한 개 크기")] [SerializeField] private Vector2 _iconSize = new Vector2(110, 30);
        [Tooltip("빠른 칸 상자 한 개 크기")] [SerializeField] private Vector2 _quickSlotSize = new Vector2(230, 40);
        [Tooltip("아이템 픽셀 그림 (P-3 ⓒ) — 빠른 칸 오른쪽에. 비면 글자만")] [SerializeField] private ProjectM.Presentation.ItemVisualCatalog _itemIcons;

        private static readonly Color HintColor = new Color(1f, 0.84f, 0.31f);
        private static readonly Color DeadColor = new Color(1f, 0.4f, 0.35f);

        private HudModel _model;
        private string _hint = string.Empty;
        private readonly List<(Image box, TMP_Text label)> _icons = new List<(Image, TMP_Text)>();
        private readonly List<(Image box, TMP_Text label)> _slotBoxes = new List<(Image, TMP_Text)>();
        private readonly List<Image> _slotIcons = new List<Image>();
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

        private RectTransform _infoPanel;
        private TMP_Text _infoText;
        private static readonly Color InfoColor = new Color(0.08f, 0.08f, 0.1f, 0.88f);

        /// <summary>
        /// 정보 모드 창 (3-24, SPD — E): 화면 아래 가운데 상자에 칸 설명. null · 빈 문자열 = 숨김.
        /// 상자는 처음 부를 때 안내 줄과 같은 캔버스 · 폰트로 만든다 (Setup 변경 없음).
        /// </summary>
        public void SetInfo(string info)
        {
            if (string.IsNullOrEmpty(info))
            {
                if (_infoPanel != null) _infoPanel.gameObject.SetActive(false);
                return;
            }
            if (_infoPanel == null && !CreateInfoPanel()) return;
            _infoText.text = info;
            _infoPanel.gameObject.SetActive(true);
        }

        private bool CreateInfoPanel()
        {
            if (_hintText == null || _hintText.canvas == null) return false;
            var go = new GameObject("ExamineInfo", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _infoPanel = (RectTransform)go.transform;
            _infoPanel.SetParent(_hintText.canvas.transform, false);
            _infoPanel.anchorMin = _infoPanel.anchorMax = new Vector2(0.5f, 0f);
            _infoPanel.pivot = new Vector2(0.5f, 0f);
            _infoPanel.anchoredPosition = new Vector2(0f, 24f);
            go.GetComponent<Image>().color = InfoColor;
            go.GetComponent<Image>().raycastTarget = false;
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 10, 10);
            layout.childControlWidth = layout.childControlHeight = true;
            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(_infoPanel, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.font = _hintText.font;
            text.fontSize = _hintText.fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            _infoText = text;
            return true;
        }

        /// <summary>강조할 빠른 칸 (조준 중인 칸 · 등록할 칸). -1 = 없음.</summary>
        public void SetHighlightSlot(int slot)
        {
            if (_highlightSlot == slot) return;
            _highlightSlot = slot;
            Redraw();
        }

        /// <summary>화면 좌표가 가리키는 빠른 칸 번호, 없으면 -1 (클릭 판정 — EventSystem 없이).</summary>
        private RectTransform _attackPanel;
        private TMP_Text _attackName;
        private RectTransform _attackHpFill;
        private static readonly Color AttackPanelColor = new Color(0.25f, 0.06f, 0.06f, 0.9f);
        private static readonly Color AttackHpColor = new Color(0.85f, 0.2f, 0.2f, 1f);
        private const float AttackPanelWidth = 220f;

        /// <summary>
        /// 공격 대상 표시 (3-25, SPD): 오른쪽 아래 상자 — 이름 · HP 막대 · 키 안내. name이 null이면 숨김.
        /// 상자는 처음 부를 때 안내 줄과 같은 캔버스 · 폰트로 만든다 (Setup 변경 없음).
        /// </summary>
        public void SetAttackTarget(string name, int hp, int maxHp)
        {
            if (name == null)
            {
                if (_attackPanel != null) _attackPanel.gameObject.SetActive(false);
                return;
            }
            if (_attackPanel == null && !CreateAttackPanel()) return;
            _attackName.text = $"{name}\n<size=70%><color=#FFD54F>{HudText.AttackIndicatorHint}</color></size>";
            float t = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            _attackHpFill.anchorMax = new Vector2(t, 1f);
            _attackPanel.gameObject.SetActive(true);
        }

        /// <summary>화면 좌표가 공격 대상 상자 위인지 (클릭 = 공격).</summary>
        public bool AttackIndicatorAt(Vector2 screenPos) =>
            _attackPanel != null && _attackPanel.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(_attackPanel, screenPos, null);

        private bool CreateAttackPanel()
        {
            if (_hintText == null || _hintText.canvas == null) return false;
            var go = new GameObject("AttackIndicator", typeof(RectTransform), typeof(Image));
            _attackPanel = (RectTransform)go.transform;
            _attackPanel.SetParent(_hintText.canvas.transform, false);
            _attackPanel.anchorMin = _attackPanel.anchorMax = _attackPanel.pivot = new Vector2(1f, 0f);
            _attackPanel.anchoredPosition = new Vector2(-24f, 24f);
            _attackPanel.sizeDelta = new Vector2(AttackPanelWidth, 84f);
            go.GetComponent<Image>().color = AttackPanelColor;

            var textGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(_attackPanel, false);
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(12f, 22f);
            textRect.offsetMax = new Vector2(-12f, -6f);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.font = _hintText.font;
            text.fontSize = _hintText.fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            _attackName = text;

            var barGo = new GameObject("HpBar", typeof(RectTransform), typeof(Image));
            var bar = (RectTransform)barGo.transform;
            bar.SetParent(_attackPanel, false);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = new Vector2(12f, 10f);
            bar.offsetMax = new Vector2(-12f, 20f);
            barGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            barGo.GetComponent<Image>().raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            _attackHpFill = (RectTransform)fillGo.transform;
            _attackHpFill.SetParent(bar, false);
            _attackHpFill.anchorMin = Vector2.zero;
            _attackHpFill.anchorMax = Vector2.one;
            _attackHpFill.offsetMin = _attackHpFill.offsetMax = Vector2.zero;
            fillGo.GetComponent<Image>().color = AttackHpColor;
            fillGo.GetComponent<Image>().raycastTarget = false;
            return true;
        }

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
            if (_armorLabel != null) _armorLabel.text = _model.Text.ArmorLabel(_model.Armor, _model.Strength)
                + "  " + HudText.GoldLabel(_model.Gold) // 3-15a
                + (_model.Hunger != HungerState.None ? "  " + HudText.HungerLabel(_model.Hunger) : string.Empty); // 3-14
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
                if (i >= _slotBoxes.Count)
                {
                    _slotBoxes.Add(CreateBox(_quickSlots, "QuickSlot", _quickSlotSize, TextAlignmentOptions.Left, Color.white));
                    _slotIcons.Add(CreateSlotIcon((RectTransform)_slotBoxes[i].box.transform));
                }
                var (box, label) = _slotBoxes[i];
                SetSlotIcon(_slotIcons[i], label, slots[i]);
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

        /// <summary>빠른 칸 오른쪽 정사각 아이콘 (P-3 ⓒ).</summary>
        private Image CreateSlotIcon(RectTransform box)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(box, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-2f, 0f);
            rect.sizeDelta = new Vector2(_quickSlotSize.y, _quickSlotSize.y);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;
            return image;
        }

        private void SetSlotIcon(Image icon, TMP_Text label, InventoryRef entry)
        {
            Sprite sprite = null;
            if (_itemIcons != null && !entry.IsEmpty && entry.Kind == InventoryKind.Item && !_itemIcons.TryGetIcon(_model.Text.VisualOf(entry.Id), out sprite))
                _itemIcons.TryGetIcon(entry.Id, out sprite);
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            label.rectTransform.offsetMax = new Vector2(sprite != null ? -_quickSlotSize.y : 0f, 0f);
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
