using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.UI;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-7a 무기 기본 (결정 2026-10-19 — Docs/장비_데이터.md 4번): 1/3 배율 · 장착(차례 1) · 피해 굴림 · 명중 배수 · 창 2칸 · 2틱 · 배치 · 저장.</summary>
    public class WeaponTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10, null, accuracy: 10, evasion: 5);
        private static readonly UnitDef DummyDef = new UnitDef("enemy_melee", 50, 2, 6); // 회피 0 — 항상 맞음
        private static readonly WeaponRules Rules = new WeaponRules(3, 2); // BALANCE 13번

        // Docs/장비_데이터.md 1번 표 (SPD 크기)
        public static ItemBook Book() => new ItemBook(new[]
        {
            new ItemDef("fusion_device", ItemUse.Fusion),
            new ItemDef("spellbook_fire", ItemUse.LearnSpell, "fire_basic"),
            new ItemDef("shortsword", ItemUse.EquipWeapon, weapon: new WeaponDef(15, 3, 100, 1, 1)),
            new ItemDef("hand_axe", ItemUse.EquipWeapon, weapon: new WeaponDef(12, 3, 132, 1, 1)),
            new ItemDef("spear", ItemUse.EquipWeapon, weapon: new WeaponDef(20, 4, 100, 2, 2)),
            new ItemDef("quarterstaff", ItemUse.EquipWeapon, weapon: new WeaponDef(12, 3, 100, 1, 1)),
            new ItemDef("dirk", ItemUse.EquipWeapon, weapon: new WeaponDef(12, 3, 100, 1, 1)),
            new ItemDef("sickle", ItemUse.EquipWeapon, weapon: new WeaponDef(20, 3, 68, 1, 1)),
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

        private static Session Start(string map = null)
        {
            var grid = GridMovementTests.Parse(map != null ? map.Split('|') : new[]
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
            world.Events.Subscribe<WeaponEquippedEvent>(s.Events.Add);
            world.Events.Subscribe<UnitDamagedEvent>(s.Events.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount, Magic.InventorySlotCount);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static Unit Dummy(Session s, GridPos pos) => UnitSpawner.SpawnEnemy(s.World, DummyDef, pos, new Idle());

        private static void Give(Session s, string itemId) => Inventory.TryAdd(s.World, null, s.Items, s.Player, InventoryRef.Item(itemId));

        private static void Equip(Session s, string itemId)
        {
            int index = System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item(itemId));
            Assert.That(index, Is.GreaterThanOrEqualTo(0), itemId + " 인벤토리에 없음");
            s.Clock.SubmitPlayerCommand(new EquipWeaponCommand(s.World, s.Items, index, itemId, Config.DefaultActionCostTicks));
            s.World.Events.Flush();
        }

        private static List<T> Of<T>(Session s) where T : ISimEvent => s.Events.FindAll(e => e is T).ConvertAll(e => (T)e);

        // ── 수치 (1/3 배율) ──

        [TestCase("shortsword", 0, 1, 5)]
        [TestCase("shortsword", 3, 2, 8)]
        [TestCase("hand_axe", 0, 1, 4)]
        [TestCase("spear", 0, 1, 7)]
        [TestCase("spear", 1, 1, 8)]
        [TestCase("spear", 3, 2, 11)]
        [TestCase("sickle", 0, 1, 7)]
        [TestCase("sickle", 3, 2, 10)]
        [TestCase("dirk", 2, 1, 6)]
        public void Resolve_MatchesDesignTable(string id, int upgrade, int min, int max)
        {
            var items = Book();
            Assert.That(items.TryGet(id, out var def), Is.True);
            var w = Weapons.Resolve(id, def.Weapon, items.WeaponRules, upgrade);
            Assert.That((w.MinDamage, w.MaxDamage), Is.EqualTo((min, max)), "Docs/장비_데이터.md 4-1 표");
        }

        [Test]
        public void Resolve_CarriesAccuracyCostReach()
        {
            var items = Book();
            items.TryGet("spear", out var spear);
            items.TryGet("hand_axe", out var axe);
            var s = Weapons.Resolve("spear", spear.Weapon, items.WeaponRules);
            var a = Weapons.Resolve("hand_axe", axe.Weapon, items.WeaponRules);
            Assert.That((s.AttackCostTicks, s.Reach, s.AccuracyPercent), Is.EqualTo((2, 2, 100)));
            Assert.That((a.AttackCostTicks, a.Reach, a.AccuracyPercent), Is.EqualTo((1, 1, 132)));
        }

        // ── 장착 ──

        [Test]
        public void Equip_TakesOneTurn_MovesItemOutOfInventory()
        {
            var s = Start();
            Give(s, "shortsword");
            long before = s.World.Scheduler.CurrentTick;
            Equip(s, "shortsword");
            Assert.That(s.World.Scheduler.CurrentTick - before, Is.EqualTo(1), "차례 1 (결정 2026-10-19)");
            Assert.That(s.Player.Weapon.ItemId, Is.EqualTo("shortsword"));
            Assert.That((s.Player.Weapon.MinDamage, s.Player.Weapon.MaxDamage), Is.EqualTo((1, 5)));
            Assert.That(s.Player.InventorySlots, Has.No.Member(InventoryRef.Item("shortsword")));
            var e = Of<WeaponEquippedEvent>(s);
            Assert.That(e, Has.Count.EqualTo(1));
            Assert.That((e[0].Weapon.ItemId, e[0].Previous), Is.EqualTo(("shortsword", (string)null)));
        }

        [Test]
        public void Equip_Swap_ReturnsOldWeaponToInventory()
        {
            var s = Start();
            Give(s, "shortsword");
            Give(s, "spear");
            Equip(s, "shortsword");
            Equip(s, "spear");
            Assert.That(s.Player.Weapon.ItemId, Is.EqualTo("spear"));
            Assert.That(s.Player.InventorySlots, Has.Member(InventoryRef.Item("shortsword")));
            Assert.That(s.Player.InventorySlots, Has.No.Member(InventoryRef.Item("spear")));
            Assert.That(Of<WeaponEquippedEvent>(s)[1].Previous, Is.EqualTo("shortsword"));
        }

        [Test]
        public void Equip_NotAWeapon_DoesNothing()
        {
            var s = Start();
            Give(s, "spellbook_fire");
            int index = System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("spellbook_fire"));
            Assert.That(Weapons.Equip(s.World, s.Items, s.Player, index), Is.False);
            Assert.That(s.Player.Weapon.IsEmpty, Is.True);
        }

        // ── 피해 · 명중 ──

        [Test]
        public void WeaponDamage_RollsWithinRange_UnarmedFixed()
        {
            var s = Start();
            var dummy = Dummy(s, new GridPos(2, 1));
            s.Clock.SubmitPlayerCommand(new AttackCommand(s.World, dummy.Id, 1));
            s.World.Events.Flush();
            Assert.That(Of<UnitDamagedEvent>(s)[0].Amount, Is.EqualTo(3), "맨손 = AttackDamage 고정");

            Give(s, "sickle");
            Equip(s, "sickle");
            s.Events.Clear();
            var seen = new HashSet<int>();
            for (int i = 0; i < 40 && dummy.IsAlive; i++)
            {
                s.Clock.SubmitPlayerCommand(new AttackCommand(s.World, dummy.Id, 1));
                s.World.Events.Flush();
            }
            foreach (var d in Of<UnitDamagedEvent>(s))
            {
                Assert.That(d.Amount, Is.InRange(1, 7), "손낫 1~7");
                seen.Add(d.Amount);
            }
            Assert.That(seen.Count, Is.GreaterThan(1), "굴림 — 한 값만 나오지 않음");
        }

        [Test]
        public void AccuracyPercent_FromWeapon()
        {
            var s = Start();
            Assert.That(MeleeWeapon.AccuracyPercentOf(s.Player), Is.EqualTo(100), "맨손");
            Give(s, "hand_axe");
            Equip(s, "hand_axe");
            Assert.That(MeleeWeapon.AccuracyPercentOf(s.Player), Is.EqualTo(132));
        }

        // ── 창: 2칸 · 2틱 ──

        [Test]
        public void Spear_ReachesTwoCells_StraightAndDiagonal_WhenMiddleEmpty()
        {
            var s = Start();
            Give(s, "spear");
            Equip(s, "spear");
            var straight = Dummy(s, new GridPos(3, 1));
            var diagonal = Dummy(s, new GridPos(3, 3));
            var knight = Dummy(s, new GridPos(2, 3));
            Assert.That(MeleeWeapon.CanReach(s.World, s.Player, straight.Pos), Is.True, "직선 2칸");
            Assert.That(MeleeWeapon.CanReach(s.World, s.Player, diagonal.Pos), Is.True, "대각선 2칸");
            Assert.That(MeleeWeapon.CanReach(s.World, s.Player, knight.Pos), Is.False, "(1, 2)는 사이 칸이 없음");

            Dummy(s, new GridPos(2, 1)); // 사이 칸에 유닛
            Assert.That(MeleeWeapon.CanReach(s.World, s.Player, straight.Pos), Is.False, "사이 칸에 유닛 → 못 침 (결정 2026-10-19)");
        }

        [Test]
        public void Spear_BlockedByWall()
        {
            var s = Start("#######|#.....#|#.#...#|#######"); // 첫 줄 = 위 (y 큰 쪽) — (2, 1)이 벽
            Give(s, "spear");
            Equip(s, "spear");
            var target = Dummy(s, new GridPos(3, 1));
            Assert.That(MeleeWeapon.CanReach(s.World, s.Player, target.Pos), Is.False, "사이 칸이 벽");
        }

        [Test]
        public void ShortWeapon_CannotReachTwo()
        {
            var s = Start();
            Give(s, "shortsword");
            Equip(s, "shortsword");
            var target = Dummy(s, new GridPos(3, 1));
            Assert.That(PlayerActions.TryAttackAt(s.World, Config, s.Player, target.Pos, out _), Is.False);
        }

        [Test]
        public void Spear_AttackCostsTwoTicks_AndHitsAtRange()
        {
            var s = Start();
            Give(s, "spear");
            Equip(s, "spear");
            var target = Dummy(s, new GridPos(3, 1));
            Assert.That(PlayerActions.TryAttackAt(s.World, Config, s.Player, target.Pos, out var attack), Is.True);
            Assert.That(attack.CostTicks, Is.EqualTo(2), "창 공격 = 2틱 (결정 2026-10-19)");
            s.Events.Clear();
            s.Clock.SubmitPlayerCommand(attack);
            s.World.Events.Flush();
            var hit = Of<UnitDamagedEvent>(s);
            Assert.That(hit, Has.Count.EqualTo(1));
            Assert.That(hit[0].Amount, Is.InRange(1, 7));
            Assert.That(PlayerActions.TryStepTo(s.World, Config, s.Player, new GridPos(2, 2), out var move), Is.True);
            Assert.That(move.CostTicks, Is.EqualTo(1), "이동은 그대로 1틱");
        }

        // ── 배치 ──

        [Test]
        public void PlanWeapons_TwoDistinctWeapons_FloorsOneToFour_BooksUnchanged()
        {
            var config = new LootConfig(2, 3, 2);
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var a = new World(GridMovementTests.Parse("###", "#.#", "###"), seed);
                var b = new World(GridMovementTests.Parse("###", "#.#", "###"), seed);
                Loot.PlanSpellbooks(a, Book(), config, 5);
                Loot.PlanSpellbooks(b, Book(), config, 5);
                var books = new List<PendingFloorItem>(a.Run.PendingFloorItems);
                Loot.PlanWeapons(a, Book(), config, 5);

                var plan = a.Run.PendingFloorItems;
                Assert.That(plan.Count, Is.EqualTo(books.Count + 2), $"시드 {seed}");
                for (int i = 0; i < books.Count; i++)
                    Assert.That((plan[i].Floor, plan[i].ItemId), Is.EqualTo((books[i].Floor, books[i].ItemId)), "책 계획 그대로");
                var weapons = new HashSet<string>();
                for (int i = books.Count; i < plan.Count; i++)
                {
                    Assert.That(plan[i].Floor, Is.InRange(1, 4), "보스 층 제외");
                    Assert.That(Book().TryGet(plan[i].ItemId, out var def) && def.Use == ItemUse.EquipWeapon, Is.True);
                    weapons.Add(plan[i].ItemId);
                }
                Assert.That(weapons.Count, Is.EqualTo(2), "같은 종 안 겹침");
                Assert.That(plan[books.Count].Floor, Is.EqualTo(1), "무기 하나는 반드시 1층 (사용자 요청 2026-10-19)");

                Loot.PlanWeapons(b, Book(), config, 5);
                Assert.That(b.Run.PendingFloorItems, Is.EqualTo(plan), "같은 시드 = 같은 계획");
            }
        }

        [Test]
        public void PlanWeapons_ZeroConfigured_AddsNothing()
        {
            var w = new World(GridMovementTests.Parse("###", "#.#", "###"), 3UL);
            Loot.PlanWeapons(w, Book(), new LootConfig(2, 3, 0), 5);
            Assert.That(w.Run.PendingFloorItems, Is.Empty);
        }

        // ── 저장 · 표시 ──

        [Test]
        public void Snapshot_RoundTripsWeapon()
        {
            var s = Start();
            Give(s, "spear");
            Equip(s, "spear");
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => new Idle());
            Assert.That(restored.Units.Get(s.Player.Id).Weapon, Is.EqualTo(s.Player.Weapon));
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(15));
        }

        [Test]
        public void Hud_ShowsWeaponAndLogsEquip_ResultShowsName()
        {
            var s = Start();
            var text = new HudText(itemNames: new Dictionary<string, string> { { "shortsword", "소검" } });
            var hud = new HudModel(8, text);
            hud.Bind(s.World.Events, s.Player);
            Assert.That(text.WeaponLabel(hud.Weapon, hud.UnarmedDamage), Is.EqualTo("무기: 맨손 3"));
            Give(s, "shortsword");
            Equip(s, "shortsword");
            Assert.That(text.WeaponLabel(hud.Weapon, hud.UnarmedDamage), Is.EqualTo("무기: 소검 1~5"));
            Assert.That(hud.Log, Does.Contain("소검을 들었다"));
            Assert.That(ResultModel.From(s.World, 1, s.Player, text).Equipment, Is.EqualTo("소검"));
        }

        // 검산: ItemCatalog · LootConfig 에셋이 BALANCE 11 · 13번과 같은지
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset");
            Assert.That(catalog, Is.Not.Null, "ItemCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var book = catalog.ToBook();
            Assert.That((book.WeaponRules.DamageDivisor, book.WeaponRules.MinDamageBase), Is.EqualTo((3, 2)));
            var expected = new Dictionary<string, (int max, int eff, int acc, int cost, int reach)>
            {
                { "shortsword", (15, 3, 100, 1, 1) }, { "hand_axe", (12, 3, 132, 1, 1) }, { "spear", (20, 4, 100, 2, 2) },
                { "quarterstaff", (12, 3, 100, 1, 1) }, { "dirk", (12, 3, 100, 1, 1) }, { "sickle", (20, 3, 68, 1, 1) },
            };
            foreach (var pair in expected)
            {
                Assert.That(book.TryGet(pair.Key, out var def), Is.True, pair.Key);
                Assert.That(def.Use, Is.EqualTo(ItemUse.EquipWeapon));
                var w = def.Weapon;
                Assert.That((w.MaxDamage, w.UpgradeEfficiency, w.AccuracyPercent, w.AttackCostTicks, w.Reach), Is.EqualTo(pair.Value), pair.Key);
            }
            var loot = AssetDatabase.LoadAssetAtPath<LootConfigAsset>("Assets/_Project/Data/Items/LootConfig.asset").ToConfig();
            Assert.That(loot.WeaponsPerRun, Is.EqualTo(2));
        }
    }
}
