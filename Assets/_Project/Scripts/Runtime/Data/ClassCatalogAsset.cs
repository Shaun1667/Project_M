using System;
using System.Collections.Generic;
using ProjectM.Units;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 직업 · 형태 표 (Assets/_Project/Data/Units/ClassCatalog.asset, 3-21b — Docs/직업_루멘.md). 기준은 BALANCE.md 21번.
    /// 직업 ID는 CharacterCatalog 줄의 ClassId와 같다. 이름 · 설명은 선택 창 표시 전용.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Class Catalog", fileName = "ClassCatalog")]
    public sealed class ClassCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct SpellSwap
        {
            public string From;
            public string To;
        }

        [Serializable]
        public struct Form
        {
            public string Id;
            [Tooltip("선택 창 이름 (표시 전용)")] public string DisplayName;
            [Tooltip("선택 창 설명 (표시 전용, 줄바꿈 가능)")] [TextArea] public string Description;
            [Tooltip("고르면 배우는 주문 (SpellCatalog) — 빈 빠른 칸에 차례로 등록")] public List<string> LearnSpells;
            [Tooltip("고르면 같은 칸에서 바뀌는 주문")] public List<SpellSwap> ReplaceSpells;
        }

        [Serializable]
        public struct ClassEntry
        {
            public string Id;
            [Tooltip("이 층 보스를 쓰러뜨리면 형태를 고른다")] [Min(1)] public int ChooseAtFloor;
            public List<Form> Forms;
        }

        [SerializeField] private List<ClassEntry> _classes = new List<ClassEntry>();

        public IReadOnlyList<ClassEntry> Classes => _classes;

        public bool TryGetClass(string id, out ClassEntry entry)
        {
            foreach (var c in _classes)
                if (c.Id == id) { entry = c; return true; }
            entry = default;
            return false;
        }

        public ClassBook ToBook()
        {
            var list = new List<ClassDef>(_classes.Count);
            foreach (var c in _classes)
            {
                var forms = new List<ClassFormDef>();
                foreach (var f in c.Forms ?? new List<Form>())
                {
                    var swaps = new List<(string, string)>();
                    foreach (var s in f.ReplaceSpells ?? new List<SpellSwap>()) swaps.Add((s.From, s.To));
                    forms.Add(new ClassFormDef(f.Id, f.LearnSpells ?? new List<string>(), swaps));
                }
                list.Add(new ClassDef(c.Id, c.ChooseAtFloor, forms));
            }
            return new ClassBook(list);
        }

        /// <summary>에디터 Setup용: 같은 ID의 직업이 없을 때만 추가 (사용자가 고친 값은 그대로).</summary>
        public bool AddIfMissing(ClassEntry entry)
        {
            foreach (var c in _classes)
                if (c.Id == entry.Id) return false;
            _classes.Add(entry);
            return true;
        }
    }
}
