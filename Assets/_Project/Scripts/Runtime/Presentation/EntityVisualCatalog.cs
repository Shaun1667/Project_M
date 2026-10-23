using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectM.Presentation
{
    /// <summary>
    /// VisualId → 엔티티 모습. 시뮬레이션은 "player" 같은 ID만 알고, 실제 모습은 여기서 고른다.
    /// G-1 (3D, 결정 2026-10-18): Prefab(Varco 모델, 원점 = 발밑 중앙)이 있으면 그것, 없으면 그레이박스 캡슐(Height · Tint).
    /// G-2a: Variants(모습 변형)가 있으면 엔티티 ID로 하나를 고른다 — 능력은 같고 표시만 다름 (근접 적 = 토끼 · 쥐 · 슬라임).
    /// P-3 ⓑ (2D 픽셀, 2026-10-23): EntityPresenter가 2D 모드면 픽셀 스프라이트(대기 · 걷기 앞 · 뒤 · 옆)를 쓴다 — 없으면 Sprite(그레이박스)를 Tint로.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Visuals/Entity Visual Catalog", fileName = "EntityVisualCatalog")]
    public sealed class EntityVisualCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Id;
            [Tooltip("2D 때의 스프라이트 (G-1부터 안 씀 — 아이콘 등에 쓸 수 있어 남겨 둠)")] public Sprite Sprite;
            [Tooltip("그레이박스 캡슐 색 (프리팹은 텍스처 그대로 — G-2a)")] public Color Tint;
            [Tooltip("3D 모델 프리팹 (원점 = 발밑 중앙, 앞 = +Z). 비면 그레이박스 캡슐")] public GameObject Prefab;
            [Tooltip("그레이박스 캡슐 키 (m). 0이면 0.9")] [Min(0)] public float Height;
            [Tooltip("모습 변형 (G-2a, 결정 2026-10-18 — 표시만). 비어 있지 않으면 Prefab 대신 엔티티 ID로 하나를 고른다")] public GameObject[] Variants;
            [Tooltip("2D 대기 모습 (앞을 봄, 64 PPU · 아래 가운데 피벗). 비면 앞 걷기 첫 장")] public Sprite PixelIdle;
            [Tooltip("2D 걷기 — 앞(아래로 감)")] public Sprite[] WalkFront;
            [Tooltip("2D 걷기 — 뒤(위로 감)")] public Sprite[] WalkBack;
            [Tooltip("2D 걷기 — 옆(오른쪽으로 감, 왼쪽은 뒤집기)")] public Sprite[] WalkSide;

            /// <summary>2D 픽셀 그림이 하나라도 있는지.</summary>
            public bool HasPixel => PixelIdle != null || (WalkFront != null && WalkFront.Length > 0);
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        /// <summary>
        /// 이 엔티티가 쓸 프리팹: 변형이 있으면 ID로 고정해 하나(같은 엔티티 = 늘 같은 모습, 난수 안 씀), 없으면 Prefab. 둘 다 없으면 null (캡슐).
        /// </summary>
        public static GameObject PickPrefab(Entry entry, int entityId)
        {
            var variants = entry.Variants;
            if (variants != null && variants.Length > 0)
            {
                var picked = variants[(entityId & int.MaxValue) % variants.Length];
                if (picked != null) return picked;
            }
            return entry.Prefab;
        }

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

        /// <summary>2D 픽셀 그림 연결 (P-3 ⓑ Setup · 테스트).</summary>
        public void SetPixel(string id, Sprite idle, Sprite[] front, Sprite[] back, Sprite[] side)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id != id) continue;
                var e = _entries[i];
                e.PixelIdle = idle;
                e.WalkFront = front;
                e.WalkBack = back;
                e.WalkSide = side;
                _entries[i] = e;
                return;
            }
            _entries.Add(new Entry { Id = id, Tint = Color.white, PixelIdle = idle, WalkFront = front, WalkBack = back, WalkSide = side });
        }

        /// <summary>모습 변형 연결 (G-2a · 테스트). null이나 빈 배열이면 변형을 지운다.</summary>
        public void SetVariants(string id, GameObject[] variants)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id != id) continue;
                var e = _entries[i];
                e.Variants = variants;
                _entries[i] = e;
                return;
            }
            _entries.Add(new Entry { Id = id, Tint = Color.white, Variants = variants });
        }
    }
}
