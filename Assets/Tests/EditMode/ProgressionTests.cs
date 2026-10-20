using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-6 경험치 · 레벨업 · 3-7 명중 · 회피 (SPD 참고, 결정 2026-10-19).</summary>
    public class ProgressionTests
    {
        // BALANCE.md 12번과 같은 값
        private static readonly ProgressionConfig Progress = new ProgressionConfig(30, 5, 5, 5, 1, 1, 1);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ProjectM.Magic.MagicConfig Magic = new ProjectM.Magic.MagicConfig(3, 1, 4);
        private static readonly StatusDef Paralyze = new StatusDef(StatusType.Paralyze, 5, preventsAction: true);

        // BALANCE.md 3번 (SPD 영웅 · 쥐)
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10, null, accuracy: 10, evasion: 5);
        private static readonly UnitDef RatDef = new UnitDef("enemy_melee", 8, 2, 6, 0, null, accuracy: 8, evasion: 2, expReward: 1, expMaxLevel: 5);

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;
            public readonly List<ISimEvent> Events = new List<ISimEvent>();
        }

        private sealed class Idle : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static Session Start(UnitDef playerDef = null, ProgressionConfig progression = null)
        {
            var map = GridMovementTests.Parse(
                "#######",
                "#.....#",
                "#.....#",
                "#######");
            var world = new World(map, 7UL);
            var vision = world.InstallCoreSystems(new ShadowcastSightRule(), Magic, new StatusBook(new[] { Paralyze }), TerrainBook.Empty,
                progression ?? Progress);
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            world.Events.Subscribe<ExpGainedEvent>(s.Events.Add);
            world.Events.Subscribe<LevelUpEvent>(s.Events.Add);
            world.Events.Subscribe<AttackMissedEvent>(s.Events.Add);
            world.Events.Subscribe<UnitDamagedEvent>(s.Events.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, playerDef ?? PlayerDef, new GridPos(1, 1));
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static Unit SpawnRat(Session s, GridPos pos, UnitDef def = null) =>
            UnitSpawner.SpawnEnemy(s.World, def ?? RatDef, pos, new Idle());

        private static void Kill(Session s, Unit enemy)
        {
            Combat.ApplyDamage(s.World, s.Player.Id, enemy, enemy.Hp, DamageCause.Attack);
            s.Clock.SubmitPlayerCommand(new WaitCommand(1)); // ⑥ 사망 판정 → 경험치
        }

        private static List<T> Of<T>(Session s) where T : ISimEvent => s.Events.FindAll(e => e is T).ConvertAll(e => (T)e);

        // ── 3-6 경험치 · 레벨업 ──

        [Test]
        public void ExpToNext_IsSpdFormula()
        {
            Assert.That(Progress.ExpToNext(1), Is.EqualTo(10));
            Assert.That(Progress.ExpToNext(2), Is.EqualTo(15));
            Assert.That(Progress.ExpToNext(10), Is.EqualTo(55));
        }

        [Test]
        public void GainExp_LevelUpRaisesStatsAndCurrentValues()
        {
            var s = Start();
            Combat.ApplyDamage(s.World, EntityId.None, s.Player, 8, DamageCause.Attack); // HP 12
            Assert.That(ProjectM.Magic.Mana.TrySpend(s.World, s.Player, 6), Is.True); // MP 4

            Progression.GainExp(s.World, s.Player, 10, Progress);
            s.World.Events.Flush();

            Assert.That((s.Player.Level, s.Player.Exp), Is.EqualTo((2, 0)));
            Assert.That((s.Player.MaxHp, s.Player.Hp), Is.EqualTo((25, 17)), "최대 +5 · 현재도 +5");
            Assert.That((s.Player.MaxMana, s.Player.Mana), Is.EqualTo((11, 5)), "최대 +1 · 현재도 +1");
            Assert.That((s.Player.Accuracy, s.Player.Evasion), Is.EqualTo((11, 6)));
            var up = Of<LevelUpEvent>(s);
            Assert.That(up, Has.Count.EqualTo(1));
            Assert.That(up[0].Level, Is.EqualTo(2));
            var gained = Of<ExpGainedEvent>(s);
            Assert.That((gained[0].Amount, gained[0].Exp, gained[0].ExpToNext, gained[0].Level), Is.EqualTo((10, 0, 15, 2)));
        }

        [Test]
        public void GainExp_ManyLevelsAtOnce_CarriesRemainder()
        {
            var s = Start();
            Progression.GainExp(s.World, s.Player, 10 + 15 + 7, Progress);
            s.World.Events.Flush();
            Assert.That((s.Player.Level, s.Player.Exp), Is.EqualTo((3, 7)));
            Assert.That(Of<LevelUpEvent>(s), Has.Count.EqualTo(2), "레벨마다 하나씩");
            Assert.That(s.Player.MaxHp, Is.EqualTo(30));
        }

        [Test]
        public void GainExp_StopsAtMaxLevel()
        {
            var s = Start(progression: new ProgressionConfig(3, 5, 5, 5, 1, 1, 1));
            Progression.GainExp(s.World, s.Player, 1000, new ProgressionConfig(3, 5, 5, 5, 1, 1, 1));
            Assert.That((s.Player.Level, s.Player.Exp), Is.EqualTo((3, 0)), "남는 경험치는 버림");
            Assert.That(Progression.GainExp(s.World, s.Player, 5, new ProgressionConfig(3, 5, 5, 5, 1, 1, 1)), Is.EqualTo(0));
        }

        [Test]
        public void EnemyDeath_GivesExpToPlayer()
        {
            var s = Start();
            var rat = SpawnRat(s, new GridPos(2, 1));
            Kill(s, rat);
            Assert.That(s.Player.Exp, Is.EqualTo(1));
            Assert.That(Of<ExpGainedEvent>(s), Has.Count.EqualTo(1));
        }

        [Test]
        public void EnemyDeath_NoExpAboveEnemyMaxLevel()
        {
            var s = Start();
            Progression.GainExp(s.World, s.Player, 10 + 15 + 20 + 25, Progress);
            Assert.That((s.Player.Level, s.Player.Exp), Is.EqualTo((5, 0)));
            Kill(s, SpawnRat(s, new GridPos(2, 1)));
            Assert.That(s.Player.Exp, Is.EqualTo(1), "레벨 5 = 최고 레벨 이하 → 받음");

            Progression.GainExp(s.World, s.Player, 29, Progress);
            Assert.That((s.Player.Level, s.Player.Exp), Is.EqualTo((6, 0)));
            s.World.Events.Flush(); // 레벨업 경험치 이벤트를 먼저 비운다 — 안 그러면 아래 Kill의 Flush 때 들어와 "경험치 없음" 검사가 틀림
            s.Events.Clear();
            Kill(s, SpawnRat(s, new GridPos(3, 1)));
            Assert.That(s.Player.Exp, Is.EqualTo(0), "레벨 6 > 최고 레벨 5 → 0 (반복 사냥 방지)");
            Assert.That(Of<ExpGainedEvent>(s), Is.Empty);
        }

        [Test]
        public void NoProgressionConfig_NoExp()
        {
            var map = GridMovementTests.Parse("#####", "#...#", "#####");
            var world = new World(map, 1UL);
            world.InstallCoreSystems(new ShadowcastSightRule(), Magic, new StatusBook(new StatusDef[0]), TerrainBook.Empty);
            var clock = new TurnClock(world.Scheduler, world.PlayerInput);
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1));
            var rat = UnitSpawner.SpawnEnemy(world, RatDef, new GridPos(2, 1), new Idle());
            Combat.ApplyDamage(world, player.Id, rat, rat.Hp, DamageCause.Attack);
            clock.SubmitPlayerCommand(new WaitCommand(1));
            Assert.That(player.Exp, Is.EqualTo(0));
        }

        // ── 3-7 명중 · 회피 ──

        [Test]
        public void RollHit_ZeroEvasion_AlwaysHitsWithoutRolling()
        {
            // 같은 시드 두 판: 한쪽만 판정 → 다음 난수가 같으면 판정이 난수를 쓰지 않은 것
            var s = Start();
            SpawnRat(s, new GridPos(2, 1), new UnitDef("enemy_melee", 8, 2, 6));
            var before = s.World.Rng.Get(RngStream.Combat).NextFloat();
            var s2 = Start();
            var dummy2 = SpawnRat(s2, new GridPos(2, 1), new UnitDef("enemy_melee", 8, 2, 6));
            Assert.That(Combat.RollHit(s2.World, s2.Player, dummy2), Is.True);
            Assert.That(s2.World.Rng.Get(RngStream.Combat).NextFloat(), Is.EqualTo(before), "회피 0은 난수를 쓰지 않는다 — 이전 테스트 · 상태 확률이 그대로");
        }

        [Test]
        public void RollHit_HighEvasion_Misses_AttackPublishesMiss()
        {
            var s = Start(new UnitDef("player", 20, 3, 8, 10, null, accuracy: 0, evasion: 5));
            var wall = SpawnRat(s, new GridPos(2, 1), new UnitDef("enemy_melee", 8, 2, 6, 0, null, accuracy: 0, evasion: 1000));
            wall.LastSeenTargetPos = s.Player.Pos; // 3-7c: 플레이어를 본 적 — 기습(항상 명중)이 아니게
            s.Clock.SubmitPlayerCommand(new AttackCommand(s.World, wall.Id, 1));
            s.World.Events.Flush();
            Assert.That(wall.Hp, Is.EqualTo(8), "명중 0 vs 회피 1000 → 빗나감");
            var miss = Of<AttackMissedEvent>(s);
            Assert.That(miss, Has.Count.EqualTo(1));
            Assert.That((miss[0].Attacker, miss[0].Target), Is.EqualTo((s.Player.Id, wall.Id)));
        }

        [Test]
        public void RollHit_TargetCannotAct_AlwaysHits()
        {
            var s = Start(new UnitDef("player", 20, 3, 8, 10, null, accuracy: 0, evasion: 5));
            var wall = SpawnRat(s, new GridPos(2, 1), new UnitDef("enemy_melee", 8, 2, 6, 0, null, accuracy: 0, evasion: 1000));
            StatusEffects.Apply(s.World, wall, Paralyze);
            Assert.That(Combat.RollHit(s.World, s.Player, wall), Is.True, "마비(행동 불가) → 회피 0");
        }

        [Test]
        public void RollHit_SpdRatVsHero_HitRateRoughlyMatches()
        {
            // SPD: 영웅 명중 10 vs 쥐 회피 2 → 거의 맞음, 쥐 명중 8 vs 영웅 회피 5 → 대체로 맞음
            var s = Start();
            var rat = SpawnRat(s, new GridPos(2, 1));
            int heroHits = 0, ratHits = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (Combat.RollHit(s.World, s.Player, rat)) heroHits++;
                if (Combat.RollHit(s.World, rat, s.Player)) ratHits++;
            }
            // 기대값: P(U·a ≥ V·d) — a ≥ d면 1 − d/(2a). 영웅 0.9, 쥐 1 − 5/16 ≈ 0.69 (BALANCE 12번 검산)
            Assert.That(heroHits, Is.InRange(860, 940));
            Assert.That(ratHits, Is.InRange(640, 740));
        }

        // ── 저장 ──

        [Test]
        public void Snapshot_RoundTripsLevelAndCombatStats()
        {
            var s = Start();
            SpawnRat(s, new GridPos(3, 1));
            Progression.GainExp(s.World, s.Player, 12, Progress);
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => new Idle());
            var p = restored.Units.Get(s.Player.Id);
            Assert.That((p.Level, p.Exp, p.MaxHp, p.MaxMana, p.Accuracy, p.Evasion), Is.EqualTo((2, 2, 25, 11, 11, 6)));
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
        }

        // 검산: 프로젝트 에셋이 BALANCE.md 3번 · 12번과 같은 값인지
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            const string hint = " 없음 — 메뉴 ProjectM/Setup/Main Scene 실행";
            var config = AssetDatabase.LoadAssetAtPath<ProgressionConfigAsset>("Assets/_Project/Data/Units/ProgressionConfig.asset");
            Assert.That(config, Is.Not.Null, "ProgressionConfig.asset" + hint);
            var c = config.ToConfig();
            Assert.That((c.MaxLevel, c.ExpBase, c.ExpPerLevel), Is.EqualTo((30, 5, 5)));
            Assert.That((c.HpPerLevel, c.ManaPerLevel, c.AccuracyPerLevel, c.EvasionPerLevel), Is.EqualTo((5, 1, 1, 1)));

            var player = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/Player.asset").ToDef();
            var enemy = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/MeleeEnemy.asset").ToDef();
            var boss = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/Boss.asset").ToDef();
            Assert.That((player.Accuracy, player.Evasion, player.ExpReward), Is.EqualTo((10, 5, 0)));
            Assert.That((enemy.Accuracy, enemy.Evasion, enemy.ExpReward, enemy.ExpMaxLevel), Is.EqualTo((8, 2, 1, 5)));
            Assert.That((boss.Accuracy, boss.Evasion, boss.ExpReward, boss.ExpMaxLevel), Is.EqualTo((10, 8, 10, 30)));
        }
    }
}
