using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    public class SpellTests
    {
        // BALANCE.md 5번과 같은 값. 주문 모양 · 피해만 보는 테스트는 상태 이상 없이 (상태는 StatusTests)
        private static SpellBook BalanceBook() => new SpellBook(new[]
        {
            new SpellDef("fire_basic", Element.Fire, SpellShape.Bolt, 6, 5, 3),
            new SpellDef("ice_basic", Element.Ice, SpellShape.Bolt, 6, 3, 2),
            new SpellDef("lightning_basic", Element.Lightning, SpellShape.Beam, 5, 3, 4),
            new SpellDef("poison_basic", Element.Poison, SpellShape.Burst, 5, 2, 3, 1),
        });

        // BALANCE.md 5번 · 6번 전체 (상태 이상 포함) — 에셋 검산용
        internal static ProjectM.Status.StatusBook BalanceStatuses() => new ProjectM.Status.StatusBook(new[]
        {
            new ProjectM.Status.StatusDef(ProjectM.Status.StatusType.Burn, 3, damagePerTick: 1),
            new ProjectM.Status.StatusDef(ProjectM.Status.StatusType.Slow, 3, extraActionCostTicks: 1),
            new ProjectM.Status.StatusDef(ProjectM.Status.StatusType.Poison, 5, damagePerTick: 1),
            new ProjectM.Status.StatusDef(ProjectM.Status.StatusType.Paralyze, 2, preventsAction: true),
            new ProjectM.Status.StatusDef(ProjectM.Status.StatusType.Wet, 3), // 3-2
            new ProjectM.Status.StatusDef(ProjectM.Status.StatusType.Frozen, 2, preventsAction: true), // 3-3a
        });

        // 상태 동작 시험용: 확률 100%로 고정 (실제 수치는 BalanceBookFull — 3-1부터 70%)
        internal static SpellBook BalanceBookWithStatuses() => new SpellBook(new[]
        {
            new SpellDef("fire_basic", Element.Fire, SpellShape.Bolt, 6, 5, 3, 0, ProjectM.Status.StatusType.Burn, 100),
            new SpellDef("ice_basic", Element.Ice, SpellShape.Bolt, 6, 3, 2, 0, ProjectM.Status.StatusType.Slow, 100),
            new SpellDef("lightning_basic", Element.Lightning, SpellShape.Beam, 5, 3, 4, 0, ProjectM.Status.StatusType.Paralyze, 30),
            new SpellDef("poison_basic", Element.Poison, SpellShape.Burst, 5, 2, 3, 1, ProjectM.Status.StatusType.Poison, 100),
        }, BalanceStatuses());

        // BALANCE.md 7번 지형 효과
        internal static ProjectM.Status.TerrainBook BalanceTerrains() => new ProjectM.Status.TerrainBook(new[]
        {
            new ProjectM.Status.TerrainDef(ProjectM.Status.TerrainEffectType.FireZone, 4, ProjectM.Status.StatusType.Burn),
            new ProjectM.Status.TerrainDef(ProjectM.Status.TerrainEffectType.PoisonCloud, 4, ProjectM.Status.StatusType.Poison),
            // 3-2: 물 타일 12틱 · 화염 지대 위 안 깔림, 오염된 물 8틱 · 젖음 + 피해 1
            new ProjectM.Status.TerrainDef(ProjectM.Status.TerrainEffectType.Water, 12, ProjectM.Status.StatusType.Wet,
                blockedBy: ProjectM.Status.TerrainEffectType.FireZone),
            new ProjectM.Status.TerrainDef(ProjectM.Status.TerrainEffectType.ContaminatedWater, 8, ProjectM.Status.StatusType.Wet, damagePerTick: 1),
            // 3-3a: 빙판 8틱 · 둔화
            new ProjectM.Status.TerrainDef(ProjectM.Status.TerrainEffectType.IcePatch, 8, ProjectM.Status.StatusType.Slow),
        }, BalanceStatuses());

        // BALANCE.md 5 · 6 · 7번 전체 (상태 + 지형) — 에셋 검산 · 지형 · 반응 테스트용. reactions = 반응 표 (없으면 반응 없음)
        internal static SpellBook BalanceBookFull(System.Func<ProjectM.Status.StatusBook, ProjectM.Status.ReactionBook> reactions = null)
        {
            var statuses = BalanceStatuses();
            var terrains = BalanceTerrains();
            return new SpellBook(new[]
            {
                // 3-1: 화상 · 둔화 · 중독 70% (REACTIONS.md D3), 불은 화염 지대를 남기지 않음 (D2)
                new SpellDef("fire_basic", Element.Fire, SpellShape.Bolt, 6, 5, 3, 0, ProjectM.Status.StatusType.Burn, 70),
                new SpellDef("ice_basic", Element.Ice, SpellShape.Bolt, 6, 3, 2, 0, ProjectM.Status.StatusType.Slow, 70),
                new SpellDef("lightning_basic", Element.Lightning, SpellShape.Beam, 5, 3, 4, 0, ProjectM.Status.StatusType.Paralyze, 30),
                new SpellDef("poison_basic", Element.Poison, SpellShape.Burst, 5, 2, 3, 1, ProjectM.Status.StatusType.Poison, 70,
                    ProjectM.Status.TerrainEffectType.PoisonCloud),
                // 3-2 물 (원본 6-1): 3×3, 피해 0, 마나 2, 사거리 5, 젖음 100%, 물 타일
                new SpellDef("water_basic", Element.Water, SpellShape.Burst, 5, 0, 2, 1, ProjectM.Status.StatusType.Wet, 100,
                    ProjectM.Status.TerrainEffectType.Water),
            }, statuses, terrains, reactions?.Invoke(statuses));
        }

        private const int Fire = 0, Ice = 1, Lightning = 2, Poison = 3;

        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10,
            new[] { "fire_basic", "ice_basic", "lightning_basic", "poison_basic" });
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 8, 2, 6);
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        // y3 · y2 · y1 바닥, (5,1)은 기둥
        private static GridMap Room() => GridMovementTests.Parse(
            "############",
            "#..........#",
            "#..........#",
            "#....#.....#",
            "############");

        private sealed class Session
        {
            public World World;
            public SpellBook Book;
            public TurnClock Clock;
            public Unit Player;
            public readonly List<Unit> Enemies = new List<Unit>();
            public readonly List<SpellCastEvent> Casts = new List<SpellCastEvent>();

            public CastBlock Cast(int slot, GridPos target)
            {
                var block = SpellCasting.TryCreateCast(World, Config, Book, Targeting, Sight, slot, target, out var command);
                if (block == CastBlock.None) Clock.SubmitPlayerCommand(command);
                return block;
            }
        }

        // 적은 AI 없이 제자리 (피해 계산만 보려고) — 대기만 하는 행위자
        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static Session Start(GridPos player, params GridPos[] enemies)
        {
            var world = new World(Room(), 9UL);
            var s = new Session { World = world, Book = BalanceBook(), Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            world.InstallCoreSystems(Sight, Magic, s.Book.Statuses, s.Book.Terrains);
            world.Events.Subscribe<SpellCastEvent>(s.Casts.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, player, Magic.QuickSlotCount);
            foreach (var p in enemies) s.Enemies.Add(UnitSpawner.SpawnEnemy(world, EnemyDef, p, new IdleAi()));
            world.Events.Flush();
            return s;
        }

        [Test]
        public void Bolt_HitsFirstUnitOnLine_SpendsMana()
        {
            var s = Start(new GridPos(1, 2), new GridPos(4, 2), new GridPos(6, 2));
            Assert.That(s.Cast(Fire, new GridPos(6, 2)), Is.EqualTo(CastBlock.None));

            Assert.That(s.Enemies[0].Hp, Is.EqualTo(8 - 5), "앞의 적이 막아서 맞음");
            Assert.That(s.Enemies[1].Hp, Is.EqualTo(8), "뒤의 적은 안 맞음");
            Assert.That(s.Player.Mana, Is.EqualTo(10 - 3));
            Assert.That(s.Casts.Count, Is.EqualTo(1));
            Assert.That(s.Casts[0].Preview.AffectedCells, Is.EqualTo(new List<GridPos> { new GridPos(4, 2) }));
        }

        [Test]
        public void Beam_PiercesAllUnitsToRange()
        {
            var s = Start(new GridPos(1, 2), new GridPos(3, 2), new GridPos(5, 2), new GridPos(7, 2));
            Assert.That(s.Cast(Lightning, new GridPos(3, 2)), Is.EqualTo(CastBlock.None), "가까운 적을 조준해도 사거리 끝까지");

            Assert.That(s.Enemies[0].Hp, Is.EqualTo(5));
            Assert.That(s.Enemies[1].Hp, Is.EqualTo(5));
            Assert.That(s.Enemies[2].Hp, Is.EqualTo(8), "(7,2)는 사거리 5 밖");
            Assert.That(s.Player.Mana, Is.EqualTo(6));
        }

        [Test]
        public void Burst_HitsAreaAroundImpact_IncludingCaster()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 3), new GridPos(6, 2), new GridPos(9, 2));
            Assert.That(s.Cast(Poison, new GridPos(6, 2)), Is.EqualTo(CastBlock.None));
            Assert.That(s.Enemies[0].Hp, Is.EqualTo(6), "(5,3)은 폭발 범위 안");
            Assert.That(s.Enemies[1].Hp, Is.EqualTo(6), "맞은 대상");
            Assert.That(s.Enemies[2].Hp, Is.EqualTo(8), "범위 밖");
            Assert.That(s.Player.Hp, Is.EqualTo(20));

            // 바로 옆 칸에 터뜨리면 시전자도 맞는다 (규칙 10)
            Assert.That(s.Cast(Poison, new GridPos(2, 2)), Is.EqualTo(CastBlock.None));
            Assert.That(s.Player.Hp, Is.EqualTo(18));
        }

        [Test]
        public void Bolt_StopsBeforeWall()
        {
            var s = Start(new GridPos(1, 1), new GridPos(7, 1));
            var preview = SpellCasting.Preview(s.World, Targeting, s.Player.Pos, s.Book.TryGet("fire_basic", out var f) ? f : null, new GridPos(4, 1));
            Assert.That(preview.Path, Is.EqualTo(new List<GridPos> { new GridPos(2, 1), new GridPos(3, 1), new GridPos(4, 1) }));
            Assert.That(preview.AffectedCells, Is.Empty, "빈 칸에 쏘면 아무도 안 맞음");
        }

        [Test]
        public void Check_ReportsWhyCastIsBlocked()
        {
            var s = Start(new GridPos(1, 1), new GridPos(7, 1), new GridPos(9, 3));
            CastBlock Check(int slot, GridPos target) =>
                SpellCasting.Check(s.World, s.Book, Targeting, Sight, s.Player, slot, target, out _, out _);

            Assert.That(Check(Fire, new GridPos(1, 1)), Is.EqualTo(CastBlock.TargetIsSelf));
            Assert.That(Check(Fire, new GridPos(8, 3)), Is.EqualTo(CastBlock.OutOfRange), "거리 7 > 사거리 6");
            Assert.That(Check(Fire, new GridPos(6, 1)), Is.EqualTo(CastBlock.NotVisible), "기둥 (5,1) 뒤");
            Assert.That(Check(Fire, new GridPos(3, 2)), Is.EqualTo(CastBlock.None));

            Mana.TrySpend(s.World, s.Player, 9);
            Assert.That(Check(Fire, new GridPos(3, 2)), Is.EqualTo(CastBlock.NotEnoughMana));

            s.Player.QuickSlots[Fire] = InventoryRef.Empty;
            Assert.That(Check(Fire, new GridPos(3, 2)), Is.EqualTo(CastBlock.EmptySlot));
            s.Player.InventorySlots[0] = s.Player.QuickSlots[Fire] = InventoryRef.Spell("no_such_spell"); // 데이터에서 빠진 주문
            Assert.That(Check(Fire, new GridPos(3, 2)), Is.EqualTo(CastBlock.UnknownSpell));
        }

        [Test]
        public void Cast_TakesATurn_AndKillsAtZero()
        {
            var s = Start(new GridPos(1, 2), new GridPos(3, 2));
            long tick = s.World.Scheduler.CurrentTick;
            s.Cast(Fire, new GridPos(3, 2));  // 8 → 3
            Assert.That(s.World.Scheduler.CurrentTick, Is.EqualTo(tick + 1), "기본 행동 비용 1틱");
            s.Cast(Fire, new GridPos(3, 2));  // 3 → 0
            Assert.That(s.World.Units.TryGet(s.Enemies[0].Id, out _), Is.False, "⑥ 사망 판정에서 제거");
        }

        [Test]
        public void Defs_RejectInvalidValues()
        {
            Assert.Throws<ArgumentException>(() => new SpellDef("", Element.Fire, SpellShape.Bolt, 1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellDef("a", Element.Fire, SpellShape.Bolt, 0, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellDef("a", Element.Fire, SpellShape.Burst, 3, 1, 1, 0), "폭발은 반지름 1 이상");
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellDef("a", Element.Fire, SpellShape.Bolt, 3, 1, 1, 1), "투사체는 반지름 0");
            Assert.Throws<ArgumentException>(() => new SpellBook(new[]
            {
                new SpellDef("a", Element.Fire, SpellShape.Bolt, 1, 1, 1), new SpellDef("a", Element.Ice, SpellShape.Bolt, 1, 1, 1),
            }), "같은 ID");
        }

        // 검산: 프로젝트 에셋이 BALANCE.md 5번 표와 같은 값인지. BALANCE.md를 고치면 같이 고친다.
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset");
            var statusAsset = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset");
            Assert.That(asset, Is.Not.Null, "SpellCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            Assert.That(statusAsset, Is.Not.Null, "StatusCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var terrainAsset = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset");
            Assert.That(terrainAsset, Is.Not.Null, "TerrainCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var statuses = statusAsset.ToBook();
            var book = asset.ToBook(statuses, terrainAsset.ToBook(statuses));
            var expected = BalanceBookFull();
            Assert.That(book.Count, Is.EqualTo(expected.Count));
            foreach (var id in new[] { "fire_basic", "ice_basic", "lightning_basic", "poison_basic", "water_basic" })
            {
                Assert.That(book.TryGet(id, out var a), Is.True, id);
                expected.TryGet(id, out var e);
                Assert.That((a.Element, a.Shape, a.Range, a.Damage, a.ManaCost, a.Radius, a.AppliesStatus, a.StatusChancePercent, a.LeavesTerrain),
                    Is.EqualTo((e.Element, e.Shape, e.Range, e.Damage, e.ManaCost, e.Radius, e.AppliesStatus, e.StatusChancePercent, e.LeavesTerrain)), id);
            }
        }
    }
}
