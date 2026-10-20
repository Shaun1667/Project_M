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
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount);
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
            Assert.That(s.Player.QuickSlots, Is.EqualTo(Array.ConvertAll(StartingSpells, InventoryRef.Spell)));
            Assert.That(s.Player.InventorySlots.Length, Is.EqualTo(MagicConfig.DefaultInventorySlotCount));
        }

        [Test]
        public void StartingSpells_FillInventoryAndQuickSlots_TooManyThrow()
        {
            var two = new UnitDef("p", 1, 1, 1, 0, new[] { "a", "b" });
            var unit = Unit.FromDef(new EntityId(1), Faction.Player, two, new GridPos(0, 0), 4, 20);
            Assert.That(unit.QuickSlots, Is.EqualTo(new[] { InventoryRef.Spell("a"), InventoryRef.Spell("b"), InventoryRef.Empty, InventoryRef.Empty }));
            Assert.That(unit.InventorySlots[1], Is.EqualTo(InventoryRef.Spell("b")));
            Assert.That(unit.InventorySlots[2].IsEmpty, Is.True);

            Assert.Throws<ArgumentException>(() => Unit.FromDef(new EntityId(2), Faction.Player, PlayerDef, new GridPos(0, 0), 3, 20));
            Assert.Throws<ArgumentException>(() => Unit.FromDef(new EntityId(3), Faction.Player, PlayerDef, new GridPos(0, 0), 6, 3));
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
            s.Player.QuickSlots[3] = InventoryRef.Empty;
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => new MeleeChaserAi(w, Config, new ShadowcastSightRule()));

            var p = restored.Units.Get(s.Player.Id);
            Assert.That(p.Mana, Is.EqualTo(7));
            Assert.That(p.MaxMana, Is.EqualTo(10));
            Assert.That(p.QuickSlots, Is.EqualTo(new[] { InventoryRef.Spell("fire_basic"), InventoryRef.Spell("ice_basic"), InventoryRef.Spell("lightning_basic"), InventoryRef.Empty }));
            Assert.That(p.InventorySlots, Is.EqualTo(s.Player.InventorySlots), "인벤토리 칸 그대로 (3-2a)");
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
        }

        // 2-6: 10틱마다 최대 체력의 1% (2026-10-20부터 소수점 누적 — 최대 20이면 50틱마다 1), 플레이어만, 최대치에서 멈춤
        private static readonly MagicConfig MagicWithHpRegen = new MagicConfig(3, 1, 4, 10, 1);

        private static Session StartWithHpRegen(params GridPos[] enemies)
        {
            var world = new World(Room(), 5UL);
            world.InstallCoreSystems(new ShadowcastSightRule(), MagicWithHpRegen, new ProjectM.Status.StatusBook(new ProjectM.Status.StatusDef[0]), ProjectM.Status.TerrainBook.Empty);
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), MagicWithHpRegen.QuickSlotCount);
            foreach (var p in enemies) UnitSpawner.SpawnEnemy(world, EnemyDef, p, new WaitAi());
            world.Events.Flush();
            return s;
        }

        private sealed class WaitAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        [Test]
        public void HpRegen_PlayerHealsOnePercentPerTenTicks_UpToMax()
        {
            var s = StartWithHpRegen();
            var healed = new List<UnitHealedEvent>();
            s.World.Events.Subscribe<UnitHealedEvent>(healed.Add);
            Combat.ApplyDamage(s.World, EntityId.None, s.Player, 3); // 17/20

            s.Wait(49); // 틱 0 ~ 48 — 회복 주기 4번 = 0.8
            Assert.That(s.Player.Hp, Is.EqualTo(17));
            s.Wait(1); // 틱 49 → 5번째 주기, 누적 1.0
            Assert.That(s.Player.Hp, Is.EqualTo(18), "최대 체력 20의 1% = 0.2씩 누적 → 50틱에 1");

            s.Wait(200);
            Assert.That(s.Player.Hp, Is.EqualTo(20), "최대치에서 멈춤");
            s.World.Events.Flush();
            Assert.That(healed.Count, Is.EqualTo(3));
            Assert.That(healed[0].Amount, Is.EqualTo(1));
            Assert.That(healed[2].Hp, Is.EqualTo(20));
        }

        [Test]
        public void HpRegen_EnemiesDoNotHeal()
        {
            var s = StartWithHpRegen(new GridPos(7, 2));
            var enemy = s.World.Units.ToSortedList().Find(u => u.Faction == Faction.Enemy);
            Combat.ApplyDamage(s.World, EntityId.None, enemy, 3);
            s.Wait(30);
            Assert.That(enemy.Hp, Is.EqualTo(5));
        }

        [Test]
        public void HpRegen_AmountIsExactPercent_FractionsCarry()
        {
            int Sum(MagicConfig c, int maxHp, int cycles)
            {
                int total = 0;
                for (int k = 1; k <= cycles; k++) total += c.HpRegenAmount(maxHp, k);
                return total;
            }
            Assert.That(Sum(MagicWithHpRegen, 20, 4), Is.EqualTo(0), "0.2 × 4 = 0.8");
            Assert.That(Sum(MagicWithHpRegen, 20, 5), Is.EqualTo(1));
            Assert.That(Sum(MagicWithHpRegen, 20, 100), Is.EqualTo(20), "100주기(1000틱) = 최대 체력 100%");
            Assert.That(MagicWithHpRegen.HpRegenAmount(100, 1), Is.EqualTo(1), "최대 100이면 주기마다 1");
            Assert.That(Sum(MagicWithHpRegen, 150, 2), Is.EqualTo(3), "1.5 × 2");
            Assert.That(Magic.HpRegenAmount(20, 1), Is.EqualTo(0), "주기 0 = 회복 없음");
        }

        [Test]
        public void Config_RejectsInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicConfig(0, 1, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicConfig(3, -1, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicConfig(3, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicConfig(3, 1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicConfig(3, 1, 4, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagicConfig(3, 1, 4, 10, 101));
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
            Assert.That(m.QuickSlotCount, Is.EqualTo(6), "3-2a 빠른 칸 6개");
            Assert.That(m.InventorySlotCount, Is.EqualTo(20), "3-2a 인벤토리 20칸");
            Assert.That(m.HpRegenIntervalTicks, Is.EqualTo(10), "2-6 체력 자동 회복 주기");
            Assert.That(m.HpRegenPercent, Is.EqualTo(1));

            var p = player.ToDef();
            Assert.That(p.MaxMana, Is.EqualTo(10));
            Assert.That(p.StartingSpells, Is.Empty, "3-1부터 시작 칸은 빈 칸 — 주문은 게임 중에 얻음 (REACTIONS.md D8)");
            Assert.That(enemy.ToDef().MaxMana, Is.EqualTo(0));
        }
    }
}
