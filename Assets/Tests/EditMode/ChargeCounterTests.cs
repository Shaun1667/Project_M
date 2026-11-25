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
    /// <summary>
    /// 3-57 적 주문 가로채기 (결정 2026-11-25 — 상쇄 + 역류): 적이 모으는 힘의 예고 칸에 상극 속성 주문이 닿으면
    /// 힘이 흩어지고(예고 칸 · 다음 차례 주문 사라짐) 모으던 적이 역류 피해 + 마비. 반응 표 Charge 줄 데이터 (BALANCE.md 8번).
    /// </summary>
    public class ChargeCounterTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly EnemyBook Book = new EnemyBook(new[] { new EnemyTraits("charger", chargeSpellId: "slam", chargePercent: 100) });

        private sealed class Run
        {
            public World World;
            public Unit Player;
            public Unit Boss;
            public MeleeChaserAi Ai;
            public SpellBook Spells;
        }

        private static Run Start()
        {
            var world = new World(GridMovementTests.Parse("############", "#..........#", "#..........#", "#..........#", "############"), 9UL);
            var statuses = SpellTests.BalanceStatuses();
            var terrains = new TerrainBook(new[] { new TerrainDef(TerrainEffectType.Warning, 2) }, statuses);
            world.InstallCoreSystems(Sight, new MagicConfig(3, 1, 4), statuses, terrains);
            var reactions = new ReactionBook(new[]
            {
                new ReactionDef("counter_water", ReactionTrigger.Charge, StatusType.None, TerrainEffectType.None, Element.Lightning, 30,
                    extraDamage: 6, consumeTrigger: true, targetStatus: StatusType.Paralyze, chargeElement: Element.Water),
            }, new ReactionConfig(4, 10, 5), statuses);
            var spells = new SpellBook(new[]
            {
                new SpellDef("slam", Element.Water, SpellShape.Burst, 3, 6, 0, radius: 1),
                new SpellDef("zap", Element.Lightning, SpellShape.Burst, 8, 0, 0, radius: 0),
                new SpellDef("flame", Element.Fire, SpellShape.Burst, 8, 0, 0, radius: 0),
            }, statuses, terrains, reactions);
            world.SetEnemyRules(new EnemyRules(Book, spells, statuses, terrains));
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 40, 3, 8), new GridPos(2, 2));
            var ai = new MeleeChaserAi(world, Config, Sight, statuses, Book, spells, new BresenhamTargetingRule());
            var boss = UnitSpawner.SpawnEnemy(world, new UnitDef("charger", 20, 2, 10), new GridPos(3, 2), ai);
            world.Events.Flush();
            return new Run { World = world, Player = player, Boss = boss, Ai = ai, Spells = spells };
        }

        private static void Act(Run r)
        {
            r.Ai.NextCommand(new TickContext(0, r.Boss.Id)).Execute(new TickContext(0, r.Boss.Id));
            r.World.Events.Flush();
        }

        private static void CastAt(Run r, string spellId, GridPos cell)
        {
            r.Spells.TryGet(spellId, out var spell);
            SpellCasting.ApplyEffect(r.World, r.Spells, r.Player.Id, spell, SpellCasting.PreviewAt(r.World, spell, cell));
            r.World.Events.Flush();
        }

        // 물을 모으는 적의 예고 칸 하나에 번개 → 힘이 흩어짐 · 예고 칸 모두 지움 · 역류 6 · 마비 · 다음 차례에 터지지 않음
        [Test]
        public void CounterElement_CancelsChargeAndBackfires()
        {
            var r = Start();
            Act(r);
            Assert.That(r.Boss.ChargeTarget, Is.EqualTo((GridPos?)new GridPos(2, 2)));
            Assert.That(r.Boss.ChargeSpellId, Is.EqualTo("slam"));
            var canceled = new List<BossChargeCanceledEvent>();
            r.World.Events.Subscribe<BossChargeCanceledEvent>(canceled.Add);

            CastAt(r, "zap", new GridPos(1, 1)); // 예고 칸 구석 (보스 · 플레이어 칸 아님)
            Assert.That(canceled.Count, Is.EqualTo(1));
            Assert.That(r.Boss.ChargeTarget.HasValue, Is.False);
            Assert.That(r.Boss.ChargeSpellId, Is.Null);
            Assert.That(r.World.Terrain.Has(new GridPos(1, 3), TerrainEffectType.Warning), Is.False, "맞지 않은 예고 칸도 지움");
            Assert.That(r.Boss.Hp, Is.EqualTo(14), "역류 6");
            Assert.That(r.Boss.Statuses.Exists(s => s.Type == StatusType.Paralyze), Is.True);
            Assert.That(r.Player.Hp, Is.EqualTo(40));
        }

        // 상극이 아닌 속성(불)은 힘을 흩뜨리지 않는다 → 다음 차례에 그대로 터짐
        [Test]
        public void OtherElement_LeavesCharge()
        {
            var r = Start();
            Act(r);
            CastAt(r, "flame", new GridPos(1, 1));
            Assert.That(r.Boss.ChargeTarget.HasValue, Is.True);
            Assert.That(r.Boss.Hp, Is.EqualTo(20));
            Act(r);
            Assert.That(r.Player.Hp, Is.EqualTo(34), "그대로 6");
        }

        // 예고 칸 밖에 맞히면 아무 일 없음
        [Test]
        public void OutsideWarning_DoesNothing()
        {
            var r = Start();
            Act(r);
            CastAt(r, "zap", new GridPos(8, 2));
            Assert.That(r.Boss.ChargeTarget.HasValue, Is.True);
        }

        // 스냅샷 v36: 모으는 주문 ID 왕복
        [Test]
        public void Snapshot_RoundTripsChargeSpellId()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(36));
            var r = Start();
            Act(r);
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(r.World.Capture())), (w, id) => r.Ai);
            Assert.That(restored.Units.TryGet(r.Boss.Id, out var b) && b.ChargeSpellId == "slam", Is.True);
        }

        // 검산: 힘을 모으는 보스 5종마다 상극 줄 하나 (BALANCE.md 8번 — 물←번개 · 대지←바람 · 얼음←불 · 불←물 · 번개←대지, 역류 6 · 마비)
        [Test]
        public void ProjectAssets_EveryChargeBossHasCounter()
        {
            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var reactions = AssetDatabase.LoadAssetAtPath<ReactionCatalogAsset>("Assets/_Project/Data/Reactions/ReactionCatalog.asset").ToBook(statuses);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            var expected = new Dictionary<string, Element>
            {
                { "boss_slime_slam", Element.Lightning }, { "boss_sand_storm", Element.Wind }, { "boss_blizzard", Element.Fire },
                { "boss_lava_eruption", Element.Water }, { "boss_time_collapse", Element.Earth },
            };
            foreach (var pair in expected)
            {
                Assert.That(spells.TryGet(pair.Key, out var charge), Is.True, pair.Key);
                var r = reactions.FindCharge(charge.Element, pair.Value);
                Assert.That(r, Is.Not.Null, pair.Key);
                Assert.That((r.ExtraDamage, r.ConsumeTrigger, r.TargetStatus, r.TargetStatusChancePercent), Is.EqualTo((6, true, StatusType.Paralyze, 100)), pair.Key);
            }
        }
    }
}
