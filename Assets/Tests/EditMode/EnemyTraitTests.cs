using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;
using EntityId = ProjectM.Core.EntityId;

namespace ProjectM.Tests
{
    /// <summary>3-17b 적 행동 데이터 (결정 2026-11-04 — 구역당 4종 · 행동 다르게): 빠름 · 느림 · 명중 시 상태 · 맞으면 지형 · 속성 배수 · 원거리 주문 · 거리 벌림 (BALANCE.md 20번).</summary>
    public class EnemyTraitTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();

        private static readonly EnemyBook Book = new EnemyBook(new[]
        {
            new EnemyTraits("fast", moveSteps: 2),
            new EnemyTraits("slow", moveCostTicks: 2),
            new EnemyTraits("stinger", hitStatus: StatusType.Poison, hitStatusPercent: 100),
            new EnemyTraits("slime", hurtTerrain: TerrainEffectType.Water),
            new EnemyTraits("mummy", damagePercent: new Dictionary<Element, int> { { Element.Fire, 150 } }),
            new EnemyTraits("shaman", spellId: "enemy_bolt", keepDistance: 3),
        });

        private static SpellBook Spells(StatusBook statuses, TerrainBook terrains) =>
            new SpellBook(new[] { new SpellDef("enemy_bolt", Element.Fire, SpellShape.Bolt, 5, 2, 2), new SpellDef("fire", Element.Fire, SpellShape.Bolt, 6, 4, 0) }, statuses, terrains);

        private sealed class Run
        {
            public World World;
            public Unit Player;
            public MeleeChaserAi Ai;
        }

        // 한 줄 방: 플레이어 (1,1), 적은 at
        private static Run Start(string kind, GridPos at, int mana = 0)
        {
            var world = new World(GridMovementTests.Parse("############", "#..........#", "#..........#", "############"), 3UL);
            var statuses = SpellTests.BalanceStatuses();
            var terrains = SpellTests.BalanceTerrains();
            world.InstallCoreSystems(Sight, new MagicConfig(3, 1, 4), statuses, terrains);
            var spells = Spells(statuses, terrains);
            world.SetEnemyRules(new EnemyRules(Book, spells, statuses, terrains));
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 30, 3, 8), new GridPos(1, 1));
            var ai = new MeleeChaserAi(world, Config, Sight, statuses, Book, spells, Targeting);
            UnitSpawner.SpawnEnemy(world, new UnitDef(kind, 10, 2, 8, mana, accuracy: 100), at, ai);
            world.Events.Flush();
            return new Run { World = world, Player = player, Ai = ai };
        }

        private static Unit Enemy(World world)
        {
            foreach (var u in world.Units.ToSortedList()) if (u.Faction == Faction.Enemy) return u;
            return null;
        }

        private static ICommand Next(Run r) => r.Ai.NextCommand(new TickContext(0, Enemy(r.World).Id));

        [Test]
        public void Fast_MovesTwoCells_Slow_CostsTwoTicks()
        {
            var r = Start("fast", new GridPos(8, 1));
            var move = Next(r) as MoveCommand;
            Assert.That(move, Is.Not.Null);
            Assert.That(move.Then.HasValue, Is.True, "빠름: 둘째 칸");
            Assert.That(GridMovement.Chebyshev(move.Then.Value, r.Player.Pos), Is.EqualTo(5));

            var s = Start("slow", new GridPos(8, 1));
            var slow = Next(s) as MoveCommand;
            Assert.That(slow.CostTicks, Is.EqualTo(2));
            Assert.That(slow.Then.HasValue, Is.False);
        }

        [Test]
        public void Stinger_PoisonsOnHit()
        {
            var r = Start("stinger", new GridPos(2, 1));
            var attack = Next(r);
            Assert.That(attack, Is.InstanceOf<AttackCommand>());
            attack.Execute(new TickContext(0, Enemy(r.World).Id));
            Assert.That(r.Player.Hp, Is.LessThan(30));
            Assert.That(r.Player.HasStatus(StatusType.Poison), Is.True);
        }

        [Test]
        public void Slime_LeavesWaterWhenHurt()
        {
            var r = Start("slime", new GridPos(5, 1));
            var slime = Enemy(r.World);
            Combat.ApplyDamage(r.World, r.Player.Id, slime, 3, DamageCause.Attack);
            Assert.That(r.World.Terrain.Has(slime.Pos, TerrainEffectType.Water), Is.True);
        }

        // 약점: 불 주문 · 화상은 1.5배, 공격은 그대로
        [Test]
        public void Mummy_TakesExtraFireDamage()
        {
            var r = Start("mummy", new GridPos(5, 1));
            var mummy = Enemy(r.World);
            Combat.ApplyDamage(r.World, r.Player.Id, mummy, 4, DamageCause.Spell("fire"));
            Assert.That(mummy.Hp, Is.EqualTo(10 - 6));
            Combat.ApplyDamage(r.World, r.Player.Id, mummy, 2, DamageCause.Attack);
            Assert.That(mummy.Hp, Is.EqualTo(2));
            Combat.ApplyDamage(r.World, EntityId.None, mummy, 1, DamageCause.FromStatus(StatusType.Burn));
            Assert.That(mummy.Hp, Is.EqualTo(1), "화상 1 × 1.5 = 1 (내림)");
        }

        // 주술사: 사거리 안 · 마나 있음 = 주문, 가까우면 물러남, 마나 없으면 붙어 싸움
        [Test]
        public void Shaman_CastsKeepsDistance_ThenMelee()
        {
            var r = Start("shaman", new GridPos(5, 1), mana: 4);
            Assert.That(Next(r), Is.InstanceOf<CastSpellCommand>(), "거리 4 · 사거리 5");

            var far = Start("shaman", new GridPos(9, 1), mana: 4);
            Assert.That(Next(far), Is.InstanceOf<MoveCommand>(), "거리 8 — 다가감");

            var close = Start("shaman", new GridPos(2, 1), mana: 1);
            var step = Next(close) as MoveCommand;
            Assert.That(step, Is.Not.Null, "마나 1 < 비용 2 — 주문 못 씀, 거리 1 < 3이라 물러남");
            Assert.That(GridMovement.Chebyshev(step.To, close.Player.Pos), Is.EqualTo(2));

            var empty = Start("shaman", new GridPos(2, 1), mana: 0);
            Assert.That(Next(empty), Is.InstanceOf<AttackCommand>(), "마나 0 — 붙어 싸움");
        }

        // 검산: 1구역 적 4종 = BALANCE.md 20번
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            var book = catalog.ToEnemyBook();
            Assert.That(catalog.TryGetEnemy("enemy_rat", out var rat) && (rat.MaxHp, rat.AttackDamage, rat.Accuracy, rat.Evasion) == (8, 2, 8, 2), Is.True);
            Assert.That(book.TryGet("enemy_bunny", out var bunny) && bunny.MoveSteps == 2, Is.True);
            Assert.That(book.TryGet("enemy_slime", out var slime) && slime.HurtTerrain == TerrainEffectType.Water && slime.DamagePercent(Element.Fire) == 50, Is.True);
            Assert.That(book.TryGet("enemy_goblin_shaman", out var goblin) && goblin.SpellId == "enemy_fire_bolt" && goblin.KeepDistance == 3, Is.True);
            Assert.That(catalog.TryGetEnemy("enemy_goblin_shaman", out var g) && g.MaxMana == 4, Is.True);
            var zone1 = catalog.ToBook().ZoneOf(1);
            var ids = new List<string>();
            foreach (var e in zone1.Enemies) ids.Add(e.Def.VisualId + ":" + e.Weight);
            Assert.That(ids, Is.EqualTo(new[] { "enemy_rat:3", "enemy_bunny:2", "enemy_slime:2", "enemy_goblin_shaman:1" }));
            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            Assert.That(spells.TryGet("enemy_fire_bolt", out var bolt) && (bolt.Range, bolt.Damage, bolt.ManaCost) == (5, 2, 2), Is.True);
            Assert.That(spells, Is.Not.Null);
        }
    }
}
