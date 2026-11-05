using System;
using System.Collections.Generic;
using ProjectM.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectM.UI
{
    /// <summary>
    /// 타이틀 화면 (3-27, 결정 2026-11-05): 첫 화면(이어하기 · 게임 시작 · 종료) → 모드 카드(턴제 · 실시간) + 시드 입력 칸 → 시작.
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

        private readonly TitleMenuModel _model = new TitleMenuModel();
        private RectTransform _mainRoot, _newRoot;
        private readonly List<(Image box, TMP_Text label)> _mainItems = new List<(Image, TMP_Text)>();
        private (Image box, TMP_Text label) _turnCard, _realCard, _startButton;
        private TMP_Text _seedText, _hint;
        private Image _seedBox;
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

        /// <summary>첫 화면을 연다. 콜백은 TitleMenuModel 이벤트 그대로 (호출한 쪽이 한 번만 구독).</summary>
        public void Open(bool canContinue)
        {
            if (_panel == null) return;
            Build();
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
            if (Mouse.current != null) _lastMouse = Mouse.current.position.ReadValue();
            _model.Open(canContinue);
        }

        private void Update()
        {
            if (!_model.IsOpen) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (_model.Screen == TitleScreen.Main)
            {
                if (keyboard != null)
                {
                    if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) _model.Move(-1);
                    else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) _model.Move(1);
                    else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                    {
                        _model.Confirm();
                        return;
                    }
                }
                if (mouse != null)
                {
                    var pos = mouse.position.ReadValue();
                    int hover = -1;
                    for (int i = 0; i < _mainItems.Count; i++)
                        if (_mainItems[i].box.gameObject.activeSelf && Contains(_mainItems[i].box, pos)) hover = i;
                    if (hover >= 0 && (pos - _lastMouse).sqrMagnitude > 0.01f) _model.Select(hover);
                    _lastMouse = pos;
                    if (hover >= 0 && mouse.leftButton.wasPressedThisFrame)
                    {
                        _model.Select(hover);
                        _model.Confirm();
                    }
                }
            }
            else if (_model.Screen == TitleScreen.NewGame)
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
            }
        }

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

            _mainRoot.gameObject.SetActive(_model.Screen == TitleScreen.Main);
            _newRoot.gameObject.SetActive(_model.Screen == TitleScreen.NewGame);
            for (int i = 0; i < _mainItems.Count; i++)
            {
                bool used = i < _model.Items.Count;
                _mainItems[i].box.gameObject.SetActive(used);
                if (!used) continue;
                _mainItems[i].label.text = HudText.TitleItemName(_model.Items[i]);
                _mainItems[i].box.color = i == _model.Selected ? SelectedColor : ItemColor;
            }
            _turnCard.box.color = _model.Mode == RunMode.TurnBased ? SelectedColor : ItemColor;
            _realCard.box.color = _model.Mode == RunMode.RealTime ? SelectedColor : ItemColor;
            _seedText.text = _model.SeedText.Length > 0
                ? $"{HudText.SeedLabel}  {_model.SeedText}<color=#FFD54F>_</color>"
                : $"{HudText.SeedLabel}  <color=#777777>{HudText.SeedPlaceholder}</color><color=#FFD54F>_</color>";
            _hint.text = _model.Screen == TitleScreen.Main ? HudText.TitleHint : HudText.NewGameHint;
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

            Text(_panel, HudText.GameTitle, 96, new Vector2(0.5f, 0.78f), new Vector2(1200, 140));
            Text(_panel, HudText.GameSubtitle, 32, new Vector2(0.5f, 0.68f), new Vector2(1200, 60)).color = new Color(0.75f, 0.75f, 0.8f);
            _hint = Text(_panel, string.Empty, 24, new Vector2(0.5f, 0.06f), new Vector2(1600, 50));
            _hint.color = new Color(0.65f, 0.65f, 0.65f);

            _mainRoot = Group("Main");
            for (int i = 0; i < 3; i++)
                _mainItems.Add(Button(_mainRoot, string.Empty, new Vector2(0.5f, 0.48f - i * 0.1f), new Vector2(420, 70), 36));

            _newRoot = Group("NewGame");
            Text(_newRoot, HudText.NewGameTitle, 40, new Vector2(0.5f, 0.6f), new Vector2(1200, 60));
            _turnCard = Card(_newRoot, HudText.ModeTurnName, HudText.ModeTurnDesc, new Vector2(0.38f, 0.42f));
            _realCard = Card(_newRoot, HudText.ModeRealName, HudText.ModeRealDesc, new Vector2(0.62f, 0.42f));
            var seed = Button(_newRoot, string.Empty, new Vector2(0.5f, 0.22f), new Vector2(760, 60), 28);
            _seedBox = seed.box;
            _seedBox.color = SeedBoxColor;
            _seedText = seed.label;
            _seedText.alignment = TextAlignmentOptions.Left;
            _seedText.margin = new Vector4(20, 0, 20, 0);
            _startButton = Button(_newRoot, HudText.StartButton, new Vector2(0.5f, 0.12f), new Vector2(300, 64), 34);
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
