using ProjectM.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 2D 픽셀 맵 (P-3a, 결정 2026-10-21 — 3D TilemapPresenter를 대신함): 칸 종류를 PixelTileSet 타일로 Tilemap 한 장에 칠한다.
    /// 벽은 아래 칸이 걸을 수 있으면 앞면, 아니면 윗면. 벽 윗면은 열린 이웃(바닥 · 벽 앞면) 쪽 변에 밝은 테두리를
    /// 덮개 Tilemap(_rims)에 칠한다(2026-10-22). 칸이 바뀌면 둘레 칸을 다시 고른다. 빈 칸(Empty)은 칠하지 않음(배경색).
    /// 시야는 안개 Tilemap(FogPresenter)이 위에서 가린다. MapLoadedEvent · TileChangedEvent만 본다. 규칙을 갖지 않는다.
    /// </summary>
    public sealed class PixelMapPresenter : MonoBehaviour
    {
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private PixelTileSet _tiles;
        [Tooltip("벽 윗면 테두리 덮개 (맵 위 · 안개 아래)")] [SerializeField] private Tilemap _rims;

        private SimEventBus _bus;
        private TileType[] _types = System.Array.Empty<TileType>();
        private int _width, _height;

        public Tilemap Tilemap => _tilemap;

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Tilemap tilemap, PixelTileSet tiles, Tilemap rims = null)
        {
            _tilemap = tilemap;
            _tiles = tiles;
            _rims = rims;
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
            if (_rims != null) _rims.ClearAllTiles();
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
            if (_rims == null) return;
            for (int i = 0; i < count; i++) tiles[i] = RimAt(i % _width, i / _width);
            _rims.SetTiles(positions, tiles);
        }

        private void OnTileChanged(TileChangedEvent e)
        {
            if (!InBounds(e.Pos.X, e.Pos.Y)) return;
            _types[e.Pos.Y * _width + e.Pos.X] = e.Type;
            // 위 칸 벽의 앞면/윗면, 둘레 벽 윗면의 테두리(이웃이 앞면이 되는지는 그 아래 칸까지 봄)가 바뀔 수 있음
            for (int y = e.Pos.Y - 1; y <= e.Pos.Y + 2; y++)
                for (int x = e.Pos.X - 1; x <= e.Pos.X + 1; x++)
                    Repaint(x, y);
        }

        private void Repaint(int x, int y)
        {
            if (!InBounds(x, y)) return;
            var cell = new Vector3Int(x, y, 0);
            _tilemap.SetTile(cell, TileAt(x, y));
            if (_rims != null) _rims.SetTile(cell, RimAt(x, y));
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
                case TileType.Wall: return IsWalkable(x, y - 1) ? _tiles.WallFront(x) : _tiles.WallTop(x, y);
                default: return null;
            }
        }

        /// <summary>그 칸의 테두리 덮개 (테스트용 공개). 벽 윗면만 — 열린 변 + 안쪽 모서리(양옆 변이 닫혔는데 대각선만 열림).</summary>
        public TileBase RimAt(int x, int y)
        {
            if (_tiles == null || !IsWallTop(x, y)) return null;
            bool n = IsOpen(x, y + 1), e = IsOpen(x + 1, y), s = IsOpen(x, y - 1), w = IsOpen(x - 1, y);
            int edges = (n ? 1 : 0) | (e ? 2 : 0) | (s ? 4 : 0) | (w ? 8 : 0);
            int corners = (!n && !e && IsOpen(x + 1, y + 1) ? 1 : 0) | (!s && !e && IsOpen(x + 1, y - 1) ? 2 : 0)
                | (!s && !w && IsOpen(x - 1, y - 1) ? 4 : 0) | (!n && !w && IsOpen(x - 1, y + 1) ? 8 : 0);
            return _tiles.Rim(edges | corners << 4);
        }

        /// <summary>벽 윗면에서 보이는 쪽: 걸을 수 있는 칸 또는 벽 앞면.</summary>
        private bool IsOpen(int x, int y) => IsWalkable(x, y) || (IsWall(x, y) && IsWalkable(x, y - 1));
        private bool IsWallTop(int x, int y) => IsWall(x, y) && !IsWalkable(x, y - 1);
        private bool IsWall(int x, int y) => InBounds(x, y) && _types[y * _width + x] == TileType.Wall;

        private bool IsWalkable(int x, int y) => InBounds(x, y) && GridMap.IsWalkableType(_types[y * _width + x]);
        private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height;
    }
}
