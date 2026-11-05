using System.Collections.Generic;
using ProjectM.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectM.UI
{
    /// <summary>
    /// 타이틀 화면 (3-27 · 3-30, 결정 2026-11-05): 첫 화면(게임 시작 · 종료) → 저장 슬롯 5칸 → 빈 칸 = 모드 카드 + 시드 칸 → 시작 /
    /// 찬 칸 = 요약(캐릭터 · 능력치 · 층 …) + 시작 · 데이터 삭제 · 뒤로 → 삭제는 한 번 더 묻기.
    /// 상태는 TitleMenuModel, 여기서는 그리기 · 입력만. 글자 입력은 Input System의 Keyboard.onTextInput (EventSystem 없이).
    /// 칸 · 글은 처음 열 때 _panel 아래에 만든다 (판 · 폰트는 Setup/Main Scene이 연결).
    /// </summary>
    public sealed class TitlePresenter : MonoBehaviour
    {
        [Tooltip("화면 전체 (열려 있을 때만 켠다)")] [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_FontAsset _font;

        private static readonly Color PanelColor = new Color(0.02f, 0.02f, 0.04f, 1f);
        private static readonly Color ItemColor = new Color(0.14f, 0.14f, 0.17f, 1f);
        private static readonly Color SelectedColor = new Color(0.55f, 0.45f, 0.12f, 1f);
        private static readonly Color SeedBoxColor = new Color(0.08f, 0.08f, 0.1f, 1f);
        private static readonly Color DangerColor = new Color(0.6f, 0.18f, 0.15f, 1f);

        private readonly TitleMenuModel _model = new TitleMenuModel();
        private RectTransform _mainRoot, _newRoot, _slotsRoot, _detailRoot, _confirmRoot;
        private readonly List<(Image box, TMP_Text label)> _mainItems = new List<(Image, TMP_Text)>();
        private readonly List<(Image box, TMP_Text label)> _slotItems = new List<(Image, TMP_Text)>();
        private readonly List<(Image box, TMP_Text label)> _detailButtons = new List<(Image, TMP_Text)>();
        private readonly List<(Image box, TMP_Text label)> _confirmButtons = new List<(Image, TMP_Text)>();
        private (Image box, TMP_Text label) _turnCard, _realCard, _startButton;
        private TMP_Text _seedText, _hint, _detailTitle, _detailBody, _confirmText;
        private Vector2 _lastMouse;
        private bool _built;
        private Keyboard _typingKeyboard;

        public TitleMenuModel Model => _model;
        public bool IsOpen => _model.IsOpen;

        public void Configure(RectTransform panel, TMP_FontAsset font)
        {
            _panel = panel;
            _font = font;
        }

        private void Awake()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
            _model.Changed += Redraw;
        }

        private void OnDestroy()
        {
            _model.Changed -= Redraw;
            StopTyping();
        }

        /// <summary>첫 화면을 연다. slots = 저장 슬롯 정보 (슬롯 1부터). 콜백은 TitleMenuModel 이벤트 그대로.</summary>
        public void Open(IReadOnlyList<SlotInfo> slots)
        {
            if (_panel == null) return;
            Build();
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
            if (Mouse.current != null) _lastMouse = Mouse.current.position.ReadValue();
            _model.Open(slots);
        }

        private void Update()
        {
            if (!_model.IsOpen) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (_model.Screen == TitleScreen.NewGame)
            {
                if (keyboard != null)
                {
                    if (keyboard.escapeKey.wasPressedThisFrame) { _model.Back(); return; }
                    if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) _model.ToggleMode();
                    if (keyboard.backspaceKey.wasPressedThisFrame) _model.Backspace();
                    if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) { _model.Confirm(); return; }
                }
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    var pos = mouse.position.ReadValue();
                    if (Contains(_turnCard.box, pos)) _model.SetMode(RunMode.TurnBased);
                    else if (Contains(_realCard.box, pos)) _model.SetMode(RunMode.RealTime);
                    else if (Contains(_startButton.box, pos)) _model.Confirm();
                }
                return;
            }

            // 첫 화면 · 슬롯 · 요약 · 삭제 확인: 위아래(또는 좌우) 고르기 · Enter/Space/클릭 확정 · Esc 뒤로
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) { _model.Back(); return; }
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) _model.Move(-1);
                else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) _model.Move(1);
                else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    _model.Confirm();
                    return;
                }
            }
            if (mouse == null) return;
            var buttons = ButtonsOf(_model.Screen);
            var at = mouse.position.ReadValue();
            int hover = -1;
            for (int i = 0; i < buttons.Count; i++)
                if (buttons[i].box.gameObject.activeSelf && Contains(buttons[i].box, at)) hover = i;
            if (hover >= 0 && (at - _lastMouse).sqrMagnitude > 0.01f) _model.Select(hover);
            _lastMouse = at;
            if (hover >= 0 && mouse.leftButton.wasPressedThisFrame)
            {
                _model.Select(hover);
                _model.Confirm();
            }
        }

        private List<(Image box, TMP_Text label)> ButtonsOf(TitleScreen screen) =>
            screen == TitleScreen.Slots ? _slotItems : screen == TitleScreen.SlotDetail ? _detailButtons : screen == TitleScreen.ConfirmDelete ? _confirmButtons : _mainItems;

        private static bool Contains(Image box, Vector2 screen) =>
            box != null && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)box.transform, screen, null);

        // ── 글자 입력 (시드 칸) ──

        private void StartTyping()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _typingKeyboard == keyboard) return;
            StopTyping();
            _typingKeyboard = keyboard;
            _typingKeyboard.onTextInput += OnTextInput;
        }

        private void StopTyping()
        {
            if (_typingKeyboard == null) return;
            _typingKeyboard.onTextInput -= OnTextInput;
            _typingKeyboard = null;
        }

        private void OnTextInput(char c)
        {
            if (_model.Screen == TitleScreen.NewGame) _model.Type(c); // 제어 문자(Enter · Backspace)는 모델이 무시
        }

        // ── 그리기 ──

        private void Redraw()
        {
            if (!_built || _panel == null) return;
            bool open = _model.IsOpen;
            _panel.gameObject.SetActive(open);
            if (open && _model.Screen == TitleScreen.NewGame) StartTyping(); else StopTyping();
            if (!open) return;

            var screen = _model.Screen;
            _mainRoot.gameObject.SetActive(screen == TitleScreen.Main);
            _newRoot.gameObject.SetActive(screen == TitleScreen.NewGame);
            _slotsRoot.gameObject.SetActive(screen == TitleScreen.Slots);
            _detailRoot.gameObject.SetActive(screen == TitleScreen.SlotDetail);
            _confirmRoot.gameObject.SetActive(screen == TitleScreen.ConfirmDelete);

            for (int i = 0; i < _mainItems.Count; i++)
            {
                bool used = i < _model.Items.Count;
                _mainItems[i].box.gameObject.SetActive(used);
                if (!used) continue;
                _mainItems[i].label.text = HudText.TitleItemName(_model.Items[i]);
                _mainItems[i].box.color = i == _model.Selected ? SelectedColor : ItemColor;
            }
            for (int i = 0; i < _slotItems.Count; i++)
            {
                bool used = i < _model.Slots.Count;
                _slotItems[i].box.gameObject.SetActive(used);
                if (!used) continue;
                var slot = _model.Slots[i];
                _slotItems[i].label.text = HudText.SlotLabel(i + 1, slot.Filled, slot.Label);
                _slotItems[i].box.color = i == _model.SlotSelected ? SelectedColor : ItemColor;
            }
            if (screen == TitleScreen.SlotDetail || screen == TitleScreen.ConfirmDelete)
            {
                var slot = _model.SlotSelected < _model.Slots.Count ? _model.Slots[_model.SlotSelected] : default;
                _detailTitle.text = HudText.SlotLabel(_model.CurrentSlot, slot.Filled, slot.Label);
                _detailBody.text = slot.Detail;
                _confirmText.text = HudText.SlotDeleteQuestion(_model.CurrentSlot);
            }
            for (int i = 0; i < _detailButtons.Count; i++)
                _detailButtons[i].box.color = i == _model.OptionSelected ? (i == (int)SlotAction.Delete ? DangerColor : SelectedColor) : ItemColor;
            for (int i = 0; i < _confirmButtons.Count; i++)
                _confirmButtons[i].box.color = i == _model.OptionSelected ? (i == 0 ? DangerColor : SelectedColor) : ItemColor;

            _turnCard.box.color = _model.Mode == RunMode.TurnBased ? SelectedColor : ItemColor;
            _realCard.box.color = _model.Mode == RunMode.RealTime ? SelectedColor : ItemColor;
            _seedText.text = _model.SeedText.Length > 0
                ? $"{HudText.SeedLabel}  {_model.SeedText}<color=#FFD54F>_</color>"
                : $"{HudText.SeedLabel}  <color=#777777>{HudText.SeedPlaceholder}</color><color=#FFD54F>_</color>";
            _hint.text = screen == TitleScreen.NewGame ? HudText.NewGameHint
                : screen == TitleScreen.Main ? HudText.TitleHint : screen == TitleScreen.Slots ? HudText.SlotHint : HudText.SlotChoiceHint;
        }

        private void Build()
        {
            if (_built) return;
            _built = true;
            var bg = _panel.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = PanelColor;
                bg.raycastTarget = true;
            }

            Text(_panel, HudText.GameTitle, 96, new Vector2(0.5f, 0.84f), new Vector2(1200, 140));
            Text(_panel, HudText.GameSubtitle, 32, new Vector2(0.5f, 0.75f), new Vector2(1200, 60)).color = new Color(0.75f, 0.75f, 0.8f);
            _hint = Text(_panel, string.Empty, 24, new Vector2(0.5f, 0.05f), new Vector2(1600, 50));
            _hint.color = new Color(0.65f, 0.65f, 0.65f);

            _mainRoot = Group("Main");
            for (int i = 0; i < 2; i++)
                _mainItems.Add(Button(_mainRoot, string.Empty, new Vector2(0.5f, 0.48f - i * 0.1f), new Vector2(420, 70), 36));

            _slotsRoot = Group("Slots");
            Text(_slotsRoot, HudText.SlotsTitle, 40, new Vector2(0.5f, 0.66f), new Vector2(1200, 60));
            for (int i = 0; i < ProjectM.Data.RunSaveStore.SlotCount; i++)
            {
                var b = Button(_slotsRoot, string.Empty, new Vector2(0.5f, 0.57f - i * 0.085f), new Vector2(900, 72), 28);
                b.label.alignment = TextAlignmentOptions.Left;
                b.label.margin = new Vector4(24, 0, 24, 0);
                _slotItems.Add(b);
            }

            _detailRoot = Group("SlotDetail");
            _detailTitle = Text(_detailRoot, string.Empty, 34, new Vector2(0.5f, 0.66f), new Vector2(1200, 60));
            var box = Button(_detailRoot, string.Empty, new Vector2(0.5f, 0.43f), new Vector2(900, 330), 26);
            box.box.color = SeedBoxColor;
            _detailBody = box.label;
            _detailBody.alignment = TextAlignmentOptions.TopLeft;
            _detailBody.margin = new Vector4(32, 24, 32, 24);
            string[] detailNames = { HudText.SlotStart, HudText.SlotDelete, HudText.SlotBack };
            for (int i = 0; i < detailNames.Length; i++)
                _detailButtons.Add(Button(_detailRoot, detailNames[i], new Vector2(0.32f + i * 0.18f, 0.16f), new Vector2(300, 64), 30));

            _confirmRoot = Group("ConfirmDelete");
            _confirmText = Text(_confirmRoot, string.Empty, 34, new Vector2(0.5f, 0.45f), new Vector2(1200, 120));
            string[] confirmNames = { HudText.SlotDeleteYes, HudText.SlotDeleteNo };
            for (int i = 0; i < confirmNames.Length; i++)
                _confirmButtons.Add(Button(_confirmRoot, confirmNames[i], new Vector2(0.41f + i * 0.18f, 0.28f), new Vector2(300, 64), 30));

            _newRoot = Group("NewGame");
            Text(_newRoot, HudText.NewGameTitle, 40, new Vector2(0.5f, 0.66f), new Vector2(1200, 60));
            _turnCard = Card(_newRoot, HudText.ModeTurnName, HudText.ModeTurnDesc, new Vector2(0.38f, 0.45f));
            _realCard = Card(_newRoot, HudText.ModeRealName, HudText.ModeRealDesc, new Vector2(0.62f, 0.45f));
            var seed = Button(_newRoot, string.Empty, new Vector2(0.5f, 0.23f), new Vector2(760, 60), 28);
            seed.box.color = SeedBoxColor;
            _seedText = seed.label;
            _seedText.alignment = TextAlignmentOptions.Left;
            _seedText.margin = new Vector4(20, 0, 20, 0);
            _startButton = Button(_newRoot, HudText.StartButton, new Vector2(0.5f, 0.13f), new Vector2(300, 64), 34);
            _startButton.box.color = SelectedColor;
        }

        private RectTransform Group(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_panel, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private TMP_Text Text(RectTransform parent, string text, float size, Vector2 anchor, Vector2 box)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = box;
            var t = go.GetComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        private (Image box, TMP_Text label) Button(RectTransform parent, string text, Vector2 anchor, Vector2 size, float fontSize)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = ItemColor;
            var label = Text(rect, text, fontSize, new Vector2(0.5f, 0.5f), size);
            return (image, label);
        }

        private (Image box, TMP_Text label) Card(RectTransform parent, string title, string body, Vector2 anchor)
        {
            var card = Button(parent, string.Empty, anchor, new Vector2(400, 300), 28);
            var titleText = Text((RectTransform)card.box.transform, title, 40, new Vector2(0.5f, 0.85f), new Vector2(380, 60));
            Text((RectTransform)card.box.transform, body, 24, new Vector2(0.5f, 0.42f), new Vector2(360, 200)).color = new Color(0.85f, 0.85f, 0.85f);
            return (card.box, titleText);
        }
    }
}
