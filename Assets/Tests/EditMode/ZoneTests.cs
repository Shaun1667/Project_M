using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Magic;
using ProjectM.Units;
using UnityEditor;
using EntityId = ProjectM.Core.EntityId;

namespace ProjectM.Tests
{
    /// <summary>3-17a 구역 (결정 2026-11-04 — 5구역 · 25층, SPD): 구역 표 · 세기 · 적 종류 뽑기 · 중간 보스 층(봉인된 계단) · 마지막 보스 = 승리 (BALANCE.md 20번).</summary>
    public class ZoneTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8);
        private static readonly UnitDef Rat = new UnitDef("rat", 8, 2, 6, accuracy: 8, evasion: 2);
        private static readonly UnitDef Bunny = new UnitDef("bunny", 6, 1, 6);
        private static readonly UnitDef BossDef = new UnitDef("boss", 60, 5, 10);
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        // 2구역 4층: 1~2층(보스 2층) · 3~4층(세기 200%, 보스 4층)
        private static ZoneBook Book() => new ZoneBook(new[]
        {
            new ZoneDef("a", 1, 2, new[] { new ZoneEnemy(Rat, 3), new ZoneEnemy(Bunny, 1) }, 100, BossDef),
            new ZoneDef("b", 3, 4, new[] { new ZoneEnemy(Rat, 1) }, 200, BossDef),
        });

        [Test]
        public void Book_ZonesBossFloorsAndScaling()
        {
            var book = Book();
            Assert.That(book.FloorCount, Is.EqualTo(4));
            Assert.That(book.ZoneOf(1).Name, Is.EqualTo("a"));
            Assert.That(book.ZoneOf(3).Name, Is.EqualTo("b"));
            Assert.That(book.IsBossFloor(2) && book.IsBossFloor(4) && !book.IsBossFloor(1) && !book.IsBossFloor(3), Is.True);
            var strong = book.ZoneOf(3).Enemies[0].Def;
            Assert.That((strong.MaxHp, strong.AttackDamage, strong.Accuracy, strong.Evasion), Is.EqualTo((16, 4, 16, 4)), "200%");
            Assert.That(book.BossOf(4).MaxHp, Is.EqualTo(120));
            Assert.Throws<System.ArgumentException>(() => new ZoneBook(new[] { new ZoneDef("x", 2, 3, new[] { new ZoneEnemy(Rat, 1) }) }), "1층부터");
        }

        // 무게대로 뽑힌다 (쥐 3 : 토끼 1) — 같은 시드 = 같은 결과
        [Test]
        public void PickEnemy_FollowsWeights_Deterministic()
        {
            var book = Book();
            var first = new List<string>();
            int rats = 0;
            var rng = new RngSet(7UL).Get(RngStream.EnemyKind);
            for (int i = 0; i < 400; i++)
            {
                var d = book.PickEnemy(1, rng);
                if (d.VisualId == "rat") rats++;
                if (i < 10) first.Add(d.VisualId);
            }
            Assert.That(rats, Is.InRange(260, 340));
            var again = new RngSet(7UL).Get(RngStream.EnemyKind);
            for (int i = 0; i < 10; i++) Assert.That(book.PickEnemy(1, again).VisualId, Is.EqualTo(first[i]));
        }

        private static (World World, FloorTransition Stairs, Unit Player, List<RunEndedEvent> Ends) Start()
        {
            var dungeon = new DungeonGenConfig(50, 50, 10, 20, 7, 20, 10, 3, 5, 1000, 10, floorCount: 4);
            var generator = new DungeonGenerator(dungeon);
            DungeonFloor floor = null;
            var world = World.Create(11UL, rng => (floor = generator.Generate(rng, stairsUp: false, stairsDown: true)).Map);
            var vision = world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var spawn = new SpawnConfig(5, 8);
            var idle = new IdleAi();
            var stairs = new FloorTransition(world, generator, 4, Rat, spawn, (w, id) => idle, vision, zones: Book());
            var ends = new List<RunEndedEvent>();
            world.Events.Subscribe<RunEndedEvent>(ends.Add);
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, floor.Start, Magic.QuickSlotCount);
            vision.Refresh();
            world.Events.Flush();
            world.Units.Teleport(player.Id, floor.Exit);
            Assert.That(stairs.TryUse(), Is.True, "1 → 2층");
            world.Events.Flush();
            return (world, stairs, player, ends);
        }

        private static void KillBoss(World world)
        {
            Assert.That(world.Units.TryGet(world.Run.BossId, out var boss), Is.True);
            Combat.ApplyDamage(world, EntityId.None, boss, 9999);
            new TurnClock(world.Scheduler, world.PlayerInput).SubmitPlayerCommand(PlayerActions.Wait(Config));
            world.Events.Flush();
        }

        // 중간 보스 층: 봉인된 계단(벽) → 보스가 죽으면 내려가는 계단, 승리 아님
        [Test]
        public void MidBoss_UnsealsStairs_NotVictory()
        {
            var (world, _, _, ends) = Start();
            Assert.That(world.Floor, Is.EqualTo(2));
            var sealedPos = world.Map.Find(TileType.SealedStairs);
            Assert.That(sealedPos.HasValue, Is.True);
            Assert.That(world.Map.IsWalkable(sealedPos.Value), Is.False);
            Assert.That(world.Map.Find(TileType.StairsDown).HasValue, Is.False);
            var unsealed = new List<StairsUnsealedEvent>();
            world.Events.Subscribe<StairsUnsealedEvent>(unsealed.Add);
            KillBoss(world);
            Assert.That(world.Map.Get(sealedPos.Value), Is.EqualTo(TileType.StairsDown));
            Assert.That(unsealed.Count, Is.EqualTo(1));
            Assert.That(ends, Is.Empty, "승리 아님");
            Assert.That(world.Run.Outcome, Is.EqualTo(RunOutcome.None));
        }

        // 열린 계단으로 3층(새 구역, 200% 적) → 4층 마지막 보스 = 승리
        [Test]
        public void LastBoss_IsVictory_AndZoneEnemiesScaled()
        {
            var (world, stairs, player, ends) = Start();
            KillBoss(world);
            world.Units.Teleport(player.Id, world.Map.Find(TileType.StairsDown).Value);
            Assert.That(stairs.TryUse(), Is.True, "2 → 3층");
            world.Events.Flush();
            int enemies = 0;
            foreach (var u in world.Units.ToSortedList())
                if (u.Faction == Faction.Enemy) { enemies++; Assert.That(u.MaxHp, Is.EqualTo(16), "3층 = 구역 b · 200%"); }
            Assert.That(enemies, Is.GreaterThan(0));
            world.Units.Teleport(player.Id, world.Map.Find(TileType.StairsDown).Value);
            Assert.That(stairs.TryUse(), Is.True, "3 → 4층");
            world.Events.Flush();
            Assert.That(world.Map.Find(TileType.SealedStairs).HasValue, Is.False, "마지막 보스 방엔 봉인 계단 없음");
            KillBoss(world);
            Assert.That(ends.Count, Is.EqualTo(1));
            Assert.That(ends[0].Outcome, Is.EqualTo(RunOutcome.Victory));
        }

        // 검산: ZoneCatalog = BALANCE.md 20번 — 5구역 · 25층, DungeonGenConfig 층 수와 같음, 보스 층 5 · 10 · 25
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            var book = catalog.ToBook();
            var dungeon = AssetDatabase.LoadAssetAtPath<DungeonGenConfigAsset>("Assets/_Project/Data/Dungeon/DungeonGenConfig.asset").ToConfig();
            Assert.That(book.Zones.Count, Is.EqualTo(5));
            Assert.That(book.FloorCount, Is.EqualTo(25));
            Assert.That(dungeon.FloorCount, Is.EqualTo(book.FloorCount));
            var bossFloors = new List<int>();
            for (int f = 1; f <= 25; f++) if (book.IsBossFloor(f)) bossFloors.Add(f);
            Assert.That(bossFloors, Is.EqualTo(new[] { 5, 10, 25 }));
            var percents = new List<int>();
            foreach (var z in book.Zones) percents.Add(z.StatPercent);
            Assert.That(percents, Is.EqualTo(new[] { 100, 150, 200, 250, 300 }));
            Assert.That(catalog.LootPlanFloors, Is.EqualTo(10));
        }
    }
}
