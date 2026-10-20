using System.Collections.Generic;
using System.Linq;
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
    /// <summary>3-7b 힘 · 강화 (결정 2026-10-19): 요구 힘 · 명중 벌점 · 남는 힘 보너스 · 힘 물약 · 강화 주문서(무기 자체에) · 차례 1 · 배치 · 저장.</summary>
    public class StrengthUpgradeTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10, null, accuracy: 10, evasion: 5, strength: 10);
        private static readonly UnitDef DummyDef = new UnitDef("enemy_melee", 200, 2, 6);

        // BALANCE 9 · 13번 (요구 힘 12)
        private static ItemBook Book() => new ItemBook(new[]
        {
            new ItemDef("shortsword", ItemUse.EquipWeapon, weapon: new WeaponDef(15, 3, 100, 1, 1, 12)),
            new ItemDef("hand_axe", ItemUse.EquipWeapon, weapon: new WeaponDef(12, 3, 132, 1, 1, 12)),
            new ItemDef("potion_strength", ItemUse.Potion, amount: 1, effect: ConsumableEffect.Strength),
            new ItemDef("scroll_upgrade", ItemUse.Scroll, amount: 1, effect: ConsumableEffect.Upgrade),
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
            var world = new World(grid, 5UL);
            var vision = world.InstallCoreSystems(new ShadowcastSightRule(), Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput), Items = Book() };
            world.Events.Subscribe<StrengthChangedEvent>(s.Events.Add);
            world.Events.Subscribe<WeaponUpgradedEvent>(s.Events.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount, Magic.InventorySlotCount);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static void Give(Session s, string id, int upgrade = 0) => Inventory.TryAdd(s.World, null, s.Items, s.Player, InventoryRef.Item(id, upgrade));

        private static int IndexOf(Session s, InventoryRef entry) => System.Array.IndexOf(s.Player.InventorySlots, entry);

        private static void Equip(Session s, string id, int upgrade = 0)
        {
            var entry = InventoryRef.Item(id, upgrade);
            s.Clock.SubmitPlayerCommand(new EquipWeaponCommand(s.World, s.Items, IndexOf(s, entry), entry, 1));
            s.World.Events.Flush();
        }

        private static void Use(Session s, string id)
        {
            var entry = InventoryRef.Item(id);
            s.Clock.SubmitPlayerCommand(new UseConsumableCommand(s.World, s.Items, IndexOf(s, entry), entry, 1));
            s.World.Events.Flush();
        }

        // ── 요구 힘 (사용자 결정: 첫 강화 −1, 그 뒤 2단계마다 −1) ──

        [TestCase(0, 12)]
        [TestCase(1, 11)]
        [TestCase(2, 11)]
        [TestCase(3, 10)]
        [TestCase(4, 10)]
        [TestCase(5, 9)]
        public void StrengthRequirement_FirstUpgradeThenEveryTwo(int upgrade, int expected)
        {
            Book().TryGet("shortsword", out var def);
            Assert.That(Weapons.StrengthRequirement(def.Weapon, upgrade), Is.EqualTo(expected));
        }

        [Test]
        public void StrengthDeficit_DividesAccuracyBy1Point5Each()
        {
            var s = Start();
            Give(s, "shortsword");
            Equip(s, "shortsword");
            Assert.That(MeleeWeapon.StrengthDeficit(s.Player), Is.EqualTo(2), "힘 10 · 요구 12");
            Assert.That(MeleeWeapon.StrengthAccuracyFactor(s.Player), Is.EqualTo(1f / 2.25f).Within(1e-5f));
            Give(s, "potion_strength");
            Use(s, "potion_strength");
            Give(s, "potion_strength");
            Use(s, "potion_strength");
            Assert.That(MeleeWeapon.StrengthAccuracyFactor(s.Player), Is.EqualTo(1f), "힘 12 = 벌점 없음");
        }

        [Test]
        public void ExcessStrength_AddsZeroToExcessBonus()
        {
            var s = Start();
            Give(s, "shortsword");
            Equip(s, "shortsword");
            for (int i = 0; i < 4; i++) { Give(s, "potion_strength"); Use(s, "potion_strength"); }
            Assert.That(MeleeWeapon.ExcessStrength(s.Player), Is.EqualTo(2), "힘 14 · 요구 12");
            var seen = new HashSet<int>();
            for (int i = 0; i < 300; i++) seen.Add(MeleeWeapon.RollDamage(s.World, s.Player));
            Assert.That(seen.Min(), Is.GreaterThanOrEqualTo(1));
            Assert.That(seen.Max(), Is.EqualTo(5 + 2), "최대 5 + 보너스 0~2");
        }

        [Test]
        public void Unarmed_NoStrengthEffect()
        {
            var s = Start();
            Assert.That(MeleeWeapon.StrengthDeficit(s.Player), Is.EqualTo(0));
            Assert.That(MeleeWeapon.ExcessStrength(s.Player), Is.EqualTo(0));
            Assert.That(MeleeWeapon.RollDamage(s.World, s.Player), Is.EqualTo(3));
        }

        // ── 힘 물약 ──

        [Test]
        public void StrengthPotion_PlusOne_TakesTurn_Consumed()
        {
            var s = Start();
            Give(s, "potion_strength");
            long before = s.World.Scheduler.CurrentTick;
            Use(s, "potion_strength");
            Assert.That(s.World.Scheduler.CurrentTick - before, Is.EqualTo(1), "차례 1 (결정 2026-10-19)");
            Assert.That(s.Player.Strength, Is.EqualTo(11));
            Assert.That(s.Player.InventorySlots, Has.No.Member(InventoryRef.Item("potion_strength")));
            var e = s.Events.Find(x => x is StrengthChangedEvent) as StrengthChangedEvent;
            Assert.That((e.Strength, e.Gained), Is.EqualTo((11, 1)));
        }

        // ── 강화 주문서 ──

        [Test]
        public void UpgradeScroll_NoWeapon_NotUsed()
        {
            var s = Start();
            Give(s, "scroll_upgrade");
            Assert.That(Consumables.Check(s.Items, s.Player, InventoryRef.Item("scroll_upgrade")), Is.EqualTo(ConsumeResult.NoWeapon));
            Assert.That(Consumables.Use(s.World, s.Items, s.Player, IndexOf(s, InventoryRef.Item("scroll_upgrade"))), Is.EqualTo(ConsumeResult.NoWeapon));
            Assert.That(s.Player.InventorySlots, Has.Member(InventoryRef.Item("scroll_upgrade")), "그대로 남음");
        }

        [Test]
        public void UpgradeScroll_RaisesHeldWeapon()
        {
            var s = Start();
            Give(s, "shortsword");
            Equip(s, "shortsword");
            Give(s, "scroll_upgrade");
            Use(s, "scroll_upgrade");
            var w = s.Player.Weapon;
            Assert.That((w.Upgrade, w.MinDamage, w.MaxDamage, w.StrengthRequirement), Is.EqualTo((1, 1, 6, 11)));
            Assert.That(s.Player.InventorySlots, Has.No.Member(InventoryRef.Item("scroll_upgrade")));
            Assert.That(s.Events.Exists(x => x is WeaponUpgradedEvent), Is.True);
        }

        [Test]
        public void Upgrade_StaysWithWeapon_WhenSwapped()
        {
            var s = Start();
            Give(s, "shortsword");
            Give(s, "hand_axe");
            Equip(s, "shortsword");
            Give(s, "scroll_upgrade");
            Use(s, "scroll_upgrade");
            Equip(s, "hand_axe");
            Assert.That(s.Player.InventorySlots, Has.Member(InventoryRef.Item("shortsword", 1)), "소검+1로 인벤토리에 (결정 2026-10-19: 무기 자체에)");
            Assert.That(s.Player.Weapon.Upgrade, Is.EqualTo(0), "손도끼는 +0");
            Equip(s, "shortsword", 1);
            Assert.That((s.Player.Weapon.ItemId, s.Player.Weapon.Upgrade, s.Player.Weapon.MaxDamage), Is.EqualTo(("shortsword", 1, 6)));
        }

        // ── 배치 ──

        [Test]
        public void PlanConsumables_ThreeScrollsTwoPotions_AfterExistingPlan()
        {
            var config = new LootConfig(2, 3, 0, 3, 2);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var a = new World(GridMovementTests.Parse("###", "#.#", "###"), seed);
                var b = new World(GridMovementTests.Parse("###", "#.#", "###"), seed);
                Loot.PlanConsumables(a, Book(), config, 5);
                Loot.PlanConsumables(b, Book(), config, 5);
                var plan = a.Run.PendingFloorItems;
                int scrolls = 0, potions = 0;
                foreach (var p in plan)
                {
                    Assert.That(p.Floor, Is.InRange(1, 4), $"시드 {seed}: 보스 층 제외");
                    if (p.ItemId == "scroll_upgrade") scrolls++;
                    if (p.ItemId == "potion_strength") potions++;
                }
                Assert.That((scrolls, potions), Is.EqualTo((3, 2)));
                Assert.That(b.Run.PendingFloorItems, Is.EqualTo(plan), "같은 시드 = 같은 계획");
            }
        }

        // ── 저장 · 표시 ──

        [Test]
        public void Snapshot_RoundTripsStrengthAndUpgrades()
        {
            var s = Start();
            Give(s, "shortsword");
            Give(s, "hand_axe", 2);
            Equip(s, "shortsword");
            Give(s, "scroll_upgrade");
            Use(s, "scroll_upgrade");
            Give(s, "potion_strength");
            Use(s, "potion_strength");
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => new Idle());
            var p = restored.Units.Get(s.Player.Id);
            Assert.That(p.Strength, Is.EqualTo(11));
            Assert.That(p.Weapon, Is.EqualTo(s.Player.Weapon));
            Assert.That(p.InventorySlots, Has.Member(InventoryRef.Item("hand_axe", 2)));
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(15));
        }

        [Test]
        public void Hud_ShowsStrengthAndUpgrade()
        {
            var s = Start();
            var text = new HudText(itemNames: new Dictionary<string, string> { { "shortsword", "소검" }, { "hand_axe", "손도끼" } });
            var hud = new HudModel(8, text);
            hud.Bind(s.World.Events, s.Player);
            Give(s, "shortsword");
            Equip(s, "shortsword");
            Assert.That(text.WeaponLabel(hud.Weapon, hud.UnarmedDamage, hud.Strength), Is.EqualTo("무기: 소검 1~5 · 힘 10/12"));
            Give(s, "scroll_upgrade");
            Use(s, "scroll_upgrade");
            Assert.That(text.WeaponLabel(hud.Weapon, hud.UnarmedDamage, hud.Strength), Is.EqualTo("무기: 소검+1 1~6 · 힘 10/11"));
            Assert.That(hud.Log, Does.Contain("강화: 소검+1 (1~6, 요구 힘 11)"));
            Give(s, "potion_strength");
            Use(s, "potion_strength");
            Assert.That(hud.Strength, Is.EqualTo(11));
            Assert.That(hud.Log, Does.Contain("힘이 올랐다 (힘 11)"));
            Assert.That(text.Entry(InventoryRef.Item("hand_axe", 2)), Is.EqualTo("손도끼+2"));
            Assert.That(ResultModel.From(s.World, 1, s.Player, text).Equipment, Is.EqualTo("소검+1"));
        }

        // 검산: 에셋이 BALANCE 3 · 9 · 11 · 13번과 같은지
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var book = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            foreach (var id in new[] { "shortsword", "hand_axe", "spear", "quarterstaff", "dirk", "sickle" })
            {
                Assert.That(book.TryGet(id, out var def), Is.True, id);
                Assert.That(def.Weapon.StrengthRequirement, Is.EqualTo(12), id);
            }
            Assert.That(book.TryGet("potion_strength", out var potion) && potion.Use == ItemUse.Potion && potion.Effect == ConsumableEffect.Strength && potion.Amount == 1, Is.True);
            Assert.That(book.TryGet("scroll_upgrade", out var scroll) && scroll.Use == ItemUse.Scroll && scroll.Effect == ConsumableEffect.Upgrade && scroll.Amount == 1, Is.True);
            var loot = AssetDatabase.LoadAssetAtPath<LootConfigAsset>("Assets/_Project/Data/Items/LootConfig.asset").ToConfig();
            Assert.That((loot.UpgradeScrollsPerRun, loot.StrengthPotionsPerRun), Is.EqualTo((3, 2)));
            var player = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/Player.asset").ToDef();
            Assert.That(player.Strength, Is.EqualTo(10));
        }
    }
}
