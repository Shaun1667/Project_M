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
    /// <summary>3-10a 갑옷 (결정 2026-10-20 — SPD 5종 · ÷3): 방어 범위 · 요구 힘 · 입기/바꾸기 · 방어 굴림 · 힘 모자람 벌점 · 회피 · 강화 · 배치 · 저장 · 표시.</summary>
    public class ArmorTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10, null, accuracy: 10, evasion: 5, strength: 10);

        // BALANCE 15번
        internal static ItemBook Book() => new ItemBook(new[]
        {
            new ItemDef("shortsword", ItemUse.EquipWeapon, weapon: new WeaponDef(15, 3, 100, 1, 1, 12)),
            new ItemDef("scroll_upgrade", ItemUse.Scroll, amount: 1, effect: ConsumableEffect.Upgrade),
            new ItemDef("armor_cloth", ItemUse.EquipArmor, armor: new ArmorDef(1, 10)),
            new ItemDef("armor_leather", ItemUse.EquipArmor, armor: new ArmorDef(2, 12)),
            new ItemDef("armor_mail", ItemUse.EquipArmor, armor: new ArmorDef(3, 14)),
            new ItemDef("armor_scale", ItemUse.EquipArmor, armor: new ArmorDef(4, 16)),
            new ItemDef("armor_plate", ItemUse.EquipArmor, armor: new ArmorDef(5, 18)),
        }, new WeaponRules(3, 2), new ArmorRules(3, 2));

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
        }

        private static Session Start(ulong seed = 5UL, int strength = 10)
        {
            var world = new World(GridMovementTests.Parse("#######", "#.....#", "#.....#", "#######"), seed);
            var vision = world.InstallCoreSystems(new ShadowcastSightRule(), Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput), Items = Book() };
            var def = strength == 10 ? PlayerDef : new UnitDef("player", 20, 3, 8, 10, null, accuracy: 10, evasion: 5, strength: strength);
            s.Player = UnitSpawner.SpawnPlayer(world, def, new GridPos(1, 1), Magic.QuickSlotCount, Magic.InventorySlotCount);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static void Give(Session s, string id, int upgrade = 0) => Inventory.TryAdd(s.World, null, s.Items, s.Player, InventoryRef.Item(id, upgrade));

        private static void Equip(Session s, string id, int upgrade = 0)
        {
            var entry = InventoryRef.Item(id, upgrade);
            s.Clock.SubmitPlayerCommand(new EquipWeaponCommand(s.World, s.Items, System.Array.IndexOf(s.Player.InventorySlots, entry), entry, 1));
            s.World.Events.Flush();
        }

        // ── 수치 (BALANCE 15번 표) ──

        [TestCase("armor_cloth", 0, 10, 0, 2)]
        [TestCase("armor_leather", 0, 12, 0, 4)]
        [TestCase("armor_mail", 0, 14, 0, 6)]
        [TestCase("armor_scale", 0, 16, 0, 8)]
        [TestCase("armor_plate", 0, 18, 0, 10)]
        [TestCase("armor_cloth", 1, 9, 1, 3)]
        [TestCase("armor_plate", 3, 16, 3, 25)]
        public void Resolve_MatchesSpdFormula(string id, int upgrade, int strength, int min, int max)
        {
            var book = Book();
            book.TryGet(id, out var def);
            var a = Armors.Resolve(id, def.Armor, book.ArmorRules, upgrade);
            Assert.That((a.StrengthRequirement, a.DefenseMin, a.DefenseMax), Is.EqualTo((strength, min, max)), "SPD: 강화 ~ 티어 × (2 + 강화)");
        }

        [TestCase("armor_cloth", 0, 1)]
        [TestCase("armor_leather", 0, 1)]
        [TestCase("armor_mail", 0, 2)]
        [TestCase("armor_scale", 0, 3)]
        [TestCase("armor_plate", 0, 3)]
        public void ShownDefense_DividedByThree(string id, int min, int max)
        {
            var book = Book();
            book.TryGet(id, out var def);
            var a = Armors.Resolve(id, def.Armor, book.ArmorRules);
            Assert.That((a.ShownMin, a.ShownMax), Is.EqualTo((min, max)));
        }

        // ── 입기 ──

        [Test]
        public void Equip_TakesTurn_PreviousGoesToInventory_KeepsUpgrade()
        {
            var s = Start();
            Assert.That(Armors.Wear(s.World, s.Items, s.Player, "armor_cloth", 1), Is.True);
            Give(s, "armor_leather", 2);
            long before = s.World.Scheduler.CurrentTick;
            Equip(s, "armor_leather", 2);
            Assert.That(s.World.Scheduler.CurrentTick - before, Is.EqualTo(1), "차례 1");
            Assert.That((s.Player.Armor.ItemId, s.Player.Armor.Upgrade), Is.EqualTo(("armor_leather", 2)));
            Assert.That(s.Player.InventorySlots, Has.Member(InventoryRef.Item("armor_cloth", 1)), "입던 갑옷은 강화째 인벤토리로");
            Assert.That(s.Player.InventorySlots, Has.No.Member(InventoryRef.Item("armor_leather", 2)));
            Assert.That(s.Player.Weapon.IsEmpty, Is.True, "무기는 그대로");
        }

        // ── 방어 ──

        [Test]
        public void Defense_RollsWithinDividedRange()
        {
            var seen = new HashSet<int>();
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var s = Start(seed, 18);
                Armors.Wear(s.World, s.Items, s.Player, "armor_plate");
                int blocked = Armors.RollDefense(s.World, s.Player);
                Assert.That(blocked, Is.InRange(0, 3), "판금 0~10 ÷ 3");
                seen.Add(blocked);
            }
            Assert.That(seen, Is.EquivalentTo(new[] { 0, 1, 2, 3 }), "고르게 나옴");
        }

        [Test]
        public void Defense_StrengthDeficit_SubtractsTwoPerPoint_AndCutsEvasion()
        {
            var s = Start();
            Armors.Wear(s.World, s.Items, s.Player, "armor_plate"); // 요구 18, 힘 10 → 8 모자람 → 0~10 − 16 → 항상 0
            Assert.That(Armors.StrengthDeficit(s.Player), Is.EqualTo(8));
            for (int i = 0; i < 20; i++) Assert.That(Armors.RollDefense(s.World, s.Player), Is.EqualTo(0));
            Assert.That(Armors.EvasionFactor(s.Player), Is.EqualTo((float)System.Math.Pow(1.5, -8)).Within(1e-6));
            Armors.Wear(s.World, s.Items, s.Player, "armor_cloth");
            Assert.That(Armors.EvasionFactor(s.Player), Is.EqualTo(1f), "천 갑옷 요구 10 = 힘 10");
        }

        [Test]
        public void ApplyDefense_ReducesMeleeDamage_NotBelowZero()
        {
            for (ulong seed = 1; seed <= 50; seed++)
            {
                var s = Start(seed, 16);
                Armors.Wear(s.World, s.Items, s.Player, "armor_plate", 3); // 3~25 ÷ 3 = 1~8
                int after = MeleeWeapon.ApplyDefense(s.World, s.Player, 5);
                Assert.That(after, Is.InRange(0, 4), "최소 1은 막음");
            }
        }

        [Test]
        public void NoArmor_NoRandomUsed()
        {
            var a = Start(9UL);
            var b = Start(9UL);
            MeleeWeapon.ApplyDefense(a.World, a.Player, 3);
            Assert.That(a.World.Rng.Get(RngStream.Combat).Range(0, 1000), Is.EqualTo(b.World.Rng.Get(RngStream.Combat).Range(0, 1000)),
                "맨몸 · 무기 방어 0이면 난수를 쓰지 않음 — 기존 판정 순서 그대로");
        }

        // ── 강화 ──

        [Test]
        public void UpgradeScroll_NoWeapon_UpgradesArmor_WithWeapon_UpgradesWeapon()
        {
            var s = Start();
            Armors.Wear(s.World, s.Items, s.Player, "armor_cloth");
            Give(s, "scroll_upgrade");
            s.Clock.SubmitPlayerCommand(new UseConsumableCommand(s.World, new ConsumableRules(s.Items), System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("scroll_upgrade")),
                InventoryRef.Item("scroll_upgrade"), 1));
            s.World.Events.Flush();
            Assert.That(s.Player.Armor.Upgrade, Is.EqualTo(1), "무기가 없으면 갑옷");
            Assert.That(s.Player.Armor.StrengthRequirement, Is.EqualTo(9));

            Give(s, "shortsword");
            Equip(s, "shortsword");
            Give(s, "scroll_upgrade");
            s.Clock.SubmitPlayerCommand(new UseConsumableCommand(s.World, new ConsumableRules(s.Items), System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("scroll_upgrade")),
                InventoryRef.Item("scroll_upgrade"), 1, UpgradeTarget.Armor));
            s.World.Events.Flush();
            Assert.That((s.Player.Armor.Upgrade, s.Player.Weapon.Upgrade), Is.EqualTo((2, 0)), "갑옷을 고르면 갑옷");
            Assert.That(Consumables.ResolveTarget(s.Player, UpgradeTarget.Auto), Is.EqualTo(UpgradeTarget.Weapon), "Auto는 무기 먼저");
        }

        [Test]
        public void UpgradeScroll_NothingEquipped_NotUsed()
        {
            var s = Start();
            Give(s, "scroll_upgrade");
            Assert.That(Consumables.Check(s.Items, s.Player, InventoryRef.Item("scroll_upgrade")), Is.EqualTo(ConsumeResult.NoWeapon));
        }

        // ── 배치 ──

        [Test]
        public void PlanArmors_TwoDistinctTierTwoPlus_Deterministic()
        {
            var config = new LootConfig(2, 3, 0, 0, 0, 2);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var a = new World(GridMovementTests.Parse("###", "#.#", "###"), seed);
                var b = new World(GridMovementTests.Parse("###", "#.#", "###"), seed);
                Loot.PlanArmors(a, Book(), config, 5);
                Loot.PlanArmors(b, Book(), config, 5);
                var plan = a.Run.PendingFloorItems;
                Assert.That(plan.Count, Is.EqualTo(2));
                Assert.That(plan.Select(p => p.ItemId).Distinct().Count(), Is.EqualTo(2), "같은 종 안 겹침");
                Assert.That(plan.Any(p => p.ItemId == "armor_cloth"), Is.False, "천 갑옷은 입고 시작");
                Assert.That(plan.All(p => p.Floor >= 1 && p.Floor <= 4), Is.True);
                Assert.That(b.Run.PendingFloorItems, Is.EqualTo(plan));
            }
        }

        // ── 저장 · 표시 ──

        [Test]
        public void Snapshot_RoundTripsArmor()
        {
            var s = Start();
            Armors.Wear(s.World, s.Items, s.Player, "armor_mail", 2);
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(s.World.Capture())), (w, id) => new Idle());
            PlayerActions.TryGetPlayer(restored, out var p);
            Assert.That(p.Armor, Is.EqualTo(s.Player.Armor));
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(17), "3-10a 갑옷");
        }

        [Test]
        public void HudText_ArmorLabel()
        {
            var book = Book();
            book.TryGet("armor_cloth", out var def);
            var text = new HudText(itemNames: new Dictionary<string, string> { { "armor_cloth", "천 갑옷" } });
            Assert.That(text.ArmorLabel(Armors.Resolve("armor_cloth", def.Armor, book.ArmorRules), 10), Is.EqualTo("갑옷: 천 갑옷 방어 0~1 · 힘 10/10"));
            Assert.That(text.ArmorLabel(EquippedArmor.Empty, 10), Is.EqualTo("갑옷: 없음"));
            Assert.That(text.Worn("armor_cloth"), Is.EqualTo("천 갑옷을 입었다"));
        }

        // 검산: 에셋이 BALANCE 3 · 11 · 15번과 같은지
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var book = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            Assert.That((book.ArmorRules.DefenseDivisor, book.ArmorRules.DeficitPenalty), Is.EqualTo((3, 2)));
            var expected = Book();
            foreach (var id in new[] { "armor_cloth", "armor_leather", "armor_mail", "armor_scale", "armor_plate" })
            {
                Assert.That(book.TryGet(id, out var a), Is.True, id);
                expected.TryGet(id, out var e);
                Assert.That((a.Use, a.Armor.Tier, a.Armor.StrengthRequirement), Is.EqualTo((e.Use, e.Armor.Tier, e.Armor.StrengthRequirement)), id);
            }
            var loot = AssetDatabase.LoadAssetAtPath<LootConfigAsset>("Assets/_Project/Data/Items/LootConfig.asset").ToConfig();
            Assert.That(loot.ArmorsPerRun, Is.EqualTo(2));
            var player = AssetDatabase.LoadAssetAtPath<UnitDefAsset>("Assets/_Project/Data/Units/Player.asset").ToDef();
            Assert.That(player.StartingArmor, Is.EqualTo("armor_cloth"));
        }
    }
}
