using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectM.UI
{
    /// <summary>
    /// 캐릭터 선택 창 (결정 2026-10-21): 화면을 덮는 판 + 제목 + 캐릭터 카드(초상 · 이름) 줄 + 안내.
    /// 조작: ← → · A D = 고르기, Enter · Space = 시작, 마우스 = 가리켜 고르고 클릭으로 시작. 상태는 CharacterSelectModel, 여기는 그리기 · 입력 전달만.
    /// 카드는 Open 때 만든다 (EventSystem 없이 화면 좌표로 판정).
    /// </summary>
    public sealed class CharacterSelectPresenter : MonoBehaviour
    {
        [Tooltip("창 전체 (열려 있을 때만 켠다)")] [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Vector2 _cardSize = new Vector2(330, 620);
        [SerializeField] private float _spacing = 40;
        [Tooltip("초상 확대 배율 (정수 — 픽셀이 번지지 않게)")] [SerializeField] private int _portraitScale = 4;

        private static readonly Color PanelColor = new Color(0.03f, 0.03f, 0.05f, 0.97f);
        private static readonly Color CardColor = new Color(0.14f, 0.14f, 0.17f, 1f);
        private static readonly Color CardSelectedColor = new Color(0.55f, 0.45f, 0.12f, 1f);

        private readonly CharacterSelectModel _model = new CharacterSelectModel();
        private readonly List<(Image box, RectTransform rect)> _cards = new List<(Image, RectTransform)>();
        private TMP_Text _title;
        private TMP_Text _hint;
        private Action<int> _onConfirm;
        private Vector2 _lastMouse;

        public bool IsOpen => _model.IsOpen;
        public CharacterSelectModel Model => _model;

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(RectTransform panel, TMP_FontAsset font)
        {
            _panel = panel;
            _font = font;
        }

        private void Awake()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
            _model.Changed += Redraw;
            _model.Confirmed += OnConfirmed;
        }

        private void OnDestroy()
        {
            _model.Changed -= Redraw;
            _model.Confirmed -= OnConfirmed;
        }

        /// <summary>
        /// 창을 연다. portraits · names는 같은 길이, onConfirm(고른 칸 번호)은 확정 때 한 번.
        /// illustrations(같은 길이, 칸이 null이면 없음)가 있으면 그 카드는 일러스트를 카드에 맞춰 보여 주고, 없으면 64px 초상을 정수 배율로.
        /// </summary>
        public void Open(IReadOnlyList<Sprite> portraits, IReadOnlyList<string> names, int initial, Action<int> onConfirm,
            IReadOnlyList<Sprite> illustrations = null)
        {
            if (_panel == null) return;
            _onConfirm = onConfirm;
            Build(portraits, names, illustrations);
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
            if (Mouse.current != null) _lastMouse = Mouse.current.position.ReadValue();
            _model.Open(names.Count, initial);
        }

        private void OnConfirmed(int index)
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
            var callback = _onConfirm;
            _onConfirm = null;
            callback?.Invoke(index);
        }

        private void Update()
        {
            if (!_model.IsOpen) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) _model.Move(-1);
                else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) _model.Move(1);
                else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    _model.Confirm();
                    return;
                }
            }
            var mouse = Mouse.current;
            if (mouse == null) return;
            var screen = mouse.position.ReadValue();
            bool clicked = mouse.leftButton.wasPressedThisFrame;
            if (!clicked && (screen - _lastMouse).sqrMagnitude < 0.01f) return;
            _lastMouse = screen;
            int card = CardAt(screen);
            if (card < 0) return;
            _model.Select(card);
            if (clicked) _model.Confirm();
        }

        /// <summary>화면 좌표의 카드 번호, 없으면 -1.</summary>
        public int CardAt(Vector2 screen)
        {
            for (int i = 0; i < _cards.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(_cards[i].rect, screen, null)) return i;
            return -1;
        }

        private void Build(IReadOnlyList<Sprite> portraits, IReadOnlyList<string> names, IReadOnlyList<Sprite> illustrations)
        {
            foreach (var c in _cards) Destroy(c.rect.gameObject);
            _cards.Clear();
            var image = _panel.GetComponent<Image>();
            if (image != null) image.color = PanelColor;
            if (_title == null) _title = MakeText(_panel, "Title", new Vector2(0, 380), new Vector2(900, 60), 44);
            _title.text = HudText.CharacterSelectTitle;
            if (_hint == null) _hint = MakeText(_panel, "Hint", new Vector2(0, -380), new Vector2(1200, 40), 26);
            _hint.text = HudText.CharacterSelectHint;

            float total = names.Count * _cardSize.x + (names.Count - 1) * _spacing;
            for (int i = 0; i < names.Count; i++)
            {
                var go = new GameObject("Card" + i, typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(_panel, false);
                rect.sizeDelta = _cardSize;
                rect.anchoredPosition = new Vector2(-total / 2f + _cardSize.x / 2f + i * (_cardSize.x + _spacing), -10);
                var box = go.GetComponent<Image>();
                box.color = CardColor;
                box.raycastTarget = false;

                var illustration = illustrations != null && i < illustrations.Count ? illustrations[i] : null;
                var sprite = illustration != null ? illustration : i < portraits.Count ? portraits[i] : null;
                if (sprite != null)
                {
                    var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                    var pr = (RectTransform)portrait.transform;
                    pr.SetParent(rect, false);
                    if (illustration != null)
                    {
                        // 일러스트: 이름 줄 위 영역에 비율 유지로 맞춤 (부드러운 필터로 가져옴)
                        var area = new Vector2(_cardSize.x - 16, _cardSize.y - 70);
                        float scale = Mathf.Min(area.x / sprite.rect.width, area.y / sprite.rect.height);
                        pr.sizeDelta = sprite.rect.size * scale;
                        pr.anchoredPosition = new Vector2(0, 27);
                    }
                    else
                    {
                        pr.sizeDelta = sprite.rect.size * _portraitScale;
                        pr.anchoredPosition = new Vector2(0, 25);
                    }
                    var pi = portrait.GetComponent<Image>();
                    pi.sprite = sprite;
                    pi.raycastTarget = false;
                }
                var label = MakeText(rect, "Name", new Vector2(0, -_cardSize.y / 2f + 30), new Vector2(_cardSize.x - 20, 40), 28);
                label.text = names[i];
                _cards.Add((box, rect));
            }
        }

        private TMP_Text MakeText(RectTransform parent, string name, Vector2 pos, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private void Redraw()
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].box.color = i == _model.Selected ? CardSelectedColor : CardColor;
        }
    }
}
