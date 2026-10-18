using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Units;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 지형 이벤트(MapLoaded · TileChanged) → 바닥 Tilemap + 3D 모듈 (G-1 벽, G-2a 바닥 · 계단 · 기둥). 규칙은 모르고 그리기만 한다.
    /// 3D를 켜면(_walls3D) 칸마다 모듈을 세운다:
    /// - 벽: 걸을 수 있는 칸과 이웃한 벽만 (안쪽 벽은 안 보이므로 생략). 외톨이 벽(상하좌우에 벽 없음)은 기둥 모듈 + 그 아래 바닥 (결정 2026-10-18).
    ///   벽 모듈이 없으면 그레이박스 큐브(높이 _wallHeight).
    /// - 그 밖의 타입: TileVisualSet에 모듈이 있으면 모듈(Tilemap에는 안 그림), 없으면 Tilemap 타일.
    /// 높은 모듈(벽 · 기둥 · 계단)은 바닥 안개 덮개보다 솟으므로 시야를 직접 따른다: 안 본 칸 = 숨김, 본 적 있는 칸 = 어둡게, 보이는 칸 = 그대로.
    /// 낮은 모듈(바닥 판)은 그 위의 안개 Tilemap이 가린다.
    /// </summary>
    public sealed class TilemapPresenter : MonoBehaviour
    {
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TileVisualSet _visuals;
        [Header("3D 모듈 (G-1 · G-2a)")]
        [Tooltip("켜면 벽 · 모듈이 있는 타입을 3D로 그린다")] [SerializeField] private bool _walls3D;
        [Tooltip("벽 모듈 프리팹 (원점 = 바닥 중앙, 1 × 1 m). 비면 TileVisualSet의 벽 모듈 → 그것도 없으면 그레이박스 큐브")] [SerializeField] private GameObject _wallPrefab;
        [Tooltip("그레이박스 큐브 재질")] [SerializeField] private Material _wallMaterial;
        [Tooltip("그레이박스 벽 높이 (m)")] [Min(0.1f)] [SerializeField] private float _wallHeight = 1.1f;
        [Tooltip("본 적 있지만 지금 안 보이는 모듈의 밝기")] [Range(0f, 1f)] [SerializeField] private float _exploredBrightness = 0.35f;

        /// <summary>이보다 높은 모듈은 시야를 직접 따른다 (m). 바닥 판(약 0.06 m)은 안개 덮개가 가린다.</summary>
        private const float TallModuleHeight = 0.15f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private enum Shown : byte { Unknown = 0, Hidden, Dim, Visible }

        private sealed class CellView
        {
            public GameObject Root;
            public Renderer[] Renderers;
            public Color BaseColor;
            public bool Tall;
            public bool IsWall;
            public Shown Shown;
        }

        private SimEventBus _bus;
        private Transform _moduleRoot;
        private readonly Dictionary<GridPos, CellView> _cells = new Dictionary<GridPos, CellView>();
        private TileType[] _types = System.Array.Empty<TileType>();
        private int _width, _height;
        private VisibilityChangedEvent _visibility;
        private bool _revealAll;
        private MaterialPropertyBlock _block;

        public Tilemap Tilemap => _tilemap;

        /// <summary>세운 벽(벽 블록 · 기둥) 칸 수.</summary>
        public int WallCount
        {
            get
            {
                int count = 0;
                foreach (var cell in _cells.Values)
                    if (cell.IsWall) count++;
                return count;
            }
        }

        /// <summary>3D 모듈을 세운 칸 수 (벽 포함).</summary>
        public int ModuleCount => _cells.Count;

        /// <summary>디버그: 시야와 상관없이 모듈을 모두 밝게 그린다 (표시만).</summary>
        public bool RevealAll
        {
            get => _revealAll;
            set
            {
                _revealAll = value;
                ApplyVisibility();
            }
        }

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Tilemap tilemap, TileVisualSet visuals)
        {
            _tilemap = tilemap;
            _visuals = visuals;
        }

        /// <summary>테스트 · 에디터 Setup용: 3D 켜기.</summary>
        public void ConfigureWalls(bool walls3D, Material material = null, float height = 1.1f, GameObject prefab = null)
        {
            _walls3D = walls3D;
            _wallMaterial = material;
            _wallHeight = height;
            _wallPrefab = prefab;
        }

        public bool TryGetWall(GridPos pos, out Renderer[] renderers)
        {
            bool found = _cells.TryGetValue(pos, out var cell) && cell.IsWall;
            renderers = found ? cell.Renderers : null;
            return found;
        }

        /// <summary>그 칸의 3D 모듈 (벽 · 바닥 · 계단 · 기둥).</summary>
        public bool TryGetModule(GridPos pos, out GameObject root)
        {
            bool found = _cells.TryGetValue(pos, out var cell);
            root = found ? cell.Root : null;
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
            ClearModules();
            _visibility = null;
            _width = e.Width;
            _height = e.Height;
            int count = e.Width * e.Height;
            _types = new TileType[count];
            var positions = new Vector3Int[count];
            var tiles = new TileBase[count];
            for (int i = 0; i < count; i++)
            {
                _types[i] = e.Cells[i];
                positions[i] = new Vector3Int(i % e.Width, i / e.Width, 0);
                tiles[i] = TileFor(_types[i]);
            }
            _tilemap.SetTiles(positions, tiles);

            if (!_walls3D) return;
            for (int i = 0; i < count; i++)
                BuildCell(new GridPos(i % e.Width, i / e.Width));
            ApplyVisibility();
        }

        private void OnTileChanged(TileChangedEvent e)
        {
            _tilemap.SetTile(new Vector3Int(e.Pos.X, e.Pos.Y, 0), TileFor(e.Type));
            if (!InBounds(e.Pos)) return;
            _types[e.Pos.Y * _width + e.Pos.X] = e.Type;
            if (!_walls3D) return;
            // 이웃의 모양(가장자리 벽 · 외톨이 벽)도 바뀔 수 있다 → 자기 칸 + 8방향을 다시 세운다
            RebuildCell(e.Pos);
            foreach (var dir in GridMovement.Directions)
                if (InBounds(e.Pos + dir)) RebuildCell(e.Pos + dir);
            ApplyVisibility();
        }

        private void OnVisibilityChanged(VisibilityChangedEvent e)
        {
            _visibility = e;
            ApplyVisibility();
        }

        /// <summary>Tilemap에 그릴 타일: 3D로 그리는 타입(3D 모드의 벽 · 모듈이 있는 타입)은 비운다.</summary>
        private TileBase TileFor(TileType type)
        {
            if (_walls3D && (type == TileType.Wall || (_visuals != null && _visuals.TryGetModule(type, out _, out _)))) return null;
            return _visuals != null ? _visuals.Get(type) : null;
        }

        private bool InBounds(GridPos p) => p.X >= 0 && p.Y >= 0 && p.X < _width && p.Y < _height;

        private TileType TypeAt(GridPos p) => InBounds(p) ? _types[p.Y * _width + p.X] : TileType.Empty;

        private bool TouchesWalkable(GridPos pos)
        {
            foreach (var dir in GridMovement.Directions)
            {
                var n = pos + dir;
                if (InBounds(n) && GridMap.IsWalkableType(TypeAt(n))) return true;
            }
            return false;
        }

        /// <summary>외톨이 벽: 상하좌우 네 칸에 벽이 하나도 없음 (보스 방 기둥 · 홀로 선 벽).</summary>
        private bool IsIsolatedWall(GridPos pos)
        {
            return TypeAt(pos + new GridPos(1, 0)) != TileType.Wall && TypeAt(pos + new GridPos(-1, 0)) != TileType.Wall
                && TypeAt(pos + new GridPos(0, 1)) != TileType.Wall && TypeAt(pos + new GridPos(0, -1)) != TileType.Wall;
        }

        private void RebuildCell(GridPos pos)
        {
            if (_cells.TryGetValue(pos, out var old))
            {
                DestroySafe(old.Root);
                _cells.Remove(pos);
            }
            BuildCell(pos);
        }

        private void BuildCell(GridPos pos)
        {
            var type = TypeAt(pos);
            GameObject floorModule = null;
            bool floorRotate = false;
            if (_visuals != null) _visuals.TryGetModule(TileType.Floor, out floorModule, out floorRotate);

            if (type == TileType.Wall)
            {
                if (!TouchesWalkable(pos)) return;
                var root = CreateCellRoot(pos, "Wall");
                var pillar = _visuals != null ? _visuals.PillarModule : null;
                GameObject wallModule = _wallPrefab;
                if (wallModule == null && _visuals != null) _visuals.TryGetModule(TileType.Wall, out wallModule, out _);
                Color baseColor = Color.white;
                if (pillar != null && IsIsolatedWall(pos))
                {
                    root.name = $"Pillar {pos.X},{pos.Y}";
                    Spawn(pillar, root.transform, 0);
                    if (floorModule != null) Spawn(floorModule, root.transform, floorRotate ? RotationIndex(pos) : 0);
                }
                else if (wallModule != null) Spawn(wallModule, root.transform, 0);
                else
                {
                    CreateGreyboxWall(root.transform);
                    if (_wallMaterial != null && _wallMaterial.HasProperty(BaseColorId)) baseColor = _wallMaterial.GetColor(BaseColorId);
                }
                AddCell(pos, root, baseColor, tall: true, isWall: true);
                return;
            }

            if (_visuals == null || !_visuals.TryGetModule(type, out var module, out var rotate)) return;
            var cellRoot = CreateCellRoot(pos, type.ToString());
            var instance = Spawn(module, cellRoot.transform, rotate ? RotationIndex(pos) : 0);
            AddCell(pos, cellRoot, Color.white, tall: ModuleHeight(instance) > TallModuleHeight, isWall: false);
        }

        private GameObject CreateCellRoot(GridPos pos, string label)
        {
            if (_moduleRoot == null)
            {
                _moduleRoot = new GameObject("Modules3D").transform;
                _moduleRoot.SetParent(transform, false);
                _moduleRoot.rotation = Quaternion.identity; // Grid가 돌아 있어도 모듈은 세워서
                _moduleRoot.localScale = Vector3.one;
            }
            var center = _tilemap.layoutGrid != null
                ? _tilemap.layoutGrid.GetCellCenterWorld(new Vector3Int(pos.X, pos.Y, 0))
                : new Vector3(pos.X + 0.5f, 0f, pos.Y + 0.5f);
            var root = new GameObject($"{label} {pos.X},{pos.Y}");
            root.transform.SetParent(_moduleRoot, false);
            root.transform.SetPositionAndRotation(center, Quaternion.identity);
            return root;
        }

        private static GameObject Spawn(GameObject prefab, Transform parent, int rotationIndex)
        {
            var instance = Instantiate(prefab, parent, false);
            instance.transform.localRotation = Quaternion.Euler(0f, 90f * rotationIndex, 0f) * prefab.transform.localRotation;
            return instance;
        }

        private void CreateGreyboxWall(Transform parent)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var collider = wall.GetComponent<Collider>();
            if (collider != null) DestroySafe(collider);
            wall.name = "Greybox";
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = Vector3.up * (_wallHeight * 0.5f);
            wall.transform.localScale = new Vector3(1f, _wallHeight, 1f);
            if (_wallMaterial != null) wall.GetComponent<Renderer>().sharedMaterial = _wallMaterial;
        }

        private void AddCell(GridPos pos, GameObject root, Color baseColor, bool tall, bool isWall)
        {
            _cells[pos] = new CellView
            {
                Root = root,
                Renderers = root.GetComponentsInChildren<Renderer>(true),
                BaseColor = baseColor,
                Tall = tall,
                IsWall = isWall,
            };
        }

        /// <summary>바닥 무늬 돌리기: 위치로만 정한다 (같은 맵 = 같은 모습, 난수 안 씀).</summary>
        private static int RotationIndex(GridPos pos) => ((pos.X * 73856093) ^ (pos.Y * 19349663)) & 3;

        private static float ModuleHeight(GameObject instance)
        {
            float top = 0f;
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                top = Mathf.Max(top, r.bounds.max.y - instance.transform.position.y);
            return top;
        }

        private void ClearModules()
        {
            if (_moduleRoot != null) DestroySafe(_moduleRoot.gameObject);
            _moduleRoot = null;
            _cells.Clear();
        }

        /// <summary>높은 모듈만 시야를 따른다. 상태가 바뀐 칸만 다시 칠한다.</summary>
        private void ApplyVisibility()
        {
            if (_cells.Count == 0) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            foreach (var pair in _cells)
            {
                var cell = pair.Value;
                var shown = Shown.Visible;
                if (cell.Tall && !_revealAll && _visibility != null)
                {
                    int index = pair.Key.Y * _visibility.Width + pair.Key.X;
                    bool visible = _visibility.IsVisible(pair.Key);
                    bool explored = visible || (index >= 0 && index < _visibility.Explored.Length && _visibility.Explored[index]);
                    shown = visible ? Shown.Visible : explored ? Shown.Dim : Shown.Hidden;
                }
                if (shown == cell.Shown) continue;
                cell.Shown = shown;
                var color = shown == Shown.Dim ? cell.BaseColor * _exploredBrightness : cell.BaseColor;
                color.a = cell.BaseColor.a;
                foreach (var r in cell.Renderers)
                {
                    if (r == null) continue;
                    r.enabled = shown != Shown.Hidden;
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
