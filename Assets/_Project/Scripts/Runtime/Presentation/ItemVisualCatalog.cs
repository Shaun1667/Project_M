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
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

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

        /// <summary>픽셀 그림 (P-3 ⓒ). 겉모습 키(물약 · 주문서)나 아이템 ID.</summary>
        public bool TryGetIcon(string id, out Sprite icon)
        {
            foreach (var e in _entries)
            {
                if (e.ItemId != id || e.Icon == null) continue;
                icon = e.Icon;
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
    }
}
