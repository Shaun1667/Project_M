using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    public class ReactionTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10,
            new[] { "fire_basic", "ice_basic", "lightning_basic", "poison_basic" });
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 30, 2, 6); // 체력을 넉넉히 — 피해 합계만 본다
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        private const int Fire = 0, Ice = 1, Lightning = 2, Poison = 3;

        // REACTIONS.md 3-1 표 · BALANCE.md 8번
        internal static ReactionBook BalanceReactions(StatusBook statuses) => new ReactionBook(new[]
        {
            new ReactionDef("toxic_ignite", ReactionTrigger.Status, StatusType.Poison, TerrainEffectType.None, Element.Fire, 10,
                extraDamage: 2, group: "toxic_ignite"),
            new ReactionDef("toxic_burst", ReactionTrigger.Status, StatusType.Poison, TerrainEffectType.None, Element.Lightning, 20,
                explosionRadius: 1, explosionDamageFromRemaining: true),
            new ReactionDef("extinguish", ReactionTrigger.Terrain, StatusType.None, TerrainEffectType.FireZone, Element.Ice, 0,
                extraDamage: 2, consumeTrigger: true),
            new ReactionDef("toxic_ignite_cloud", ReactionTrigger.Terrain, StatusType.None, TerrainEffectType.PoisonCloud, Element.Fire, 10,
                extraDamage: 2, group: "toxic_ignite"),
            new ReactionDef("gas_explosion", ReactionTrigger.Terrain, StatusType.None, TerrainEffectType.PoisonCloud, Element.Lightning, 20,
                consumeTrigger: true, explosionRadius: 1, explosionDamage: 2, explosionStatus: StatusType.Poison, explosionStatusChancePercent: 70),
        }, new ReactionConfig(4, 3, 5), statuses);

        // 주문 자체는 상태 · 지형 없이 (반응만 보려고). 상태 · 지형 규칙은 BALANCE 값
        private static SpellBook Book(params SpellDef[] extra)
        {
            var statuses = SpellTests.BalanceStatuses();
            var terrains = new TerrainBook(new[]
            {
                new TerrainDef(TerrainEffectType.FireZone, 4, StatusType.Burn),
                new TerrainDef(TerrainEffectType.PoisonCloud, 4, StatusType.Poison),
            }, statuses);
            var spells = new List<SpellDef>
            {
                new SpellDef("fire_basic", Element.Fire, SpellShape.Bolt, 6, 5, 3),
                new SpellDef("ice_basic", Element.Ice, SpellShape.Bolt, 6, 3, 2),
                new SpellDef("lightning_basic", Element.Lightning, SpellShape.Beam, 5, 3, 4),
                new SpellDef("poison_basic", Element.Poison, SpellShape.Burst, 5, 2, 3, 1),
            };
            spells.AddRange(extra);
            return new SpellBook(spells, statuses, terrains, BalanceReactions(statuses));
        }

        private static GridMap Room() => GridMovementTests.Parse(
            "############",
            "#..........#",
            "#..........#",
            "#..........#",
            "############");

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private sealed class Session
        {
            public World World;
            public SpellBook Book;
            public TurnClock Clock;
            public Unit Player;
            public readonly List<Unit> Enemies = new List<Unit>();
            public readonly List<string> Reactions = new List<string>();
            public readonly List<UnitDamagedEvent> Hits = new List<UnitDamagedEvent>();

            public void Cast(int slot, GridPos target)
            {
                var block = SpellCasting.TryCreateCast(World, Config, Book, Targeting, Sight, slot, target, out var command);
                Assert.That(block, Is.EqualTo(CastBlock.None));
                Reactions.Clear();
                Hits.Clear();
                Clock.SubmitPlayerCommand(command);
            }

            /// <summary>시전자(플레이어)가 준 피해만 — ④ 지속 피해는 공격자가 없다.</summary>
            public List<int> HitsOn(Unit unit) =>
                Hits.FindAll(h => h.Target == unit.Id && h.Attacker == Player.Id).ConvertAll(h => h.Amount);

            public void Poison(Unit unit)
            {
                Book.Statuses.TryGet(StatusType.Poison, out var def);
                StatusEffects.Apply(World, unit, def);
            }

            public void Place(GridPos pos, TerrainEffectType type)
            {
                Book.Terrains.TryGet(type, out var def);
                TerrainEffects.Place(World, pos, def);
            }
        }

        private static Session Start(SpellBook book, GridPos player, params GridPos[] enemies)
        {
            var world = new World(Room(), 5UL);
            var s = new Session { World = world, Book = book, Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            world.InstallCoreSystems(Sight, Magic, book.Statuses, book.Terrains);
            world.Events.Subscribe<ReactionTriggeredEvent>(e => s.Reactions.Add(e.ReactionId));
            world.Events.Subscribe<UnitDamagedEvent>(s.Hits.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, player, Magic.SpellSlotCount);
            foreach (var p in enemies) s.Enemies.Add(UnitSpawner.SpawnEnemy(world, EnemyDef, p, new IdleAi()));
            world.Events.Flush();
            return s;
        }

        // #1 독화: 중독된 대상에 불 → 추가 피해 2, 중독은 남는다
        [Test]
        public void ToxicIgnite_PoisonedTargetHitByFire()
        {
            var s = Start(Book(), new GridPos(1, 2), new GridPos(4, 2));
            var enemy = s.Enemies[0];
            s.Poison(enemy);
            s.Cast(Fire, enemy.Pos);
            Assert.That(s.Reactions, Is.EqualTo(new[] { "toxic_ignite" }));
            Assert.That(s.HitsOn(enemy), Is.EqualTo(new[] { 5, 2 }), "주문 5 → 반응 2");
            Assert.That(enemy.HasStatus(StatusType.Poison), Is.True, "소모 안 함");
        }

        // #2 유독성 폭발: 중독된 대상에 번개 → 대상 중심 반지름 1, 피해 = 남은 중독 틱 (상한 max(3, 5) = 5)
        [Test]
        public void ToxicBurst_ExplodesByRemainingPoison()
        {
            var s = Start(Book(), new GridPos(1, 2), new GridPos(4, 2), new GridPos(5, 3), new GridPos(8, 3));
            var target = s.Enemies[0];
            s.Poison(target); // 5틱
            s.Cast(Lightning, target.Pos);
            Assert.That(s.Reactions, Is.EqualTo(new[] { "toxic_burst" }));
            Assert.That(s.HitsOn(target), Is.EqualTo(new[] { 3, 5 }), "번개 3 + 폭발 5");
            Assert.That(s.HitsOn(s.Enemies[1]), Is.EqualTo(new[] { 5 }), "옆 칸 (5,3)은 폭발만");
            Assert.That(s.HitsOn(s.Enemies[2]), Is.Empty, "반지름 밖");
            Assert.That(target.HasStatus(StatusType.Poison), Is.True, "소모 안 함");
        }

        [Test]
        public void Explosion_AlsoHitsCasterInRange()
        {
            var s = Start(Book(), new GridPos(1, 2), new GridPos(2, 2));
            s.Poison(s.Enemies[0]);
            s.Cast(Lightning, s.Enemies[0].Pos);
            Assert.That(s.Hits.Exists(h => h.Target == s.Player.Id && h.Amount == 5), Is.True, "바로 옆이면 나도 폭발에 맞음");
        }

        // #11 가스 폭발: 번개가 지나간 독구름 칸마다 폭발, 구름 제거. 폭발이 겹치면 대상당 1회
        [Test]
        public void GasExplosion_ConsumesCloud_OverlapHitsOnce()
        {
            var s = Start(Book(), new GridPos(1, 2), new GridPos(6, 3));
            for (int x = 5; x <= 7; x++)
                for (int y = 1; y <= 3; y++)
                    s.Place(new GridPos(x, y), TerrainEffectType.PoisonCloud);

            s.Cast(Lightning, new GridPos(4, 2)); // 광선 (2,2)~(6,2) — 구름 칸 (5,2) · (6,2)
            Assert.That(s.Reactions, Is.EqualTo(new[] { "gas_explosion", "gas_explosion" }));
            Assert.That(s.World.Terrain.Has(new GridPos(5, 2), TerrainEffectType.PoisonCloud), Is.False);
            Assert.That(s.World.Terrain.Has(new GridPos(6, 2), TerrainEffectType.PoisonCloud), Is.False);
            Assert.That(s.World.Terrain.Has(new GridPos(7, 2), TerrainEffectType.PoisonCloud), Is.True, "광선이 안 닿은 칸은 남음");
            Assert.That(s.HitsOn(s.Enemies[0]), Is.EqualTo(new[] { 2 }), "두 폭발 범위에 다 있어도 1회");
        }

        // 독화는 상태(#1) · 지형(#10) 한 그룹 — 중독된 채 독구름 위에 있어도 +2 한 번
        [Test]
        public void ToxicIgnite_StatusAndCloud_SameGroupOnce()
        {
            var s = Start(Book(), new GridPos(1, 2), new GridPos(4, 2));
            var enemy = s.Enemies[0];
            s.Poison(enemy);
            s.Place(enemy.Pos, TerrainEffectType.PoisonCloud);
            s.Cast(Fire, enemy.Pos);
            Assert.That(s.Reactions, Is.EqualTo(new[] { "toxic_ignite" }), "상태 먼저, 지형 쪽은 같은 그룹이라 생략");
            Assert.That(s.HitsOn(enemy), Is.EqualTo(new[] { 5, 2 }));
        }

        // #8 소화: 화염 지대 위 대상에 얼음 → +2 후 지대 제거 (화염 지대는 지금 게임에서 안 생기므로 직접 놓음)
        [Test]
        public void Extinguish_IceOnFireZone()
        {
            var s = Start(Book(), new GridPos(1, 2), new GridPos(4, 2));
            var enemy = s.Enemies[0];
            s.Place(enemy.Pos, TerrainEffectType.FireZone);
            s.Cast(Ice, enemy.Pos);
            Assert.That(s.Reactions, Is.EqualTo(new[] { "extinguish" }));
            Assert.That(s.HitsOn(enemy), Is.EqualTo(new[] { 3, 2 }));
            Assert.That(s.World.Terrain.CellCount, Is.EqualTo(0));
        }

        // 주문당 반응 최대 4회
        [Test]
        public void ReactionsPerCast_CappedAtFour()
        {
            var s = Start(Book(), new GridPos(1, 2), new GridPos(10, 3));
            for (int x = 2; x <= 6; x++) s.Place(new GridPos(x, 2), TerrainEffectType.PoisonCloud);
            s.Cast(Lightning, new GridPos(2, 2)); // 광선이 구름 5칸을 지남
            Assert.That(s.Reactions.Count, Is.EqualTo(4));
            Assert.That(s.World.Terrain.Has(new GridPos(6, 2), TerrainEffectType.PoisonCloud), Is.True, "5번째는 발동 안 함");
        }

        // 우선순위 같으면 상태 반응 먼저 (유독성 폭발 20 · 가스 폭발 20)
        [Test]
        public void SamePriority_StatusBeforeTerrain()
        {
            var s = Start(Book(), new GridPos(1, 2), new GridPos(3, 2));
            var enemy = s.Enemies[0];
            s.Place(new GridPos(2, 2), TerrainEffectType.PoisonCloud); // 광선 첫 칸
            s.Poison(enemy);
            s.Cast(Lightning, enemy.Pos);
            Assert.That(s.Reactions, Is.EqualTo(new[] { "toxic_burst", "gas_explosion" }));
        }

        // 연쇄 금지: 폭발이 중독된 이웃에 닿아도 그 이웃의 유독성 폭발은 안 일어남
        [Test]
        public void ReactionsDoNotChain()
        {
            var s = Start(Book(), new GridPos(1, 2), new GridPos(4, 2), new GridPos(4, 3));
            s.Poison(s.Enemies[0]);
            s.Poison(s.Enemies[1]); // 광선 밖, 폭발 범위 안
            s.Cast(Lightning, s.Enemies[0].Pos);
            Assert.That(s.Reactions, Is.EqualTo(new[] { "toxic_burst" }));
            Assert.That(s.HitsOn(s.Enemies[1]), Is.EqualTo(new[] { 5 }));
        }

        // 근접 공격은 반응을 일으키지 않는다 (4-6)
        [Test]
        public void MeleeDoesNotReact()
        {
            var s = Start(Book(), new GridPos(1, 2), new GridPos(2, 2));
            s.Poison(s.Enemies[0]);
            s.Reactions.Clear();
            Assert.That(PlayerActions.TryStep(s.World, Config, new GridPos(1, 0), out var attack), Is.True);
            s.Clock.SubmitPlayerCommand(attack);
            Assert.That(s.Reactions, Is.Empty);
        }

        // 순서 4-1: 원래 피해 → 반응 → 새 상태. 주문이 새로 거는 상태로는 그 주문에서 반응하지 않는다
        [Test]
        public void NewStatusFromSameCast_DoesNotReact()
        {
            var book = Book(new SpellDef("fire_poison", Element.Fire, SpellShape.Bolt, 6, 1, 0, 0, StatusType.Poison, 100));
            var s = Start(book, new GridPos(1, 2), new GridPos(4, 2));
            s.Player.SpellSlots[0] = "fire_poison";
            s.Cast(0, s.Enemies[0].Pos);
            Assert.That(s.Reactions, Is.Empty, "첫 시전: 중독은 반응 뒤에 걸림");
            s.Cast(0, s.Enemies[0].Pos);
            Assert.That(s.Reactions, Is.EqualTo(new[] { "toxic_ignite" }), "두 번째: 이미 중독");
        }

        // 상한: 추가 피해 = min(반응 값, max(주문 피해, 3))
        [Test]
        public void ExtraDamage_IsCapped()
        {
            var statuses = SpellTests.BalanceStatuses();
            var reactions = new ReactionBook(new[]
            {
                new ReactionDef("big", ReactionTrigger.Status, StatusType.Poison, TerrainEffectType.None, Element.Ice, 10, extraDamage: 10),
            }, new ReactionConfig(4, 3, 5), statuses);
            var book = new SpellBook(new[] { new SpellDef("ice_basic", Element.Ice, SpellShape.Bolt, 6, 1, 0) }, statuses, null, reactions);
            var s = Start(book, new GridPos(1, 2), new GridPos(4, 2));
            s.Player.SpellSlots[1] = "ice_basic";
            s.Poison(s.Enemies[0]);
            s.Cast(Ice, s.Enemies[0].Pos);
            Assert.That(s.HitsOn(s.Enemies[0]), Is.EqualTo(new[] { 1, 3 }), "주문 피해 1 → 상한 3");
        }

        [Test]
        public void Book_RejectsDuplicatesAndMissingStatus()
        {
            var statuses = SpellTests.BalanceStatuses();
            var cfg = new ReactionConfig(4, 3, 5);
            Assert.Throws<ArgumentException>(() => new ReactionBook(new[]
            {
                new ReactionDef("a", ReactionTrigger.Status, StatusType.Poison, TerrainEffectType.None, Element.Fire, 1),
                new ReactionDef("b", ReactionTrigger.Status, StatusType.Poison, TerrainEffectType.None, Element.Fire, 1),
            }, cfg, statuses), "같은 대상 · 속성");
            Assert.Throws<ArgumentException>(() => new ReactionBook(new[]
            {
                new ReactionDef("a", ReactionTrigger.Terrain, StatusType.None, TerrainEffectType.PoisonCloud, Element.Fire, 1,
                    explosionRadius: 1, explosionStatus: StatusType.Poison),
            }, cfg, new StatusBook(new StatusDef[0])), "StatusBook에 없는 상태");
            Assert.Throws<ArgumentException>(() => new ReactionDef("x", ReactionTrigger.Status, StatusType.None, TerrainEffectType.None, Element.Fire, 1));
        }

        // 검산: ReactionCatalog 에셋이 BALANCE.md 8번 (REACTIONS.md 3-1 표)와 같은지
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ReactionCatalogAsset>("Assets/_Project/Data/Reactions/ReactionCatalog.asset");
            Assert.That(asset, Is.Not.Null, "ReactionCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var statuses = SpellTests.BalanceStatuses();
            var book = asset.ToBook(statuses);
            var expected = BalanceReactions(statuses);

            Assert.That((book.Config.MaxReactionsPerCast, book.Config.ExtraDamageCapMin, book.Config.ExplosionDamageCapMin),
                Is.EqualTo((expected.Config.MaxReactionsPerCast, expected.Config.ExtraDamageCapMin, expected.Config.ExplosionDamageCapMin)));
            Assert.That(book.Count, Is.EqualTo(expected.Count));
            foreach (var e in expected.All)
            {
                var a = ((List<ReactionDef>)new List<ReactionDef>(book.All)).Find(r => r.Id == e.Id);
                Assert.That(a, Is.Not.Null, e.Id);
                Assert.That((a.Group, a.Trigger, a.Status, a.Terrain, a.Element, a.Priority, a.ExtraDamage, a.ConsumeTrigger,
                        a.ExplosionRadius, a.ExplosionDamage, a.ExplosionDamageFromRemaining, a.ExplosionStatus, a.ExplosionStatusChancePercent),
                    Is.EqualTo((e.Group, e.Trigger, e.Status, e.Terrain, e.Element, e.Priority, e.ExtraDamage, e.ConsumeTrigger,
                        e.ExplosionRadius, e.ExplosionDamage, e.ExplosionDamageFromRemaining, e.ExplosionStatus, e.ExplosionStatusChancePercent)), e.Id);
            }
        }
    }
}
