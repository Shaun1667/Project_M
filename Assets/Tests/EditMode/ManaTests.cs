using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Magic;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    public class ManaTests
    {
        // BALANCE.md 4번과 같은 값
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly string[] StartingSpells = { "fire_basic", "ice_basic", "lightning_basic", "poison_basic" };
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10, StartingSpells);
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 8, 2, 6);
        private static readonly GameConfig Config = new GameConfig(1, 1f);

        private static GridMap Room() => GridMovementTests.Parse(
            "##########",
            "#........#",
            "#........#",
            "##########");

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;
            public readonly List<ManaChangedEvent> ManaEvents = new List<ManaChangedEvent>();

            public void Wait(int turns)
            {
                for (int i = 0; i < turns; i++) Clock.SubmitPlayerCommand(PlayerActions.Wait(Config));
            }
        }

        private static Session Start(params GridPos[] enemies)
        {
            var world = new World(Room(), 5UL);
            world.InstallCoreSystems(new ShadowcastSightRule(), Magic, new ProjectM.Status.StatusBook(new ProjectM.Status.StatusDef[0]), ProjectM.Status.TerrainBook.Empty);
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            world.Events.Subscribe<ManaChangedEvent>(s.ManaEvents.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.SpellSlotCount);
            var ai = new MeleeChaserAi(world, Config, new ShadowcastSightRule());
            foreach (var p in enemies) UnitSpawner.SpawnEnemy(world, EnemyDef, p, ai);
            world.Events.Flush();
            return s;
        }

        [Test]
        public void Player_StartsFull_WithStartingSpellsInSlots()
        {
            var s = Start();
            Assert.That(s.Player.Mana, Is.EqualTo(10));
            Assert.That(s.Player.MaxMana, Is.EqualTo(10));
            Assert.That(s.Player.SpellSlots, Is.EqualTo(StartingSpells));
        }

        [Test]
        public void SpellSlots_FewerSpellsLeaveEmptySlots_TooManyThrow()
        {
            var two = new UnitDef("p", 1, 1, 1, 0, new[] { "a", "b" });
            var unit = Unit.FromDef(new EntityId(1), Faction.Player, two, new GridPos(0, 0), 4);
            Assert.That(unit.SpellSlots, Is.EqualTo(new[] { "a", "b", null, null }));

            Assert.Throws<ArgumentException>(() => Unit.FromDef(new EntityId(2), Faction.Player, PlayerDef, new GridPos(0, 0), 3));
        }

        [Test]
        public void TrySpend_SubtractsOrRefuses()
        {
            var s = Start();
            Assert.That(Mana.TrySpend(s.World, s.Player, 4), Is.True);
            Assert.That(s.Player.Mana, Is.EqualTo(6));
            Assert.That(Mana.TrySpend(s.World, s.Player, 7), Is.False, "모자람");
            Assert.That(s.Player.Mana, Is.EqualTo(6), "실패하면 그대로");
            s.World.Events.Flush();
            Assert.That(s.ManaEvents.Count, Is.EqualTo(1));
            Assert.That(s.ManaEvents[0].Mana, Is.EqualTo(6));
        }

        // 3틱마다 1: 틱 0 · 1 · 2 → 틱 2 끝에 +1
        [Test]
        public void Regen_OnePerThreeTicks_CappedAtMax()
        {
            var s = Start();
            Mana.TrySpend(s.World, s.Player, 5); // 5
            s.Wait(2);
            Assert.That(s.Player.Mana, Is.EqualTo(5));
            s.Wait(1);
            Assert.That(s.Player.Mana, Is.EqualTo(6));
            s.Wait(3);
            Assert.That(s.Player.Mana, Is.EqualTo(7));

            s.Wait(30);
            Assert.That(s.Player.Mana, Is.EqualTo(10), "최대치에서 멈춤");
        }

        [Test]
        public void Regen_SkipsUnitsWithoutMana()
        {
            var s = Start(new GridPos(8, 2));
            var enemy = s.World.Units.ToSortedList()[1];
            s.ManaEvents.Clear();
            s.Wait(6);
            Assert.That(enemy.MaxMana, Is.EqualTo(0));
            Assert.That(enemy.Mana, Is.EqualTo(0));
            Assert.That(s.ManaEvents, Is.Empty, "플레이어는 가득 차 있고 적은 마나가 없다");
        }

        [Test]
        public void Snapshot_RoundTripsManaAndSlots()
        {
            var s = Start();
            Mana.TrySpend(s.World, s.Player, 3);
            s.Player.SpellSlots[3] = null;
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => new MeleeChaserAi(w, Config, new ShadowcastSightRule()));

            var p = restored.Units.Get(s.Player.Id);
            Assert.That(p.Mana, Is.EqualTo(7));
            Assert.That(p.MaxMana, Is.EqualTo(10));
            Assert.That(p.SpellSlots, Is.EqualTo(new[] { "fire_basic", "ice_basic", "lightning_basic", null }));
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
        }

        [Test]
        public void Config_RejectsInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicConfig(0, 1, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicConfig(3, -1, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicConfig(3, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicConfig(3, 1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnitDef("x", 1, 1, 1, -1));
            Assert.Throws<ArgumentException>(() => new UnitDef("x", 1, 1, 1, 0, new[] { "" }));
        }

        // 검산: 프로젝트 에셋이 BALANCE.md 4번 표와 같은 값인지. BALANCE.md를 고치면 같이 고친다.
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var magic = AssetDatabase.LoadAssetAtPath<MagicConfigAsset>("Assets/_Project/Data/Magic/MagicConfig.asset");
            var player = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/Player.asset");
            var enemy = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/MeleeEnemy.asset");
            Assert.That(magic, Is.Not.Null, "MagicConfig.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");

            var m = magic.ToConfig();
            Assert.That(m.ManaRegenIntervalTicks, Is.EqualTo(3));
            Assert.That(m.ManaRegenAmount, Is.EqualTo(1));
            Assert.That(m.SpellSlotCount, Is.EqualTo(4));

            var p = player.ToDef();
            Assert.That(p.MaxMana, Is.EqualTo(10));
            Assert.That(p.StartingSpells, Is.EqualTo(StartingSpells));
            Assert.That(enemy.ToDef().MaxMana, Is.EqualTo(0));
        }
    }
}
