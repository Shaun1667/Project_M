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
            [Tooltip("게임 안 64px 스프라이트 (4등신 · 격자 맞춤, 점 필터) — 일러스트가 없으면 선택 창 초상으로도 씀")] public Sprite Portrait;
            [Tooltip("원본 일러스트 (7등신 도트 일러스트, 결정 2026-10-21) — 있으면 선택 창 카드에 이것을 보여 줌")] public Sprite Illustration;
            [Header("직업 (3-21a)")]
            [Tooltip("직업 ID (루멘 = lumen — Docs/직업_루멘.md). 비면 직업 없음")] public string ClassId;
            [Tooltip("이 캐릭터만 시작할 때 아는 주문 (SpellCatalog) — Player.asset 시작 주문 앞에 붙음 = 빠른 칸 앞쪽부터")] public List<string> StartingSpells;
        }

        /// <summary>에디터 Setup용 (3-21a): 직업이 비어 있으면 직업 · 시작 주문을 채운다 (사용자가 고친 값은 그대로).</summary>
        public bool SetClassIfEmpty(string id, string classId, params string[] startingSpells)
        {
            for (int i = 0; i < _characters.Count; i++)
            {
                if (_characters[i].Id != id || !string.IsNullOrEmpty(_characters[i].ClassId)) continue;
                var e = _characters[i];
                e.ClassId = classId;
                e.StartingSpells = new List<string>(startingSpells);
                _characters[i] = e;
                return true;
            }
            return false;
        }

        [SerializeField] private List<Entry> _characters = new List<Entry>();

        public IReadOnlyList<Entry> Characters => _characters;

        /// <summary>에디터 Setup용: 같은 ID가 없으면 추가, 있으면 초상 · 일러스트가 비었을 때만 채운다 (사용자가 고친 이름은 그대로). 바뀌었으면 true.</summary>
        public bool AddOrFill(Entry entry)
        {
            for (int i = 0; i < _characters.Count; i++)
            {
                if (_characters[i].Id != entry.Id) continue;
                var e = _characters[i];
                bool changed = false;
                if (e.Portrait == null && entry.Portrait != null) { e.Portrait = entry.Portrait; changed = true; }
                if (e.Illustration == null && entry.Illustration != null) { e.Illustration = entry.Illustration; changed = true; }
                _characters[i] = e;
                return changed;
            }
            _characters.Add(entry);
            return true;
        }
    }
}
