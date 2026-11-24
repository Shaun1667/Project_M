using System.Linq;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.UI;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>
    /// 3-55 마법 강화 (결정 2026-11-24 — 사용자): 강화 주문서를 배운 주문에 쓰면 카드 3장 중 하나 · 종류마다 3단계 · 주문당 5번 ·
    /// 모양에 맞는 강화만 · 같은 판 = 같은 카드 · 효과(SpellModifiers · 관통 · 지속) · 융합 재료 · 스냅샷 v35 · 타임머신 · 에셋 (BALANCE 26번).
    /// </summary>
    public class SpellUpgradeTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();
        private static readonly string[] Starting = { "fire_basic", "ice_basic", "lightning_basic", "poison_basic", "water_basic" };
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 30, Starting);
        private static readonly UnitDef EnemyDef = new UnitDef("enemy", 50, 1, 6);

        private sealed class Idle : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        /// <summary>BALANCE 26번 초안 값.</summary>
        private static SpellUpgradeConfig Config() => new SpellUpgradeConfig(5, 3, 2, new[]
        {
            new SpellUpgradeDef(SpellUpgradeKind.Damage, new[] { 1, 2, 3 }),
            new SpellUpgradeDef(SpellUpgradeKind.Area, new[] { 1, 2, 3 }),
            new SpellUpgradeDef(SpellUpgradeKind.StatusChance, new[] { 15, 30, 45 }),
            new SpellUpgradeDef(SpellUpgradeKind.Duration, new[] { 1, 2, 3 }),
            new SpellUpgradeDef(SpellUpgradeKind.ManaCost, new[] { 1, 2, 3 }),
            new SpellUpgradeDef(SpellUpgradeKind.HitCount, new[] { 1, 2, 3 }),
        });

        private static (World world, Unit player, SpellBook spells) Start(ulong seed = 7UL)
        {
            var world = new World(GridMovementTests.Parse(
                "##########",
                "#........#",
                "#........#",
                "#........#",
                "##########"), seed);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            world.SetSpellUpgradeRules(Config());
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 2), Magic.QuickSlotCount);
            world.Events.Flush();
            return (world, player, SpellTests.BalanceBookFull());
        }

        private static void Force(World world, string spellId, SpellUpgradeKind kind, int level) => SpellUpgrades.SetLevel(world, spellId, kind, level);

        // 카드는 맞는 종류 중 3장 · 같은 종류 없음 · 같은 판 · 같은 횟수면 늘 같음
        [Test]
        public void Offer_ThreeDistinct_Deterministic()
        {
            var (world, _, spells) = Start();
            var a = SpellUpgrades.Offer(world, spells, "fire_basic");
            Assert.That(a.Count, Is.EqualTo(3));
            Assert.That(a.Distinct().Count(), Is.EqualTo(3));
            Assert.That(SpellUpgrades.Offer(world, spells, "fire_basic"), Is.EqualTo(a), "창을 다시 열어도 같은 카드");
            var (other, _, _) = Start();
            Assert.That(SpellUpgrades.Offer(other, spells, "fire_basic"), Is.EqualTo(a), "같은 시드 = 같은 카드");
            Assert.That(SpellUpgrades.Offer(world, spells, "nope"), Is.Empty);
        }

        // 모양 · 값에 맞는 강화만: 물(피해 0 · 젖음 100%)은 범위 · 지속 · 마나만, 줄기는 범위 없음
        [Test]
        public void Fits_ByShapeAndValues()
        {
            var config = Config();
            var (_, _, spells) = Start();
            bool Fits(string id, SpellUpgradeKind kind, int level = 0) { spells.TryGet(id, out var s); return SpellUpgrades.Fits(config, s, kind, level); }
            Assert.That(Fits("water_basic", SpellUpgradeKind.Damage), Is.False);
            Assert.That(Fits("water_basic", SpellUpgradeKind.HitCount), Is.False);
            Assert.That(Fits("water_basic", SpellUpgradeKind.StatusChance), Is.False, "젖음 100%");
            Assert.That(Fits("water_basic", SpellUpgradeKind.Area), Is.True);
            Assert.That(Fits("water_basic", SpellUpgradeKind.Duration), Is.True);
            Assert.That(Fits("water_basic", SpellUpgradeKind.ManaCost), Is.True, "마나 2 → 1");
            Assert.That(Fits("water_basic", SpellUpgradeKind.ManaCost, 1), Is.False, "이미 1");
            Assert.That(Fits("lightning_basic", SpellUpgradeKind.Area), Is.False, "줄기는 범위 없음");
            Assert.That(Fits("fire_basic", SpellUpgradeKind.Area), Is.True, "화살 = 관통");
            Assert.That(Fits("fire_basic", SpellUpgradeKind.StatusChance, 1), Is.True, "70 + 15 < 100");
            Assert.That(Fits("fire_basic", SpellUpgradeKind.StatusChance, 2), Is.False, "70 + 30 = 100");
            Assert.That(Fits("fire_basic", SpellUpgradeKind.Damage, 3), Is.False, "3단계 끝");
        }

        // 고른 강화 = 단계 +1 · 카드에 없는 강화는 막힘 · 주문당 5번이면 카드 없음
        [Test]
        public void Upgrade_Levels_NotOffered_MaxPerSpell()
        {
            var (world, player, spells) = Start();
            var offer = SpellUpgrades.Offer(world, spells, "fire_basic");
            var missing = new[] { SpellUpgradeKind.Damage, SpellUpgradeKind.Area, SpellUpgradeKind.StatusChance, SpellUpgradeKind.Duration,
                SpellUpgradeKind.ManaCost, SpellUpgradeKind.HitCount }.First(k => !offer.Contains(k));
            Assert.That(SpellUpgrades.Upgrade(world, spells, player, "fire_basic", missing), Is.EqualTo(SpellUpgradeResult.NotOffered));
            Assert.That(SpellUpgrades.Upgrade(world, spells, player, "fire_basic", offer[0]), Is.EqualTo(SpellUpgradeResult.Done));
            Assert.That(SpellUpgrades.Level(world, "fire_basic", offer[0]), Is.EqualTo(1));
            Assert.That(SpellUpgrades.Count(world, "fire_basic"), Is.EqualTo(1));

            Force(world, "fire_basic", SpellUpgradeKind.Damage, 3);
            Force(world, "fire_basic", SpellUpgradeKind.HitCount, 2);
            Assert.That(SpellUpgrades.Count(world, "fire_basic"), Is.GreaterThanOrEqualTo(5));
            Assert.That(SpellUpgrades.Offer(world, spells, "fire_basic"), Is.Empty, "주문당 5번");
            Assert.That(SpellUpgrades.Check(world, spells, player, "fire_basic", SpellUpgradeKind.Area), Is.EqualTo(SpellUpgradeResult.Maxed));
            Assert.That(SpellUpgrades.Check(world, spells, player, "nope", SpellUpgradeKind.Area), Is.EqualTo(SpellUpgradeResult.NotOwned));
        }

        // 효과는 SpellModifiers.For 한 곳: 피해 · 마나(최소 1) · 반지름 · 관통 · 연속 · 상태 확률 · 지속 — 적이 쓰면 그대로
        [Test]
        public void Effects_ThroughSpellModifiers_PlayerOnly()
        {
            var (world, player, spells) = Start();
            spells.TryGet("fire_basic", out var fire);
            spells.TryGet("poison_basic", out var poison);
            spells.TryGet("ice_basic", out var ice);
            Force(world, "fire_basic", SpellUpgradeKind.Damage, 2);
            Force(world, "fire_basic", SpellUpgradeKind.Area, 1);
            Force(world, "fire_basic", SpellUpgradeKind.HitCount, 1);
            Force(world, "fire_basic", SpellUpgradeKind.StatusChance, 1);
            var f = SpellModifiers.For(world, player, fire);
            Assert.That((f.Damage, f.Pierce, f.HitCount, f.StatusChancePercent), Is.EqualTo((7, 1, 2, 85)));

            Force(world, "poison_basic", SpellUpgradeKind.Area, 2);
            Force(world, "poison_basic", SpellUpgradeKind.Duration, 1);
            Force(world, "poison_basic", SpellUpgradeKind.ManaCost, 3);
            var p = SpellModifiers.For(world, player, poison);
            Assert.That((p.Radius, p.StatusDurationBonus, p.TerrainDurationBonus, p.ManaCost), Is.EqualTo((3, 1, 2, 1)), "마나 3 − 3 → 최소 1");

            Assert.That(SpellUpgrades.ApplyTo(world, player, ice), Is.SameAs(ice), "강화 없는 주문은 그대로");
            var enemy = UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(5, 2), new Idle());
            Assert.That(SpellUpgrades.ApplyTo(world, enemy, fire), Is.SameAs(fire), "적은 플레이어 강화를 받지 않음");
        }

        // 관통: 조준한 칸까지 가는 길의 유닛 관통 + 1명
        [Test]
        public void Pierce_HitsUnitsAlongPath()
        {
            var (world, player, spells) = Start();
            foreach (var x in new[] { 3, 5, 7 }) UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(x, 2), new Idle());
            spells.TryGet("fire_basic", out var fire);
            var plain = SpellCasting.Preview(world, Targeting, player.Pos, SpellModifiers.For(world, player, fire), new GridPos(7, 2));
            Assert.That(plain.AffectedCells, Is.EqualTo(new[] { new GridPos(3, 2) }), "강화 전: 첫 적에서 멈춤");
            Force(world, "fire_basic", SpellUpgradeKind.Area, 1);
            var pierced = SpellCasting.Preview(world, Targeting, player.Pos, SpellModifiers.For(world, player, fire), new GridPos(7, 2));
            Assert.That(pierced.AffectedCells, Is.EqualTo(new[] { new GridPos(3, 2), new GridPos(5, 2) }), "관통 I = 2명");
            Assert.That(pierced.Path.Last(), Is.EqualTo(new GridPos(5, 2)), "둘째에서 멈춤");
        }

        // 지속: 거는 상태 + 틱 · 남기는 지형 + 틱 (시전)
        [Test]
        public void Duration_AddsStatusAndTerrainTicks()
        {
            var (world, player, _) = Start();
            var statuses = SpellTests.BalanceStatuses();
            statuses.TryGet(StatusType.Poison, out var poisonStatus);
            var enemy = UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(5, 2), new Idle());
            StatusEffects.Apply(world, enemy, poisonStatus, 2);
            Assert.That(enemy.Statuses.Find(s => s.Type == StatusType.Poison).Remaining, Is.EqualTo(7), "중독 5 + 2");

            var book = new SpellBook(new[]
            {
                new SpellDef("poison_test", Element.Poison, SpellShape.Burst, 5, 0, 3, 1, StatusType.Poison, 100, TerrainEffectType.PoisonCloud),
            }, statuses, SpellTests.BalanceTerrains());
            book.TryGet("poison_test", out var spell);
            Force(world, "poison_test", SpellUpgradeKind.Duration, 1);
            var upgraded = SpellModifiers.For(world, player, spell);
            var other = UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(7, 2), new Idle());
            var preview = SpellCasting.Preview(world, Targeting, player.Pos, upgraded, other.Pos);
            SpellCasting.ApplyEffect(world, book, player.Id, upgraded, preview);
            Assert.That(other.Statuses.Find(s => s.Type == StatusType.Poison).Remaining, Is.EqualTo(6), "중독 5 + 1");
            Assert.That(world.Terrain.At(other.Pos).First(t => t.Type == TerrainEffectType.PoisonCloud).Remaining, Is.EqualTo(6), "독구름 4 + 2");
        }

        // 강화 주문서: 주문 · 강화를 고르면 차례 안에서 강화 · 주문서 하나 사라짐. 카드에 없는 강화면 쓰지 않음
        [Test]
        public void UpgradeScroll_OnSpell_ConsumesAndUpgrades()
        {
            var (world, player, spells) = Start();
            var items = new ItemBook(new[] { new ItemDef("scroll_upgrade", ItemUse.Scroll, amount: 1, effect: ConsumableEffect.Upgrade) });
            var rules = new ConsumableRules(items, spells: spells);
            Inventory.TryAdd(world, spells, items, player, InventoryRef.Item("scroll_upgrade"));
            int index = System.Array.FindIndex(player.InventorySlots, e => e.Id == "scroll_upgrade");
            var offer = SpellUpgrades.Offer(world, spells, "ice_basic");
            var missing = new[] { SpellUpgradeKind.Damage, SpellUpgradeKind.Area, SpellUpgradeKind.StatusChance, SpellUpgradeKind.Duration,
                SpellUpgradeKind.ManaCost, SpellUpgradeKind.HitCount }.First(k => !offer.Contains(k));

            Assert.That(Consumables.Use(world, rules, player, index, UpgradeTarget.Spell, InventoryRef.Spell("ice_basic"), missing),
                Is.EqualTo(ConsumeResult.CannotUpgradeSpell));
            Assert.That(player.InventorySlots[index].Id, Is.EqualTo("scroll_upgrade"), "막히면 주문서는 그대로");

            Assert.That(Consumables.Use(world, rules, player, index, UpgradeTarget.Spell, InventoryRef.Spell("ice_basic"), offer[1]),
                Is.EqualTo(ConsumeResult.Used));
            Assert.That(SpellUpgrades.Level(world, "ice_basic", offer[1]), Is.EqualTo(1));
            Assert.That(System.Array.Exists(player.InventorySlots, e => e.Id == "scroll_upgrade"), Is.False);
        }

        // 융합 재료: 강화한 피해 · 마나가 융합 주문에 합쳐짐
        [Test]
        public void Fusion_UsesUpgradedIngredients()
        {
            var (world, player, spells) = Start();
            var items = new ItemBook(new[] { new ItemDef("fusion_device", ItemUse.Fusion) });
            Inventory.TryAdd(world, spells, items, player, InventoryRef.Item("fusion_device"));
            var fusions = new FusionBook(new[] { new FusionDef("lava", Element.Fire, Element.Water, TerrainEffectType.FireZone, 6) },
                SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            FusionCasting.Check(world, spells, fusions, items, Targeting, Sight, player, "fire_basic", "water_basic", new GridPos(5, 2), out var before, out _);
            Force(world, "fire_basic", SpellUpgradeKind.Damage, 2);
            Force(world, "water_basic", SpellUpgradeKind.ManaCost, 1);
            FusionCasting.Check(world, spells, fusions, items, Targeting, Sight, player, "fire_basic", "water_basic", new GridPos(5, 2), out var after, out _);
            Assert.That((after.Damage - before.Damage, before.ManaCost - after.ManaCost), Is.EqualTo((2, 1)));
        }

        // 스냅샷 v35 왕복 · 타임머신은 강화를 지금 그대로 가져감
        [Test]
        public void Snapshot_RoundTrip_AndTimeMachineCarryOver()
        {
            var (world, player, _) = Start();
            Force(world, "fire_basic", SpellUpgradeKind.Damage, 2);
            Force(world, "poison_basic", SpellUpgradeKind.Area, 1);
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(35));
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => new Idle());
            Assert.That(restored.Run.SpellUpgrades.Select(u => (u.SpellId, u.Kind, u.Level)),
                Is.EqualTo(new[] { ("fire_basic", SpellUpgradeKind.Damage, 2), ("poison_basic", SpellUpgradeKind.Area, 1) }));

            var past = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => new Idle());
            Force(world, "ice_basic", SpellUpgradeKind.HitCount, 1); // 되돌리기 전에 새로 강화
            PlayerActions.TryGetPlayer(past, out var pastPlayer);
            TimeRelics.CarryOver(world, player, past, pastPlayer);
            Assert.That(past.Run.SpellUpgradeLevel("ice_basic", SpellUpgradeKind.HitCount), Is.EqualTo(1));
        }

        [Test]
        public void Text_NamesByShape_AndLog()
        {
            Assert.That(HudText.SpellUpgradeName(SpellUpgradeKind.Area, SpellShape.Bolt), Is.EqualTo("관통 강화"));
            Assert.That(HudText.SpellUpgradeName(SpellUpgradeKind.Area, SpellShape.Burst), Is.EqualTo("범위 강화"));
            Assert.That(HudText.SpellUpgradeName(SpellUpgradeKind.Area, SpellShape.Multi), Is.EqualTo("대상 수 강화"));
            Assert.That(HudText.SpellUpgradeEffect(SpellUpgradeKind.Duration, SpellShape.Burst, 2, 2, true, true), Is.EqualTo("상태 +2턴\n지형 +4턴"));
            Assert.That(HudText.SpellUpgradeEffect(SpellUpgradeKind.HitCount, SpellShape.Bolt, 1, 2, false, false), Is.EqualTo("한 대상 2번"));
            var text = new HudText(new System.Collections.Generic.Dictionary<string, string> { { "fire_basic", "불꽃 화살" } });
            Assert.That(text.SpellUpgraded("fire_basic", SpellUpgradeKind.Damage, SpellShape.Bolt, 2), Is.EqualTo("불꽃 화살 — 피해 강화 II"));
        }

        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var config = AssetDatabase.LoadAssetAtPath<SpellUpgradeConfigAsset>("Assets/_Project/Data/Magic/SpellUpgradeConfig.asset").ToConfig();
            Assert.That((config.MaxPerSpell, config.ChoiceCount, config.TerrainTicksPerLevel), Is.EqualTo((5, 3, 2)));
            var expected = Config();
            Assert.That(config.Upgrades.Select(u => u.Kind), Is.EqualTo(expected.Upgrades.Select(u => u.Kind)));
            for (int i = 0; i < expected.Upgrades.Count; i++)
                Assert.That(config.Upgrades[i].Values, Is.EqualTo(expected.Upgrades[i].Values), expected.Upgrades[i].Kind.ToString());
        }
    }
}
