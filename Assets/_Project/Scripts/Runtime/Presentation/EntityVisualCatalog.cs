using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectM.Presentation
{
    /// <summary>
    /// VisualId → 엔티티 모습. 시뮬레이션은 "player" 같은 ID만 알고, 실제 스프라이트는 여기서 고른다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Visuals/Entity Visual Catalog", fileName = "EntityVisualCatalog")]
    public sealed class EntityVisualCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Id;
            public Sprite Sprite;
            public Color Tint;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public bool TryGet(string id, out Entry entry)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id == id)
                {
                    entry = _entries[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }

        /// <summary>테스트 · 에디터 Setup용.</summary>
        public void Set(string id, Sprite sprite, Color tint)
        {
            var entry = new Entry { Id = id, Sprite = sprite, Tint = tint };
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id == id)
                {
                    _entries[i] = entry;
                    return;
                }
            }
            _entries.Add(entry);
        }
    }
}
