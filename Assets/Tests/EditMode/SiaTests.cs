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
    /// <summary>
    /// 3-22a 시아 · 너클 (Docs/직업_시아.md, 결정 2026-11-05): 너클 = 한 번 공격 2타(명중 · 피해 · 방어 따로) · 한 대 1~2 · 요구 힘 10 ·
    /// 스냅샷 v26 · 도약(뛰어넘기 — 유닛을 넘고 벽은 못 넘음) · 너클을 들면 시아 기술 강화 · 에셋 값 (BALANCE.md 13 · 22번).
    /// </summary>
    public class SiaTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly WeaponRules Rules = new WeaponRules(3, 2); // BALANCE 13번
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef SiaDef = new UnitDef("hero_black", 20, 3, 8, 10, new[] { "sia_leap" }, accuracy: 10, evasion: 5, strength: 10);
        private static readonly UnitDef DummyDef = new UnitDef("enemy_melee", 50, 2, 6); // 회피 0 — 항상 맞음

        private static readonly WeaponDef KnuckleDef = new WeaponDef(5, 3, 100, 1, 1, strengthRequirement: 10, hitsPerAttack: 2);

        private static ItemBook Items() => new ItemBook(new[]
        {
            new ItemDef("knuckle", ItemUse.EquipWeapon, weapon: KnuckleDef),
            new ItemDef("shortsword", ItemUse.EquipWeapon, weapon: new WeaponDef(15, 3, 100, 1, 1, strengthRequirement: 12)),
        }, Rules);

        private static SpellBook Book() => new SpellBook(new[]
        {
            new SpellDef("sia_leap", Element.Neutral, SpellShape.Leap, 3, 0, 2, boostWeaponId: "knuckle", boostDamagePercent: 150, boostRange: 1),
        }, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());

        private sealed class Idle : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private sealed class Session
        {
            public World World;
            public SpellBook Book;
            public ItemBook Items;
            public TurnClock Clock;
            public Unit Player;
        }

        private static Session Start()
        {
            var grid = GridMovementTests.Parse(
                "############",
                "#..........#",
                "#.....#....#",
                "#..........#",
                "############");
            var world = new World(grid, 6UL);
            var s = new Session { World = world, Book = Book(), Items = Items(), Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            var vision = world.InstallCoreSystems(Sight, Magic, s.Book.Statuses, s.Book.Terrains);
            s.Player = UnitSpawner.SpawnPlayer(world, SiaDef, new GridPos(1, 2), Magic.QuickSlotCount, Magic.InventorySlotCount);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static void Hold(Session s, string itemId)
        {
            Inventory.TryAdd(s.World, s.Book, s.Items, s.Player, InventoryRef.Item(itemId));
            Assert.That(Weapons.Equip(s.World, s.Items, s.Player, System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item(itemId))), Is.True);
        }

        private static CastBlock Cast(Session s, string spellId, GridPos target)
        {
            var block = SpellCasting.TryCreateCastSpell(s.World, Config, s.Book, Targeting, Sight, spellId, target, out var command);
            if (block == CastBlock.None) s.Clock.SubmitPlayerCommand(command);
            s.World.Events.Flush();
            return block;
        }

        // ── 너클 ──

        // 수치 (1/3 배율): +0 한 대 1~2 · +1 1~3 · 요구 힘 10 (+1 → 9) · 2타
        [Test]
        public void Knuckle_Resolve_MatchesDesign()
        {
            var w0 = Weapons.Resolve("knuckle", KnuckleDef, Rules);
            Assert.That((w0.MinDamage, w0.MaxDamage, w0.StrengthRequirement, w0.HitsPerAttack, w0.AttackCostTicks, w0.Reach), Is.EqualTo((1, 2, 10, 2, 1, 1)));
            var w1 = Weapons.Resolve("knuckle", KnuckleDef, Rules, 1);
            Assert.That((w1.MinDamage, w1.MaxDamage, w1.StrengthRequirement), Is.EqualTo((1, 3, 9)));
            Assert.That(EquippedWeapon.Empty.HitsPerAttack, Is.EqualTo(1), "맨손 1타");
            Assert.That(Weapons.Resolve("shortsword", new WeaponDef(15, 3, 100, 1, 1), Rules).HitsPerAttack, Is.EqualTo(1));
        }

        // 한 번 공격(차례 1) = 피해 2번 (각 1~2), 행동은 한 번
        [Test]
        public void Knuckle_AttackHitsTwice_OneTurn()
        {
            var s = Start();
            Hold(s, "knuckle");
            var enemy = UnitSpawner.SpawnEnemy(s.World, DummyDef, new GridPos(2, 2), new Idle());
            var hits = new List<UnitDamagedEvent>();
            s.World.Events.Subscribe<UnitDamagedEvent>(hits.Add);
            s.World.Events.Flush();
            long tick = s.World.Scheduler.CurrentTick;
            s.Clock.SubmitPlayerCommand(new AttackCommand(s.World, enemy.Id, Config.DefaultActionCostTicks));
            s.World.Events.Flush();
            var mine = hits.FindAll(h => h.Target == enemy.Id);
            Assert.That(mine.Count, Is.EqualTo(2), "2타");
            Assert.That(mine.TrueForAll(h => h.Amount >= 1 && h.Amount <= 2), Is.True);
            Assert.That(s.World.Scheduler.CurrentTick - tick, Is.EqualTo(1), "차례 1");
        }

        // 첫 타에 쓰러지면 두 번째 타는 없음
        [Test]
        public void Knuckle_StopsWhenTargetDies()
        {
            var s = Start();
            Hold(s, "knuckle");
            var weak = UnitSpawner.SpawnEnemy(s.World, new UnitDef("enemy_melee", 1, 2, 6), new GridPos(2, 2), new Idle());
            var hits = new List<UnitDamagedEvent>();
            s.World.Events.Subscribe<UnitDamagedEvent>(hits.Add);
            s.World.Events.Flush();
            s.Clock.SubmitPlayerCommand(new AttackCommand(s.World, weak.Id, Config.DefaultActionCostTicks));
            s.World.Events.Flush();
            Assert.That(hits.FindAll(h => h.Target == weak.Id).Count, Is.EqualTo(1));
        }

        // 스냅샷 v26: 든 무기의 타격 수 왕복
        [Test]
        public void Snapshot_RoundTripsHitsPerAttack()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(26));
            var s = Start();
            Hold(s, "knuckle");
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(s.World.Capture())), (w, id) => null);
            Assert.That(restored.Units.TryGet(s.Player.Id, out var p), Is.True);
            Assert.That(p.Weapon, Is.EqualTo(s.Player.Weapon));
            Assert.That(p.Weapon.HitsPerAttack, Is.EqualTo(2));
        }

        // ── 도약 ──

        // 사이의 유닛을 넘어 3칸 앞 빈 칸으로 · 마나 2 · 차례 1
        [Test]
        public void Leap_JumpsOverUnit_ToEmptyCell()
        {
            var s = Start();
            UnitSpawner.SpawnEnemy(s.World, DummyDef, new GridPos(2, 2), new Idle());
            s.World.Events.Flush();
            Assert.That(Cast(s, "sia_leap", new GridPos(4, 2)), Is.EqualTo(CastBlock.None));
            Assert.That(s.Player.Pos, Is.EqualTo(new GridPos(4, 2)));
            Assert.That(s.Player.Mana, Is.EqualTo(10 - 2));
        }

        // 막힘: 유닛이 선 칸 · 벽 너머 · 사거리 밖
        [Test]
        public void Leap_Blocked_OccupiedWallOrRange()
        {
            var s = Start();
            var enemy = UnitSpawner.SpawnEnemy(s.World, DummyDef, new GridPos(3, 1), new Idle());
            s.World.Events.Flush();
            Assert.That(Cast(s, "sia_leap", enemy.Pos), Is.EqualTo(CastBlock.Blocked), "유닛이 있는 칸");
            s.World.Units.Teleport(s.Player.Id, new GridPos(5, 2)); // 오른쪽 (6,2)가 벽
            s.World.Events.Flush();
            Assert.That(Cast(s, "sia_leap", new GridPos(7, 2)), Is.Not.EqualTo(CastBlock.None), "벽 너머");
            Assert.That(Cast(s, "sia_leap", new GridPos(9, 1)), Is.EqualTo(CastBlock.OutOfRange));
            Assert.That(s.Player.Pos, Is.EqualTo(new GridPos(5, 2)));
        }

        // 너클을 들면 도약 사거리 +1 (3 → 4), 다른 무기는 그대로
        [Test]
        public void Knuckle_BoostsLeapRange()
        {
            var s = Start();
            Assert.That(Cast(s, "sia_leap", new GridPos(5, 1)), Is.EqualTo(CastBlock.OutOfRange), "거리 4");
            Hold(s, "shortsword");
            Assert.That(Cast(s, "sia_leap", new GridPos(5, 1)), Is.EqualTo(CastBlock.OutOfRange), "소검은 강화 없음");
            Hold(s, "knuckle");
            Assert.That(Cast(s, "sia_leap", new GridPos(5, 1)), Is.EqualTo(CastBlock.None));
            Assert.That(s.Player.Pos, Is.EqualTo(new GridPos(5, 1)));
        }

        // ── 검산 ──

        // BALANCE.md 13번 너클 줄 · 22번 도약 · 시아 캐릭터 줄 · 무기 후보(한 판 무기 · 무기고)
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            Assert.That(items.TryGet("knuckle", out var knuckle) && knuckle.Use == ItemUse.EquipWeapon, Is.True);
            var w = knuckle.Weapon;
            Assert.That((w.MaxDamage, w.UpgradeEfficiency, w.AccuracyPercent, w.AttackCostTicks, w.Reach, w.StrengthRequirement, w.HitsPerAttack),
                Is.EqualTo((5, 3, 100, 1, 1, 10, 2)));
            var resolved = Weapons.Resolve("knuckle", w, items.WeaponRules);
            Assert.That((resolved.MinDamage, resolved.MaxDamage), Is.EqualTo((1, 2)));

            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            Assert.That(spells.TryGet("sia_leap", out var leap), Is.True);
            Assert.That((leap.Element, leap.Shape, leap.Range, leap.Damage, leap.ManaCost, leap.BoostWeaponId, leap.BoostRange),
                Is.EqualTo((Element.Neutral, SpellShape.Leap, 3, 0, 2, "knuckle", 1)));

            var characters = AssetDatabase.LoadAssetAtPath<CharacterCatalogAsset>("Assets/_Project/Data/Units/CharacterCatalog.asset");
            var sia = new List<CharacterCatalogAsset.Entry>(characters.Characters).Find(c => c.Id == "hero_black");
            Assert.That(sia.ClassId, Is.EqualTo("sia"));
            Assert.That(sia.StartingSpells, Is.EqualTo(new[] { "sia_leap" }));

            var decor = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset");
            Assert.That(decor.AddArmoryItemIfMissing("knuckle"), Is.False, "무기고 후보에 이미 있음");
        }
    }
}
