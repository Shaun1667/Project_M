using ProjectM.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>지형 이벤트(MapLoaded · TileChanged) → Tilemap. 규칙은 모르고 그리기만 한다.</summary>
    public sealed class TilemapPresenter : MonoBehaviour
    {
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TileVisualSet _visuals;

        private SimEventBus _bus;

        public Tilemap Tilemap => _tilemap;

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Tilemap tilemap, TileVisualSet visuals)
        {
            _tilemap = tilemap;
            _visuals = visuals;
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
            int count = e.Width * e.Height;
            var positions = new Vector3Int[count];
            var tiles = new TileBase[count];
            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3Int(i % e.Width, i / e.Width, 0);
                tiles[i] = _visuals.Get(e.Cells[i]);
            }
            _tilemap.SetTiles(positions, tiles);
        }

        private void OnTileChanged(TileChangedEvent e)
        {
            _tilemap.SetTile(new Vector3Int(e.Pos.X, e.Pos.Y, 0), _visuals.Get(e.Type));
        }
    }
}
