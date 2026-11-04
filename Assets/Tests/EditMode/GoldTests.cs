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
    /// <summary>3-15a 금화 (SPD — 결정 2026-11-04): 바닥 무더기(층이 깊을수록) · 적이 떨어뜨림 · 걸어 들어가면 지갑으로 · 스냅샷 v21 (BALANCE.md 11번).</summary>
    public class GoldTests
    {
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);
        private static readonly ItemBook Items = new ItemBook(new[] { new ItemDef("gold", ItemUse.Gold) });

        /// <summary>BALANCE.md 11번 금화 값.</summary>
        private static LootConfig BalanceLoot() => new LootConfig(2, 3, 2, 3, 2, 2, goldPilesPerFloor: 3, goldMinBase: 30, goldMinPerFloor: 10,
            goldMaxBase: 60, goldMaxPerFloor: 20, enemyGoldPercent: 20, enemyGoldMin: 5, enemyGoldMax: 15);

        private static (World World, Unit Player, TurnClock Clock) Start()
        {
            var world = new World(GridMovementTests.Parse("############", "#..........#", "#..........#", "#..........#", "############"), 5UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            world.Scheduler.AddSystem(TickPhase.ManaRegen, new ItemPickupSystem(world, Items));
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 2), Magic.QuickSlotCount);
            world.Events.Flush();
            return (world, player, new TurnClock(world.Scheduler, world.PlayerInput));
        }

        [Test]
        public void WalkingOntoGold_AddsToWallet_NotBackpack()
        {
            var (world, player, clock) = Start();
            world.FloorItems.Add(new GridPos(2, 2), Loot.GoldItemId, 37);
            var changes = new System.Collections.Generic.List<GoldChangedEvent>();
            world.Events.Subscribe<GoldChangedEvent>(changes.Add);
            Assert.That(PlayerActions.TryStep(world, StepConfig, new GridPos(1, 0), out var step), Is.True);
            clock.SubmitPlayerCommand(step);
            world.Events.Flush();
            Assert.That(world.Run.Gold, Is.EqualTo(37));
            Assert.That(world.FloorItems.Count, Is.EqualTo(0));
            Assert.That(System.Array.TrueForAll(player.InventorySlots, e => e.IsEmpty), Is.True, "배낭 칸을 안 씀");
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].Delta, Is.EqualTo(37));
        }

        // 층 무더기: 3개, 금액 = [30 + 10 × 층, 60 + 20 × 층], 시작 칸에서 3칸 이상
        [Test]
        public void PlaceGold_PilesPerFloor_AmountByDepth()
        {
            var (world, _, _) = Start();
            Loot.PlaceGold(world, new GridPos(1, 2), BalanceLoot());
            Assert.That(world.FloorItems.Count, Is.EqualTo(3));
            foreach (var item in world.FloorItems.All)
            {
                Assert.That(item.ItemId, Is.EqualTo(Loot.GoldItemId));
                Assert.That(item.Quantity, Is.InRange(30 + 10 * world.Floor, 60 + 20 * world.Floor));
                Assert.That(GridMovement.Chebyshev(item.Pos, new GridPos(1, 2)), Is.GreaterThanOrEqualTo(3));
            }
        }

        [Test]
        public void EnemyDrop_UsesChance()
        {
            var (world, _, _) = Start();
            var enemy = UnitSpawner.SpawnEnemy(world, new UnitDef("enemy_melee", 5, 1, 6), new GridPos(8, 2), new WaitAi());
            Loot.DropGold(world, new LootConfig(2, 3, enemyGoldPercent: 100, enemyGoldMin: 5, enemyGoldMax: 15), enemy);
            Assert.That(world.FloorItems.ItemsAt(new GridPos(8, 2))[0].Quantity, Is.InRange(5, 15));
            Loot.DropGold(world, new LootConfig(2, 3, enemyGoldPercent: 0, enemyGoldMin: 5, enemyGoldMax: 15), enemy);
            Assert.That(world.FloorItems.Count, Is.EqualTo(1), "0%면 안 떨어뜨림");
        }

        // 스냅샷 v21: 지갑 · 바닥 무더기 금액 왕복
        [Test]
        public void Snapshot_RoundTripsGold()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(21), "3-15a 금화");
            var (world, _, _) = Start();
            world.FloorItems.Add(new GridPos(5, 2), Loot.GoldItemId, 88);
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => null);
            Assert.That(restored.FloorItems.ItemsAt(new GridPos(5, 2))[0].Quantity, Is.EqualTo(88));
        }

        [Test]
        public void Texts()
        {
            Assert.That(HudText.GoldPicked(37), Is.EqualTo("금화 37개를 주웠다"));
        }

        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var c = AssetDatabase.LoadAssetAtPath<LootConfigAsset>("Assets/_Project/Data/Items/LootConfig.asset").ToConfig();
            var e = BalanceLoot();
            Assert.That((c.GoldPilesPerFloor, c.GoldMinBase, c.GoldMinPerFloor, c.GoldMaxBase, c.GoldMaxPerFloor, c.EnemyGoldPercent, c.EnemyGoldMin, c.EnemyGoldMax),
                Is.EqualTo((e.GoldPilesPerFloor, e.GoldMinBase, e.GoldMinPerFloor, e.GoldMaxBase, e.GoldMaxPerFloor, e.EnemyGoldPercent, e.EnemyGoldMin, e.EnemyGoldMax)));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            Assert.That(items.TryGet(Loot.GoldItemId, out var gold) && gold.Use == ItemUse.Gold, Is.True);
        }

        private sealed class WaitAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }
    }
}
