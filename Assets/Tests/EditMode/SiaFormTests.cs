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
    /// 3-22b 시아 형태 (Docs/직업_시아.md 3번, 결정 2026-11-05): 연타(연타 자세 = 기본 공격 +1 · 연타 4회 · 질풍 연격 돌진 2회) /
    /// 강타(붕권 직선 · 진각 내 둘레 · 정권) · 너클 강화 · 추가 타격 효과(3-22c 빠른 손이 쓸 코드) · 에셋 값 (BALANCE.md 22번).
    /// </summary>
    public class SiaFormTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef SiaDef = new UnitDef("hero_black", 20, 3, 8, 30, new[] { "sia_leap" }, accuracy: 10, evasion: 5, strength: 10);
        private static readonly UnitDef DummyDef = new UnitDef("enemy_melee", 50, 2, 6); // 회피 0 — 근접은 늘 맞음

        private sealed class Idle : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static SpellBook Book() => new SpellBook(new[]
        {
            new SpellDef("sia_leap", Element.Neutral, SpellShape.Leap, 3, 0, 2, boostWeaponId: "knuckle", boostDamagePercent: 150, boostRange: 1),
            new SpellDef("sia_combo", Element.Neutral, SpellShape.Bolt, 1, 2, 3, hitCount: 4, boostWeaponId: "knuckle", boostDamagePercent: 150, boostRange: 1),
            new SpellDef("sia_rush", Element.Neutral, SpellShape.Dash, 3, 3, 4, hitCount: 2, boostWeaponId: "knuckle", boostDamagePercent: 150, boostRange: 1),
            new SpellDef("sia_crush", Element.Neutral, SpellShape.Beam, 3, 6, 4, boostWeaponId: "knuckle", boostDamagePercent: 150, boostRange: 1),
            new SpellDef("sia_quake", Element.Neutral, SpellShape.Around, 1, 5, 4, 1, boostWeaponId: "knuckle", boostDamagePercent: 150, boostRange: 1),
            new SpellDef("sia_fist", Element.Neutral, SpellShape.Bolt, 1, 9, 3, boostWeaponId: "knuckle", boostDamagePercent: 150, boostRange: 1),
        }, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());

        // BALANCE.md 22번 형태 (선택 층 1 — 테스트 세계는 1층). extraHit = 3-22c 빠른 손 시험용 특성
        private static ClassBook Classes(int extraHit = 0) => new ClassBook(new[]
        {
            new ClassDef("sia", 1, new[]
            {
                new ClassFormDef("combo", new[] { "sia_combo", "sia_rush" }, null, new[] { new TalentEffectDef(TalentEffect.MeleeDamage, new[] { 1 }) }),
                new ClassFormDef("smash", new[] { "sia_crush", "sia_quake", "sia_fist" }),
            },
            new[] { new TalentTier(2, 6) },
            extraHit > 0 ? new[] { new TalentDef("quick_hands", 1, 1, new[] { new TalentEffectDef(TalentEffect.MeleeExtraHitChance, new[] { extraHit }) }) } : null),
        });

        private sealed class Session
        {
            public World World;
            public SpellBook Book;
            public ClassBook Classes;
            public TurnClock Clock;
            public Unit Player;
            public ItemBook Items;
        }

        private static Session Start(int extraHit = 0)
        {
            var grid = GridMovementTests.Parse(
                "############",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "############");
            var world = new World(grid, 8UL);
            var s = new Session { World = world, Book = Book(), Classes = Classes(extraHit), Clock = new TurnClock(world.Scheduler, world.PlayerInput),
                Items = new ItemBook(new[] { new ItemDef("knuckle", ItemUse.EquipWeapon, weapon: new WeaponDef(5, 3, 100, 1, 1, strengthRequirement: 10, hitsPerAttack: 2)) },
                    new WeaponRules(3, 2)) };
            var vision = world.InstallCoreSystems(Sight, Magic, s.Book.Statuses, s.Book.Terrains);
            world.SetClassRules(s.Classes);
            s.Player = UnitSpawner.SpawnPlayer(world, SiaDef, new GridPos(3, 2), Magic.QuickSlotCount, Magic.InventorySlotCount);
            ClassForms.SetClass(world, "sia");
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static Unit Dummy(Session s, GridPos pos)
        {
            var u = UnitSpawner.SpawnEnemy(s.World, DummyDef, pos, new Idle());
            s.World.Events.Flush();
            return u;
        }

        private static CastBlock Cast(Session s, string spellId, GridPos target)
        {
            var block = SpellCasting.TryCreateCastSpell(s.World, Config, s.Book, Targeting, Sight, spellId, target, out var command);
            if (block == CastBlock.None) s.Clock.SubmitPlayerCommand(command);
            s.World.Events.Flush();
            return block;
        }

        private static void Attack(Session s, Unit target)
        {
            s.Clock.SubmitPlayerCommand(new AttackCommand(s.World, target.Id, Config.DefaultActionCostTicks));
            s.World.Events.Flush();
        }

        // 연타: 연타 · 질풍 연격을 배워 빠른 칸 2 · 3, 강타: 붕권 · 진각 · 정권을 2 · 3 · 4
        [Test]
        public void ChooseForms_LearnSkills()
        {
            var combo = Start();
            Assert.That(ClassForms.Choose(combo.World, combo.Classes, combo.Book, combo.Player, "combo"), Is.EqualTo(ChooseFormResult.Chosen));
            Assert.That(new[] { combo.Player.QuickSlotSpell(0), combo.Player.QuickSlotSpell(1), combo.Player.QuickSlotSpell(2) },
                Is.EqualTo(new[] { "sia_leap", "sia_combo", "sia_rush" }));
            var smash = Start();
            ClassForms.Choose(smash.World, smash.Classes, smash.Book, smash.Player, "smash");
            Assert.That(new[] { smash.Player.QuickSlotSpell(1), smash.Player.QuickSlotSpell(2), smash.Player.QuickSlotSpell(3) },
                Is.EqualTo(new[] { "sia_crush", "sia_quake", "sia_fist" }));
        }

        // 연타 자세: 기본 근접 공격(맨손 3) +1 = 4 — 강타 형태 · 형태 전은 3
        [Test]
        public void ComboStance_AddsMeleeDamage()
        {
            var before = Start();
            var a = Dummy(before, new GridPos(4, 2));
            Attack(before, a);
            Assert.That(a.Hp, Is.EqualTo(50 - 3), "형태 전");
            var combo = Start();
            ClassForms.Choose(combo.World, combo.Classes, combo.Book, combo.Player, "combo");
            var b = Dummy(combo, new GridPos(4, 2));
            Attack(combo, b);
            Assert.That(b.Hp, Is.EqualTo(50 - 4));
            var smash = Start();
            ClassForms.Choose(smash.World, smash.Classes, smash.Book, smash.Player, "smash");
            var c = Dummy(smash, new GridPos(4, 2));
            Attack(smash, c);
            Assert.That(c.Hp, Is.EqualTo(50 - 3), "강타는 형태 효과 없음");
        }

        // 너클 + 연타 자세: 타격마다 +1 → 한 대 2~3 × 2타
        [Test]
        public void ComboStance_WithKnuckle_EachHit()
        {
            var s = Start();
            ClassForms.Choose(s.World, s.Classes, s.Book, s.Player, "combo");
            Inventory.TryAdd(s.World, s.Book, s.Items, s.Player, InventoryRef.Item("knuckle"));
            Weapons.Equip(s.World, s.Items, s.Player, System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("knuckle")));
            var enemy = Dummy(s, new GridPos(4, 2));
            var hits = new List<UnitDamagedEvent>();
            s.World.Events.Subscribe<UnitDamagedEvent>(hits.Add);
            Attack(s, enemy);
            var mine = hits.FindAll(h => h.Target == enemy.Id);
            Assert.That(mine.Count, Is.EqualTo(2));
            Assert.That(mine.TrueForAll(h => h.Amount >= 2 && h.Amount <= 3), Is.True);
        }

        // 진각: 내 둘레 8칸만 — 2칸 떨어진 적 · 나는 안 맞음
        [Test]
        public void Quake_HitsAroundCaster_NotSelf()
        {
            var s = Start();
            ClassForms.Choose(s.World, s.Classes, s.Book, s.Player, "smash");
            var n = Dummy(s, new GridPos(3, 3));
            var e = Dummy(s, new GridPos(4, 2));
            var sw = Dummy(s, new GridPos(2, 1));
            var far = Dummy(s, new GridPos(5, 2));
            int hp = s.Player.Hp;
            Assert.That(Cast(s, "sia_quake", e.Pos), Is.EqualTo(CastBlock.None));
            Assert.That((n.Hp, e.Hp, sw.Hp, far.Hp), Is.EqualTo((45, 45, 45, 50)));
            Assert.That(s.Player.Hp, Is.EqualTo(hp));
        }

        // 붕권 = 직선 3칸 관통 6, 정권 = 옆 칸 9 · 너클이면 13 · 사거리 2, 질풍 연격 = 달려가 3 × 2
        [Test]
        public void SmashAndRush_Damage_KnuckleBoost()
        {
            var s = Start();
            ClassForms.Choose(s.World, s.Classes, s.Book, s.Player, "smash");
            var a = Dummy(s, new GridPos(4, 2));
            var b = Dummy(s, new GridPos(6, 2));
            var c = Dummy(s, new GridPos(7, 2));
            Assert.That(Cast(s, "sia_crush", a.Pos), Is.EqualTo(CastBlock.None));
            Assert.That((a.Hp, b.Hp, c.Hp), Is.EqualTo((44, 44, 50)), "3칸까지");
            Assert.That(Cast(s, "sia_fist", a.Pos), Is.EqualTo(CastBlock.None));
            Assert.That(a.Hp, Is.EqualTo(44 - 9));

            Inventory.TryAdd(s.World, s.Book, s.Items, s.Player, InventoryRef.Item("knuckle"));
            Weapons.Equip(s.World, s.Items, s.Player, System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("knuckle")));
            s.Book.TryGet("sia_fist", out var fist);
            var boosted = SpellModifiers.For(s.World, s.Player, fist);
            Assert.That((boosted.Damage, boosted.Range), Is.EqualTo((13, 2)), "9 × 150% = 13 (내림)");

            var r = Start();
            ClassForms.Choose(r.World, r.Classes, r.Book, r.Player, "combo");
            var target = Dummy(r, new GridPos(6, 2));
            Assert.That(Cast(r, "sia_rush", target.Pos), Is.EqualTo(CastBlock.None));
            Assert.That(r.Player.Pos, Is.EqualTo(new GridPos(5, 2)));
            Assert.That(target.Hp, Is.EqualTo(50 - 6), "3 × 2");
        }

        // 3-22c 빠른 손이 쓸 코드: 추가 타격 100%면 맨손 공격이 2번 (추가 타격은 다시 굴리지 않음), 특성이 없으면 1번
        [Test]
        public void ExtraHitChance_StrikesOnceMore()
        {
            var s = Start(extraHit: 100);
            Assert.That(Talents.Spend(s.World, s.Classes, s.Player, "quick_hands"), Is.EqualTo(SpendTalentResult.NoPoints), "레벨 1");
            Progression.GainExp(s.World, s.Player, 10, new ProgressionConfig(30, 5, 5, 5, 1, 1, 1));
            Assert.That(Talents.Spend(s.World, s.Classes, s.Player, "quick_hands"), Is.EqualTo(SpendTalentResult.Spent));
            var enemy = Dummy(s, new GridPos(4, 2));
            var hits = new List<UnitDamagedEvent>();
            s.World.Events.Subscribe<UnitDamagedEvent>(hits.Add);
            Attack(s, enemy);
            Assert.That(hits.FindAll(h => h.Target == enemy.Id).Count, Is.EqualTo(2));
        }

        // 검산: BALANCE.md 22번 = ClassCatalog 시아 줄 + SpellCatalog 형태 기술 5줄
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ClassCatalogAsset>("Assets/_Project/Data/Units/ClassCatalog.asset");
            Assert.That(catalog.ToBook().TryGet("sia", out var sia), Is.True);
            Assert.That((sia.ChooseAtFloor, sia.Forms.Count, sia.Tiers.Count), Is.EqualTo((5, 2, 2)));
            Assert.That(sia.FormOf("combo").LearnSpells, Is.EqualTo(new[] { "sia_combo", "sia_rush" }));
            Assert.That(sia.FormOf("combo").PassiveEffects.Count, Is.EqualTo(1));
            Assert.That((sia.FormOf("combo").PassiveEffects[0].Effect, sia.FormOf("combo").PassiveEffects[0].ValueAt(1)), Is.EqualTo((TalentEffect.MeleeDamage, 1)));
            Assert.That(sia.FormOf("smash").LearnSpells, Is.EqualTo(new[] { "sia_crush", "sia_quake", "sia_fist" }));
            Assert.That(sia.FormOf("smash").PassiveEffects, Is.Empty);

            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            foreach (var (id, shape, range, damage, mana, radius, hits) in new[]
            {
                ("sia_combo", SpellShape.Bolt, 1, 2, 3, 0, 4),
                ("sia_rush", SpellShape.Dash, 3, 3, 4, 0, 2),
                ("sia_crush", SpellShape.Beam, 3, 6, 4, 0, 1),
                ("sia_quake", SpellShape.Around, 1, 5, 4, 1, 1),
                ("sia_fist", SpellShape.Bolt, 1, 9, 3, 0, 1),
            })
            {
                Assert.That(spells.TryGet(id, out var d), Is.True, id);
                Assert.That((d.Element, d.Shape, d.Range, d.Damage, d.ManaCost, d.Radius, d.HitCount), Is.EqualTo((Element.Neutral, shape, range, damage, mana, radius, hits)), id);
                Assert.That((d.BoostWeaponId, d.BoostDamagePercent, d.BoostRange), Is.EqualTo(("knuckle", 150, 1)), id + " 너클 강화");
            }
        }
    }
}
