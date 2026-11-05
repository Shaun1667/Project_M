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
    /// <summary>3-28a 유물 (결정 2026-11-05): 유물 칸 2 · 같은 종류 하나 · 효과(특성 효과 표) · 배치 · 보스 보상 · 스냅샷 v28 (BALANCE 24번).</summary>
    public class RelicTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);

        private static ItemDef RelicDef(string id, TalentEffect effect, int value, string spells = null) =>
            new ItemDef(id, ItemUse.Relic, relicEffects: new[]
            {
                new TalentEffectDef(effect, new[] { value }, spells != null ? new[] { spells } : null),
            });

        /// <summary>BALANCE 24번 유물 5종 (강화 0 값).</summary>
        private static ItemBook Book() => new ItemBook(new[]
        {
            RelicDef("relic_elemental_crystal", TalentEffect.SpellDamage, 2, TalentEffectDef.ElementalSpells),
            RelicDef("relic_life_stone", TalentEffect.MaxHp, 10),
            RelicDef("relic_mana_spring", TalentEffect.ManaRegenInterval, -1),
            RelicDef("relic_predator_fang", TalentEffect.HitManaChance, 25, TalentEffectDef.AllSpells),
            RelicDef("relic_shadow_cloak", TalentEffect.UnseenDamagePercent, 30, TalentEffectDef.AllSpells),
        });

        private static (World world, Unit player, ItemBook items) Start()
        {
            var world = new World(GridMovementTests.Parse("#######", "#.....#", "#.....#", "#######"), 3UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var items = Book();
            world.SetRelicRules(items);
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(2, 1), Magic.QuickSlotCount);
            world.Events.Flush();
            return (world, player, items);
        }

        private static int Give(World world, ItemBook items, Unit player, string id)
        {
            Inventory.TryAdd(world, null, items, player, InventoryRef.Item(id));
            return System.Array.FindIndex(player.InventorySlots, e => e.Id == id);
        }

        // 빈 칸부터 두 칸, 셋째는 첫 칸과 바꿈(빼낸 것은 배낭), 같은 종류는 못 낌
        [Test]
        public void Equip_TwoSlots_SwapFirst_NoDuplicates()
        {
            var (world, player, items) = Start();
            Assert.That(Relics.Equip(world, items, player, Give(world, items, player, "relic_elemental_crystal")), Is.EqualTo(RelicEquipResult.Equipped));
            Assert.That(Relics.Equip(world, items, player, Give(world, items, player, "relic_predator_fang")), Is.EqualTo(RelicEquipResult.Equipped));
            Assert.That((player.Relics[0].Id, player.Relics[1].Id), Is.EqualTo(("relic_elemental_crystal", "relic_predator_fang")));

            int dup = Give(world, items, player, "relic_predator_fang");
            Assert.That(Relics.Equip(world, items, player, dup), Is.EqualTo(RelicEquipResult.SameKind));
            Assert.That(player.InventorySlots[dup].Id, Is.EqualTo("relic_predator_fang"), "같은 종류는 배낭에 그대로");
            Inventory.RemoveAt(world, player, dup);

            Assert.That(Relics.Equip(world, items, player, Give(world, items, player, "relic_shadow_cloak")), Is.EqualTo(RelicEquipResult.Equipped));
            Assert.That(player.Relics[0].Id, Is.EqualTo("relic_shadow_cloak"), "첫 칸과 바꿈");
            Assert.That(System.Array.Exists(player.InventorySlots, e => e.Id == "relic_elemental_crystal"), Is.True, "빼낸 유물은 배낭으로");        }

        // 생명석 부적: 끼면 최대 체력 · 체력 +10, 빼면 되돌아감 (체력은 최대를 넘지 않음)
        [Test]
        public void LifeStone_RaisesAndRestoresMaxHp()
        {
            var (world, player, items) = Start();
            Relics.Equip(world, items, player, Give(world, items, player, "relic_life_stone"));
            Assert.That((player.MaxHp, player.Hp), Is.EqualTo((30, 30)));
            Relics.Equip(world, items, player, Give(world, items, player, "relic_elemental_crystal")); // 둘째 칸
            Relics.Equip(world, items, player, Give(world, items, player, "relic_shadow_cloak")); // 첫 칸(생명석)과 바꿈
            Assert.That((player.MaxHp, player.Hp), Is.EqualTo((20, 20)));
        }

        // 효과: 원소 증폭 = 속성 주문만 +2 · 포식자 = 근접 맞힘 마나 25% · 마나 샘 = 회복 간격 −1 · 그림자 망토 = 못 본 적 +30%
        [Test]
        public void Effects_UseTalentTable()
        {
            var (world, player, items) = Start();
            var fire = new SpellDef("fire_basic", Element.Fire, SpellShape.Bolt, 6, 5, 3);
            var skill = new SpellDef("skill", Element.Neutral, SpellShape.Bolt, 2, 4, 0);
            Assert.That(SpellModifiers.For(world, player, fire, unlimitedRange: false).Damage, Is.EqualTo(5));
            Relics.Equip(world, items, player, Give(world, items, player, "relic_elemental_crystal"));
            Assert.That(SpellModifiers.For(world, player, fire, unlimitedRange: false).Damage, Is.EqualTo(7), "속성 주문 +2");
            Assert.That(SpellModifiers.For(world, player, skill).Damage, Is.EqualTo(4), "무속성 직업 기술은 그대로");

            Relics.Equip(world, items, player, Give(world, items, player, "relic_predator_fang"));
            Assert.That(Talents.MeleeHitManaPercent(world, player), Is.EqualTo(25));
            Assert.That(SpellModifiers.For(world, player, skill).ManaOnHitPercent, Is.EqualTo(25), "주문도");

            Relics.Equip(world, items, player, Give(world, items, player, "relic_mana_spring")); // 첫 칸(원소 증폭)과 바꿈
            Assert.That(Talents.ManaRegenIntervalBonus(world, player), Is.EqualTo(-1));
            Assert.That(SpellModifiers.For(world, player, fire, unlimitedRange: false).Damage, Is.EqualTo(5), "뺀 유물은 효과 없음");
        }

        // 마나 샘: 3틱 → 2틱마다 회복 (틱 번호로만)
        [Test]
        public void ManaSpring_ShortensRegenInterval()
        {
            var (world, player, items) = Start();
            Mana.TrySpend(world, player, player.Mana);
            var clock = new TurnClock(world.Scheduler, world.PlayerInput);
            for (int i = 0; i < 6; i++) clock.SubmitPlayerCommand(new WaitCommand(1));
            int plain = player.Mana;
            Mana.TrySpend(world, player, player.Mana);
            Relics.Equip(world, items, player, Give(world, items, player, "relic_mana_spring"));
            for (int i = 0; i < 6; i++) clock.SubmitPlayerCommand(new WaitCommand(1));
            Assert.That(player.Mana, Is.GreaterThan(plain), "같은 6틱에 더 많이 회복");
        }

        // 배치: 한 판에 종류가 겹치지 않게 n개 · 보스 보상은 그 칸에
        [Test]
        public void Loot_PlansDistinctRelics_BossDrops()
        {
            var (world, _, items) = Start();
            var loot = new LootConfig(1, 1, relicsPerRun: 3, bossRelicPercent: 100);
            Loot.PlanRelics(world, items, loot, 10);
            var planned = new HashSet<string>();
            foreach (var p in world.Run.PendingFloorItems)
                if (Relics.IsRelic(items, p.ItemId)) Assert.That(planned.Add(p.ItemId) && p.Floor >= 1 && p.Floor <= 9, Is.True, p.ItemId);
            Assert.That(planned.Count, Is.EqualTo(3));

            Loot.DropBossRelic(world, items, loot, new GridPos(4, 2));
            Assert.That(Relics.IsRelic(items, world.FloorItems.At(new GridPos(4, 2)).Count > 0 ? world.FloorItems.At(new GridPos(4, 2))[0] : null), Is.True);
        }

        // 스냅샷 v28: 낀 유물(강화 단계 포함) 왕복
        [Test]
        public void Snapshot_RoundTripsRelics()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(28));
            var (world, player, items) = Start();
            Relics.Equip(world, items, player, Give(world, items, player, "relic_shadow_cloak"));
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => null);
            Assert.That(PlayerActions.TryGetPlayer(restored, out var back), Is.True);
            Assert.That((back.Relics[0].Id, back.Relics[1].IsEmpty), Is.EqualTo(("relic_shadow_cloak", true)));
        }

        // 화면 글: 효과 한 줄 · 장비 줄 이름
        [Test]
        public void HudText_RelicLines()
        {
            var items = Book();
            items.TryGet("relic_elemental_crystal", out var crystal);
            Assert.That(HudText.RelicEffect(crystal.RelicEffects[0], 1), Is.EqualTo("속성 주문 피해 +2"));
            items.TryGet("relic_mana_spring", out var spring);
            Assert.That(HudText.RelicEffect(spring.RelicEffects[0], 1), Is.EqualTo("마나 회복 간격 -1턴"));
            Assert.That(HudText.EquipSlotNames[HudText.FirstRelicSlot], Is.EqualTo("유물"));
            Assert.That(HudText.EquipSlotNames[HudText.FirstRelicSlot + 1], Is.EqualTo("유물"));
        }

        // 프로젝트 에셋 = BALANCE 24 · 11번
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            var expected = Book();
            foreach (var id in expected.Ids)
            {
                Assert.That(catalog.TryGet(id, out var actual) && actual.Use == ItemUse.Relic, Is.True, id);
                expected.TryGet(id, out var want);
                Assert.That(actual.RelicEffects[0].Effect, Is.EqualTo(want.RelicEffects[0].Effect), id);
                Assert.That(actual.RelicEffects[0].ValueAt(1), Is.EqualTo(want.RelicEffects[0].ValueAt(1)), id);
                Assert.That(actual.RelicEffects[0].SpellIds, Is.EqualTo(want.RelicEffects[0].SpellIds), id);
            }
            var loot = AssetDatabase.LoadAssetAtPath<LootConfigAsset>("Assets/_Project/Data/Items/LootConfig.asset").ToConfig();
            Assert.That((loot.RelicsPerRun, loot.BossRelicPercent), Is.EqualTo((2, 100)));
        }
    }
}
