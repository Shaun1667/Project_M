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
    /// 3-21a 루멘 빛의 창 (Docs/직업_루멘.md, 결정 2026-11-05): 무속성(반응 · 약점 · 저항 없음) · 공통 기술 2개 ·
    /// 창(spear)을 들면 피해 ×1.5 · 사거리 +1 (SpellModifiers 한 곳 — 미리보기 = 시전) · 캐릭터 시작 주문 (BALANCE.md 21번).
    /// </summary>
    public class LightSpearTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("hero_blond", 20, 3, 8, 10, new[] { "light_spear_thrust", "light_spear_throw" });
        private static readonly UnitDef DummyDef = new UnitDef("enemy_melee", 50, 2, 6);

        private sealed class Idle : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        // BALANCE.md 21번 — 공통 기술 2개 (무속성, 창 강화 150% · +1)
        private static SpellBook Book(ReactionBook reactions = null)
        {
            var statuses = SpellTests.BalanceStatuses();
            return new SpellBook(new[]
            {
                new SpellDef("light_spear_thrust", Element.Neutral, SpellShape.Beam, 2, 4, 2, boostWeaponId: "spear", boostDamagePercent: 150, boostRange: 1),
                new SpellDef("light_spear_throw", Element.Neutral, SpellShape.Bolt, 5, 4, 3, boostWeaponId: "spear", boostDamagePercent: 150, boostRange: 1),
            }, statuses, SpellTests.BalanceTerrains(), reactions);
        }

        private sealed class Session
        {
            public World World;
            public SpellBook Book;
            public TurnClock Clock;
            public Unit Player;
            public ItemBook Items;
        }

        private static Session Start(SpellBook book = null)
        {
            var grid = GridMovementTests.Parse(
                "############",
                "#..........#",
                "#..........#",
                "#..........#",
                "############");
            var world = new World(grid, 5UL);
            var s = new Session { World = world, Book = book ?? Book(), Clock = new TurnClock(world.Scheduler, world.PlayerInput), Items = WeaponTests.Book() };
            var vision = world.InstallCoreSystems(Sight, Magic, s.Book.Statuses, s.Book.Terrains);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 2), Magic.QuickSlotCount, Magic.InventorySlotCount);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static CastBlock Cast(Session s, string spellId, GridPos target)
        {
            var block = SpellCasting.TryCreateCastSpell(s.World, Config, s.Book, Targeting, Sight, spellId, target, out var command);
            if (block == CastBlock.None) s.Clock.SubmitPlayerCommand(command);
            s.World.Events.Flush();
            return block;
        }

        private static void HoldSpear(Session s)
        {
            Assert.That(Inventory.TryAdd(s.World, s.Book, s.Items, s.Player, InventoryRef.Item("spear")), Is.True);
            int index = System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("spear"));
            Assert.That(Weapons.Equip(s.World, s.Items, s.Player, index), Is.True);
            Assert.That(s.Player.Weapon.ItemId, Is.EqualTo("spear"));
        }

        // 시작 주문 = 빠른 칸 1 · 2 (캐릭터 시작 주문은 Player.asset 주문 앞)
        [Test]
        public void StartingSpells_FillFirstQuickSlots_CharacterSpellsFirst()
        {
            var s = Start();
            Assert.That(s.Player.QuickSlotSpell(0), Is.EqualTo("light_spear_thrust"));
            Assert.That(s.Player.QuickSlotSpell(1), Is.EqualTo("light_spear_throw"));

            var baseDef = new UnitDef("hero_blond", 20, 3, 8, 10, new[] { "fire_basic" });
            var withClass = baseDef.WithExtraStartingSpells(new[] { "light_spear_thrust", "light_spear_throw", "fire_basic" });
            Assert.That(withClass.StartingSpells, Is.EqualTo(new[] { "light_spear_thrust", "light_spear_throw", "fire_basic" }), "직업 주문이 앞, 겹치면 한 번");
            Assert.That(baseDef.WithExtraStartingSpells(null), Is.SameAs(baseDef));
        }

        // 찌르기 = 앞 2칸 관통 · 던지기 = 첫 유닛 · 마나 — 항상 명중 · 고정 피해
        [Test]
        public void ThrustPiercesTwo_ThrowHitsFirst()
        {
            var s = Start();
            var a = UnitSpawner.SpawnEnemy(s.World, DummyDef, new GridPos(2, 2), new Idle());
            var b = UnitSpawner.SpawnEnemy(s.World, DummyDef, new GridPos(3, 2), new Idle());
            var c = UnitSpawner.SpawnEnemy(s.World, DummyDef, new GridPos(4, 2), new Idle());
            s.World.Events.Flush();
            Assert.That(Cast(s, "light_spear_thrust", new GridPos(2, 2)), Is.EqualTo(CastBlock.None));
            Assert.That((a.Hp, b.Hp, c.Hp), Is.EqualTo((46, 46, 50)), "2칸 관통, 3칸째는 안 닿음");
            Assert.That(s.Player.Mana, Is.EqualTo(8));
            Assert.That(Cast(s, "light_spear_throw", new GridPos(4, 2)), Is.EqualTo(CastBlock.None));
            Assert.That((a.Hp, b.Hp, c.Hp), Is.EqualTo((42, 46, 50)), "던지기 = 처음 만난 유닛");
            Assert.That(s.Player.Mana, Is.EqualTo(5));
        }

        // 창을 들면: 미리보기(CheckSpell)와 실제 피해가 같은 강화 값 — 4 × 150% = 6, 사거리 5 + 1 = 6
        [Test]
        public void HoldingSpear_BoostsDamageAndRange_PreviewEqualsCast()
        {
            var s = Start();
            var far = UnitSpawner.SpawnEnemy(s.World, DummyDef, new GridPos(7, 2), new Idle()); // 거리 6
            s.World.Events.Flush();
            Assert.That(SpellCasting.CheckSpell(s.World, s.Book, Targeting, Sight, s.Player, "light_spear_throw", far.Pos, out var plain, out _),
                Is.EqualTo(CastBlock.OutOfRange), "창 없으면 사거리 5");
            Assert.That(plain.Range, Is.EqualTo(5));

            HoldSpear(s);
            Assert.That(SpellCasting.CheckSpell(s.World, s.Book, Targeting, Sight, s.Player, "light_spear_throw", far.Pos, out var boosted, out _),
                Is.EqualTo(CastBlock.None));
            Assert.That((boosted.Range, boosted.Damage, boosted.ManaCost), Is.EqualTo((6, 6, 3)), "마나는 그대로");
            Assert.That(Cast(s, "light_spear_throw", far.Pos), Is.EqualTo(CastBlock.None));
            Assert.That(far.Hp, Is.EqualTo(50 - 6));

            // 찌르기: 사거리 2 → 3 (3칸째까지 관통), 피해 6
            var a = UnitSpawner.SpawnEnemy(s.World, DummyDef, new GridPos(2, 2), new Idle());
            var b = UnitSpawner.SpawnEnemy(s.World, DummyDef, new GridPos(3, 2), new Idle());
            var c = UnitSpawner.SpawnEnemy(s.World, DummyDef, new GridPos(4, 2), new Idle());
            s.World.Events.Flush();
            Assert.That(Cast(s, "light_spear_thrust", new GridPos(2, 2)), Is.EqualTo(CastBlock.None));
            Assert.That((a.Hp, b.Hp, c.Hp), Is.EqualTo((44, 44, 44)));
        }

        // 다른 무기(소검)는 강화하지 않는다 · 강화 필드가 없는 주문은 그대로 (같은 객체)
        [Test]
        public void OtherWeapon_OrPlainSpell_NotBoosted()
        {
            var s = Start();
            Inventory.TryAdd(s.World, s.Book, s.Items, s.Player, InventoryRef.Item("shortsword"));
            Weapons.Equip(s.World, s.Items, s.Player, System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("shortsword")));
            s.Book.TryGet("light_spear_throw", out var throwDef);
            Assert.That(SpellModifiers.For(s.Player, throwDef), Is.SameAs(throwDef));
            var fire = new SpellDef("fire_basic", Element.Fire, SpellShape.Bolt, 6, 5, 3);
            Assert.That(SpellModifiers.For(s.Player, fire), Is.SameAs(fire));
        }

        // 무속성: 중독된 적에 맞아도 반응 없음 (불이면 독화 +2) — 프로젝트 반응 표 그대로
        [Test]
        public void Neutral_TriggersNoReaction()
        {
            var statuses = SpellTests.BalanceStatuses();
            var reactions = AssetDatabase.LoadAssetAtPath<ReactionCatalogAsset>("Assets/_Project/Data/Reactions/ReactionCatalog.asset").ToBook(statuses);
            var s = Start(Book(reactions));
            var enemy = UnitSpawner.SpawnEnemy(s.World, DummyDef, new GridPos(3, 2), new Idle());
            statuses.TryGet(StatusType.Poison, out var poison);
            StatusEffects.Apply(s.World, enemy, poison);
            var triggered = new List<ReactionTriggeredEvent>();
            s.World.Events.Subscribe<ReactionTriggeredEvent>(triggered.Add);
            s.World.Events.Flush();
            int before = enemy.Hp;
            Assert.That(Cast(s, "light_spear_throw", enemy.Pos), Is.EqualTo(CastBlock.None));
            Assert.That(triggered, Is.Empty);
            Assert.That(before - enemy.Hp, Is.GreaterThanOrEqualTo(4).And.LessThanOrEqualTo(5), "4 (+ 같은 틱 중독 1), 독화 +2 없음");
        }

        // 무속성은 약점 · 저항을 받지 않는다 — 적 표에 넣으면 예외, 배수는 늘 100
        [Test]
        public void Neutral_IgnoresEnemyPercents()
        {
            var traits = new EnemyTraits("golem", damagePercent: new Dictionary<Element, int> { { Element.Fire, 0 } });
            Assert.That(traits.DamagePercent(Element.Neutral), Is.EqualTo(100));
            Assert.Throws<System.ArgumentException>(() => new EnemyTraits("x", damagePercent: new Dictionary<Element, int> { { Element.Neutral, 50 } }));
        }

        // 검산: BALANCE.md 21번 = SpellCatalog 두 줄 + CharacterCatalog 루멘 줄
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            Assert.That(spells.TryGet("light_spear_thrust", out var thrust), Is.True);
            Assert.That((thrust.Element, thrust.Shape, thrust.Range, thrust.Damage, thrust.ManaCost, thrust.AppliesStatus, thrust.LeavesTerrain),
                Is.EqualTo((Element.Neutral, SpellShape.Beam, 2, 4, 2, StatusType.None, TerrainEffectType.None)));
            Assert.That(spells.TryGet("light_spear_throw", out var toss), Is.True);
            Assert.That((toss.Element, toss.Shape, toss.Range, toss.Damage, toss.ManaCost), Is.EqualTo((Element.Neutral, SpellShape.Bolt, 5, 4, 3)));
            foreach (var def in new[] { thrust, toss })
                Assert.That((def.BoostWeaponId, def.BoostDamagePercent, def.BoostRange), Is.EqualTo(("spear", 150, 1)), def.Id);

            var characters = AssetDatabase.LoadAssetAtPath<CharacterCatalogAsset>("Assets/_Project/Data/Units/CharacterCatalog.asset");
            var lumen = new List<CharacterCatalogAsset.Entry>(characters.Characters).Find(c => c.Id == "hero_blond");
            Assert.That(lumen.ClassId, Is.EqualTo("lumen"));
            Assert.That(lumen.StartingSpells, Is.EqualTo(new[] { "light_spear_thrust", "light_spear_throw" }));
            var sia = new List<CharacterCatalogAsset.Entry>(characters.Characters).Find(c => c.Id == "hero_black");
            Assert.That(sia.ClassId, Is.EqualTo("sia"), "3-22a: 시아 직업 (Docs/직업_시아.md)");
        }
    }
}
