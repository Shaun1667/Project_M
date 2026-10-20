using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 지형 효과 오버레이 (2-4): 바닥 위 · 유닛 아래 Tilemap 한 장에 칸 색을 칠한다 (화염 지대 주황 · 독구름 보라 · 물 파랑 · 오염된 물 탁한 초록).
    /// 한 칸에 여러 효과가 있으면 색을 섞는다. TerrainChangedEvent만 보고 그린다. 안개(정렬 20)가 덮으므로 안 보이는 칸은 가려진다.
    /// G-3: 종류마다 바닥 무늬 타일(그을음 · 물결 · 얼음 금 등, Setup이 만든 흰 무늬 — 색은 위 색)을 쓰고, 없으면 흰 사각형. 파티클은 FxPresenter.
    /// </summary>
    public sealed class TerrainOverlayPresenter : MonoBehaviour
    {
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TileBase _tile;
        [SerializeField] private Color _fireZoneColor = new Color(1f, 0.4f, 0.05f, 0.5f);
        [SerializeField] private Color _poisonCloudColor = new Color(0.55f, 0.2f, 0.9f, 0.45f);
        [SerializeField] private Color _waterColor = new Color(0.15f, 0.45f, 1f, 0.45f);
        [SerializeField] private Color _contaminatedWaterColor = new Color(0.4f, 0.6f, 0.15f, 0.5f);
        [SerializeField] private Color _icePatchColor = new Color(0.75f, 0.92f, 1f, 0.55f);
        [SerializeField] private Color _paralyticGasColor = new Color(0.95f, 0.85f, 0.2f, 0.45f); // 3-9 마비 가스

        [Header("G-3 바닥 무늬 (비면 흰 사각형)")]
        [SerializeField] private TileBase _fireZoneTile;
        [SerializeField] private TileBase _poisonCloudTile;
        [SerializeField] private TileBase _waterTile;
        [SerializeField] private TileBase _contaminatedWaterTile;
        [SerializeField] private TileBase _icePatchTile;
        [SerializeField] private TileBase _paralyticGasTile;

        private readonly Dictionary<GridPos, List<TerrainEffectType>> _cells = new Dictionary<GridPos, List<TerrainEffectType>>();
        private SimEventBus _bus;

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Tilemap tilemap, TileBase tile)
        {
            _tilemap = tilemap;
            _tile = tile;
        }

        public void Bind(SimEventBus bus)
        {
            Unbind();
            _bus = bus;
            _bus.Subscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Subscribe<TerrainChangedEvent>(HandleTerrainChanged);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Unsubscribe<TerrainChangedEvent>(HandleTerrainChanged);
            _bus = null;
        }

        private void OnDestroy() => Unbind();

        public Color ColorOf(TerrainEffectType type)
        {
            switch (type)
            {
                case TerrainEffectType.FireZone: return _fireZoneColor;
                case TerrainEffectType.PoisonCloud: return _poisonCloudColor;
                case TerrainEffectType.Water: return _waterColor;
                case TerrainEffectType.ContaminatedWater: return _contaminatedWaterColor;
                case TerrainEffectType.IcePatch: return _icePatchColor;
                case TerrainEffectType.ParalyticGas: return _paralyticGasColor;
                default: return Color.clear;
            }
        }

        public TileBase TileOf(TerrainEffectType type)
        {
            TileBase tile = null;
            switch (type)
            {
                case TerrainEffectType.FireZone: tile = _fireZoneTile; break;
                case TerrainEffectType.PoisonCloud: tile = _poisonCloudTile; break;
                case TerrainEffectType.Water: tile = _waterTile; break;
                case TerrainEffectType.ContaminatedWater: tile = _contaminatedWaterTile; break;
                case TerrainEffectType.IcePatch: tile = _icePatchTile; break;
                case TerrainEffectType.ParalyticGas: tile = _paralyticGasTile != null ? _paralyticGasTile : _poisonCloudTile; break; // 무늬는 독구름과 같게
            }
            return tile != null ? tile : _tile;
        }

        private void OnMapLoaded(MapLoadedEvent e)
        {
            _cells.Clear();
            _tilemap.ClearAllTiles();
        }

        private void HandleTerrainChanged(TerrainChangedEvent e)
        {
            if (!_cells.TryGetValue(e.Pos, out var list))
            {
                list = new List<TerrainEffectType>();
                _cells[e.Pos] = list;
            }
            if (e.Remaining > 0) { if (!list.Contains(e.Type)) list.Add(e.Type); }
            else list.Remove(e.Type);
            Paint(e.Pos, list);
        }

        private void Paint(GridPos pos, List<TerrainEffectType> list)
        {
            var cell = new Vector3Int(pos.X, pos.Y, 0);
            if (list.Count == 0)
            {
                _tilemap.SetTile(cell, null);
                _cells.Remove(pos);
                return;
            }

            // 여러 효과 = 색 평균, 투명도는 가장 진한 쪽
            Color sum = Color.clear;
            float alpha = 0f;
            foreach (var type in list)
            {
                var c = ColorOf(type);
                sum += c;
                alpha = Mathf.Max(alpha, c.a);
            }
            var color = sum / list.Count;
            color.a = alpha;

            _tilemap.SetTile(cell, TileOf(list[list.Count - 1])); // 무늬는 가장 나중에 놓인 효과
            _tilemap.SetTileFlags(cell, TileFlags.None);
            _tilemap.SetColor(cell, color);
        }
    }
}
