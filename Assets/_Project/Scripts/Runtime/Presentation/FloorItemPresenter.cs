using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Units;
using UnityEngine;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 바닥 아이템 (3-5 마법서): FloorItemPlacedEvent → 칸 위에 떠서 천천히 도는 책(그레이박스 납작한 상자, 색 = FxCatalog "item.&lt;ID&gt;" 틴트)
    /// + 같은 키의 반짝임 루프 효과. 주우면(ItemPickedUpEvent) 지우고, MapLoaded(새 판 · 층 이동)에 모두 지운다.
    /// 지금 보이는 칸만 그린다 (디버그 밝히기 = 전부). 규칙은 모른다 — 모델이 생기면 FxCatalog 프리팹 대신 모델로 바꾼다.
    /// </summary>
    public sealed class FloorItemPresenter : MonoBehaviour
    {
        [SerializeField] private EntityPresenter _entities;
        [Tooltip("item.<아이템 ID> 키: 프리팹 = 반짝임 루프, 틴트 = 책 색 (메뉴 ProjectM/Setup/FX가 채움)")] [SerializeField] private FxCatalog _catalog;
        [SerializeField] private Vector3 _bookSize = new Vector3(0.34f, 0.07f, 0.44f);
        [Tooltip("바닥에서 뜬 높이 · 위아래 흔들림 (m)")] [SerializeField] private float _hoverHeight = 0.35f;
        [SerializeField] private float _bobAmount = 0.06f;
        [Tooltip("도는 속도 (도/초)")] [SerializeField] private float _spinSpeed = 45f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private sealed class View
        {
            public GridPos Pos;
            public string ItemId;
            public Transform Root;
            public Transform Book;
            public float Phase;
        }

        private readonly List<View> _views = new List<View>();
        private SimEventBus _bus;
        private VisibilityChangedEvent _visibility;
        private bool _revealAll;
        private float _time;
        private MaterialPropertyBlock _block;

        public int Count => _views.Count;

        /// <summary>디버그: 시야와 상관없이 모두 그린다 (표시만).</summary>
        public bool RevealAll
        {
            get => _revealAll;
            set
            {
                _revealAll = value;
                foreach (var v in _views) ApplyVisibility(v);
            }
        }

        /// <summary>테스트 · 에디터 Setup용.</summary>
        public void Configure(EntityPresenter entities, FxCatalog catalog)
        {
            _entities = entities;
            _catalog = catalog;
        }

        public bool Has(GridPos pos, string itemId) => _views.Exists(v => v.Pos == pos && v.ItemId == itemId);

        public void Bind(SimEventBus bus)
        {
            Unbind();
            Clear();
            _bus = bus;
            _bus.Subscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Subscribe<VisibilityChangedEvent>(OnVisibilityChanged);
            _bus.Subscribe<FloorItemPlacedEvent>(OnPlaced);
            _bus.Subscribe<ItemPickedUpEvent>(OnPickedUp);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Unsubscribe<VisibilityChangedEvent>(OnVisibilityChanged);
            _bus.Unsubscribe<FloorItemPlacedEvent>(OnPlaced);
            _bus.Unsubscribe<ItemPickedUpEvent>(OnPickedUp);
            _bus = null;
        }

        private void OnDestroy()
        {
            Unbind();
            Clear();
        }

        private void Update() => Advance(Time.deltaTime);

        private void OnMapLoaded(MapLoadedEvent e)
        {
            Clear();
            _visibility = null;
        }

        private void OnVisibilityChanged(VisibilityChangedEvent e)
        {
            _visibility = e;
            foreach (var v in _views) ApplyVisibility(v);
        }

        private void OnPlaced(FloorItemPlacedEvent e)
        {
            var root = new GameObject($"item {e.ItemId}").transform;
            root.SetParent(transform, false);
            root.position = CellCenter(e.Pos);

            var book = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var collider = book.GetComponent<Collider>();
            if (collider != null) DestroySafe(collider); // 규칙 2: 충돌체 없음
            book.name = "Book";
            book.transform.SetParent(root, false);
            book.transform.localScale = _bookSize;

            var color = new Color(0.8f, 0.75f, 0.6f);
            if (_catalog != null && TryGetTintOnly("item." + e.ItemId, out var fx))
            {
                if (fx.Tint.a > 0f) color = fx.Tint;
                if (fx.Prefab != null)
                {
                    var glow = Instantiate(fx.Prefab, root, false);
                    glow.name = "Glow";
                    glow.transform.localPosition = Vector3.up * fx.Height;
                    glow.transform.localScale = Vector3.one * (fx.Scale > 0f ? fx.Scale : 1f);
                    foreach (var ps in glow.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var main = ps.main;
                        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                        if (fx.Tint.a > 0f) main.startColor = new ParticleSystem.MinMaxGradient(fx.Tint);
                        ps.Play(false);
                    }
                }
            }
            var renderer = book.GetComponent<Renderer>();
            if (_block == null) _block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(_block);

            var view = new View { Pos = e.Pos, ItemId = e.ItemId, Root = root, Book = book.transform, Phase = (e.Pos.X * 0.37f + e.Pos.Y * 0.61f) % 1f * Mathf.PI * 2f };
            _views.Add(view);
            Pose(view);
            ApplyVisibility(view);
        }

        /// <summary>프리팹 없이 색만 지정한 줄도 쓴다 (FxCatalog.TryGet은 프리팹 없는 줄을 건너뜀).</summary>
        private bool TryGetTintOnly(string key, out FxCatalog.Entry entry)
        {
            foreach (var e in _catalog.Entries)
            {
                if (e.Key != key) continue;
                entry = e;
                return true;
            }
            entry = default;
            return false;
        }

        private void OnPickedUp(ItemPickedUpEvent e)
        {
            int index = _views.FindIndex(v => v.Pos == e.Pos && v.ItemId == e.ItemId);
            if (index < 0) return;
            if (_views[index].Root != null) DestroySafe(_views[index].Root.gameObject);
            _views.RemoveAt(index);
        }

        /// <summary>떠 있는 책을 dt초 진행 (Update가 부름 · 테스트는 직접). 표시만.</summary>
        public void Advance(float dt)
        {
            _time += Mathf.Max(0f, dt);
            foreach (var v in _views) Pose(v);
        }

        private void Pose(View v)
        {
            if (v.Book == null) return;
            v.Book.localPosition = Vector3.up * (_hoverHeight + _bobAmount * Mathf.Sin(_time * 2.2f + v.Phase));
            v.Book.localRotation = Quaternion.Euler(0f, _time * _spinSpeed + v.Phase * Mathf.Rad2Deg, 0f);
        }

        private void ApplyVisibility(View v)
        {
            if (v.Root == null) return;
            bool visible = _revealAll || _visibility == null || _visibility.IsVisible(v.Pos);
            if (v.Root.gameObject.activeSelf != visible) v.Root.gameObject.SetActive(visible);
        }

        private Vector3 CellCenter(GridPos pos) => _entities != null ? _entities.CellCenter(pos) : new Vector3(pos.X + 0.5f, 0f, pos.Y + 0.5f);

        private void Clear()
        {
            foreach (var v in _views)
                if (v.Root != null) DestroySafe(v.Root.gameObject);
            _views.Clear();
        }

        private static void DestroySafe(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
