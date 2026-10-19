using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 아이템 ID → 바닥에 놓인 모습 (3-7a 무기). 시뮬레이션은 아이템 ID만 알고, 모델은 여기서 고른다 (CLAUDE.md 아트 규칙 — ID로만 참조).
    /// 없는 아이템은 FloorItemPresenter가 그레이박스 상자(마법서)로 그린다. 표는 메뉴 ProjectM/Setup/Varco Models가 채운다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Visuals/Item Visual Catalog", fileName = "ItemVisualCatalog")]
    public sealed class ItemVisualCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string ItemId;
            [Tooltip("모델 프리팹 (세워 둔 모습, 원점 = 가운데). 바닥에서는 눕혀서 떠 돈다")] public GameObject Prefab;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public bool TryGet(string itemId, out GameObject prefab)
        {
            foreach (var e in _entries)
            {
                if (e.ItemId != itemId || e.Prefab == null) continue;
                prefab = e.Prefab;
                return true;
            }
            prefab = null;
            return false;
        }

        /// <summary>테스트 · 에디터 Setup용: 있으면 바꾸고 없으면 추가.</summary>
        public void Set(string itemId, GameObject prefab)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].ItemId != itemId) continue;
                _entries[i] = new Entry { ItemId = itemId, Prefab = prefab };
                return;
            }
            _entries.Add(new Entry { ItemId = itemId, Prefab = prefab });
        }
    }
}
