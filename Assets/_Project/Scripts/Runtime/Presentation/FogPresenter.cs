using ProjectM.Core;
using ProjectM.Units;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 시야 안개 (1-7): 지형 위 Tilemap 한 장. 안 본 칸 = 검정, 본 적 있는 칸 = 어둡게, 지금 보이는 칸 = 안개 없음.
    /// VisibilityChangedEvent만 보고 그리며, 바뀐 칸만 다시 칠한다. 안개 타일은 흰 사각형 + 칸별 색(TileFlags.None).
    /// </summary>
    public sealed class FogPresenter : MonoBehaviour
    {
        private enum CellState : byte { Unknown = 0, Unexplored, Explored, Visible }

        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TileBase _fogTile;
        [SerializeField] private Color _unexploredColor = Color.black;
        [SerializeField] private Color _exploredColor = new Color(0f, 0f, 0f, 0.6f);

        private SimEventBus _bus;
        private CellState[] _states = System.Array.Empty<CellState>();
        private int _width;

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Tilemap tilemap, TileBase fogTile)
        {
            _tilemap = tilemap;
            _fogTile = fogTile;
        }

        public void Bind(SimEventBus bus)
        {
            Unbind();
            _bus = bus;
            _bus.Subscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Subscribe<VisibilityChangedEvent>(OnVisibilityChanged);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Unsubscribe<VisibilityChangedEvent>(OnVisibilityChanged);
            _bus = null;
        }

        private void OnDestroy() => Unbind();

        private void OnMapLoaded(MapLoadedEvent e)
        {
            _tilemap.ClearAllTiles();
            _width = e.Width;
            _states = new CellState[e.Width * e.Height];
            for (int i = 0; i < _states.Length; i++)
                Paint(i, CellState.Unexplored);
        }

        private void OnVisibilityChanged(VisibilityChangedEvent e)
        {
            if (e.Width * e.Height != _states.Length) return; // 맵보다 먼저 온 이벤트
            for (int i = 0; i < _states.Length; i++)
            {
                var state = e.Visible[i] ? CellState.Visible : e.Explored[i] ? CellState.Explored : CellState.Unexplored;
                if (state != _states[i]) Paint(i, state);
            }
        }

        private void Paint(int index, CellState state)
        {
            _states[index] = state;
            var pos = new Vector3Int(index % _width, index / _width, 0);
            if (state == CellState.Visible)
            {
                _tilemap.SetTile(pos, null);
                return;
            }
            _tilemap.SetTile(pos, _fogTile);
            _tilemap.SetTileFlags(pos, TileFlags.None);
            _tilemap.SetColor(pos, state == CellState.Explored ? _exploredColor : _unexploredColor);
        }
    }
}
