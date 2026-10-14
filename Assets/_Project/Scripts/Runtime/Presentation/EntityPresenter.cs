using System.Collections.Generic;
using ProjectM.Core;
using UnityEngine;
using EntityId = ProjectM.Core.EntityId; // Unity 6의 UnityEngine.EntityId와 구분

namespace ProjectM.Presentation
{
    /// <summary>
    /// 엔티티 이벤트(Spawned · Moved · Removed) → 스프라이트. 지금은 칸 중앙으로 즉시 옮긴다 (이동 연출은 비주얼 단계).
    /// </summary>
    public sealed class EntityPresenter : MonoBehaviour
    {
        [SerializeField] private Grid _grid;
        [SerializeField] private EntityVisualCatalog _catalog;
        [SerializeField] private int _sortingOrder = 10;

        private readonly Dictionary<EntityId, SpriteRenderer> _views = new Dictionary<EntityId, SpriteRenderer>();
        private SimEventBus _bus;

        public int ViewCount => _views.Count;

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Grid grid, EntityVisualCatalog catalog)
        {
            _grid = grid;
            _catalog = catalog;
        }

        public bool TryGetView(EntityId id, out SpriteRenderer view) => _views.TryGetValue(id, out view);

        public Vector3 CellCenter(GridPos pos) => _grid.GetCellCenterWorld(new Vector3Int(pos.X, pos.Y, 0));

        public void Bind(SimEventBus bus)
        {
            Unbind();
            _bus = bus;
            _bus.Subscribe<EntitySpawnedEvent>(OnSpawned);
            _bus.Subscribe<EntityMovedEvent>(OnMoved);
            _bus.Subscribe<EntityRemovedEvent>(OnRemoved);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<EntitySpawnedEvent>(OnSpawned);
            _bus.Unsubscribe<EntityMovedEvent>(OnMoved);
            _bus.Unsubscribe<EntityRemovedEvent>(OnRemoved);
            _bus = null;
        }

        private void OnDestroy() => Unbind();

        private void OnSpawned(EntitySpawnedEvent e)
        {
            if (_views.ContainsKey(e.Id))
            {
                Debug.LogWarning($"[EntityPresenter] 이미 있는 엔티티 {e.Id} 생성 이벤트 — 무시");
                return;
            }

            var go = new GameObject($"{e.VisualId} {e.Id}");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = _sortingOrder;

            if (_catalog != null && _catalog.TryGet(e.VisualId, out var visual))
            {
                renderer.sprite = visual.Sprite;
                renderer.color = visual.Tint;
            }
            else
            {
                Debug.LogWarning($"[EntityPresenter] VisualId '{e.VisualId}'가 카탈로그에 없다 ({e.Id})");
            }

            go.transform.position = CellCenter(e.Pos);
            _views.Add(e.Id, renderer);
        }

        private void OnMoved(EntityMovedEvent e)
        {
            if (_views.TryGetValue(e.Id, out var view))
                view.transform.position = CellCenter(e.To);
        }

        private void OnRemoved(EntityRemovedEvent e)
        {
            if (!_views.TryGetValue(e.Id, out var view)) return;
            _views.Remove(e.Id);
            if (Application.isPlaying) Destroy(view.gameObject);
            else DestroyImmediate(view.gameObject);
        }
    }
}
