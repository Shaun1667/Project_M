using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-39 SPD 3~5티어 무기 (결정 2026-11-17): 데이터 표 · 최소 피해 = 티어 · 공격 지연(사이 · 시미터 · 글레이브) · 채찍 3칸 · 저장.</summary>
    public class WeaponTierTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10, null, accuracy: 10, evasion: 5);
        private static readonly UnitDef DummyDef = new UnitDef("enemy_melee", 500, 2, 6); // 회피 0 — 항상 맞음, 오래 버팀
        private static readonly WeaponRules Rules = new WeaponRules(3, 2); // BALANCE 13번

        private static ItemBook Book() => new ItemBook(new[]
        {
            new ItemDef("sword", ItemUse.EquipWeapon, weapon: new WeaponDef(20, 4, 100, 1, 1, tier: 3)),
            new ItemDef("sai", ItemUse.EquipWeapon, weapon: new WeaponDef(10, 2, 100, 1, 1, tier: 3, attackDelayPercent: 50)),
            new ItemDef("scimitar", ItemUse.EquipWeapon, weapon: new WeaponDef(16, 4, 100, 1, 1, tier: 3, attackDelayPercent: 80)),
            new ItemDef("whip", ItemUse.EquipWeapon, weapon: new WeaponDef(12, 3, 100, 1, 3, tier: 3)),
            new ItemDef("greatsword", ItemUse.EquipWeapon, weapon: new WeaponDef(30, 6, 100, 1, 1, tier: 5)),
            new ItemDef("greataxe", ItemUse.EquipWeapon, weapon: new WeaponDef(45, 6, 100, 1, 1, tier: 5)),
            new ItemDef("glaive", ItemUse.EquipWeapon, weapon: new WeaponDef(40, 8, 100, 1, 2, tier: 5, attackDelayPercent: 150)),
        }, Rules);

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
            var grid = GridMovementTests.Parse(new[]
            {
                "#########",
                "#.......#",
                "#.......#",
                "#.......#",
                "#.......#",
                "#########",
            });
            var world = new World(grid, 11UL);
            var vision = world.InstallCoreSystems(new ShadowcastSightRule(), Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput), Items = Book() };
            world.Events.Subscribe<UnitDamagedEvent>(s.Events.Add);
            world.Events.Subscribe<AttackMissedEvent>(s.Events.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount, Magic.InventorySlotCount);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static Unit Dummy(Session s, GridPos pos) => UnitSpawner.SpawnEnemy(s.World, DummyDef, pos, new Idle());

        private static void Wield(Session s, string itemId)
        {
            Inventory.TryAdd(s.World, null, s.Items, s.Player, InventoryRef.Item(itemId));
            int index = System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item(itemId));
            s.Clock.SubmitPlayerCommand(new EquipWeaponCommand(s.World, s.Items, index, itemId, Config.DefaultActionCostTicks));
            s.World.Events.Flush();
            Assert.That(s.Player.Weapon.ItemId, Is.EqualTo(itemId));
        }

        /// <summary>한 번 공격하고 (쓴 틱, 친 횟수 = 맞음 + 빗나감)을 돌려준다.</summary>
        private static (long ticks, int swings) Attack(Session s, Unit target)
        {
            s.Events.Clear();
            long before = s.World.Scheduler.CurrentTick;
            s.Clock.SubmitPlayerCommand(new AttackCommand(s.World, target.Id, MeleeWeapon.PlanAttack(s.Player, Config.DefaultActionCostTicks)));
            s.World.Events.Flush();
            int swings = s.Events.FindAll(e => (e is UnitDamagedEvent d && d.Target == target.Id) || e is AttackMissedEvent).Count;
            return (s.World.Scheduler.CurrentTick - before, swings);
        }

        // ── 최소 피해 = 티어 (SPD) ──

        [TestCase("sword", 0, 1, 7)]
        [TestCase("greatsword", 0, 2, 10)]
        [TestCase("greatsword", 3, 3, 16)]
        [TestCase("greataxe", 0, 2, 15)]
        public void Resolve_MinDamageUsesTier(string id, int upgrade, int min, int max)
        {
            var items = Book();
            Assert.That(items.TryGet(id, out var def), Is.True);
            var w = Weapons.Resolve(id, def.Weapon, items.WeaponRules, upgrade);
            Assert.That((w.MinDamage, w.MaxDamage), Is.EqualTo((min, max)), "BALANCE 13번 3~5티어 표");
        }

        // ── 공격 지연 ──

        [Test]
        public void Sai_SwingsTwicePerTick()
        {
            var s = Start();
            Wield(s, "sai");
            var dummy = Dummy(s, new GridPos(2, 1));
            for (int i = 0; i < 3; i++)
                Assert.That(Attack(s, dummy), Is.EqualTo((1L, 2)), "지연 50% = 1틱에 2번");
            Assert.That(s.Player.AttackCredit, Is.EqualTo(0));
        }

        [Test]
        public void Scimitar_ExtraSwingEveryFourthAttack()
        {
            var s = Start();
            Wield(s, "scimitar");
            var dummy = Dummy(s, new GridPos(2, 1));
            var swings = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                var r = Attack(s, dummy);
                Assert.That(r.ticks, Is.EqualTo(1L));
                swings.Add(r.swings);
            }
            Assert.That(swings, Is.EqualTo(new[] { 1, 1, 1, 2, 1, 1, 1, 2 }), "지연 80% = 1틱당 1.25번");
        }

        [Test]
        public void Glaive_AlternatesTwoAndOneTicks()
        {
            var s = Start();
            Wield(s, "glaive");
            var dummy = Dummy(s, new GridPos(2, 1));
            var ticks = new List<long>();
            for (int i = 0; i < 4; i++)
            {
                var r = Attack(s, dummy);
                Assert.That(r.swings, Is.EqualTo(1));
                ticks.Add(r.ticks);
            }
            Assert.That(ticks, Is.EqualTo(new[] { 2L, 1L, 2L, 1L }), "지연 150% = 평균 1.5틱");
        }

        [Test]
        public void NoDelayWeapon_KeepsOldCost()
        {
            var s = Start();
            Wield(s, "sword");
            var dummy = Dummy(s, new GridPos(2, 1));
            Assert.That(Attack(s, dummy), Is.EqualTo((1L, 1)));
            Assert.That(s.Player.AttackCredit, Is.EqualTo(0));
        }

        // ── 채찍 3칸 ──

        [Test]
        public void Whip_ReachesThreeCells_StraightAndDiagonal_WhenPathEmpty()
        {
            var s = Start();
            Wield(s, "whip");
            var straight = Dummy(s, new GridPos(4, 1));
            var diagonal = Dummy(s, new GridPos(4, 4));
            var bent = Dummy(s, new GridPos(4, 2));
            Assert.That(MeleeWeapon.CanReach(s.World, s.Player, straight.Pos), Is.True, "직선 3칸");
            Assert.That(MeleeWeapon.CanReach(s.World, s.Player, diagonal.Pos), Is.True, "대각선 3칸");
            Assert.That(MeleeWeapon.CanReach(s.World, s.Player, bent.Pos), Is.False, "(3, 1)은 직선 · 대각선이 아님");

            Dummy(s, new GridPos(3, 1)); // 사이 칸에 유닛
            Assert.That(MeleeWeapon.CanReach(s.World, s.Player, straight.Pos), Is.False, "사이 칸에 유닛 → 못 침");
        }

        [Test]
        public void Whip_CannotReachFour()
        {
            var s = Start();
            Wield(s, "whip");
            var far = Dummy(s, new GridPos(5, 1));
            Assert.That(MeleeWeapon.CanReach(s.World, s.Player, far.Pos), Is.False);
        }

        // ── 저장 ──

        [Test]
        public void Snapshot_KeepsDelayAndCredit()
        {
            var s = Start();
            Wield(s, "glaive");
            var dummy = Dummy(s, new GridPos(2, 1));
            Attack(s, dummy); // 2틱, 몫 50 남음
            Assert.That(s.Player.AttackCredit, Is.EqualTo(50));

            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(31), "3-39 무기 공격 지연 · 유닛 지연 몫 (정확한 값은 아래 3-40 — 32)");
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = WorldSnapshotCodec.Decode(bytes);
            var unit = System.Array.Find(restored.Units, u => u.Id == s.Player.Id);
            Assert.That(unit.AttackCredit, Is.EqualTo(50));
            Assert.That(unit.Weapon.AttackDelayPercent, Is.EqualTo(150));
            Assert.That(unit.Weapon.Reach, Is.EqualTo(2));
        }

        // ── 3-40 0티어 시작 무기 · 계열 ──

        private static ItemBook Tier0Book() => new ItemBook(new[]
        {
            new ItemDef("short_spear", ItemUse.EquipWeapon, weapon: new WeaponDef(12, 2, 100, 1, 1, 10, family: "spear")),
            new ItemDef("bandage", ItemUse.EquipWeapon, weapon: new WeaponDef(6, 2, 100, 1, 1, 10, hitsPerAttack: 2, family: "knuckle")),
            new ItemDef("spear", ItemUse.EquipWeapon, weapon: new WeaponDef(20, 4, 100, 2, 2, 12)),
        }, Rules);

        [Test]
        public void Wield_EquipsWithoutTurn_FamilyFollowsData()
        {
            var s = Start();
            s.Items = Tier0Book();
            var equipped = new List<WeaponEquippedEvent>();
            s.World.Events.Subscribe<WeaponEquippedEvent>(equipped.Add);
            long before = s.World.Scheduler.CurrentTick;
            Assert.That(Weapons.Wield(s.World, s.Items, s.Player, "short_spear"), Is.True);
            s.World.Events.Flush();
            Assert.That(s.World.Scheduler.CurrentTick, Is.EqualTo(before), "판 시작 장착 — 차례를 쓰지 않음");
            Assert.That((s.Player.Weapon.ItemId, s.Player.Weapon.Family), Is.EqualTo(("short_spear", "spear")));
            Assert.That((s.Player.Weapon.MinDamage, s.Player.Weapon.MaxDamage), Is.EqualTo((1, 4)), "BALANCE 13-2 단창 +0");
            Assert.That(equipped, Has.Count.EqualTo(1), "손에 든 모습이 켜지도록 장착 이벤트");

            Assert.That(Weapons.Wield(s.World, s.Items, s.Player, "bandage"), Is.True);
            Assert.That((s.Player.Weapon.Family, s.Player.Weapon.HitsPerAttack, s.Player.Weapon.MaxDamage), Is.EqualTo(("knuckle", 2, 2)));
            Assert.That(Weapons.Wield(s.World, s.Items, s.Player, "spear"), Is.True);
            Assert.That(s.Player.Weapon.Family, Is.EqualTo("spear"), "계열이 없으면 자기 ID");
            Assert.That(Weapons.Wield(s.World, s.Items, s.Player, "no_such_weapon"), Is.False);
        }

        [Test]
        public void Snapshot_KeepsWeaponFamily()
        {
            var s = Start();
            s.Items = Tier0Book();
            Weapons.Wield(s.World, s.Items, s.Player, "bandage");
            s.World.Events.Flush();
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(32), "3-40 무기 계열");
            var restored = WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(s.World.Capture()));
            var unit = System.Array.Find(restored.Units, u => u.Id == s.Player.Id);
            Assert.That((unit.Weapon.ItemId, unit.Weapon.Family), Is.EqualTo(("bandage", "knuckle")));
        }

        [Test]
        public void ProjectAssets_Tier0StartingWeapons()
        {
            var book = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            Assert.That(book.TryGet("short_spear", out var spear) && book.TryGet("bandage", out var wraps), Is.True);
            book.TryGet("bandage", out var bandage);
            Assert.That((spear.Weapon.Tier, spear.Weapon.MaxDamage, spear.Weapon.UpgradeEfficiency, spear.Weapon.Reach, spear.Weapon.StrengthRequirement, spear.Weapon.Family),
                Is.EqualTo((0, 12, 2, 1, 10, "spear")));
            Assert.That((bandage.Weapon.Tier, bandage.Weapon.MaxDamage, bandage.Weapon.HitsPerAttack, bandage.Weapon.StrengthRequirement, bandage.Weapon.Family),
                Is.EqualTo((0, 6, 2, 10, "knuckle")));

            var characters = AssetDatabase.LoadAssetAtPath<CharacterCatalogAsset>("Assets/_Project/Data/Units/CharacterCatalog.asset");
            var starts = new Dictionary<string, string>();
            foreach (var c in characters.Characters) starts[c.Id] = c.StartingWeapon;
            Assert.That(starts["hero_blond"], Is.EqualTo("short_spear"), "루멘 = 단창");
            Assert.That(starts["hero_black"], Is.EqualTo("bandage"), "시아 = 붕대");
        }

        // ── 프로젝트 데이터 = BALANCE 13번 ──

        [Test]
        public void ProjectAssets_HigherTiers_MatchBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            var book = catalog.ToBook();
            // id → (티어, 최대, 효율, 명중 %, 거리, 요구 힘, 지연 %, 방어, 기습 %)
            var expected = new Dictionary<string, (int, int, int, int, int, int, int, int, int)>
            {
                { "knuckle", (1, 5, 3, 100, 1, 10, 0, 0, 0) }, { "shortsword", (2, 15, 3, 100, 1, 12, 0, 0, 0) },
                { "sword", (3, 20, 4, 100, 1, 14, 0, 0, 0) }, { "mace", (3, 16, 4, 128, 1, 14, 0, 0, 0) },
                { "scimitar", (3, 16, 4, 100, 1, 14, 80, 0, 0) }, { "round_shield", (3, 12, 3, 100, 1, 14, 0, 4, 0) },
                { "sai", (3, 10, 2, 100, 1, 14, 50, 0, 0) }, { "whip", (3, 12, 3, 100, 3, 14, 0, 0, 0) },
                { "longsword", (4, 25, 5, 100, 1, 16, 0, 0, 0) }, { "battle_axe", (4, 20, 5, 124, 1, 16, 0, 0, 0) },
                { "flail", (4, 35, 8, 80, 1, 16, 0, 0, 0) }, { "runic_blade", (4, 20, 6, 100, 1, 16, 0, 0, 0) },
                { "assassins_blade", (4, 20, 5, 100, 1, 16, 0, 0, 50) }, { "crossbow", (4, 20, 5, 100, 1, 16, 0, 0, 0) },
                { "katana", (4, 20, 5, 100, 1, 16, 0, 0, 0) },
                { "greatsword", (5, 30, 6, 100, 1, 18, 0, 0, 0) }, { "war_hammer", (5, 24, 6, 120, 1, 18, 0, 0, 0) },
                { "glaive", (5, 40, 8, 100, 2, 18, 150, 0, 0) }, { "greataxe", (5, 45, 6, 100, 1, 20, 0, 0, 0) },
                { "greatshield", (5, 18, 5, 100, 1, 18, 0, 6, 0) }, { "gauntlet", (5, 15, 3, 100, 1, 18, 50, 0, 0) },
                { "war_scythe", (5, 40, 8, 80, 1, 18, 0, 0, 0) },
            };
            foreach (var pair in expected)
            {
                Assert.That(book.TryGet(pair.Key, out var def), Is.True, pair.Key);
                var w = def.Weapon;
                Assert.That((w.Tier, w.MaxDamage, w.UpgradeEfficiency, w.AccuracyPercent, w.Reach, w.StrengthRequirement, w.AttackDelayPercent, w.Defense, w.SurpriseMinPercent),
                    Is.EqualTo(pair.Value), pair.Key);
            }
        }
    }
}
