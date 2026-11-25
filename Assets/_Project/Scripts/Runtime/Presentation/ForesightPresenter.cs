using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Units;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 예지 표시 (3-60): 조준 미리보기 아래 Tilemap 한 장에, 고른 행동 뒤 적이 갈 칸(보라) · 그 사이 죽는 적의 칸(어두운 빨강) ·
    /// 그때 모으는 힘의 중심(주황)을 칠한다. 규칙은 모른다 — GameRoot가 Foresight.Run 결과를 넘긴다. 타일은 안개와 같은 흰 사각형.
    /// </summary>
    public sealed class ForesightPresenter : MonoBehaviour
    {
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TileBase _tile;
        [SerializeField] private Color _futureColor = new Color(0.65f, 0.35f, 1f, 0.45f);
        [SerializeField] private Color _deathColor = new Color(0.55f, 0.05f, 0.1f, 0.5f);
        [SerializeField] private Color _chargeColor = new Color(1f, 0.55f, 0.1f, 0.55f);

        public bool IsShowing { get; private set; }

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Tilemap tilemap, TileBase tile)
        {
            _tilemap = tilemap;
            _tile = tile;
        }

        public void Show(IReadOnlyList<ForesightMark> marks)
        {
            Hide();
            if (_tilemap == null || marks == null) return;
            IsShowing = true;
            foreach (var m in marks)
            {
                if (m.Dies) { Paint(m.From, _deathColor); continue; }
                Paint(m.To, _futureColor);
                if (m.ChargeTarget.HasValue) Paint(m.ChargeTarget.Value, _chargeColor);
            }
        }

        public void Hide()
        {
            if (_tilemap != null) _tilemap.ClearAllTiles();
            IsShowing = false;
        }

        private void Paint(GridPos p, Color color)
        {
            var pos = new Vector3Int(p.X, p.Y, 0);
            _tilemap.SetTile(pos, _tile);
            _tilemap.SetTileFlags(pos, TileFlags.None);
            _tilemap.SetColor(pos, color);
        }
    }
}
