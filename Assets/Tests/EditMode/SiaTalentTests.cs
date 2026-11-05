using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>
    /// 3-22c 시아 특성 (Docs/직업_시아.md 5번, 결정 2026-11-05): 프로젝트 ClassCatalog의 시아 특성 12개를 그대로 읽어 효과를 본다 —
    /// 가벼운 발 · 일격필살(+ 너클) · 충격(마비) · 빠른 손 · 너클 숙련 · 형태 제한, 에셋 값 (BALANCE.md 22번).
    /// </summary>
    public class SiaTalentTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly ProgressionConfig Progress = new ProgressionConfig(30, 5, 5, 5, 1, 1, 1);
        private static readonly UnitDef SiaDef = new UnitDef("hero_black", 20, 3, 8, 30, new[] { "sia_leap" }, accuracy: 10, evasion: 5, strength: 10);
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        private static ClassBook ProjectClasses() =>
            AssetDatabase.LoadAssetAtPath<ClassCatalogAsset>("Assets/_Project/Data/Units/ClassCatalog.asset").ToBook();

        private static SpellBook ProjectSpells()
        {
            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            return AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
        }

        private sealed class Session
        {
            public World World;
            public ClassBook Classes;
            public SpellBook Spells;
            public Unit Player;
        }

        // 프로젝트 시아 줄 그대로 + 선택 층만 1 (테스트 세계는 1층이라 형태를 바로 고르려고)
        private static Session Start(int level, string form = null)
        {
            var grid = GridMovementTests.Parse(
                "##########",
                "#........#",
                "#........#",
                "##########");
            var world = new World(grid, 9UL);
            var s = new Session { World = world, Spells = ProjectSpells() };
            var project = ProjectClasses();
            Assert.That(project.TryGet("sia", out var sia), Is.True, "ClassCatalog.asset 시아 줄 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            s.Classes = new ClassBook(new[] { new ClassDef("sia", 1, sia.Forms, sia.Tiers, sia.Talents) }); // 선택 층만 1로
            var vision = world.InstallCoreSystems(Sight, Magic, s.Spells.Statuses, s.Spells.Terrains);
            world.SetClassRules(s.Classes);
            s.Player = UnitSpawner.SpawnPlayer(world, SiaDef, new GridPos(1, 1), Magic.QuickSlotCount, Magic.InventorySlotCount);
            ClassForms.SetClass(world, "sia");
            int exp = 0;
            for (int l = 1; l < level; l++) exp += 5 + 5 * l;
            if (exp > 0) Progression.GainExp(world, s.Player, exp, Progress);
            if (form != null) Assert.That(ClassForms.Choose(world, s.Classes, s.Spells, s.Player, form), Is.EqualTo(ChooseFormResult.Chosen));
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static SpellDef Effective(Session s, string id)
        {
            Assert.That(s.Spells.TryGet(id, out var def), Is.True, id);
            return SpellModifiers.For(s.World, s.Player, def);
        }

        private static void Spend(Session s, string id, int times = 1)
        {
            for (int i = 0; i < times; i++) Assert.That(Talents.Spend(s.World, s.Classes, s.Player, id), Is.EqualTo(SpendTalentResult.Spent), id);
        }

        // 1단계: 가벼운 발 = 도약 3 → 5 (2점), 단련된 몸 = 최대 체력 +3
        [Test]
        public void TierOne_LightFeet_ToughBody()
        {
            var s = Start(6);
            Spend(s, "sia_light_feet", 2);
            Assert.That(Effective(s, "sia_leap").Range, Is.EqualTo(5));
            int maxHp = s.Player.MaxHp;
            Spend(s, "sia_tough_body");
            Assert.That(s.Player.MaxHp, Is.EqualTo(maxHp + 3));
        }

        // 너클 숙련: 너클을 들 때만 명중 +2
        [Test]
        public void KnuckleMastery_OnlyWithKnuckle()
        {
            var s = Start(2);
            Spend(s, "sia_knuckle_mastery");
            Assert.That(Talents.AccuracyBonus(s.World, s.Player), Is.EqualTo(0));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            Inventory.TryAdd(s.World, s.Spells, items, s.Player, InventoryRef.Item("knuckle"));
            Weapons.Equip(s.World, items, s.Player, System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("knuckle")));
            Assert.That(Talents.AccuracyBonus(s.World, s.Player), Is.EqualTo(2));
        }

        // 강타: 일격필살 1점 = 정권 9 + 2 = 11, 너클이면 (9 + 2) × 1.5 = 16 · 충격 2점 = 붕권 · 진각 · 정권 마비 20%
        [Test]
        public void Smash_OneStrikeAndShock()
        {
            var s = Start(9, "smash");
            Spend(s, "sia_one_strike");
            Assert.That(Effective(s, "sia_fist").Damage, Is.EqualTo(11));
            Spend(s, "sia_shock", 2);
            foreach (var id in new[] { "sia_crush", "sia_quake", "sia_fist" })
            {
                var d = Effective(s, id);
                Assert.That((d.AppliesStatus, d.StatusChancePercent), Is.EqualTo((StatusType.Paralyze, 20)), id);
            }
            Assert.That(Effective(s, "sia_leap").AppliesStatus, Is.EqualTo(StatusType.None), "도약은 대상 아님");
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            Inventory.TryAdd(s.World, s.Spells, items, s.Player, InventoryRef.Item("knuckle"));
            Weapons.Equip(s.World, items, s.Player, System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("knuckle")));
            Assert.That(Effective(s, "sia_fist").Damage, Is.EqualTo(16));
        }

        // 연타: 빠른 손 확률 15 / 30 / 45 · 끝없는 연타 4 → 5 · 강타 특성은 못 찍음
        [Test]
        public void Combo_QuickHandsAndEndlessCombo()
        {
            var s = Start(12, "combo");
            Spend(s, "sia_quick_hands", 3);
            Assert.That(Talents.MeleeExtraHitPercent(s.World, s.Player), Is.EqualTo(45));
            Spend(s, "sia_endless_combo");
            Assert.That(Effective(s, "sia_combo").HitCount, Is.EqualTo(5));
            Assert.That(Talents.Spend(s.World, s.Classes, s.Player, "sia_one_strike"), Is.EqualTo(SpendTalentResult.WrongForm));
            Assert.That(Talents.MeleeDamageBonus(s.World, s.Player), Is.EqualTo(1), "연타 자세는 그대로");
        }

        // 검산: BALANCE.md 22번 특성 표 = ClassCatalog 시아 줄, 루멘 특성과 ID가 겹치지 않음
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            Assert.That(ProjectClasses().TryGet("sia", out var sia), Is.True);
            Assert.That((sia.Tiers[0].FirstLevel, sia.Tiers[0].LastLevel, sia.Tiers[1].FirstLevel, sia.Tiers[1].LastLevel), Is.EqualTo((2, 6, 7, 12)));
            var rows = new List<string>();
            foreach (var t in sia.Talents)
            {
                var parts = new List<string>();
                foreach (var e in t.Effects)
                    parts.Add($"{e.Effect}[{string.Join(",", e.Values)}]{e.WeaponId}{(e.Status != StatusType.None ? e.Status.ToString() : "")}({string.Join(",", e.SpellIds)})");
                rows.Add($"{t.Id}:{t.Tier}:{t.FormId}:{t.MaxPoints}:{string.Join(" ", parts)}");
            }
            Assert.That(rows, Is.EqualTo(new[]
            {
                "sia_tough_body:1::2:MaxHp[3,6]()",
                "sia_light_feet:1::2:SpellRange[1,2](sia_leap)",
                "sia_ki_cycle:1::2:SpellKillMana[1,2](sia_leap,sia_combo,sia_rush,sia_crush,sia_quake,sia_fist)",
                "sia_knuckle_mastery:1::2:WeaponAccuracy[2,4]knuckle()",
                "sia_quick_hands:2:combo:3:MeleeExtraHitChance[15,30,45]()",
                "sia_endless_combo:2:combo:3:SpellHitCount[1,2,3](sia_combo)",
                "sia_gale:2:combo:3:SpellRange[1,2,3](sia_rush)",
                "sia_momentum:2:combo:3:HitManaChance[20,40,60](sia_combo,sia_rush)",
                "sia_long_crush:2:smash:3:SpellRange[1,2,3](sia_crush)",
                "sia_earth_ring:2:smash:3:SpellDamage[1,2,3](sia_quake)",
                "sia_one_strike:2:smash:3:SpellDamage[2,4,6](sia_fist)",
                "sia_shock:2:smash:3:SpellStatusChance[10,20,30]Paralyze(sia_crush,sia_quake,sia_fist)",
            }));
            Assert.That(ProjectClasses().TryGet("lumen", out var lumen), Is.True);
            foreach (var t in sia.Talents) Assert.That(lumen.TalentOf(t.Id), Is.Null, t.Id + " 루멘과 겹침");
        }
    }
}
