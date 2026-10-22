using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 2D 픽셀 맵 타일 묶음 (P-3a, 결정 2026-10-21 — 64px · 위에서 비스듬히 SPD식). 데이터는 칸 종류만 알고, 어떤 그림인지는 여기서 고른다.
    /// 바닥 · 벽 윗면은 큰 그림 한 장을 N×N 칸으로 나눈 것이고 칸 위치(x, y를 N으로 나눈 나머지)로 고른다 — 이웃 칸끼리 그림이 이어진다(결정 2026-10-22).
    /// 벽 앞면은 가로 띠를 N칸으로 나눠 x로 고른다. 벽 윗면 테두리는 열린 변(아래 4비트) + 안쪽 모서리(위 4비트) = 256칸 표(쓰는 조합만 채움).
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Pixel Tile Set", fileName = "PixelTileSet")]
    public sealed class PixelTileSet : ScriptableObject
    {
        [Tooltip("큰 그림 한 변의 칸 수 (바닥 · 벽 윗면 N×N, 벽 앞면 N)")] [SerializeField] private int _span = 4;
        [Tooltip("바닥 N×N — 아래 줄부터 (y % N) * N + (x % N)")] [SerializeField] private TileBase[] _floors = new TileBase[0];
        [Tooltip("벽 앞면 N (아래 칸이 바닥) — x % N")] [SerializeField] private TileBase[] _wallFronts = new TileBase[0];
        [Tooltip("벽 윗면 N×N (아래 칸도 벽 · 맵 밖)")] [SerializeField] private TileBase[] _wallTops = new TileBase[0];
        [Tooltip("벽 윗면 테두리 256칸 — 비트 1 위 · 2 오른쪽 · 4 아래 · 8 왼쪽 변이 열림, 16 오른쪽위 · 32 오른쪽아래 · 64 왼쪽아래 · 128 왼쪽위 안쪽 모서리")] [SerializeField] private TileBase[] _rims = new TileBase[0];
        [SerializeField] private TileBase _stairsDown;
        [SerializeField] private TileBase _stairsUp;

        public TileBase StairsDown => _stairsDown;
        public TileBase StairsUp => _stairsUp;

        public TileBase Floor(int x, int y) => PickGrid(_floors, x, y);
        public TileBase WallTop(int x, int y) => PickGrid(_wallTops, x, y);
        public TileBase WallFront(int x) => _wallFronts == null || _wallFronts.Length == 0 ? null : _wallFronts[Wrap(x, _wallFronts.Length)];
        public TileBase Rim(int mask) => mask > 0 && _rims != null && mask < _rims.Length ? _rims[mask] : null;

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
        public void Configure(int span, TileBase[] floors, TileBase[] wallFronts, TileBase[] wallTops,
            TileBase[] rims, TileBase stairsDown, TileBase stairsUp)
        {
            _span = span;
            _floors = floors ?? new TileBase[0];
            _wallFronts = wallFronts ?? new TileBase[0];
            _wallTops = wallTops ?? new TileBase[0];
            _rims = rims ?? new TileBase[0];
            _stairsDown = stairsDown;
            _stairsUp = stairsUp;
        }
    }
}
