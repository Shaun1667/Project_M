using ProjectM.Core;
using ProjectM.Dungeon;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 함정 표시 (3-12a): 드러난 함정만 바닥 위 Tilemap 한 장에 종류 색 칸으로 칠한다 (그레이박스 — 그림이 생기면 타일만 바꾼다).
    /// TrapChangedEvent만 보고 그린다 — 숨겨진 함정은 이벤트가 오지 않아 안 보인다. 안개가 덮으므로 안 보이는 칸은 가려진다.
    /// </summary>
    public sealed class TrapPresenter : MonoBehaviour
    {
        [SerializeField] private Tilemap _tilemap;
        [Tooltip("흰 사각형 타일 — 색은 아래 표")] [SerializeField] private TileBase _tile;
        [SerializeField] private Color _fireColor = new Color(1f, 0.35f, 0.1f, 0.9f);
        [SerializeField] private Color _toxicGasColor = new Color(0.55f, 0.2f, 0.9f, 0.9f);
        [SerializeField] private Color _paralyticGasColor = new Color(0.95f, 0.85f, 0.2f, 0.9f);
        [SerializeField] private Color _floodColor = new Color(0.15f, 0.45f, 1f, 0.9f);
        [SerializeField] private Color _shockColor = new Color(0.6f, 0.9f, 1f, 0.9f);
        [SerializeField] private Color _teleportColor = new Color(0.9f, 0.4f, 0.9f, 0.9f);
        [SerializeField] private Color _alarmColor = new Color(1f, 0.15f, 0.15f, 0.9f);
        [Tooltip("G-11 (2026-11-05): 식물 3D 모델 — 키 \"plant_<식물>\"(plant_firebloom 등). 있으면 색 칸 대신 칸 위에 세운다 (원점 = 바닥 중앙)")]
        [SerializeField] private ItemVisualCatalog _plantModels;
        [Tooltip("G-12 (2026-11-05): 함정 종류별 문양 타일 (흰색 + 알파, 색은 위 표) — TrapType Fire(1) ~ Alarm(7) 순서. Setup/FX가 채움, 비면 마름모")]
        [SerializeField] private TileBase[] _typeTiles;

        private SimEventBus _bus;
        private readonly System.Collections.Generic.Dictionary<Vector3Int, GameObject> _plants = new System.Collections.Generic.Dictionary<Vector3Int, GameObject>();

        public Tilemap Tilemap => _tilemap;

        /// <summary>테스트 · 에디터 Setup용.</summary>
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
            _bus.Subscribe<TrapChangedEvent>(OnTrapChanged);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Unsubscribe<TrapChangedEvent>(OnTrapChanged);
            _bus = null;
        }

        private void OnDestroy() => Unbind();

        private void OnMapLoaded(MapLoadedEvent e)
        {
            if (_tilemap != null) _tilemap.ClearAllTiles();
            foreach (var plant in _plants.Values)
                if (plant != null) Remove(plant);
            _plants.Clear();
        }

        private void OnTrapChanged(TrapChangedEvent e)
        {
            if (_tilemap == null) return;
            var cell = new Vector3Int(e.Pos.X, e.Pos.Y, 0);
            if (_plants.TryGetValue(cell, out var old))
            {
                if (old != null) Remove(old);
                _plants.Remove(cell);
            }
            if (!e.Visible)
            {
                _tilemap.SetTile(cell, null);
                return;
            }
            if (TryPlantModel(e.Type, out var prefab))
            {
                _tilemap.SetTile(cell, null);
                var world = _tilemap.GetCellCenterWorld(cell);
                var plant = Instantiate(prefab, new Vector3(world.x, 0f, world.z), Quaternion.identity, transform);
                plant.name = "Plant_" + e.Type;
                _plants[cell] = plant;
                return;
            }
            var glyph = TypeTile(e.Type);
            _tilemap.SetTile(cell, glyph != null ? glyph : _tile);
            _tilemap.SetTileFlags(cell, TileFlags.None);
            _tilemap.SetColor(cell, ColorOf(e.Type));
            _tilemap.SetTransformMatrix(cell, glyph != null
                ? Matrix4x4.Scale(new Vector3(0.85f, 0.85f, 1f)) // G-12 종류 문양 (둥근 테 + 기호)
                : Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, 45f), new Vector3(0.45f, 0.45f, 1f))); // 문양이 없으면 칸 가운데 작은 마름모
        }

        /// <summary>G-12 함정 문양 — _typeTiles[종류 − 1] (Fire = 1 ~ Alarm = 7). 식물 · 없는 종류는 null.</summary>
        private TileBase TypeTile(TrapType type)
        {
            int i = (int)type - 1;
            return _typeTiles != null && i >= 0 && i < _typeTiles.Length ? _typeTiles[i] : null;
        }

        /// <summary>에디터 Setup(FX)용: 함정 문양 7개 (TrapType 1 ~ 7 순서).</summary>
        public void ConfigureTypeTiles(TileBase[] tiles)
        {
            _typeTiles = tiles;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private static void Remove(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go); // EditMode 테스트
        }

        /// <summary>식물(3-16b) 종류만 — 키 "plant_" + 종류 이름 소문자. 함정은 색 칸 그대로.</summary>
        private bool TryPlantModel(TrapType type, out GameObject prefab)
        {
            prefab = null;
            switch (type)
            {
                case TrapType.Firebloom: case TrapType.Icecap: case TrapType.Sorrowmoss: case TrapType.Sungrass: case TrapType.Fadeleaf:
                    return _plantModels != null && _plantModels.TryGet("plant_" + type.ToString().ToLowerInvariant(), out prefab);
                default:
                    return false;
            }
        }

        public Color ColorOf(TrapType type)
        {
            switch (type)
            {
                case TrapType.Fire: return _fireColor;
                case TrapType.ToxicGas: return _toxicGasColor;
                case TrapType.ParalyticGas: return _paralyticGasColor;
                case TrapType.Flood: return _floodColor;
                case TrapType.Shock: return _shockColor;
                case TrapType.Teleport: return _teleportColor;
                case TrapType.Alarm: return _alarmColor;
                case TrapType.Firebloom: return new Color(1f, 0.55f, 0.2f, 1f); // 3-16b 식물 (밝은 색)
                case TrapType.Icecap: return new Color(0.7f, 0.95f, 1f, 1f);
                case TrapType.Sorrowmoss: return new Color(0.5f, 0.8f, 0.2f, 1f);
                case TrapType.Sungrass: return new Color(1f, 0.95f, 0.3f, 1f);
                case TrapType.Fadeleaf: return new Color(0.75f, 0.6f, 1f, 1f);
                default: return Color.white;
            }
        }
    }
}
