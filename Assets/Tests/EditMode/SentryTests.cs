using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-49 감시탑 (SPD Sentry — 결정 2026-11-18): 무적 감시자 · 보이면 예고 → 다음 차례 번개(층 단계) · 움직이지 않음 · 보물 방 목록 1개 (BALANCE.md 16번).</summary>
    public class SentryTests
    {
        private sealed class WaitAi : ICommandSource { public ICommand NextCommand(in TickContext context) => new WaitCommand(1); }

        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly SentryConfig SentryCfg = new SentryConfig("enemy_sentry", new[] { "bolt_1", "bolt_2" }, 5, 6);
        private static readonly UnitDef SentryDef = new UnitDef("enemy_sentry", 10, 1, 6, 0, accuracy: 1, evasion: 0, expReward: 0, expMaxLevel: 30);

        private sealed class Run
        {
            public World World;
            public Unit Player;
            public Unit Sentry;
            public MeleeChaserAi Ai;
        }

        private static (SpellBook Spells, StatusBook Statuses, TerrainBook Terrains) Books()
        {
            var statuses = SpellTests.BalanceStatuses();
            var terrains = new TerrainBook(new[] { new TerrainDef(TerrainEffectType.Warning, 2) }, statuses);
            var spells = new SpellBook(new[]
            {
                new SpellDef("bolt_1", Element.Lightning, SpellShape.Bolt, 6, 4, 0),
                new SpellDef("bolt_2", Element.Lightning, SpellShape.Bolt, 6, 7, 0),
            }, statuses, terrains);
            return (spells, statuses, terrains);
        }

        private static Run Start(GridPos playerAt)
        {
            var world = new World(GridMovementTests.Parse("############", "#..........#", "#..........#", "#..........#", "############"), 9UL);
            var (spells, statuses, terrains) = Books();
            world.InstallCoreSystems(Sight, new MagicConfig(3, 1, 4), statuses, terrains);
            var ai = new MeleeChaserAi(world, Config, Sight, statuses, null, spells, new BresenhamTargetingRule());
            world.SetSentryRules(new SentryRules(SentryCfg, SentryDef, ai));
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 40, 3, 8), playerAt);
            var sentry = Sentries.Place(world, new GridPos(2, 2));
            world.Events.Flush();
            return new Run { World = world, Player = player, Sentry = sentry, Ai = ai };
        }

        private static void Act(Run r)
        {
            r.Ai.NextCommand(new TickContext(0, r.Sentry.Id)).Execute(new TickContext(0, r.Sentry.Id));
            r.World.Events.Flush();
        }

        // 보이면 플레이어 칸에 예고(피해 없음) → 다음 차례 그 칸에 번개. 움직이지 않는다
        [Test]
        public void TelegraphsThenStrikes_AndNeverMoves()
        {
            var r = Start(new GridPos(5, 2));
            var charges = new List<BossChargingEvent>();
            r.World.Events.Subscribe<BossChargingEvent>(charges.Add);
            Act(r);
            Assert.That(charges.Count, Is.EqualTo(1));
            Assert.That(r.Sentry.ChargeTarget, Is.EqualTo((GridPos?)new GridPos(5, 2)));
            Assert.That(r.World.Terrain.Has(new GridPos(5, 2), TerrainEffectType.Warning), Is.True);
            Assert.That(r.Player.Hp, Is.EqualTo(40), "예고만");
            Act(r);
            Assert.That(r.Player.Hp, Is.EqualTo(36), "안 피하면 1층 번개 4");
            Assert.That(r.Sentry.ChargeTarget.HasValue, Is.False);
            Assert.That(r.World.Terrain.Has(new GridPos(5, 2), TerrainEffectType.Warning), Is.False);
            Assert.That(r.Sentry.Pos, Is.EqualTo(new GridPos(2, 2)), "제자리");
        }

        // 예고 칸에서 비켜 서면 안 맞는다
        [Test]
        public void SteppingAside_Dodges()
        {
            var r = Start(new GridPos(5, 2));
            Act(r);
            r.World.Units.Teleport(r.Player.Id, new GridPos(6, 2));
            Act(r);
            Assert.That(r.Player.Hp, Is.EqualTo(40));
        }

        // 바로 옆이어도 근접 공격하지 않고, 거리 밖이면 기다리기만 한다
        [Test]
        public void NoMelee_AndIdleOutOfRange()
        {
            var adjacent = Start(new GridPos(3, 2));
            Assert.That(adjacent.Ai.NextCommand(new TickContext(0, adjacent.Sentry.Id)), Is.InstanceOf<ChargeCommand>());
            var far = Start(new GridPos(9, 2)); // 거리 7 > 6
            Assert.That(far.Ai.NextCommand(new TickContext(0, far.Sentry.Id)), Is.InstanceOf<WaitCommand>());
        }

        // 무적: 공격 · 주문 피해가 0
        [Test]
        public void Invulnerable()
        {
            var r = Start(new GridPos(5, 2));
            Combat.ApplyDamage(r.World, r.Player.Id, r.Sentry, 99, DamageCause.Attack);
            Combat.ApplyDamage(r.World, r.Player.Id, r.Sentry, 99, DamageCause.Spell("fire_basic"));
            Assert.That(r.Sentry.Hp, Is.EqualTo(r.Sentry.MaxHp));
            Assert.That(r.Sentry.IsAlive, Is.True);
        }

        // 번개 단계: 5층마다 다음 주문, 마지막 단계는 그 뒤 모두
        [Test]
        public void Config_SpellTier()
        {
            Assert.That(SentryCfg.SpellFor(1), Is.EqualTo("bolt_1"));
            Assert.That(SentryCfg.SpellFor(5), Is.EqualTo("bolt_1"));
            Assert.That(SentryCfg.SpellFor(6), Is.EqualTo("bolt_2"));
            Assert.That(SentryCfg.SpellFor(25), Is.EqualTo("bolt_2"));
        }

        // 감시자도 저장 · 복원 뒤 감시자 (모습 ID로 판정 — 따로 저장할 상태 없음, 예고 칸은 v23 ChargeTarget)
        [Test]
        public void RoundTrip_KeepsChargeTarget()
        {
            var r = Start(new GridPos(5, 2));
            Act(r);
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(r.World.Capture())), (w, id) => new WaitAi());
            Assert.That(restored.Units.TryGet(r.Sentry.Id, out var back) && back.VisualId == "enemy_sentry", Is.True);
            Assert.That(back.ChargeTarget, Is.EqualTo((GridPos?)new GridPos(5, 2)));
        }

        // 감시탑: 입구 = 문, 맨 안쪽 줄 가운데에 감시자, 그 줄에 보물 방 목록 1개
        [Test]
        public void Room_SentryOnBackRowWithPrize()
        {
            int carved = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var (world, _, room) = SpecialRoomTests.CarveFor(seed, SpecialRoomType.Sentry, w =>
                {
                    var (spells, statuses, _) = Books();
                    w.SetSentryRules(new SentryRules(SentryCfg, SentryDef, new MeleeChaserAi(w, Config, Sight, statuses, null, spells, new BresenhamTargetingRule())));
                });
                if (room == null) continue;
                carved++;
                int size = (int)System.Math.Round(System.Math.Sqrt(room.Cells.Count));
                var post = room.Cells[(size - 1) * size + size / 2];
                Assert.That(world.Map.Get(room.Entrance), Is.EqualTo(TileType.Door), $"시드 {seed}");
                Assert.That(world.Units.TryGetAt(post, out var sentry) && sentry.VisualId == "enemy_sentry", Is.True, $"시드 {seed}");
                int items = 0;
                for (int i = 0; i < room.Cells.Count; i++)
                {
                    int n = world.FloorItems.ItemsAt(room.Cells[i]).Count;
                    if (n > 0) Assert.That(i / size, Is.EqualTo(size - 1), $"시드 {seed}: 보상은 맨 안쪽 줄");
                    items += n;
                }
                Assert.That(items, Is.EqualTo(1), $"시드 {seed}");
            }
            Assert.That(carved, Is.GreaterThan(0));
        }

        // 검산: FloorDecorConfig 감시탑 값 = BALANCE.md 16번, 감시자 적 줄 · 번개 주문 줄
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var c = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToSentry();
            Assert.That((c.SentryId, c.FloorsPerTier, c.Range), Is.EqualTo(("enemy_sentry", 5, 6)));
            Assert.That(c.SpellIds, Is.EqualTo(new[] { "sentry_bolt_1", "sentry_bolt_2", "sentry_bolt_3", "sentry_bolt_4", "sentry_bolt_5" }));
            var zones = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            Assert.That(zones.TryGetEnemy("enemy_sentry", out var s), Is.True);
            Assert.That((s.SightRange, s.ExpReward), Is.EqualTo((6, 0)));
            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            int[] damage = { 4, 7, 10, 13, 16 };
            for (int i = 0; i < damage.Length; i++)
            {
                Assert.That(spells.TryGet(c.SpellIds[i], out var def), Is.True, c.SpellIds[i]);
                Assert.That((def.Element, def.Shape, def.Damage, def.ManaCost), Is.EqualTo((Element.Lightning, SpellShape.Bolt, damage[i], 0)), c.SpellIds[i]);
            }
        }
    }
}
