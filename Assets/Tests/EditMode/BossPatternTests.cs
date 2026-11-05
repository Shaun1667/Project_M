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
    /// <summary>3-17d 패턴 보스 (결정 2026-11-04 — 슬라임 소녀 · 파라오 술사): 힘 모으기 예고 → 다음 차례 터짐 · 순간이동 · 체력 절반 소환 · 스냅샷 v23 (BALANCE.md 20-2).</summary>
    public class BossPatternTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef Minion = new UnitDef("minion", 5, 1, 6);

        private static readonly EnemyBook Book = new EnemyBook(new[]
        {
            new EnemyTraits("charger", chargeSpellId: "slam", chargePercent: 100),
            new EnemyTraits("blinker", teleportPercent: 100),
            new EnemyTraits("summoner", summonDef: Minion, summonCount: 2, summonAtHpPercent: 50),
            new EnemyTraits("minion"),
        });

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private sealed class Run
        {
            public World World;
            public Unit Player;
            public Unit Boss;
            public MeleeChaserAi Ai;
        }

        private static Run Start(string kind, GridPos bossAt)
        {
            var world = new World(GridMovementTests.Parse("############", "#..........#", "#..........#", "#..........#", "############"), 9UL);
            var statuses = SpellTests.BalanceStatuses();
            var terrains = new TerrainBook(new[] { new TerrainDef(TerrainEffectType.Warning, 2) }, statuses);
            world.InstallCoreSystems(Sight, new MagicConfig(3, 1, 4), statuses, terrains);
            var spells = new SpellBook(new[] { new SpellDef("slam", Element.Water, SpellShape.Burst, 3, 6, 0, radius: 1) }, statuses, terrains);
            world.SetEnemyRules(new EnemyRules(Book, spells, statuses, terrains));
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 40, 3, 8), new GridPos(2, 2));
            var ai = new MeleeChaserAi(world, Config, Sight, statuses, Book, spells, new BresenhamTargetingRule());
            var boss = UnitSpawner.SpawnEnemy(world, new UnitDef(kind, 20, 2, 10), bossAt, ai);
            world.Events.Flush();
            return new Run { World = world, Player = player, Boss = boss, Ai = ai };
        }

        private static void Act(Run r)
        {
            r.Ai.NextCommand(new TickContext(0, r.Boss.Id)).Execute(new TickContext(0, r.Boss.Id));
            r.World.Events.Flush();
        }

        // 힘 모으기: 첫 차례 = 플레이어 칸 둘레 3×3 예고(경고 지형, 피해 없음) → 둘째 차례 = 그 칸에 피해 · 경고 지움. 보스 자신은 안 맞음
        [Test]
        public void Charge_TelegraphsThenReleases()
        {
            var r = Start("charger", new GridPos(3, 2));
            var charges = new List<BossChargingEvent>();
            r.World.Events.Subscribe<BossChargingEvent>(charges.Add);
            Act(r);
            Assert.That(charges.Count, Is.EqualTo(1));
            Assert.That(r.Boss.ChargeTarget, Is.EqualTo((GridPos?)new GridPos(2, 2)));
            Assert.That(r.World.Terrain.Has(new GridPos(1, 1), TerrainEffectType.Warning), Is.True);
            Assert.That(r.World.Terrain.Has(r.Boss.Pos, TerrainEffectType.Warning), Is.False, "보스 칸은 빠짐");
            Assert.That(r.Player.Hp, Is.EqualTo(40), "예고만");

            r.World.Units.Teleport(r.Player.Id, new GridPos(8, 2)); // 피함
            Act(r);
            Assert.That(r.Boss.ChargeTarget.HasValue, Is.False);
            Assert.That(r.Player.Hp, Is.EqualTo(40), "벗어나면 안 맞음");
            Assert.That(r.World.Terrain.Has(new GridPos(1, 1), TerrainEffectType.Warning), Is.False);
            Assert.That(r.Boss.Hp, Is.EqualTo(20));

            Act(r); // 다시 모으고 (거리 5 > 사거리 3이면 안 모음 — 다가감)
            r.World.Units.Teleport(r.Player.Id, new GridPos(r.Boss.Pos.X + 1, r.Boss.Pos.Y));
            Act(r);
            Assert.That(r.Boss.ChargeTarget.HasValue, Is.True);
            Act(r);
            Assert.That(r.Player.Hp, Is.EqualTo(34), "안 피하면 6");
        }

        // 순간이동: 플레이어가 붙으면 떨어진 칸으로
        [Test]
        public void Blinker_TeleportsAwayWhenAdjacent()
        {
            var r = Start("blinker", new GridPos(3, 2));
            Act(r);
            Assert.That(GridMovement.Chebyshev(r.Boss.Pos, r.Player.Pos), Is.GreaterThanOrEqualTo(4));
        }

        // 소환: 체력 50% 이하에서 한 번만 2마리
        [Test]
        public void Summoner_SummonsOnceAtHalfHp()
        {
            var r = Start("summoner", new GridPos(8, 2));
            Combat.ApplyDamage(r.World, r.Player.Id, r.Boss, 10);
            Act(r);
            int minions = 0;
            foreach (var u in r.World.Units.ToSortedList()) if (u.VisualId == "minion") minions++;
            Assert.That(minions, Is.EqualTo(2));
            Assert.That(r.Boss.Summoned, Is.True);
            Act(r);
            minions = 0;
            foreach (var u in r.World.Units.ToSortedList()) if (u.VisualId == "minion") minions++;
            Assert.That(minions, Is.EqualTo(2), "한 번만");
        }

        // 스냅샷 v23: 예고 칸 · 소환 여부 왕복
        [Test]
        public void Snapshot_RoundTripsBossState()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(23), "3-17d (정확한 값은 ClassFormTests — 24)");
            var r = Start("charger", new GridPos(3, 2));
            r.Boss.ChargeTarget = new GridPos(5, 1);
            r.Boss.Summoned = true;
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(r.World.Capture())), (w, id) => new IdleAi()); // 적(보스)이 있으면 AI가 필요
            Assert.That(restored.Units.TryGet(r.Boss.Id, out var b), Is.True);
            Assert.That(b.ChargeTarget, Is.EqualTo((GridPos?)new GridPos(5, 1)));
            Assert.That(b.Summoned, Is.True);
        }

        // 검산: 보스 줄 · 주문 = BALANCE.md 20-2, 5층 = 슬라임 소녀 · 10층 = 파라오 술사
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            var zones = catalog.ToBook();
            Assert.That(zones.BossOf(5).VisualId, Is.EqualTo("boss_slime_girl"));
            Assert.That(zones.BossOf(10).VisualId, Is.EqualTo("boss_pharaoh"));
            Assert.That(zones.BossOf(25).VisualId, Is.EqualTo("boss_time_guardian"), "3-20: 25층 = 시간의 수호자 (임시 보스 대체)");
            var book = catalog.ToEnemyBook();
            Assert.That(book.TryGet("boss_slime_girl", out var slime) && slime.ChargeSpellId == "boss_slime_slam" && slime.ChargePercent == 35
                && slime.HurtTerrain == TerrainEffectType.Water, Is.True);
            Assert.That(book.TryGet("boss_pharaoh", out var ph) && ph.ChargeSpellId == "boss_sand_storm" && ph.TeleportPercent == 40
                && ph.SummonDef != null && ph.SummonDef.VisualId == "enemy_mummy" && ph.SummonCount == 2 && ph.SummonAtHpPercent == 50
                && ph.SpellId == "enemy_sand_bolt", Is.True);
            Assert.That(catalog.TryGetEnemy("boss_pharaoh", out var p) && (p.MaxHp, p.AttackDamage, p.MaxMana) == (80, 4, 9), Is.True);
            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            Assert.That(terrains.TryGet(TerrainEffectType.Warning, out var warning) && warning.DurationTicks == 2, Is.True);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            Assert.That(spells.TryGet("boss_slime_slam", out var slam) && (slam.Range, slam.Damage, slam.Radius) == (3, 6, 1), Is.True);
            Assert.That(spells.TryGet("boss_sand_storm", out var storm) && (storm.Range, storm.Damage, storm.Radius) == (6, 7, 1), Is.True);
        }
    }
}
