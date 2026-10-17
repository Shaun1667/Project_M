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
        }

        public void Hide()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }
    }
}
