using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 2D 픽셀 맵 타일 묶음 (P-3a, 결정 2026-10-21 — 64px · 위에서 비스듬히 SPD식). 데이터는 칸 종류만 알고, 어떤 그림인지는 여기서 고른다.
    /// 바닥은 큰 그림 한 장을 N×N 칸으로 나눈 것이고 칸 위치(x, y를 N으로 나눈 나머지)로 고른다 — 이웃 칸끼리 그림이 이어진다(결정 2026-10-22).
    /// 벽은 얇은 벽 RuleTile 하나(PixelWallTile — 이웃을 보고 앞면 · 테두리를 고름, 2026-10-22).
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Pixel Tile Set", fileName = "PixelTileSet")]
    public sealed class PixelTileSet : ScriptableObject
    {
        [Tooltip("바닥 큰 그림 한 변의 칸 수 (N×N)")] [SerializeField] private int _span = 4;
        [Tooltip("바닥 N×N — 아래 줄부터 (y % N) * N + (x % N)")] [SerializeField] private TileBase[] _floors = new TileBase[0];
        [Tooltip("벽 — 얇은 벽 RuleTile (PixelWallTile)")] [SerializeField] private TileBase _wall;
        [SerializeField] private TileBase _stairsDown;
        [SerializeField] private TileBase _stairsUp;

        public TileBase StairsDown => _stairsDown;
        public TileBase StairsUp => _stairsUp;
        public TileBase Wall => _wall;

        public TileBase Floor(int x, int y) => PickGrid(_floors, x, y);

        /// <summary>N×N 그림에서 칸 위치로 한 장 (음수 좌표도 이어짐). 장 수가 N×N이 아니면 순서대로 돌려 씀.</summary>
        private TileBase PickGrid(TileBase[] tiles, int x, int y)
        {
            if (tiles == null || tiles.Length == 0) return null;
            int n = Mathf.Max(1, _span);
            if (tiles.Length != n * n) return tiles[Wrap(x + y, tiles.Length)];
            return tiles[Wrap(y, n) * n + Wrap(x, n)];
        }

        private static int Wrap(int v, int n) => ((v % n) + n) % n;

        /// <summary>에디터 Setup용.</summary>
        public void Configure(int span, TileBase[] floors, TileBase wall, TileBase stairsDown, TileBase stairsUp)
        {
            _span = span;
            _floors = floors ?? new TileBase[0];
            _wall = wall;
            _stairsDown = stairsDown;
            _stairsUp = stairsUp;
        }
    }
}
