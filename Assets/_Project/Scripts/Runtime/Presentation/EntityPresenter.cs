using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;
using ProjectM.Units;
using UnityEngine;
using EntityId = ProjectM.Core.EntityId; // Unity 6의 UnityEngine.EntityId와 구분

namespace ProjectM.Presentation
{
    /// <summary>엔티티 하나의 3D 모습: 칸 중앙 발밑에 놓이는 루트 + 그 아래 렌더러들 (G-1).</summary>
    public sealed class EntityView
    {
        public Transform Root { get; }
        public Renderer[] Renderers { get; }
        /// <summary>지금 그려지는지 (시야 · 디버그 밝히기).</summary>
        public bool Visible { get; internal set; } = true;

        public EntityView(Transform root, Renderer[] renderers)
        {
            Root = root;
            Renderers = renderers;
        }
    }

    /// <summary>
    /// 엔티티 이벤트(Spawned · Moved · Removed) → 3D 모습 (G-1, 결정 2026-10-18). 지금은 칸 중앙으로 즉시 옮기고 이동 방향을 바라본다 (이동 보간 · 애니메이션은 G-2).
    /// VisualId의 프리팹이 있으면 그것, 없으면 그레이박스 캡슐(카탈로그 색 · 키). 충돌체는 붙이지 않는다 (규칙 2).
    /// 시야(VisibilityChangedEvent)를 받은 뒤로는 지금 보이는 칸에 있는 엔티티만 그린다 (1-7). 시야 이벤트가 없으면 전부 그린다.
    /// 상태 이상은 몸 색 틴트 하나 (우선순위 표) — 3D 이펙트는 G-3.
    /// </summary>
    public sealed class EntityPresenter : MonoBehaviour
    {
        [SerializeField] private Grid _grid;
        [SerializeField] private EntityVisualCatalog _catalog;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private const float DefaultHeight = 0.9f;
        private const float CapsuleWidth = 0.6f;

        private readonly Dictionary<EntityId, EntityView> _views = new Dictionary<EntityId, EntityView>();
        private readonly Dictionary<EntityId, GridPos> _positions = new Dictionary<EntityId, GridPos>();
        private VisibilityChangedEvent _visibility;
        private SimEventBus _bus;
        private bool _revealAll;
        private readonly Dictionary<EntityId, Color> _baseTints = new Dictionary<EntityId, Color>();
        private readonly Dictionary<EntityId, List<StatusType>> _statuses = new Dictionary<EntityId, List<StatusType>>();
        private MaterialPropertyBlock _block;

        public int ViewCount => _views.Count;

        /// <summary>디버그: 시야와 상관없이 모든 엔티티를 그린다. 표시만 바꾸며 게임 상태는 건드리지 않는다.</summary>
        public bool RevealAll
        {
            get => _revealAll;
            set
            {
                _revealAll = value;
                foreach (var pair in _views)
                    ApplyVisibility(pair.Key, pair.Value);
            }
        }

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Grid grid, EntityVisualCatalog catalog)
        {
            _grid = grid;
            _catalog = catalog;
        }

        public bool TryGetView(EntityId id, out EntityView view) => _views.TryGetValue(id, out view);

        /// <summary>칸 중앙의 월드 좌표 (바닥 높이). Grid가 X축 90° 돌아 있으면 바닥 평면(XZ) 위 (G-1).</summary>
        public Vector3 CellCenter(GridPos pos) => _grid.GetCellCenterWorld(new Vector3Int(pos.X, pos.Y, 0));

        public void Bind(SimEventBus bus)
        {
            Unbind();
            ClearViews();
            _bus = bus;
            _bus.Subscribe<EntitySpawnedEvent>(OnSpawned);
            _bus.Subscribe<EntityMovedEvent>(OnMoved);
            _bus.Subscribe<EntityRemovedEvent>(OnRemoved);
            _bus.Subscribe<VisibilityChangedEvent>(OnVisibilityChanged);
            _bus.Subscribe<StatusChangedEvent>(OnStatusChanged);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<EntitySpawnedEvent>(OnSpawned);
            _bus.Unsubscribe<EntityMovedEvent>(OnMoved);
            _bus.Unsubscribe<EntityRemovedEvent>(OnRemoved);
            _bus.Unsubscribe<VisibilityChangedEvent>(OnVisibilityChanged);
            _bus.Unsubscribe<StatusChangedEvent>(OnStatusChanged);
            _bus = null;
        }

        private void OnDestroy() => Unbind();

        /// <summary>남은 모습을 모두 지운다 (새 판 · 새 버스에 연결할 때).</summary>
        private void ClearViews()
        {
            foreach (var view in _views.Values)
                if (view != null && view.Root != null) DestroySafe(view.Root.gameObject);
            _views.Clear();
            _positions.Clear();
            _baseTints.Clear();
            _statuses.Clear();
            _visibility = null;
        }

        private void OnSpawned(EntitySpawnedEvent e)
        {
            if (_views.ContainsKey(e.Id))
            {
                Debug.LogWarning($"[EntityPresenter] 이미 있는 엔티티 {e.Id} 생성 이벤트 — 무시");
                return;
            }

            var root = new GameObject($"{e.VisualId} {e.Id}").transform;
            root.SetParent(transform, false);
            var tint = Color.white;
            if (_catalog != null && _catalog.TryGet(e.VisualId, out var visual))
            {
                tint = visual.Tint;
                if (visual.Prefab != null) Instantiate(visual.Prefab, root, false);
                else CreateCapsule(root, visual.Height > 0f ? visual.Height : DefaultHeight);
            }
            else
            {
                Debug.LogWarning($"[EntityPresenter] VisualId '{e.VisualId}'가 카탈로그에 없다 ({e.Id})");
                CreateCapsule(root, DefaultHeight);
            }

            root.position = CellCenter(e.Pos);
            var view = new EntityView(root, root.GetComponentsInChildren<Renderer>(true));
            _views.Add(e.Id, view);
            _positions[e.Id] = e.Pos;
            _baseTints[e.Id] = tint;
            ApplyTint(e.Id, view);
            ApplyVisibility(e.Id, view);
        }

        /// <summary>그레이박스: 발밑이 바닥에 닿는 캡슐 (충돌체 제거).</summary>
        private static void CreateCapsule(Transform root, float height)
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var collider = capsule.GetComponent<Collider>();
            if (collider != null) DestroySafe(collider);
            capsule.name = "Greybox";
            capsule.transform.SetParent(root, false);
            capsule.transform.localScale = new Vector3(CapsuleWidth, height * 0.5f, CapsuleWidth); // 기본 캡슐 높이 2
            capsule.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        }

        private void OnMoved(EntityMovedEvent e)
        {
            _positions[e.Id] = e.To;
            if (!_views.TryGetValue(e.Id, out var view)) return;
            var to = CellCenter(e.To);
            var dir = to - CellCenter(e.From);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) view.Root.rotation = Quaternion.LookRotation(dir, Vector3.up); // 이동 방향을 바라봄
            view.Root.position = to;
            ApplyVisibility(e.Id, view);
        }

        private void OnRemoved(EntityRemovedEvent e)
        {
            if (!_views.TryGetValue(e.Id, out var view)) return;
            _views.Remove(e.Id);
            _positions.Remove(e.Id);
            _baseTints.Remove(e.Id);
            _statuses.Remove(e.Id);
            if (view.Root != null) DestroySafe(view.Root.gameObject);
        }

        private void OnVisibilityChanged(VisibilityChangedEvent e)
        {
            _visibility = e;
            foreach (var pair in _views)
                ApplyVisibility(pair.Key, pair.Value);
        }

        private void ApplyVisibility(EntityId id, EntityView view)
        {
            if (view == null) return;
            view.Visible = _revealAll || _visibility == null || !_positions.TryGetValue(id, out var pos) || _visibility.IsVisible(pos);
            foreach (var r in view.Renderers)
                if (r != null) r.enabled = view.Visible;
        }

        private static void DestroySafe(Object obj)
        {
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        // ── 상태 이상 틴트 (2-3 그레이박스 표시 — 3D 이펙트는 G-3) ──

        /// <summary>틴트 우선순위: 빙결(3-3a) > 마비 > 화상 > 중독 > 둔화 > 젖음(3-2). 여러 개면 앞의 것 하나만.</summary>
        private static readonly (StatusType type, Color color)[] StatusTints =
        {
            (StatusType.Frozen, new Color(0.85f, 0.97f, 1f)),
            (StatusType.Paralyze, new Color(1f, 0.95f, 0.25f)),
            (StatusType.Burn, new Color(1f, 0.5f, 0.1f)),
            (StatusType.Poison, new Color(0.7f, 0.3f, 1f)),
            (StatusType.Slow, new Color(0.35f, 0.85f, 1f)),
            (StatusType.Wet, new Color(0.25f, 0.45f, 1f)),
        };

        private const float StatusTintStrength = 0.65f;

        public static Color StatusTint(StatusType type)
        {
            foreach (var (t, c) in StatusTints)
                if (t == type) return c;
            return Color.white;
        }

        /// <summary>지금 몸 색 (기본 색 + 상태 틴트) — 테스트 · 디버그용.</summary>
        public Color CurrentTint(EntityId id)
        {
            var baseTint = _baseTints.TryGetValue(id, out var b) ? b : Color.white;
            if (!_statuses.TryGetValue(id, out var list)) return baseTint;
            foreach (var (type, color) in StatusTints)
                if (list.Contains(type)) return Color.Lerp(baseTint, color, StatusTintStrength);
            return baseTint;
        }

        private void OnStatusChanged(StatusChangedEvent e)
        {
            if (!_statuses.TryGetValue(e.Id, out var list))
            {
                list = new List<StatusType>();
                _statuses[e.Id] = list;
            }
            if (e.Remaining > 0) { if (!list.Contains(e.Type)) list.Add(e.Type); }
            else list.Remove(e.Type);

            if (_views.TryGetValue(e.Id, out var view)) ApplyTint(e.Id, view);
        }

        /// <summary>재질을 복제하지 않고 MaterialPropertyBlock으로 색만 바꾼다 (URP Lit = _BaseColor, 그 밖 = _Color).</summary>
        private void ApplyTint(EntityId id, EntityView view)
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            var color = CurrentTint(id);
            foreach (var r in view.Renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, color);
                _block.SetColor(ColorId, color);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
