using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectM.Presentation
{
    /// <summary>
    /// VisualId → 엔티티 모습. 시뮬레이션은 "player" 같은 ID만 알고, 실제 모습은 여기서 고른다.
    /// G-1 (3D, 결정 2026-10-18): Prefab(Varco 모델, 원점 = 발밑 중앙)이 있으면 그것, 없으면 그레이박스 캡슐(Height · Tint).
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Visuals/Entity Visual Catalog", fileName = "EntityVisualCatalog")]
    public sealed class EntityVisualCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Id;
            [Tooltip("2D 때의 스프라이트 (G-1부터 안 씀 — 아이콘 등에 쓸 수 있어 남겨 둠)")] public Sprite Sprite;
            [Tooltip("그레이박스 색 · 프리팹에는 곱해지는 색")] public Color Tint;
            [Tooltip("3D 모델 프리팹 (원점 = 발밑 중앙, 앞 = +Z). 비면 그레이박스 캡슐")] public GameObject Prefab;
            [Tooltip("그레이박스 캡슐 키 (m). 0이면 0.9")] [Min(0)] public float Height;
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

        /// <summary>테스트 · 에디터 Setup용. 이미 있으면 스프라이트 · 색만 바꾸고 프리팹 · 키는 그대로 둔다 (사용자가 넣은 모델을 지우지 않음).</summary>
        public void Set(string id, Sprite sprite, Color tint, float height = 0f)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id != id) continue;
                var e = _entries[i];
                e.Sprite = sprite;
                e.Tint = tint;
                if (height > 0f) e.Height = height;
                _entries[i] = e;
                return;
            }
            _entries.Add(new Entry { Id = id, Sprite = sprite, Tint = tint, Height = height });
        }

        /// <summary>모델 연결 (G-2에서 Varco 프리팹을 넣을 때 · 테스트).</summary>
        public void SetPrefab(string id, GameObject prefab)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id != id) continue;
                var e = _entries[i];
                e.Prefab = prefab;
                _entries[i] = e;
                return;
            }
            _entries.Add(new Entry { Id = id, Tint = Color.white, Prefab = prefab });
        }
    }
}
