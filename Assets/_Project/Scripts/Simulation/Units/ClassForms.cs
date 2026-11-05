using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Magic;

namespace ProjectM.Units
{
    /// <summary>
    /// 직업 형태 하나 (3-21b, Docs/직업_루멘.md — 루멘 근접형 · 원거리형): 고르면 배우는 주문 · 바뀌는 주문(예: 던지기 → 강화 던지기).
    /// 값은 ClassCatalog.asset (BALANCE.md 21번).
    /// </summary>
    public sealed class ClassFormDef
    {
        public string Id { get; }
        public IReadOnlyList<string> LearnSpells { get; }
        public IReadOnlyList<(string From, string To)> ReplaceSpells { get; }
        /// <summary>형태를 고르면 늘 켜지는 효과 (3-22b 시아 연타 자세 — 특성과 같은 효과 표, 1점 값). 비면 없음.</summary>
        public IReadOnlyList<TalentEffectDef> PassiveEffects { get; }

        public ClassFormDef(string id, IReadOnlyList<string> learnSpells = null, IReadOnlyList<(string From, string To)> replaceSpells = null,
            IReadOnlyList<TalentEffectDef> passiveEffects = null)
        {
            PassiveEffects = passiveEffects ?? Array.Empty<TalentEffectDef>();
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("형태 ID가 비어 있다.", nameof(id));
            Id = id;
            LearnSpells = learnSpells ?? Array.Empty<string>();
            ReplaceSpells = replaceSpells ?? Array.Empty<(string, string)>();
        }
    }

    /// <summary>직업 하나: 형태를 고르는 층(그 층 보스를 쓰러뜨리면 — 루멘은 5층) · 형태 목록(표 순서 = 선택 창 순서).</summary>
    public sealed class ClassDef
    {
        public string Id { get; }
        public int ChooseAtFloor { get; }
        public IReadOnlyList<ClassFormDef> Forms { get; }
        /// <summary>특성 단계별 점수 레벨 범위 (3-21c, Tiers[단계 − 1]). 비면 특성 없음.</summary>
        public IReadOnlyList<TalentTier> Tiers { get; }
        /// <summary>특성 (표 순서 = 특성 창 순서). 2단계 특성은 FormId가 Forms 중 하나.</summary>
        public IReadOnlyList<TalentDef> Talents { get; }

        public ClassDef(string id, int chooseAtFloor, IReadOnlyList<ClassFormDef> forms,
            IReadOnlyList<TalentTier> tiers = null, IReadOnlyList<TalentDef> talents = null)
        {
            Tiers = tiers ?? Array.Empty<TalentTier>();
            Talents = talents ?? Array.Empty<TalentDef>();
            var talentIds = new HashSet<string>();
            foreach (var t in Talents)
            {
                if (t == null || !talentIds.Add(t.Id)) throw new ArgumentException("특성이 비었거나 ID가 겹친다.", nameof(talents));
                if (t.Tier > Tiers.Count) throw new ArgumentException($"특성 {t.Id}의 단계 {t.Tier}에 점수 레벨 범위가 없다.", nameof(talents));
                if (t.FormId != null && !Exists(forms, t.FormId)) throw new ArgumentException($"특성 {t.Id}의 형태 {t.FormId}가 없다.", nameof(talents));
            }
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("직업 ID가 비어 있다.", nameof(id));
            if (chooseAtFloor < 1) throw new ArgumentOutOfRangeException(nameof(chooseAtFloor));
            if (forms == null || forms.Count == 0) throw new ArgumentException("형태가 없다.", nameof(forms));
            var ids = new HashSet<string>();
            foreach (var f in forms)
                if (f == null || !ids.Add(f.Id)) throw new ArgumentException("형태가 비었거나 ID가 겹친다.", nameof(forms));
            Id = id;
            ChooseAtFloor = chooseAtFloor;
            Forms = forms;
        }

        public ClassFormDef FormOf(string formId)
        {
            foreach (var f in Forms) if (f.Id == formId) return f;
            return null;
        }

        public TalentDef TalentOf(string talentId)
        {
            foreach (var t in Talents) if (t.Id == talentId) return t;
            return null;
        }

        private static bool Exists(IReadOnlyList<ClassFormDef> forms, string formId)
        {
            if (forms == null) return false;
            foreach (var f in forms) if (f != null && f.Id == formId) return true;
            return false;
        }
    }

    /// <summary>직업 표 (3-21b). 상태가 없다 — 고른 것은 RunState.ClassId · FormId (규칙 6).</summary>
    public sealed class ClassBook
    {
        private readonly Dictionary<string, ClassDef> _classes = new Dictionary<string, ClassDef>();

        public ClassBook(IEnumerable<ClassDef> classes)
        {
            if (classes == null) throw new ArgumentNullException(nameof(classes));
            foreach (var c in classes)
            {
                if (c == null || _classes.ContainsKey(c.Id)) throw new ArgumentException("직업이 비었거나 ID가 겹친다.", nameof(classes));
                _classes.Add(c.Id, c);
            }
        }

        public bool TryGet(string id, out ClassDef def)
        {
            def = null;
            return id != null && _classes.TryGetValue(id, out def);
        }
    }

    /// <summary>형태 고르기 결과.</summary>
    public enum ChooseFormResult
    {
        Chosen = 0,
        NoClass = 1,
        AlreadyChosen = 2,
        NotYet = 3,
        UnknownForm = 4,
    }

    /// <summary>
    /// 직업 · 형태 (3-21b): 판 시작에 직업을 정하고(SetClass), 그 직업의 선택 층 보스를 쓰러뜨린 뒤 형태를 한 번 고른다(Choose).
    /// 고르기는 틱을 쓰지 않는 틱 밖 변경 (층 이동 · 빠른 칸 등록과 같음) — 부른 쪽이 이벤트를 비운다.
    /// </summary>
    public static class ClassForms
    {
        /// <summary>판 시작 (캐릭터 선택): 직업 ID. null · 빈 문자열 = 직업 없음. 형태는 지운다.</summary>
        public static void SetClass(World world, string classId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.Run.ClassId = string.IsNullOrEmpty(classId) ? null : classId;
            world.Run.FormId = null;
            world.Run.SetTalents(Array.Empty<TalentPoint>()); // 3-21c
        }

        /// <summary>지금 형태를 고를 수 있는지: 직업이 표에 있고 · 아직 안 골랐고 · 선택 층 이상이고 · 선택 층의 보스가 쓰러졌는지(봉인 계단이 열림).</summary>
        public static ChooseFormResult CanChoose(World world, ClassBook classes, bool bossDefeated)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (classes == null || !classes.TryGet(world.Run.ClassId, out var def)) return ChooseFormResult.NoClass;
            if (world.Run.FormId != null) return ChooseFormResult.AlreadyChosen;
            if (world.Floor < def.ChooseAtFloor || (world.Floor == def.ChooseAtFloor && !bossDefeated)) return ChooseFormResult.NotYet;
            return ChooseFormResult.Chosen;
        }

        /// <summary>
        /// 형태를 고른다: 바뀌는 주문을 같은 칸(인벤토리 · 빠른 칸)에서 바꾸고, 배우는 주문을 넣어 빈 빠른 칸에 등록한다 (마법서와 같음).
        /// bossDefeated = 선택 층 보스를 쓰러뜨렸는지 (부르는 쪽 — 봉인 계단 열림 이벤트).
        /// </summary>
        public static ChooseFormResult Choose(World world, ClassBook classes, SpellBook spells, Unit player, string formId, bool bossDefeated = true)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            var can = CanChoose(world, classes, bossDefeated);
            if (can != ChooseFormResult.Chosen) return can;
            classes.TryGet(world.Run.ClassId, out var def);
            var form = def.FormOf(formId);
            if (form == null) return ChooseFormResult.UnknownForm;

            world.Run.FormId = form.Id;
            foreach (var (from, to) in form.ReplaceSpells)
                if (spells != null && spells.TryGet(to, out _))
                    Inventory.Replace(world, player, InventoryRef.Spell(from), InventoryRef.Spell(to));
            var learned = new List<string>();
            foreach (var id in form.LearnSpells)
            {
                if (spells == null || !spells.TryGet(id, out _)) continue;
                var entry = InventoryRef.Spell(id);
                if (Inventory.TryAdd(world, spells, null, player, entry) < 0) continue; // 인벤토리가 가득 · 이미 앎
                int quick = Array.IndexOf(player.QuickSlots, InventoryRef.Empty);
                if (quick >= 0) Inventory.AssignQuickSlot(world, player, quick, entry);
                learned.Add(id);
            }
            world.Events.Publish(new ClassFormChosenEvent(world.Run.ClassId, form.Id, learned));
            return ChooseFormResult.Chosen;
        }
    }

    /// <summary>직업 형태를 골랐다 (3-21b, HUD 로그). Learned = 실제로 배운 주문 (인벤토리가 가득 차 못 넣은 것은 빠짐).</summary>
    public sealed class ClassFormChosenEvent : ISimEvent
    {
        public string ClassId { get; }
        public string FormId { get; }
        public IReadOnlyList<string> Learned { get; }

        public ClassFormChosenEvent(string classId, string formId, IReadOnlyList<string> learned)
        {
            ClassId = classId;
            FormId = formId;
            Learned = learned;
        }
    }
}
