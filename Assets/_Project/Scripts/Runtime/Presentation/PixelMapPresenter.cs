using ProjectM.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 2D 픽셀 맵 (P-3a, 결정 2026-10-21 — 3D TilemapPresenter를 대신함): 칸 종류를 PixelTileSet 타일로 Tilemap 한 장에 칠한다.
    /// 벽은 얇은 벽 RuleTile(PixelWallTile)이 같은 Tilemap의 이웃을 보고 앞면 · 테두리를 고른다(2026-10-22) — 바닥 · 계단 칸이
    /// 바뀌면 둘레 벽을 다시 고르게 한다. 빈 칸(Empty)은 칠하지 않음(배경색).
    /// 홀로 선 벽(위 · 아래 칸이 모두 바닥 — 한 칸 기둥 · 얇은 가로벽)은 앞면은 다른 벽과 같고, 솟은 윗면을 위 칸에 덮개(_caps)로 그린다(2026-10-22 — 앞면 높이 통일).
    /// 시야는 안개 Tilemap(FogPresenter)이 위에서 가린다. MapLoadedEvent · TileChangedEvent만 본다. 규칙을 갖지 않는다.
    /// </summary>
    public sealed class PixelMapPresenter : MonoBehaviour
    {
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private PixelTileSet _tiles;
        [Tooltip("홀로 선 벽의 솟은 윗면 덮개 (맵 위 · 안개 아래)")] [SerializeField] private Tilemap _caps;

        private SimEventBus _bus;
        private TileType[] _types = System.Array.Empty<TileType>();
        private int _width, _height;

        public Tilemap Tilemap => _tilemap;

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Tilemap tilemap, PixelTileSet tiles, Tilemap caps = null)
        {
            _tilemap = tilemap;
            _tiles = tiles;
            _caps = caps;
        }

        public void Bind(SimEventBus bus)
        {
            Unbind();
            _bus = bus;
            _bus.Subscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Subscribe<TileChangedEvent>(OnTileChanged);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Unsubscribe<TileChangedEvent>(OnTileChanged);
            _bus = null;
        }

        private void OnDestroy() => Unbind();

        private void OnMapLoaded(MapLoadedEvent e)
        {
            _tilemap.ClearAllTiles();
            if (_caps != null) _caps.ClearAllTiles();
            _width = e.Width;
            _height = e.Height;
            int count = _width * _height;
            _types = new TileType[count];
            for (int i = 0; i < count; i++) _types[i] = e.Cells[i];
            var positions = new Vector3Int[count];
            var tiles = new TileBase[count];
            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3Int(i % _width, i / _width, 0);
                tiles[i] = TileAt(i % _width, i / _width);
            }
            _tilemap.SetTiles(positions, tiles);
            if (_caps == null) return;
            for (int i = 0; i < count; i++) tiles[i] = CapAt(i % _width, i / _width);
            _caps.SetTiles(positions, tiles);
        }

        private void OnTileChanged(TileChangedEvent e)
        {
            if (!InBounds(e.Pos.X, e.Pos.Y)) return;
            _types[e.Pos.Y * _width + e.Pos.X] = e.Type;
            _tilemap.SetTile(new Vector3Int(e.Pos.X, e.Pos.Y, 0), TileAt(e.Pos.X, e.Pos.Y));
            // 둘레 벽의 앞면 · 테두리(이웃이 앞면인지는 그 아래 칸까지 봄)가 바뀔 수 있음 — 바닥 타일은 이웃을 새로 그리지 않으므로 직접
            for (int y = e.Pos.Y - 1; y <= e.Pos.Y + 2; y++)
                for (int x = e.Pos.X - 1; x <= e.Pos.X + 1; x++)
                    if (InBounds(x, y))
                    {
                        _tilemap.RefreshTile(new Vector3Int(x, y, 0));
                        if (_caps != null) _caps.SetTile(new Vector3Int(x, y, 0), CapAt(x, y));
                    }
        }

        /// <summary>그 칸에 칠할 타일 (테스트용 공개).</summary>
        public TileBase TileAt(int x, int y)
        {
            if (_tiles == null || !InBounds(x, y)) return null;
            switch (_types[y * _width + x])
            {
                case TileType.Floor: return _tiles.Floor(x, y);
                case TileType.StairsDown: return _tiles.StairsDown;
                case TileType.StairsUp: return _tiles.StairsUp;
                case TileType.Wall: return _tiles.Wall;
                default: return null;
            }
        }

        /// <summary>
        /// 이 칸에 덮을 솟은 윗면 (테스트용 공개) — 바로 아래 칸이 한 칸 기둥(위 · 아래가 바닥, 양옆도 벽 아님)일 때만.
        /// 옆으로 이어진 한 줄 벽(얇은 가로벽)은 솟지 않는다 — 옆 벽보다 반 칸 튀어나와 보였음 (2026-10-23 사용자 피드백).
        /// </summary>
        public TileBase CapAt(int x, int y)
        {
            if (_tiles == null || !IsWall(x, y - 1) || !IsWalkable(x, y) || !IsWalkable(x, y - 2)) return null;
            if (IsWall(x + 1, y - 1) || IsWall(x - 1, y - 1)) return null;
            return _tiles.WallCap(3);
        }

        private bool IsWall(int x, int y) => InBounds(x, y) && _types[y * _width + x] == TileType.Wall;
        private bool IsWalkable(int x, int y) => InBounds(x, y) && GridMap.IsWalkableType(_types[y * _width + x]);

        private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height;
    }
}
