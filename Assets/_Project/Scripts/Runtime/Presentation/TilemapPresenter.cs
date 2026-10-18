using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Units;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 지형 이벤트(MapLoaded · TileChanged) → 바닥 Tilemap (+ G-1: 3D 벽). 규칙은 모르고 그리기만 한다.
    /// 3D 벽을 켜면(_walls3D) 벽 칸은 Tilemap에 그리지 않고 칸마다 블록을 세운다 — 걸을 수 있는 칸과 이웃한 벽만 (안쪽 벽은 안 보이므로 생략).
    /// 블록은 프리팹(_wallPrefab, 원점 = 바닥 중앙, 1 × 1 m)이 있으면 그것, 없으면 그레이박스 큐브(높이 _wallHeight).
    /// 벽은 바닥 안개 덮개보다 위로 솟으므로 시야를 직접 따른다: 안 본 칸 = 숨김, 본 적 있는 칸 = 어둡게, 보이는 칸 = 그대로.
    /// </summary>
    public sealed class TilemapPresenter : MonoBehaviour
    {
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TileVisualSet _visuals;
        [Header("3D 벽 (G-1)")]
        [SerializeField] private bool _walls3D;
        [Tooltip("벽 모듈 프리팹 (원점 = 바닥 중앙, 1 × 1 m). 비면 그레이박스 큐브")] [SerializeField] private GameObject _wallPrefab;
        [Tooltip("그레이박스 큐브 재질")] [SerializeField] private Material _wallMaterial;
        [Tooltip("그레이박스 벽 높이 (m)")] [Min(0.1f)] [SerializeField] private float _wallHeight = 1.1f;
        [Tooltip("본 적 있지만 지금 안 보이는 벽의 밝기")] [Range(0f, 1f)] [SerializeField] private float _exploredBrightness = 0.35f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private SimEventBus _bus;
        private Transform _wallRoot;
        private readonly Dictionary<GridPos, (GameObject obj, Renderer[] renderers)> _walls = new Dictionary<GridPos, (GameObject, Renderer[])>();
        private int _width, _height;
        private VisibilityChangedEvent _visibility;
        private bool _revealAll;
        private MaterialPropertyBlock _block;

        public Tilemap Tilemap => _tilemap;
        public int WallCount => _walls.Count;

        /// <summary>디버그: 시야와 상관없이 벽을 모두 밝게 그린다 (표시만).</summary>
        public bool RevealAll
        {
            get => _revealAll;
            set
            {
                _revealAll = value;
                ApplyWallVisibility();
            }
        }

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Tilemap tilemap, TileVisualSet visuals)
        {
            _tilemap = tilemap;
            _visuals = visuals;
        }

        /// <summary>테스트 · 에디터 Setup용: 3D 벽 켜기.</summary>
        public void ConfigureWalls(bool walls3D, Material material = null, float height = 1.1f, GameObject prefab = null)
        {
            _walls3D = walls3D;
            _wallMaterial = material;
            _wallHeight = height;
            _wallPrefab = prefab;
        }

        public bool TryGetWall(GridPos pos, out Renderer[] renderers)
        {
            bool found = _walls.TryGetValue(pos, out var wall);
            renderers = found ? wall.renderers : null;
            return found;
        }

        public void Bind(SimEventBus bus)
        {
            Unbind();
            _bus = bus;
            _bus.Subscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Subscribe<TileChangedEvent>(OnTileChanged);
            _bus.Subscribe<VisibilityChangedEvent>(OnVisibilityChanged);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Unsubscribe<TileChangedEvent>(OnTileChanged);
            _bus.Unsubscribe<VisibilityChangedEvent>(OnVisibilityChanged);
            _bus = null;
        }

        private void OnDestroy() => Unbind();

        private void OnMapLoaded(MapLoadedEvent e)
        {
            _tilemap.ClearAllTiles();
            ClearWalls();
            _visibility = null;
            _width = e.Width;
            _height = e.Height;
            int count = e.Width * e.Height;
            var positions = new Vector3Int[count];
            var tiles = new TileBase[count];
            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3Int(i % e.Width, i / e.Width, 0);
                bool wall3D = _walls3D && e.Cells[i] == TileType.Wall;
                tiles[i] = wall3D ? null : _visuals.Get(e.Cells[i]);
            }
            _tilemap.SetTiles(positions, tiles);

            if (!_walls3D) return;
            for (int i = 0; i < count; i++)
            {
                var pos = new GridPos(i % e.Width, i / e.Width);
                if (e.Cells[i] == TileType.Wall && TouchesWalkable(e.Cells, pos)) CreateWall(pos);
            }
            ApplyWallVisibility();
        }

        private void OnTileChanged(TileChangedEvent e)
        {
            bool wall3D = _walls3D && e.Type == TileType.Wall;
            _tilemap.SetTile(new Vector3Int(e.Pos.X, e.Pos.Y, 0), wall3D ? null : _visuals.Get(e.Type));
            if (!_walls3D) return;
            if (_walls.TryGetValue(e.Pos, out var old))
            {
                DestroySafe(old.obj);
                _walls.Remove(e.Pos);
            }
            if (wall3D) CreateWall(e.Pos);
            ApplyWallVisibility();
        }

        private void OnVisibilityChanged(VisibilityChangedEvent e)
        {
            _visibility = e;
            ApplyWallVisibility();
        }

        private bool TouchesWalkable(IReadOnlyList<TileType> cells, GridPos pos)
        {
            foreach (var dir in GridMovement.Directions)
            {
                var n = pos + dir;
                if (n.X < 0 || n.Y < 0 || n.X >= _width || n.Y >= _height) continue;
                if (GridMap.IsWalkableType(cells[n.Y * _width + n.X])) return true;
            }
            return false;
        }

        private void CreateWall(GridPos pos)
        {
            if (_wallRoot == null)
            {
                _wallRoot = new GameObject("Walls3D").transform;
                _wallRoot.SetParent(transform, false);
                _wallRoot.rotation = Quaternion.identity; // Grid가 돌아 있어도 벽은 세워서
                _wallRoot.localScale = Vector3.one;
            }
            var center = _tilemap.layoutGrid != null
                ? _tilemap.layoutGrid.GetCellCenterWorld(new Vector3Int(pos.X, pos.Y, 0))
                : new Vector3(pos.X + 0.5f, 0f, pos.Y + 0.5f);
            GameObject wall;
            if (_wallPrefab != null)
            {
                wall = Instantiate(_wallPrefab, _wallRoot);
                wall.transform.position = center;
                wall.transform.rotation = Quaternion.identity;
            }
            else
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var collider = wall.GetComponent<Collider>();
                if (collider != null) DestroySafe(collider);
                wall.transform.SetParent(_wallRoot, false);
                wall.transform.position = center + Vector3.up * (_wallHeight * 0.5f);
                wall.transform.rotation = Quaternion.identity;
                wall.transform.localScale = new Vector3(1f, _wallHeight, 1f);
                if (_wallMaterial != null) wall.GetComponent<Renderer>().sharedMaterial = _wallMaterial;
            }
            wall.name = $"Wall {pos.X},{pos.Y}";
            _walls[pos] = (wall, wall.GetComponentsInChildren<Renderer>(true));
        }

        private void ClearWalls()
        {
            if (_wallRoot != null) DestroySafe(_wallRoot.gameObject);
            _wallRoot = null;
            _walls.Clear();
        }

        private void ApplyWallVisibility()
        {
            if (_walls.Count == 0) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            var baseColor = _wallMaterial != null && _wallMaterial.HasProperty(BaseColorId) ? _wallMaterial.GetColor(BaseColorId) : Color.white;
            foreach (var pair in _walls)
            {
                bool visible = true, explored = true;
                if (!_revealAll && _visibility != null)
                {
                    visible = _visibility.IsVisible(pair.Key);
                    int index = pair.Key.Y * _visibility.Width + pair.Key.X;
                    explored = visible || (index >= 0 && index < _visibility.Explored.Length && _visibility.Explored[index]);
                }
                var color = visible ? baseColor : baseColor * _exploredBrightness;
                color.a = baseColor.a;
                foreach (var r in pair.Value.renderers)
                {
                    if (r == null) continue;
                    r.enabled = explored;
                    r.GetPropertyBlock(_block);
                    _block.SetColor(BaseColorId, color);
                    _block.SetColor(ColorId, color);
                    r.SetPropertyBlock(_block);
                }
            }
        }

        private static void DestroySafe(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
