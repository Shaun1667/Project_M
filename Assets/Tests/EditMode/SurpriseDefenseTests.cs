using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.UI;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-7c 기습 · 방어 (결정 2026-10-19): 못 본 적 = 기습(항상 명중 · 더크 최소 피해 상승) · 방어 굴림(육척봉 0~2, 근접만).</summary>
    public class SurpriseDefenseTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 500, 3, 8, 10, null, accuracy: 10, evasion: 0); // 체력 500 — 방어 굴림을 여러 번 받기

        // BALANCE 13번 (요구 힘 0 — 힘 효과 없이 본다)
        private static ItemBook Book() => new ItemBook(new[]
        {
            new ItemDef("shortsword", ItemUse.EquipWeapon, weapon: new WeaponDef(15, 3, 100, 1, 1)),
            new ItemDef("dirk", ItemUse.EquipWeapon, weapon: new WeaponDef(12, 3, 100, 1, 1, 0, 67, 0)),
            new ItemDef("quarterstaff", ItemUse.EquipWeapon, weapon: new WeaponDef(12, 3, 100, 1, 1, 0, 0, 2)),
        }, new WeaponRules(3, 2));

        private sealed class Idle : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;
            public ItemBook Items;
            public readonly List<ISimEvent> Events = new List<ISimEvent>();
        }

        private static Session Start()
        {
            var grid = GridMovementTests.Parse("#######", "#.....#", "#.....#", "#######");
            var world = new World(grid, 9UL);
            var vision = world.InstallCoreSystems(new ShadowcastSightRule(), Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput), Items = Book() };
            world.Events.Subscribe<SurpriseAttackEvent>(s.Events.Add);
            world.Events.Subscribe<DamageBlockedEvent>(s.Events.Add);
            world.Events.Subscribe<UnitDamagedEvent>(s.Events.Add);
            world.Events.Subscribe<AttackMissedEvent>(s.Events.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount, Magic.InventorySlotCount);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static Unit Enemy(Session s, GridPos pos, int evasion = 0) =>
            UnitSpawner.SpawnEnemy(s.World, new UnitDef("enemy_melee", 500, 2, 6, 0, null, accuracy: 1000, evasion: evasion), pos, new Idle());

        private static void Hold(Session s, string id)
        {
            Inventory.TryAdd(s.World, null, s.Items, s.Player, InventoryRef.Item(id));
            Weapons.Equip(s.World, s.Items, s.Player, System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item(id)));
            s.World.Events.Flush();
        }

        private static List<T> Of<T>(Session s) where T : ISimEvent => s.Events.FindAll(e => e is T).ConvertAll(e => (T)e);

        // ── 기습 ──

        [Test]
        public void IsSurprise_OnlyWhenEnemyHasNotSeenPlayer()
        {
            var s = Start();
            var enemy = Enemy(s, new GridPos(2, 1));
            Assert.That(MeleeWeapon.IsSurprise(s.Player, enemy), Is.True, "기억 없음 = 못 봄");
            enemy.LastSeenTargetPos = s.Player.Pos;
            Assert.That(MeleeWeapon.IsSurprise(s.Player, enemy), Is.False, "본 적");
            Assert.That(MeleeWeapon.IsSurprise(enemy, s.Player), Is.False, "적이 치는 것은 기습 아님");
        }

        [Test]
        public void Surprise_AlwaysHits_EvenUnarmed()
        {
            var s = Start();
            var dodger = Enemy(s, new GridPos(2, 1), evasion: 100000); // 보통이면 거의 못 맞힘
            s.Clock.SubmitPlayerCommand(new AttackCommand(s.World, dodger.Id, 1));
            s.World.Events.Flush();
            Assert.That(Of<SurpriseAttackEvent>(s), Has.Count.EqualTo(1));
            Assert.That(Of<AttackMissedEvent>(s), Is.Empty, "기습은 항상 명중 (결정 2026-10-19)");
            Assert.That(dodger.Hp, Is.EqualTo(497), "맨손 3");
        }

        [Test]
        public void Dirk_SurpriseMin_67Percent()
        {
            var items = Book();
            items.TryGet("dirk", out var dirk);
            var plus0 = Weapons.Resolve("dirk", dirk.Weapon, items.WeaponRules, 0);
            var plus2 = Weapons.Resolve("dirk", dirk.Weapon, items.WeaponRules, 2);
            Assert.That((plus0.MinDamage, plus0.MaxDamage, MeleeWeapon.SurpriseMin(plus0)), Is.EqualTo((1, 4, 3)), "1 + 3 × 0.67 = 3.01 → 3");
            Assert.That((plus2.MinDamage, plus2.MaxDamage, MeleeWeapon.SurpriseMin(plus2)), Is.EqualTo((1, 6, 4)), "1 + 5 × 0.67 = 4.35 → 4");
            items.TryGet("shortsword", out var sword);
            var plain = Weapons.Resolve("shortsword", sword.Weapon, items.WeaponRules);
            Assert.That(MeleeWeapon.SurpriseMin(plain), Is.EqualTo(plain.MinDamage), "더크가 아니면 그대로");
        }

        [Test]
        public void Dirk_SurpriseDamage_RollsFromRaisedMin()
        {
            var s = Start();
            Hold(s, "dirk");
            var seen = new HashSet<int>();
            for (int i = 0; i < 200; i++) seen.Add(MeleeWeapon.RollDamage(s.World, s.Player, surprise: true));
            Assert.That(seen, Is.EquivalentTo(new[] { 3, 4 }), "기습 3~4");
            seen.Clear();
            for (int i = 0; i < 200; i++) seen.Add(MeleeWeapon.RollDamage(s.World, s.Player));
            Assert.That(seen, Is.EquivalentTo(new[] { 1, 2, 3, 4 }), "보통 1~4");
        }

        // ── 방어 ──

        [Test]
        public void Quarterstaff_BlocksZeroToTwo_MeleeOnly()
        {
            var s = Start();
            Hold(s, "quarterstaff");
            Assert.That(MeleeWeapon.DefenseOf(s.Player), Is.EqualTo(2));
            var enemy = Enemy(s, new GridPos(2, 1));
            var amounts = new HashSet<int>();
            for (int i = 0; i < 60; i++)
            {
                s.Events.Clear();
                int hpBefore = s.Player.Hp;
                var attack = new AttackCommand(s.World, s.Player.Id, 1);
                attack.Execute(new TickContext(s.World.Scheduler.CurrentTick, enemy.Id));
                s.World.Events.Flush();
                int taken = hpBefore - s.Player.Hp;
                int blocked = 0;
                foreach (var b in Of<DamageBlockedEvent>(s)) blocked += b.Blocked;
                Assert.That(taken + blocked, Is.EqualTo(2), "근접 적 피해 2 = 받은 피해 + 막은 양");
                amounts.Add(taken);
            }
            Assert.That(amounts, Is.SubsetOf(new[] { 0, 1, 2 }));
            Assert.That(amounts.Count, Is.GreaterThan(1), "굴림");
        }

        [Test]
        public void NoDefense_NoRandomUsed()
        {
            var a = Start();
            var b = Start();
            var ea = Enemy(a, new GridPos(2, 1));
            Enemy(b, new GridPos(2, 1));
            Assert.That(ea.IsAlive, Is.True);
            Assert.That(MeleeWeapon.ApplyDefense(a.World, a.Player, 2), Is.EqualTo(2), "맨손 = 방어 0");
            Assert.That(a.World.Rng.Get(RngStream.Combat).NextFloat(), Is.EqualTo(b.World.Rng.Get(RngStream.Combat).NextFloat()), "난수 안 씀");
        }

        // ── 저장 · 표시 ──

        [Test]
        public void Snapshot_RoundTripsSpecials()
        {
            var s = Start();
            Hold(s, "dirk");
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => new Idle());
            var w = restored.Units.Get(s.Player.Id).Weapon;
            Assert.That((w.SurpriseMinPercent, w.Defense), Is.EqualTo((67, 0)));
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(15));
        }

        [Test]
        public void Hud_ShowsDefenseAndLogsSurpriseAndBlock()
        {
            var s = Start();
            var text = new HudText(itemNames: new Dictionary<string, string> { { "quarterstaff", "육척봉" } });
            var hud = new HudModel(8, text);
            hud.Bind(s.World.Events, s.Player);
            Hold(s, "quarterstaff");
            Assert.That(text.WeaponLabel(hud.Weapon, hud.UnarmedDamage, hud.Strength), Is.EqualTo("무기: 육척봉 1~4 · 방어 0~2"));
            var enemy = Enemy(s, new GridPos(2, 1));
            s.World.Events.Publish(new SurpriseAttackEvent(s.Player.Id, enemy.Id));
            s.World.Events.Publish(new DamageBlockedEvent(s.Player.Id, 1));
            s.World.Events.Publish(new DamageBlockedEvent(enemy.Id, 1)); // 적이 막은 것은 로그 안 함
            s.World.Events.Flush();
            Assert.That(hud.Log, Does.Contain($"기습! 적 {enemy.Id}"));
            Assert.That(hud.Log, Does.Contain("방어: 1 막음"));
            Assert.That(System.Linq.Enumerable.Count(hud.Log, l => l.StartsWith("방어")), Is.EqualTo(1));
        }

        // 검산: ItemCatalog 에셋이 BALANCE 13번과 같은지
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var book = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            book.TryGet("dirk", out var dirk);
            book.TryGet("quarterstaff", out var staff);
            book.TryGet("shortsword", out var sword);
            Assert.That((dirk.Weapon.SurpriseMinPercent, dirk.Weapon.Defense), Is.EqualTo((67, 0)));
            Assert.That((staff.Weapon.SurpriseMinPercent, staff.Weapon.Defense), Is.EqualTo((0, 2)));
            Assert.That((sword.Weapon.SurpriseMinPercent, sword.Weapon.Defense), Is.EqualTo((0, 0)));
        }
    }
}
