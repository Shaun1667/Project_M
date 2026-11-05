using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 아이템 ID → 바닥에 놓인 모습 (3-7a 무기). 시뮬레이션은 아이템 ID만 알고, 모델은 여기서 고른다 (CLAUDE.md 아트 규칙 — ID로만 참조).
    /// 없는 아이템은 FloorItemPresenter가 그레이박스 상자(마법서)로 그린다. 표는 메뉴 ProjectM/Setup/Varco Models가 채운다.
    /// P-3 ⓒ (2D 픽셀, 2026-10-26): Icon = 픽셀 그림(바닥 · 인벤토리 · 빠른 칸 공용). 물약 · 주문서는 겉모습 키(potion_crimson · scroll_tyr)로 찾는다 — 정체가 드러나지 않게.
    /// Icon 표는 메뉴 ProjectM/Setup/Main Scene이 Art/Pixel/Items에서 채운다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Visuals/Item Visual Catalog", fileName = "ItemVisualCatalog")]
    public sealed class ItemVisualCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string ItemId;
            [Tooltip("모델 프리팹 (세워 둔 모습, 원점 = 가운데). 바닥에서는 눕혀서 떠 돈다")] public GameObject Prefab;
            [Tooltip("켜면 눕히지 않고 프리팹 모습 그대로 (3-9a 물약 · 주문서 — 원점 = 바닥 중앙). 끄면 무기처럼 눕힘")] public bool Upright;
            [Tooltip("2D 픽셀 그림 (64 PPU · 가운데 피벗) — 바닥 · 인벤토리 · 빠른 칸")] public Sprite Icon;
            [Tooltip("G-17 (2026-11-05): 3D 모델을 렌더한 아이콘 (인벤토리 · 빠른 칸) — 메뉴 Setup/Item Icons가 채움")] public Sprite Icon3D;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();
        [Tooltip("G-17: 켜면 아이콘으로 3D 렌더 그림(Icon3D)을 먼저 쓴다 — 3D 그래픽일 때 Setup/Item Icons가 켬, 없으면 픽셀 그림")] [SerializeField] private bool _use3DIcons;

        public bool Use3DIcons => _use3DIcons;

        public bool TryGet(string itemId, out GameObject prefab) => TryGet(itemId, out prefab, out _);

        public bool TryGet(string itemId, out GameObject prefab, out bool upright)
        {
            foreach (var e in _entries)
            {
                if (e.ItemId != itemId || e.Prefab == null) continue;
                prefab = e.Prefab;
                upright = e.Upright;
                return true;
            }
            prefab = null;
            upright = false;
            return false;
        }

        /// <summary>아이콘 — 겉모습 키(물약 · 주문서)나 아이템 ID. G-17: 3D 아이콘을 켰으면 렌더 그림 먼저, 없으면 픽셀 그림 (P-3 ⓒ).</summary>
        public bool TryGetIcon(string id, out Sprite icon)
        {
            foreach (var e in _entries)
            {
                var sprite = _use3DIcons && e.Icon3D != null ? e.Icon3D : e.Icon;
                if (e.ItemId != id || sprite == null) continue;
                icon = sprite;
                return true;
            }
            icon = null;
            return false;
        }

        /// <summary>테스트 · 에디터 Setup용: 있으면 바꾸고 없으면 추가 (픽셀 그림은 그대로 둔다).</summary>
        public void Set(string itemId, GameObject prefab, bool upright = false)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].ItemId != itemId) continue;
                var e = _entries[i];
                e.Prefab = prefab;
                e.Upright = upright;
                _entries[i] = e;
                return;
            }
            _entries.Add(new Entry { ItemId = itemId, Prefab = prefab, Upright = upright });
        }

        /// <summary>픽셀 그림 연결 (P-3 ⓒ Setup · 테스트): 있으면 그림만 바꾸고 없으면 추가.</summary>
        public void SetIcon(string id, Sprite icon)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].ItemId != id) continue;
                var e = _entries[i];
                e.Icon = icon;
                _entries[i] = e;
                return;
            }
            _entries.Add(new Entry { ItemId = id, Icon = icon });
        }

        /// <summary>G-17 에디터 Setup · 테스트용: 3D 렌더 아이콘 연결 (있으면 그림만 바꾸고 없으면 추가).</summary>
        public void SetIcon3D(string id, Sprite icon)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].ItemId != id) continue;
                var e = _entries[i];
                e.Icon3D = icon;
                _entries[i] = e;
                return;
            }
            _entries.Add(new Entry { ItemId = id, Icon3D = icon });
        }

        /// <summary>G-17 에디터 Setup · 테스트용: 3D 아이콘을 먼저 쓸지.</summary>
        public void SetUse3DIcons(bool on) => _use3DIcons = on;

        /// <summary>에디터 Setup용 (G-17): 모델이 있는 줄의 키 · 프리팹 · 세움 여부.</summary>
        public IEnumerable<(string Id, GameObject Prefab, bool Upright)> Models()
        {
            foreach (var e in _entries)
                if (e.Prefab != null) yield return (e.ItemId, e.Prefab, e.Upright);
        }
    }
}
