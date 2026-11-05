using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectM.UI
{
    /// <summary>
    /// 결과 화면 (3-4b, 그레이박스): 화면 전체를 어둡게 덮고 가운데 제목 + 본문. 그리기만 한다 — 내용은 ResultModel.
    /// 패널 · 글상자는 Setup 메뉴가 만든다 (HUD 캔버스 아래 Result).
    /// </summary>
    public sealed class ResultPresenter : MonoBehaviour
    {
        [Tooltip("화면 전체 덮개 (보일 때만 켠다)")] [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _body;
        [Tooltip("G-18 UI 그림 (상자 틀 · 승리 / 패배 장식) — 비면 예전 그대로")] [SerializeField] private UiSkin _skin;
        [Tooltip("G-18: 장식 크기 (제목 위)")] [SerializeField] private float _emblemSize = 200f;

        private Image _emblem;

        private static readonly Color VictoryColor = new Color(1f, 0.84f, 0.31f);
        private static readonly Color DefeatColor = new Color(1f, 0.4f, 0.35f);

        public bool IsVisible => _panel != null && _panel.gameObject.activeSelf;

        private void Awake() => Hide();

        public void Show(ResultModel model)
        {
            if (_panel == null || model == null) return;
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
            if (_title != null)
            {
                _title.text = model.Title;
                _title.color = model.Outcome == ProjectM.Core.RunOutcome.Victory ? VictoryColor : DefeatColor;
            }
            if (_body != null) _body.text = model.Body();
            ApplySkin(model.Outcome == ProjectM.Core.RunOutcome.Victory);
        }

        /// <summary>G-18: 상자(제목의 부모)에 창 틀, 제목 위에 승리 / 패배 장식.</summary>
        private void ApplySkin(bool victory)
        {
            if (_skin == null || _title == null) return;
            var box = _title.rectTransform.parent as RectTransform;
            if (box == null) return;
            if (box != _panel) UiSkin.ApplyFrame(box.GetComponent<Image>(), _skin.Panel, _skin.PanelBorder);
            var sprite = victory ? _skin.Victory : _skin.Defeat;
            if (sprite == null) return;
            if (_emblem == null)
            {
                var go = new GameObject("Emblem", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(box, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, -_emblemSize * 0.25f);
                rect.sizeDelta = new Vector2(_emblemSize, _emblemSize);
                _emblem = go.GetComponent<Image>();
                _emblem.preserveAspect = true;
                _emblem.raycastTarget = false;
            }
            _emblem.sprite = sprite;
        }

        public void Hide()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }
    }
}
