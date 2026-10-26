using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 이펙트 표 (G-3): 키 → 파티클 프리팹 + 크기 · 색 · 재생 속도 · 길이. 표시 전용 데이터 — 규칙은 이 표를 모른다.
    /// 키 규칙 (FxPresenter가 찾는 순서는 각 메서드 주석):
    ///   terrain.&lt;TerrainEffectType&gt; (칸 루프) · status.&lt;StatusType&gt; (유닛 루프) ·
    ///   cast.&lt;Element&gt; (지팡이 끝) · projectile.&lt;Element&gt; (날아감) · impact.&lt;Element&gt; (맞은 칸) · burst.&lt;Element&gt; (3×3 중심) ·
    ///   fusion.&lt;융합 ID&gt; (융합 주문의 맞는 효과, 없으면 속성 키) · reaction.&lt;반응 ID&gt; (반응 칸).
    /// 가져온 팩 효과는 모두 반복(데모용)이라 Lifetime &gt; 0이면 한 번만 내고 그 시간 뒤 지운다. 값은 메뉴 ProjectM/Setup/FX가 채운다 (ART_ASSETS.md 13번).
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Fx Catalog", fileName = "FxCatalog")]
    public sealed class FxCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Key;
            public GameObject Prefab;
            [Tooltip("월드 크기 배율 (팩 원본이 커서 0.1~0.4)")] [Min(0.001f)] public float Scale;
            [Tooltip("시작 색에 곱함. a = 0이면 원래 색 그대로")] public Color Tint;
            [Tooltip("재생 속도 배율 (1초 규칙에 맞춰 빠르게)")] [Min(0.01f)] public float Speed;
            [Tooltip("한 번 재생 후 지우기까지 초. 0 = 루프 (지형 · 상태 · 투사체)")] [Min(0f)] public float Lifetime;
            [Tooltip("바닥 · 발밑에서 띄우는 높이 (m)")] public float Height;

            [Header("2D 픽셀 (P-3 ⓓ)")]
            [Tooltip("프레임 그림 (Art/Pixel/Fx/<이름>_0~3). 비어 있으면 2D에서는 이 키를 건너뛴다")] public Sprite[] Frames;
            [Tooltip("프레임/초")] [Min(0f)] public float Fps;
            [Tooltip("화면 너비 (칸) — 그림 픽셀을 정수 배로 키워 가장 가까운 크기")] [Min(0f)] public float PixelCells;
            [Tooltip("그림에 곱하는 색. a = 0이면 그대로")] public Color PixelTint;

            public bool HasFrames => Frames != null && Frames.Length > 0 && Frames[0] != null;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public int Count => _entries.Count;
        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>3D = 프리팹이 있는 줄, 2D(pixel2D) = 프레임 그림이 있는 줄만.</summary>
        public bool TryGet(string key, out Entry entry, bool pixel2D = false)
        {
            foreach (var e in _entries)
            {
                if (e.Key != key || (pixel2D ? !e.HasFrames : e.Prefab == null)) continue;
                entry = e;
                return true;
            }
            entry = default;
            return false;
        }

        /// <summary>에디터 Setup · 테스트용: 같은 키가 있으면 바꾸고 없으면 추가한다 (표가 기준 — 멱등).</summary>
        public void Set(Entry entry)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Key != entry.Key) continue;
                _entries[i] = entry;
                return;
            }
            _entries.Add(entry);
        }

        /// <summary>에디터 Setup용: 표에 없는 키를 지운다.</summary>
        public void RemoveExcept(ICollection<string> keys) => _entries.RemoveAll(e => !keys.Contains(e.Key));
    }
}
