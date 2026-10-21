using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 고를 수 있는 플레이어 캐릭터 목록 (Assets/_Project/Data/Units/CharacterCatalog.asset, 결정 2026-10-21).
    /// 지금은 외형만 다르다 — 모습 ID(EntityVisualCatalog · 픽셀 전환 뒤 스프라이트)와 선택 창 초상 · 이름. 수치는 모두 Player.asset.
    /// 능력이 달라지면 줄마다 필드를 더한다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Character Catalog", fileName = "CharacterCatalog")]
    public sealed class CharacterCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Id;
            [Tooltip("선택 창 이름 (표시 전용, 임시)")] public string DisplayName;
            [Tooltip("플레이어 유닛의 모습 ID (EntityVisualCatalog)")] public string VisualId;
            [Tooltip("선택 창 초상 (64px 픽셀 스프라이트, 점 필터)")] public Sprite Portrait;
        }

        [SerializeField] private List<Entry> _characters = new List<Entry>();

        public IReadOnlyList<Entry> Characters => _characters;

        /// <summary>에디터 Setup용: 같은 ID가 없으면 추가, 있으면 초상만 비었을 때 채운다 (사용자가 고친 이름은 그대로). 바뀌었으면 true.</summary>
        public bool AddOrFill(Entry entry)
        {
            for (int i = 0; i < _characters.Count; i++)
            {
                if (_characters[i].Id != entry.Id) continue;
                var e = _characters[i];
                if (e.Portrait != null || entry.Portrait == null) return false;
                e.Portrait = entry.Portrait;
                _characters[i] = e;
                return true;
            }
            _characters.Add(entry);
            return true;
        }
    }
}
