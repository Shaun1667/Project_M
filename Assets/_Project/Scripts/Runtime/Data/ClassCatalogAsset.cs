using System;
using System.Collections.Generic;
using ProjectM.Status;
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
        public struct TalentEffectEntry
        {
            public TalentEffect Effect;
            [Tooltip("찍은 점수별 값 (1점, 2점, …) — 누적 값")] public List<int> Values;
            [Tooltip("대상 주문 (주문 효과 · 맞힘 마나). 비면 없음")] public List<string> SpellIds;
            [Tooltip("무기 명중 효과의 무기 아이템 ID")] public string WeaponId;
            [Tooltip("상태 효과의 상태")] public StatusType Status;
        }

        [Serializable]
        public struct Talent
        {
            public string Id;
            [Tooltip("특성 창 이름 (표시 전용)")] public string DisplayName;
            [Tooltip("특성 창 설명 (표시 전용)")] [TextArea] public string Description;
            [Tooltip("1 = 공통, 2 = 형태")] [Min(1)] public int Tier;
            [Tooltip("2단계: 형태 ID. 1단계는 비움")] public string FormId;
            [Min(1)] public int MaxPoints;
            public List<TalentEffectEntry> Effects;
        }

        [Serializable]
        public struct Tier
        {
            [Tooltip("이 레벨부터 레벨업마다 이 단계 점수 1")] [Min(2)] public int FirstLevel;
            [Min(2)] public int LastLevel;
        }

        [Serializable]
        public struct ClassEntry
        {
            public string Id;
            [Tooltip("이 층 보스를 쓰러뜨리면 형태를 고른다")] [Min(1)] public int ChooseAtFloor;
            public List<Form> Forms;
            [Header("특성 (3-21c)")]
            [Tooltip("단계별 점수 레벨 범위 (0번 = 1단계)")] public List<Tier> Tiers;
            public List<Talent> Talents;
        }

        /// <summary>에디터 Setup용 (3-21c): 그 직업에 특성이 비어 있으면 단계 · 특성을 채운다.</summary>
        public bool SetTalentsIfEmpty(string classId, List<Tier> tiers, List<Talent> talents)
        {
            for (int i = 0; i < _classes.Count; i++)
            {
                if (_classes[i].Id != classId || (_classes[i].Talents != null && _classes[i].Talents.Count > 0)) continue;
                var c = _classes[i];
                c.Tiers = tiers;
                c.Talents = talents;
                _classes[i] = c;
                return true;
            }
            return false;
        }

        /// <summary>특성 ID → 표시 이름 (HUD 로그용).</summary>
        public Dictionary<string, string> TalentNames()
        {
            var names = new Dictionary<string, string>();
            foreach (var c in _classes)
                if (c.Talents != null)
                    foreach (var t in c.Talents) if (!string.IsNullOrEmpty(t.Id)) names[t.Id] = string.IsNullOrEmpty(t.DisplayName) ? t.Id : t.DisplayName;
            return names;
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
                var tiers = new List<TalentTier>();
                foreach (var t in c.Tiers ?? new List<Tier>()) tiers.Add(new TalentTier(t.FirstLevel, t.LastLevel));
                var talents = new List<TalentDef>();
                foreach (var t in c.Talents ?? new List<Talent>())
                {
                    var effects = new List<TalentEffectDef>();
                    foreach (var e in t.Effects ?? new List<TalentEffectEntry>())
                        effects.Add(new TalentEffectDef(e.Effect, e.Values ?? new List<int>(), e.SpellIds ?? new List<string>(), e.WeaponId, e.Status));
                    talents.Add(new TalentDef(t.Id, t.Tier, t.MaxPoints, effects, t.FormId));
                }
                list.Add(new ClassDef(c.Id, c.ChooseAtFloor, forms, tiers, talents));
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
