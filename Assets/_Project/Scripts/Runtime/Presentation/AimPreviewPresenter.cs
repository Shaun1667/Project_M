using ProjectM.Core;
using ProjectM.Magic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 주문 조준 미리보기 (2-2): 안개 위 Tilemap 한 장에 경로 · 맞는 칸 · 조준 칸을 색으로 칠한다.
    /// 규칙은 모른다 — 입력 쪽이 SpellCasting.Preview 결과를 넘긴다. 타일은 안개와 같은 흰 사각형(TileFlags.None).
    /// </summary>
    public sealed class AimPreviewPresenter : MonoBehaviour
    {
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TileBase _tile;
        [SerializeField] private Color _pathColor = new Color(1f, 0.9f, 0.3f, 0.35f);
        [SerializeField] private Color _affectedColor = new Color(1f, 0.45f, 0.1f, 0.55f);
        [SerializeField] private Color _cursorColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color _blockedColor = new Color(1f, 0.1f, 0.1f, 0.5f);

        public bool IsShowing { get; private set; }

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Tilemap tilemap, TileBase tile)
        {
            _tilemap = tilemap;
            _tile = tile;
        }

        /// <summary>조준 칸 + (쓸 수 있으면) 미리보기. 쓸 수 없으면 조준 칸만 빨갛게.</summary>
        public void Show(GridPos cursor, SpellPreview preview, bool castable)
        {
            Hide();
            IsShowing = true;
            if (castable && preview != null)
            {
                foreach (var p in preview.Path) Paint(p, _pathColor);
                foreach (var p in preview.AffectedCells) Paint(p, _affectedColor);
            }
            Paint(cursor, castable ? _cursorColor : _blockedColor);
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
