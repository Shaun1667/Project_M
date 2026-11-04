using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using UnityEngine;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 구역 분위기 (3-17c, 결정 2026-11-04 — 1구역 숲 속 동굴 · 2구역 사막 피라미드): 층이 바뀌면 해 색을 구역 색으로 (그레이박스 — 구역 타일 · 모델이 생기면 여기서 바꾼다).
    /// 규칙은 갖지 않는다 — FloorChangedEvent만 본다.
    /// </summary>
    public sealed class ZoneThemePresenter : MonoBehaviour
    {
        [SerializeField] private Light _sun;
        [SerializeField] private ZoneCatalogAsset _zones;
        private Color _defaultColor = Color.white;
        private bool _hasDefault;
        private SimEventBus _bus;

        public void Configure(Light sun, ZoneCatalogAsset zones)
        {
            _sun = sun;
            _zones = zones;
        }

        public void Bind(SimEventBus bus, int floor)
        {
            Unbind();
            _bus = bus;
            _bus.Subscribe<FloorChangedEvent>(OnFloorChanged);
            Apply(floor);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<FloorChangedEvent>(OnFloorChanged);
            _bus = null;
        }

        private void OnDestroy() => Unbind();

        private void OnFloorChanged(FloorChangedEvent e) => Apply(e.To);

        public void Apply(int floor)
        {
            if (_sun == null) return;
            if (!_hasDefault) { _defaultColor = _sun.color; _hasDefault = true; }
            _sun.color = _zones != null && _zones.TryGetLightColor(floor, out var c) ? new Color(c.r, c.g, c.b, 1f) : _defaultColor;
        }
    }
}
