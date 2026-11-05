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
    /// <summary>3-29 화난 상인 (결정 2026-11-05): 주문 · 던지기로 치면 적 · 보스 체력 ×150% · 가게 물건을 챙겨 씀 · 이기면 전부 떨어뜨림 (BALANCE 16번).</summary>
    public class AngryShopkeeperTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);
        private static readonly UnitDef BossDef = new UnitDef("boss_slime_girl", 40, 4, 8, accuracy: 12, evasion: 6);
        private static readonly ShopConfig Shop = new ShopConfig(1, new[] { "potion_frost", "potion_healing" }, 2, 100, 50, "shopkeeper", 999,
            angryHpPercent: 150, itemUsePercent: 100, healBelowPercent: 50);

        private static readonly ItemBook Items = new ItemBook(new[]
        {
            new ItemDef("potion_frost", ItemUse.Potion, effect: ConsumableEffect.Frost, weight: 1, value: 30),
            new ItemDef("potion_healing", ItemUse.Potion, effect: ConsumableEffect.Healing, weight: 1, value: 30),
        });

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static (World world, Unit player, Unit keeper, ShopkeeperAi ai) Start()
        {
            var world = new World(GridMovementTests.Parse("#########", "#.......#", "#.......#", "#########"), 3UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var zones = new ZoneBook(new[] { new ZoneDef("숲", 1, 5, new[] { new ZoneEnemy(new UnitDef("rat", 8, 1, 8), 1) }, boss: BossDef) });
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount);
            var keeper = UnitSpawner.SpawnNeutral(world, new UnitDef("shopkeeper", 999, 0, 6), new GridPos(5, 1), new IdleNeutralAi(), 4);
            world.FloorItems.Add(new GridPos(6, 1), "potion_frost", 1, 30);
            world.FloorItems.Add(new GridPos(6, 2), "potion_healing", 1, 30);
            world.FloorItems.Add(new GridPos(2, 2), "potion_frost"); // 가격표 없는 물건은 그대로
            var ai = new ShopkeeperAi(world, Config, new ConsumableRules(Items), new BresenhamTargetingRule(), Sight, Shop, new IdleAi());
            world.SetShopRules(new ShopRules(Shop, zones, ai, Items));
            world.Events.Flush();
            return (world, player, keeper, ai);
        }

        // 근접 · 다른 유닛의 주문은 화내지 않음, 플레이어 주문은 화냄 → 적 · 보스 체력 ×150% · 가게 물건을 챙김(가격표 물건만)
        [Test]
        public void SpellFromPlayer_Angers_TakesStock()
        {
            var (world, player, keeper, _) = Start();
            Combat.ApplyDamage(world, player.Id, keeper, 1, DamageCause.Attack);
            Assert.That(keeper.Faction, Is.EqualTo(Faction.Neutral), "근접은 그대로 (팔기)");
            Combat.ApplyDamage(world, EntityId.None, keeper, 1, DamageCause.Spell("fire_basic"));
            Assert.That(keeper.Faction, Is.EqualTo(Faction.Neutral), "플레이어가 아닌 피해");

            Combat.ApplyDamage(world, player.Id, keeper, 5, DamageCause.Spell("fire_basic"));
            Assert.That(keeper.Faction, Is.EqualTo(Faction.Enemy));
            Assert.That(keeper.MaxHp, Is.EqualTo(60), "보스 40 × 150%");
            Assert.That(keeper.Hp, Is.EqualTo(55), "가득 찬 뒤 이번 피해");
            Assert.That((keeper.AttackDamage, keeper.Accuracy, keeper.Evasion), Is.EqualTo((4, 12, 6)), "보스 능력치");
            Assert.That(System.Array.FindAll(keeper.InventorySlots, e => !e.IsEmpty).Length, Is.EqualTo(2), "가게 물건 2개");
            foreach (var item in world.FloorItems.All) Assert.That(item.Price, Is.EqualTo(0), "가격표는 바닥에 없음");
            Assert.That(world.FloorItems.Any(new GridPos(2, 2)), Is.True, "가격표 없는 물건은 그대로");
            Assert.That(Factions.AreHostile(keeper.Faction, player.Faction), Is.True);
        }

        // 던지기(아이템 피해)도 화냄
        [Test]
        public void ThrownItem_Angers()
        {
            var (world, player, keeper, _) = Start();
            Combat.ApplyDamage(world, player.Id, keeper, 2, DamageCause.Item("dart"));
            Assert.That(keeper.Faction, Is.EqualTo(Faction.Enemy));
        }

        // AI: 체력이 반 아래면 치유 물약, 아니면 보이는 플레이어에게 해로운 물약을 던짐
        [Test]
        public void Ai_HealsWhenLow_ThrowsOtherwise()
        {
            var (world, player, keeper, ai) = Start();
            Combat.ApplyDamage(world, player.Id, keeper, 5, DamageCause.Spell("fire_basic"));
            Assert.That(ai.NextCommand(new TickContext(0, keeper.Id)), Is.InstanceOf<ThrowItemCommand>(), "냉기 물약 던지기");
            Combat.ApplyDamage(world, player.Id, keeper, 40, DamageCause.Spell("fire_basic"));
            Assert.That(ai.NextCommand(new TickContext(0, keeper.Id)), Is.InstanceOf<UseConsumableCommand>(), "체력 15/60 → 치유");
        }

        // 이기면 남은 물건이 모두 그 칸에
        [Test]
        public void Defeat_DropsAllStock()
        {
            var (world, player, keeper, _) = Start();
            Combat.ApplyDamage(world, player.Id, keeper, 1, DamageCause.Spell("fire_basic"));
            var at = keeper.Pos;
            AngryShopkeeper.DropStock(world, keeper);
            Assert.That(world.FloorItems.At(at).Count, Is.EqualTo(2));
        }

        // 화면 글 · 에셋 = BALANCE 16번
        [Test]
        public void HudText_And_ProjectAssets()
        {
            Assert.That(HudText.ShopkeeperAngered(3), Does.Contain("3개"));
            var shop = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToShop();
            Assert.That((shop.AngryHpPercent, shop.ItemUsePercent, shop.HealBelowPercent), Is.EqualTo((150, 35, 50)));
        }
    }
}
