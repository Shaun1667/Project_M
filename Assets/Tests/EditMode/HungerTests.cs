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
    /// <summary>3-14 허기 · 음식 (SPD — 결정 2026-11-04): 300 배고픔 · 450 굶주림(매 턴 1 · 회복 멈춤) · 먹기 3턴 · 요리 · 고기 떨어뜨리기 · 배치 (BALANCE.md 18번).</summary>
    public class HungerTests
    {
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);

        /// <summary>BALANCE.md 18번.</summary>
        private static HungerConfig BalanceConfig() => new HungerConfig(300, 450, 1, 3, 60, new[] { StatusType.Burn, StatusType.Slow, StatusType.Poison },
            "food_mystery_meat", 15, "food_ration", "food_bread", 50, "food_ration");

        private static readonly ItemBook Items = new ItemBook(new[]
        {
            new ItemDef("food_ration", ItemUse.Food, food: 300),
            new ItemDef("food_bread", ItemUse.Food, food: 150),
            new ItemDef("food_mystery_meat", ItemUse.Food, food: 150, risky: true, fireTransformsTo: "food_cooked_meat", iceTransformsTo: "food_frozen_meat"),
            new ItemDef("food_cooked_meat", ItemUse.Food, food: 150),
            new ItemDef("food_frozen_meat", ItemUse.Food, food: 150),
        });

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;

            public void Wait(int turns)
            {
                for (int i = 0; i < turns; i++) Clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
                World.Events.Flush();
            }
        }

        private static Session Start(HungerConfig config = null)
        {
            var world = new World(GridMovementTests.Parse("#######", "#.....#", "#######"), 5UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains(), hunger: config ?? BalanceConfig());
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount);
            world.Events.Flush();
            return s;
        }

        [Test]
        public void Hunger_RisesEachTurn_Thresholds()
        {
            var c = BalanceConfig();
            Assert.That(Hunger.StateOf(c, 299), Is.EqualTo(HungerState.None));
            Assert.That(Hunger.StateOf(c, 300), Is.EqualTo(HungerState.Hungry));
            Assert.That(Hunger.StateOf(c, 450), Is.EqualTo(HungerState.Starving));

            var s = Start();
            s.Wait(10);
            Assert.That(s.Player.Hunger, Is.EqualTo(10));
        }

        // 굶주림: 매 턴 피해 1, 자동 회복 멈춤, 허기는 450에서 멈춤
        [Test]
        public void Starving_DamagesEveryTurn_NoRegen()
        {
            var s = Start(new HungerConfig(5, 10, 1, 3, 0, null));
            s.Wait(10);
            int hp = s.Player.Hp;
            s.Wait(8);
            Assert.That(s.Player.Hunger, Is.EqualTo(10), "굶주림에서 멈춤");
            Assert.That(s.Player.Hp, Is.EqualTo(hp - 8), "매 턴 1, 회복 없음");
            Assert.That(s.World.Run.LastPlayerDamage.Kind, Is.EqualTo(DamageKind.Hunger));
        }

        // 먹기: 3턴, 허기 −300 (0 아래 안 감), 단계가 바뀌면 이벤트
        [Test]
        public void Eating_TakesThreeTurns_ReducesHunger()
        {
            var s = Start(new HungerConfig(5, 10, 0, 3, 0, null));
            var changes = new System.Collections.Generic.List<HungerChangedEvent>();
            s.World.Events.Subscribe<HungerChangedEvent>(changes.Add);
            s.Wait(6);
            Assert.That(changes.Exists(e => e.State == HungerState.Hungry), Is.True);
            int index = Inventory.TryAdd(s.World, null, Items, s.Player, InventoryRef.Item("food_ration"));
            long tick = s.World.Scheduler.CurrentTick;
            s.Clock.SubmitPlayerCommand(new EatCommand(s.World, Items, null, new HungerConfig(5, 10, 0, 3, 0, null), index, InventoryRef.Item("food_ration")));
            s.World.Events.Flush();
            Assert.That(s.World.Scheduler.CurrentTick - tick, Is.EqualTo(3));
            Assert.That(s.Player.Hunger, Is.LessThanOrEqualTo(3), "0으로 내려간 뒤 남은 2틱만큼");
            Assert.That(s.Player.InventorySlots[index].IsEmpty, Is.True);
        }

        // 정체불명 고기: 100%면 나쁜 상태 하나, 구운 고기는 안전
        [Test]
        public void RiskyMeat_MayCauseBadStatus()
        {
            var config = new HungerConfig(300, 450, 1, 3, 100, new[] { StatusType.Poison });
            var s = Start(config);
            int index = Inventory.TryAdd(s.World, null, Items, s.Player, InventoryRef.Item("food_mystery_meat"));
            Assert.That(Hunger.Eat(s.World, Items, SpellTests.BalanceStatuses(), config, s.Player, index), Is.True);
            Assert.That(s.Player.HasStatus(StatusType.Poison), Is.True);

            index = Inventory.TryAdd(s.World, null, Items, s.Player, InventoryRef.Item("food_cooked_meat"));
            s.Player.Statuses.Clear();
            Hunger.Eat(s.World, Items, SpellTests.BalanceStatuses(), config, s.Player, index);
            Assert.That(s.Player.Statuses, Is.Empty);
        }

        // 요리: 바닥 고기 + 불 = 구운 고기, + 얼음 = 얼린 고기, 다른 원소는 그대로
        [Test]
        public void FloorMeat_CookedByFire_FrozenByIce()
        {
            var s = Start();
            var a = new GridPos(3, 1);
            var b = new GridPos(4, 1);
            s.World.FloorItems.Add(a, "food_mystery_meat");
            s.World.FloorItems.Add(b, "food_mystery_meat");
            Hunger.TransformFloorItems(s.World, Items, Element.Fire, new[] { a });
            Hunger.TransformFloorItems(s.World, Items, Element.Ice, new[] { b });
            Hunger.TransformFloorItems(s.World, Items, Element.Lightning, new[] { a, b });
            Assert.That(s.World.FloorItems.At(a), Is.EqualTo(new[] { "food_cooked_meat" }));
            Assert.That(s.World.FloorItems.At(b), Is.EqualTo(new[] { "food_frozen_meat" }));
        }

        // 배치: 1~마지막 전 층마다 식량 1, 빵은 확률
        [Test]
        public void PlanFood_RationEveryFloor()
        {
            var s = Start();
            Loot.PlanFood(s.World, Items, BalanceConfig(), 5);
            int rations = 0;
            foreach (var p in s.World.Run.PendingFloorItems) if (p.ItemId == "food_ration") rations++;
            Assert.That(rations, Is.EqualTo(4));
        }

        [Test]
        public void Food_Stacks()
        {
            var s = Start();
            Inventory.TryAdd(s.World, null, Items, s.Player, InventoryRef.Item("food_ration"));
            Inventory.TryAdd(s.World, null, Items, s.Player, InventoryRef.Item("food_ration"));
            Assert.That(s.Player.InventorySlots[0].Count, Is.EqualTo(2));
        }

        // 스냅샷 v20: 허기 왕복
        [Test]
        public void Snapshot_RoundTripsHunger()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(20), "3-14 허기");
            var s = Start();
            s.Wait(7);
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(s.World.Capture())), (w, id) => null);
            restored.Units.TryGet(s.Player.Id, out var p);
            Assert.That(p.Hunger, Is.EqualTo(7));
        }

        [Test]
        public void Texts()
        {
            Assert.That(HudText.HungerLog(HungerState.Hungry), Is.EqualTo("배가 고프다"));
            Assert.That(HudText.HungerLog(HungerState.Starving), Is.EqualTo("굶주리고 있다! 무언가 먹어야 한다"));
        }

        // 검산: HungerConfig.asset = BALANCE.md 18번
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<HungerConfigAsset>("Assets/_Project/Data/Units/HungerConfig.asset");
            Assert.That(asset, Is.Not.Null, "HungerConfig.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var c = asset.ToConfig();
            var e = BalanceConfig();
            Assert.That((c.HungryAt, c.StarvingAt, c.StarveDamage, c.EatTicks, c.RiskyFoodBadPercent), Is.EqualTo((e.HungryAt, e.StarvingAt, e.StarveDamage, e.EatTicks, e.RiskyFoodBadPercent)));
            Assert.That(c.RiskyFoodStatuses, Is.EqualTo(e.RiskyFoodStatuses));
            Assert.That((c.EnemyDropItem, c.EnemyDropPercent, c.FloorFoodItem, c.BonusFoodItem, c.BonusFoodPercent, c.StartingFood),
                Is.EqualTo((e.EnemyDropItem, e.EnemyDropPercent, e.FloorFoodItem, e.BonusFoodItem, e.BonusFoodPercent, e.StartingFood)));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            foreach (var id in Items.Ids)
            {
                Items.TryGet(id, out var x);
                Assert.That(items.TryGet(id, out var a), Is.True, id);
                Assert.That((a.Use, a.Food, a.Risky, a.FireTransformsTo, a.IceTransformsTo), Is.EqualTo((x.Use, x.Food, x.Risky, x.FireTransformsTo, x.IceTransformsTo)), id);
            }
        }
    }
}
