using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-16b 씨앗 · 식물 (SPD — 결정 2026-11-04): 심기 · 던져 심기 · 밟으면 발동(보이는 함정과 같은 구조) · 높은 풀에서 씨앗 (BALANCE.md 11 · 17번).</summary>
    public class PlantTests
    {
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        private static readonly ItemBook Items = new ItemBook(new[]
        {
            new ItemDef("seed_sungrass", ItemUse.Seed, plantTrap: TrapType.Sungrass),
            new ItemDef("seed_icecap", ItemUse.Seed, plantTrap: TrapType.Icecap),
        });

        private sealed class Run
        {
            public World World;
            public Unit Player;
            public Unit Enemy;
            public TurnClock Clock;
        }

        private static Run Start()
        {
            var book = ReactionTests.Book(TrapTests.BalanceTrapSpells());
            var s = ReactionTests.Start(book, new GridPos(1, 2), new GridPos(6, 2));
            s.World.Scheduler.AddSystem(TickPhase.ManaRegen, new TrapSystem(s.World, TrapTests.BalanceTraps(book), book));
            return new Run { World = s.World, Player = s.Player, Enemy = s.Enemies[0], Clock = s.Clock };
        }

        // 발밑에 심으면 보이는 식물 — 햇볕풀은 밟은(선) 유닛을 회복하고 사라진다
        [Test]
        public void PlantAtFeet_SungrassHeals()
        {
            var r = Start();
            Combat.ApplyDamage(r.World, EntityId.None, r.Player, 10);
            int index = Inventory.TryAdd(r.World, null, Items, r.Player, InventoryRef.Item("seed_sungrass"));
            r.Clock.SubmitPlayerCommand(new PlantCommand(r.World, Items, index, InventoryRef.Item("seed_sungrass"), 1));
            r.World.Events.Flush();
            Assert.That(r.Player.Hp, Is.GreaterThan(r.Player.MaxHp - 10), "50% 회복");
            Assert.That(r.World.Traps.Has(r.Player.Pos), Is.False, "한 번 쓰면 사라짐");
            Assert.That(r.Player.InventorySlots[index].IsEmpty, Is.True);
        }

        // 던지면 떨어진 칸에 심긴다 — 적이 그 칸에 있으면 다음 틱에 발동 (얼음버섯 = 빙결)
        [Test]
        public void ThrownSeed_PlantsAndTriggersOnEnemy()
        {
            var r = Start();
            int index = Inventory.TryAdd(r.World, null, Items, r.Player, InventoryRef.Item("seed_icecap"));
            Assert.That(Consumables.Throw(r.World, new ConsumableRules(Items), new BresenhamTargetingRule(), r.Player, index, r.Enemy.Pos), Is.EqualTo(ConsumeResult.Used));
            Assert.That(r.World.Traps.TryGet(r.Enemy.Pos, out var plant) && !plant.Hidden, Is.True, "보이는 식물");
            r.Clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
            r.World.Events.Flush();
            Assert.That(r.Enemy.HasStatus(StatusType.Frozen), Is.True);
        }

        // 식물은 무작위 함정 배치에 안 나온다 (무게 0)
        [Test]
        public void Plants_NotPlacedRandomly()
        {
            var book = ReactionTests.Book(TrapTests.BalanceTrapSpells());
            var traps = TrapTests.BalanceTraps(book);
            var rng = new RngSet(3).Get(RngStream.Decor);
            for (int i = 0; i < 200; i++) Assert.That((int)traps.Pick(rng).Type, Is.LessThanOrEqualTo((int)TrapType.Alarm));
        }

        // 높은 풀을 밟으면 확률로 씨앗 (100%면 반드시)
        [Test]
        public void TramplingTallGrass_DropsSeed()
        {
            var world = new World(GridMovementTests.Parse("#####", "#...#", "#####"), 3UL);
            var config = new LootConfig(2, 3, seedsPerFloor: 0, seedItems: new[] { "seed_sungrass" }, seedDropPercent: 100);
            Loot.DropSeed(world, config, new GridPos(2, 1));
            Assert.That(world.FloorItems.At(new GridPos(2, 1)), Is.EqualTo(new[] { "seed_sungrass" }));
        }

        // 검산: LootConfig 씨앗 · 아이템 씨앗 5종 = BALANCE.md
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var loot = AssetDatabase.LoadAssetAtPath<LootConfigAsset>("Assets/_Project/Data/Items/LootConfig.asset").ToConfig();
            Assert.That((loot.SeedsPerFloor, loot.SeedDropPercent), Is.EqualTo((2, 5)));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            foreach (var (id, plant) in new[] { ("seed_firebloom", TrapType.Firebloom), ("seed_icecap", TrapType.Icecap), ("seed_sorrowmoss", TrapType.Sorrowmoss),
                         ("seed_sungrass", TrapType.Sungrass), ("seed_fadeleaf", TrapType.Fadeleaf) })
            {
                Assert.That(items.TryGet(id, out var d), Is.True, id);
                Assert.That((d.Use, d.PlantTrap), Is.EqualTo((ItemUse.Seed, plant)), id);
                Assert.That(loot.SeedItems, Has.Member(id));
            }
        }
    }
}
