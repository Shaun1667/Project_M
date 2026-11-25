using System.Collections.Generic;
using System.IO;
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
    /// 3-59 반응 발견 (결정 2026-11-25 — 판마다 경험치 + 영구 도감): 플레이어 주문이 이 판에서 처음 일으킨 반응 그룹 = RunState에 남김(스냅샷 v37)
    /// + 경험치 ReactionConfig.DiscoveryExp. 같은 그룹 두 번째 · 적의 주문은 없음. 판 밖 도감 = ReactionCodexStore (BALANCE.md 8번).
    /// </summary>
    public class ReactionDiscoveryTests
    {
        private static readonly ProgressionConfig Progress = new ProgressionConfig(30, 5, 5, 5, 1, 1, 1);

        private sealed class Idle : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static ReactionBook Book(StatusBook statuses) => new ReactionBook(new[]
        {
            new ReactionDef("evaporate", ReactionTrigger.Status, StatusType.Wet, TerrainEffectType.None, Element.Fire, 10,
                extraDamage: 2, consumeTrigger: true, group: "evaporate"),
            new ReactionDef("evaporate_water", ReactionTrigger.Terrain, StatusType.None, TerrainEffectType.Water, Element.Fire, 10,
                extraDamage: 2, consumeTrigger: true, group: "evaporate"),
            new ReactionDef("toxic_ignite", ReactionTrigger.Status, StatusType.Poison, TerrainEffectType.None, Element.Fire, 10, extraDamage: 2),
        }, new ReactionConfig(4, 3, 5, discoveryExp: 5), statuses);

        private sealed class Setup
        {
            public World World;
            public Unit Player, Enemy;
            public StatusBook Statuses;
            public ReactionBook Reactions;
            public SpellDef Fire = new SpellDef("fire", Element.Fire, SpellShape.Bolt, 6, 1, 0);
        }

        private static Setup Start()
        {
            var world = new World(GridMovementTests.Parse("#######", "#.....#", "#.....#", "#######"), 5UL);
            var statuses = SpellTests.BalanceStatuses();
            world.InstallCoreSystems(new ShadowcastSightRule(), new MagicConfig(3, 1, 4), statuses, SpellTests.BalanceTerrains(), Progress);
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 30, 2, 6), new GridPos(1, 1));
            var enemy = UnitSpawner.SpawnEnemy(world, new UnitDef("dummy", 50, 1, 6), new GridPos(4, 1), new Idle());
            world.Events.Flush();
            return new Setup { World = world, Player = player, Enemy = enemy, Statuses = statuses, Reactions = Book(statuses) };
        }

        private static void Apply(Setup s, Unit unit, StatusType type)
        {
            s.Statuses.TryGet(type, out var def);
            StatusEffects.Apply(s.World, unit, def);
        }

        private static void Hit(Setup s, EntityId caster)
        {
            ReactionEngine.Resolve(s.World, s.Reactions, s.Statuses, caster, s.Fire, new List<Unit> { s.Enemy }, new List<GridPos> { s.Enemy.Pos });
            s.World.Events.Flush();
        }

        // 처음 = 기록 · 경험치 5 · 이벤트, 같은 반응 두 번째 · 같은 그룹 다른 줄(물 칸 증발) = 없음, 다른 그룹 = 또 5
        [Test]
        public void FirstReactionOfGroup_GivesExpOnce()
        {
            var s = Start();
            var events = new List<ReactionDiscoveredEvent>();
            s.World.Events.Subscribe<ReactionDiscoveredEvent>(events.Add);

            Apply(s, s.Enemy, StatusType.Wet);
            Hit(s, s.Player.Id);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That((events[0].Group, events[0].ReactionId, events[0].Exp), Is.EqualTo(("evaporate", "evaporate", 5)));
            Assert.That(s.World.Run.DiscoveredReactions, Is.EqualTo(new[] { "evaporate" }));
            Assert.That(s.Player.Exp, Is.EqualTo(5));

            Apply(s, s.Enemy, StatusType.Wet);
            Hit(s, s.Player.Id);
            TerrainEffects.Place(s.World, s.Enemy.Pos, TerrainEffectType.Water, 12);
            Hit(s, s.Player.Id);
            Assert.That(events.Count, Is.EqualTo(1), "같은 그룹은 한 번");
            Assert.That(s.Player.Exp, Is.EqualTo(5));

            Apply(s, s.Enemy, StatusType.Poison);
            Hit(s, s.Player.Id);
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(s.World.Run.DiscoveredReactions, Is.EqualTo(new[] { "evaporate", "toxic_ignite" }));
        }

        // 적이 일으킨 반응은 발견이 아니다
        [Test]
        public void EnemyCaster_DoesNotDiscover()
        {
            var s = Start();
            Apply(s, s.Player, StatusType.Wet);
            ReactionEngine.Resolve(s.World, s.Reactions, s.Statuses, s.Enemy.Id, s.Fire, new List<Unit> { s.Player }, new List<GridPos> { s.Player.Pos });
            s.World.Events.Flush();
            Assert.That(s.World.Run.DiscoveredReactions.Count, Is.EqualTo(0));
            Assert.That(s.Player.Exp, Is.EqualTo(0));
        }

        // 스냅샷 v37: 발견 목록 왕복 (순서 그대로)
        [Test]
        public void Snapshot_RoundTripsDiscoveries()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(37));
            var s = Start();
            Apply(s, s.Enemy, StatusType.Poison);
            Hit(s, s.Player.Id);
            Apply(s, s.Enemy, StatusType.Wet);
            Hit(s, s.Player.Id);
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(s.World.Capture())), (w, id) => new Idle());
            Assert.That(restored.Run.DiscoveredReactions, Is.EqualTo(new[] { "toxic_ignite", "evaporate" }));
        }

        // 판 밖 도감 파일: 더하기 · 중복 없음 · 다시 읽기 · 깨진 파일 = 빈 도감
        [Test]
        public void CodexStore_AddsOnceAndReloads()
        {
            string path = Path.Combine(Path.GetTempPath(), "projectm_codex_test.json");
            if (File.Exists(path)) File.Delete(path);
            try
            {
                Assert.That(ReactionCodexStore.Load(path), Is.Empty);
                Assert.That(ReactionCodexStore.Add("evaporate", path), Is.True);
                Assert.That(ReactionCodexStore.Add("evaporate", path), Is.False);
                Assert.That(ReactionCodexStore.Add("freeze", path), Is.True);
                Assert.That(ReactionCodexStore.Load(path), Is.EqualTo(new[] { "evaporate", "freeze" }));
                File.WriteAllText(path, "{ not json");
                Assert.That(ReactionCodexStore.Load(path), Is.Empty);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // 검산: 발견 경험치 = BALANCE.md 8번 (5)
        [Test]
        public void ProjectAsset_DiscoveryExpMatchesBalance()
        {
            var reactions = AssetDatabase.LoadAssetAtPath<ReactionCatalogAsset>("Assets/_Project/Data/Reactions/ReactionCatalog.asset").ToBook(SpellTests.BalanceStatuses());
            Assert.That(reactions.Config.DiscoveryExp, Is.EqualTo(5));
        }
    }
}
