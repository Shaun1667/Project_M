using System.Collections.Generic;
using ProjectM.Items;
using ProjectM.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectM.UI
{
    /// <summary>
    /// 인벤토리 창 (3-2a, 그레이박스): 화면 가운데 제목 + 칸 격자(InventoryMenu.Columns칸씩) + 고른 항목의 메뉴.
    /// 마법 융합 고르기(3-2b)에서는 제목이 "마법 융합 — …", 첫 주문 칸은 파랑, 주문이 아닌 칸은 어둡게.
    /// 상태는 InventoryMenu, 내용은 플레이어 인벤토리 · 빠른 칸에서 읽어 그리기만 한다. 칸 · 메뉴 상자는 처음 그릴 때 만든다.
    /// 클릭 판정은 CellAt · MenuOptionAt (EventSystem 없이 화면 좌표로).
    /// </summary>
    public sealed class InventoryPresenter : MonoBehaviour
    {
        [Tooltip("창 전체 (열려 있을 때만 켠다)")] [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Vector2 _cellSize = new Vector2(170, 56);
        [SerializeField] private float _spacing = 8;
        [SerializeField] private Vector2 _menuItemSize = new Vector2(150, 40);
        [Tooltip("아이템 픽셀 그림 (P-3 ⓒ) — 칸 왼쪽에. 비면 글자만")] [SerializeField] private ProjectM.Presentation.ItemVisualCatalog _itemIcons;

        private static readonly Color PanelColor = new Color(0.06f, 0.06f, 0.08f, 0.94f);
        private static readonly Color CellColor = new Color(0.18f, 0.18f, 0.2f, 1f);
        private static readonly Color CellEmptyColor = new Color(0.12f, 0.12f, 0.13f, 1f);
        private static readonly Color SelectedColor = new Color(0.55f, 0.45f, 0.12f, 1f);
        private static readonly Color MenuColor = new Color(0.22f, 0.22f, 0.26f, 1f);
        private static readonly Color MenuSelectedColor = new Color(0.65f, 0.52f, 0.14f, 1f);
        private static readonly Color FusionFirstColor = new Color(0.2f, 0.38f, 0.72f, 1f);

        private const float Padding = 20, TitleHeight = 44;

        private TMP_Text _title;
        private readonly List<(Image box, TMP_Text label)> _cells = new List<(Image, TMP_Text)>();
        private readonly List<Image> _cellIcons = new List<Image>();
        private RectTransform _menu;
        private readonly List<(Image box, TMP_Text label)> _menuItems = new List<(Image, TMP_Text)>();

        public bool IsVisible => _panel != null && _panel.gameObject.activeSelf;

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(RectTransform panel, TMP_FontAsset font)
        {
            _panel = panel;
            _font = font;
        }

        private void Awake()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }

        public void Hide()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }

        public void Draw(InventoryMenu menu, Unit player, HudText text)
        {
            if (_panel == null) return;
            if (menu == null || !menu.IsOpen || player == null)
            {
                Hide();
                return;
            }
            _panel.gameObject.SetActive(true);
            var slots = player.InventorySlots;
            EnsureLayout(slots.Length);

            bool fusion = menu.State == InventoryMenuState.FusionPick;
            if (fusion) _title.text = text.FusionPickTitle(menu.FusionFirst >= 0 ? slots[menu.FusionFirst] : InventoryRef.Empty);
            else if (menu.AssignTarget >= 0) _title.text = HudText.AssignHint(menu.AssignTarget);
            else _title.text = menu.State == InventoryMenuState.ChooseSlot ? text.ChooseSlotHint(menu.SelectedEntry(player)) : "인벤토리";

            for (int i = 0; i < _cells.Count; i++)
            {
                var (box, label) = _cells[i];
                var entry = slots[i];
                // 융합 고르기: 첫 주문 칸은 파랑으로 남기고, 주문이 아닌 칸은 빈 칸처럼 어둡게
                if (i == menu.Selected) box.color = SelectedColor;
                else if (fusion && i == menu.FusionFirst) box.color = FusionFirstColor;
                else box.color = entry.IsEmpty || (fusion && entry.Kind != InventoryKind.Spell) ? CellEmptyColor : CellColor;
                SetIcon(i < _cellIcons.Count ? _cellIcons[i] : null, label, entry, text);
                if (entry.IsEmpty)
                {
                    label.text = string.Empty;
                    continue;
                }
                int quick = Inventory.QuickSlotOf(player, entry);
                string kind = entry.Kind == InventoryKind.Spell ? "<color=#8FB8FF>주문</color>" : "<color=#FFC46B>도구</color>";
                label.text = $"{kind} {text.Entry(entry)}" + (quick >= 0 ? $" <color=#FFD54F>[{quick + 1}]</color>" : string.Empty);
            }

            DrawMenu(menu);
        }

        /// <summary>화면 좌표가 가리키는 인벤토리 칸, 없으면 -1.</summary>
        public int CellAt(Vector2 screenPos)
        {
            if (!IsVisible) return -1;
            for (int i = 0; i < _cells.Count; i++)
                if (Contains(_cells[i].box, screenPos)) return i;
            return -1;
        }

        /// <summary>화면 좌표가 가리키는 메뉴 줄, 없으면 -1.</summary>
        public int MenuOptionAt(Vector2 screenPos)
        {
            if (!IsVisible || _menu == null || !_menu.gameObject.activeSelf) return -1;
            for (int i = 0; i < _menuItems.Count; i++)
                if (_menuItems[i].box.gameObject.activeSelf && Contains(_menuItems[i].box, screenPos)) return i;
            return -1;
        }

        /// <summary>화면 좌표가 창 안인지 (창 밖 클릭 = 닫기 판정용).</summary>
        public bool Contains(Vector2 screenPos) => IsVisible && RectTransformUtility.RectangleContainsScreenPoint(_panel, screenPos, null);

        private static bool Contains(Image box, Vector2 screenPos) =>
            RectTransformUtility.RectangleContainsScreenPoint((RectTransform)box.transform, screenPos, null);

        private void DrawMenu(InventoryMenu menu)
        {
            bool show = menu.State == InventoryMenuState.Menu;
            _menu.gameObject.SetActive(show);
            if (!show) return;

            var options = menu.Options;
            for (int i = 0; i < options.Count; i++)
            {
                if (i >= _menuItems.Count) _menuItems.Add(CreateBox(_menu, "MenuItem", _menuItemSize));
                var (box, label) = _menuItems[i];
                box.gameObject.SetActive(true);
                var rect = (RectTransform)box.transform;
                rect.anchoredPosition = new Vector2(0, -i * (_menuItemSize.y + 4));
                box.color = i == menu.MenuIndex ? MenuSelectedColor : MenuColor;
                label.text = OptionText(options[i]);
            }
            for (int i = options.Count; i < _menuItems.Count; i++) _menuItems[i].box.gameObject.SetActive(false);

            // 고른 칸 오른쪽에 붙인다
            var cell = (RectTransform)_cells[menu.Selected].box.transform;
            _menu.anchoredPosition = cell.anchoredPosition + new Vector2(_cellSize.x + 6, 0);
            _menu.SetAsLastSibling();
        }

        private static string OptionText(InventoryMenuOption option)
        {
            switch (option)
            {
                case InventoryMenuOption.Use: return HudText.MenuUse;
                case InventoryMenuOption.Assign: return HudText.MenuAssign;
                case InventoryMenuOption.Unassign: return HudText.MenuUnassign;
                case InventoryMenuOption.Throw: return HudText.MenuThrow;
                default: return HudText.MenuClose;
            }
        }

        private void EnsureLayout(int slotCount)
        {
            if (_title != null && _cells.Count == slotCount) return;

            int columns = InventoryMenu.Columns, rows = (slotCount + columns - 1) / columns;
            float width = Padding * 2 + columns * _cellSize.x + (columns - 1) * _spacing;
            float height = Padding * 2 + TitleHeight + rows * _cellSize.y + (rows - 1) * _spacing;
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.anchoredPosition = Vector2.zero;
            _panel.sizeDelta = new Vector2(width, height);
            var bg = _panel.GetComponent<Image>();
            if (bg == null) bg = _panel.gameObject.AddComponent<Image>();
            bg.color = PanelColor;
            bg.raycastTarget = false;

            if (_title == null)
            {
                var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleGo.transform.SetParent(_panel, false);
                var rect = TopLeft((RectTransform)titleGo.transform, new Vector2(Padding, -Padding), new Vector2(width - Padding * 2, TitleHeight));
                rect.sizeDelta = new Vector2(width - Padding * 2, TitleHeight);
                _title = titleGo.GetComponent<TextMeshProUGUI>();
                Style(_title, 28, TextAlignmentOptions.TopLeft, new Color(1f, 0.84f, 0.31f));
            }

            foreach (var (box, _) in _cells) Destroy(box.gameObject);
            _cells.Clear();
            _cellIcons.Clear();
            for (int i = 0; i < slotCount; i++)
            {
                var cell = CreateBox(_panel, "Cell" + i, _cellSize);
                var rect = (RectTransform)cell.Item1.transform;
                rect.anchoredPosition = new Vector2(Padding + i % columns * (_cellSize.x + _spacing),
                    -(Padding + TitleHeight + i / columns * (_cellSize.y + _spacing)));
                _cells.Add(cell);
                _cellIcons.Add(CreateIcon(rect, _cellSize.y));
            }

            if (_menu == null)
            {
                var menuGo = new GameObject("Menu", typeof(RectTransform));
                menuGo.transform.SetParent(_panel, false);
                _menu = TopLeft((RectTransform)menuGo.transform, Vector2.zero, _menuItemSize);
            }
        }

        /// <summary>칸 왼쪽 정사각 아이콘 (P-3 ⓒ, 점 필터 그림을 정수 배로 — 64px 그림을 칸 높이에 맞춤).</summary>
        private static Image CreateIcon(RectTransform cell, float height)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(cell, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(2f, 0f);
            rect.sizeDelta = new Vector2(height, height);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;
            return image;
        }

        /// <summary>아이템 칸이면 그림을 켜고 글자를 그림 오른쪽으로 민다. 주문 · 빈 칸 · 그림 없음 = 글자만.</summary>
        private void SetIcon(Image icon, TMP_Text label, InventoryRef entry, HudText text)
        {
            if (icon == null) return;
            Sprite sprite = null;
            if (_itemIcons != null && !entry.IsEmpty && entry.Kind == InventoryKind.Item && !_itemIcons.TryGetIcon(text.VisualOf(entry.Id), out sprite))
                _itemIcons.TryGetIcon(entry.Id, out sprite);
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            label.rectTransform.offsetMin = new Vector2(sprite != null ? _cellSize.y + 2f : 8f, 0f);
        }

        private (Image, TMP_Text) CreateBox(RectTransform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            TopLeft((RectTransform)go.transform, Vector2.zero, size);
            var box = go.GetComponent<Image>();
            box.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var rect = (RectTransform)labelGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 0);
            rect.offsetMax = new Vector2(-6, 0);
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            Style(label, size.y * 0.42f, TextAlignmentOptions.Left, Color.white);
            return (box, label);
        }

        private void Style(TMP_Text label, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            if (_font != null) label.font = _font;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static RectTransform TopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
