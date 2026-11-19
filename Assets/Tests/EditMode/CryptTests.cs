using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-47 지하 묘지 (SPD Crypt — 결정 2026-11-18): 잠긴 문 · 가운데 관, 관을 열면 그 층 티어 갑옷(+1~2) + 원혼 2 (BALANCE.md 16번).</summary>
    public class CryptTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly CryptConfig Config = new CryptConfig("enemy_wraith", 2, 8,
            new[] { "armor_cloth", "armor_leather", "armor_mail", "armor_scale", "armor_plate" }, 5, 1, 2);
        private static readonly UnitDef Wraith = new UnitDef("enemy_wraith", 6, 2, 8, 0, accuracy: 8, evasion: 6, expReward: 4, expMaxLevel: 30);
        private static readonly ItemBook Items = new ItemBook(new[] { new ItemDef("armor_cloth", ItemUse.EquipArmor, armor: new ArmorDef(1, 10)) });

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static (World World, Unit Player, TurnClock Clock) TombWorld(bool rules = true)
        {
            var map = GridMovementTests.Parse("#######", "#.....#", "#.....#", "#.....#", "#######");
            map.Set(new GridPos(3, 2), TileType.Tomb);
            var world = new World(map, 3UL);
            world.InstallCoreSystems(new ShadowcastSightRule(), Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            if (rules) world.SetCryptRules(new CryptRules(Config, Wraith, new IdleAi()));
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 20, 3, 8, 10), new GridPos(2, 2), Magic.QuickSlotCount);
            world.Events.Flush();
            return (world, player, new TurnClock(world.Scheduler, world.PlayerInput));
        }

        // 관을 열면: 빈 관 · 발밑에 1층 티어 갑옷(+1~2) · 관 둘레에 원혼 2 (플레이어를 앎) · 사건 1번
        [Test]
        public void Open_DropsUpgradedArmor_WakesTwoGuardians()
        {
            var (world, player, clock) = TombWorld();
            TombOpenedEvent opened = null;
            world.Events.Subscribe<TombOpenedEvent>(e => opened = e);
            clock.SubmitPlayerCommand(new OpenTombCommand(world, new GridPos(3, 2), 1));
            world.Events.Flush();
            Assert.That(world.Map.Get(new GridPos(3, 2)), Is.EqualTo(TileType.OpenTomb));
            Assert.That(opened, Is.Not.Null);
            Assert.That(opened.ArmorId, Is.EqualTo("armor_cloth"), "1층 = 첫 티어");
            Assert.That(opened.Upgrade, Is.InRange(1, 2));
            Assert.That(opened.Guardians, Is.EqualTo(2));
            int wraiths = 0;
            foreach (var u in world.Units.ToSortedList())
                if (u.VisualId == "enemy_wraith")
                {
                    wraiths++;
                    Assert.That(GridMovement.Chebyshev(u.Pos, new GridPos(3, 2)), Is.EqualTo(1), "관 둘레");
                    Assert.That(u.LastSeenTargetPos.HasValue, Is.True, "연 사람을 안다");
                }
            Assert.That(wraiths, Is.EqualTo(2));
        }

        // 빈 관 · 먼 관은 열리지 않는다, 규칙이 없으면 빈 관이 되기만 한다
        [Test]
        public void Open_OnlyClosedAdjacentTomb()
        {
            var (world, player, _) = TombWorld();
            Assert.That(Crypts.Open(world, player, new GridPos(5, 2)), Is.False, "관이 아님");
            Assert.That(Crypts.Open(world, player, new GridPos(3, 2)), Is.True);
            Assert.That(Crypts.Open(world, player, new GridPos(3, 2)), Is.False, "빈 관");
            var (bare, barePlayer, _) = TombWorld(rules: false);
            Assert.That(Crypts.Open(bare, barePlayer, new GridPos(3, 2)), Is.True);
            Assert.That(bare.FloorItems.Count, Is.EqualTo(0));
        }

        // 갑옷 티어 = 5층마다 한 단계, 세기 = 100 + 8 × (층 − 1) %
        [Test]
        public void Config_ArmorTierAndGuardianPercent()
        {
            Assert.That(Config.ArmorFor(1), Is.EqualTo("armor_cloth"));
            Assert.That(Config.ArmorFor(5), Is.EqualTo("armor_cloth"));
            Assert.That(Config.ArmorFor(6), Is.EqualTo("armor_leather"));
            Assert.That(Config.ArmorFor(21), Is.EqualTo("armor_plate"));
            Assert.That(Config.ArmorFor(99), Is.EqualTo("armor_plate"));
            Assert.That(Config.GuardianPercent(1), Is.EqualTo(100));
            Assert.That(Config.GuardianPercent(21), Is.EqualTo(260));
            Assert.That(Wraith.Scaled(Config.GuardianPercent(21)).MaxHp, Is.EqualTo(15));
        }

        // 바닥 아이템 강화 (v33): 주우면 강화 그대로 · 스냅샷 왕복
        [Test]
        public void FloorItemUpgrade_PickedUpAndRoundTrips()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(33));
            var (world, player, _) = TombWorld();
            world.FloorItems.Add(new GridPos(4, 1), "armor_cloth", upgrade: 2);
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => null);
            Assert.That(restored.FloorItems.ItemsAt(new GridPos(4, 1))[0].Upgrade, Is.EqualTo(2));

            world.FloorItems.Add(player.Pos, "armor_cloth", upgrade: 2);
            Assert.That(ItemPickupSystem.PickUp(world, Items), Is.EqualTo(1));
            Assert.That(System.Array.Exists(player.InventorySlots, e => e.Id == "armor_cloth" && e.Upgrade == 2), Is.True);
        }

        // 지하 묘지 방: 잠긴 문 · 가운데 관 · 아이템 없음 · 방 밖 열쇠, 관은 걸을 수 없고 시야를 막지 않는다
        [Test]
        public void Room_LockedDoor_TombInCenter_KeyOutside()
        {
            int carved = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var (world, floor, room) = SpecialRoomTests.CarveFor(seed, SpecialRoomType.Crypt);
                if (room == null) continue;
                carved++;
                var map = world.Map;
                Assert.That(map.Get(room.Entrance), Is.EqualTo(TileType.LockedDoor), $"시드 {seed}");
                var center = room.Cells[room.Cells.Count / 2];
                Assert.That(map.Get(center), Is.EqualTo(TileType.Tomb));
                Assert.That(map.IsWalkable(center), Is.False);
                Assert.That(map.BlocksSight(center), Is.False);
                foreach (var c in room.Cells) Assert.That(world.FloorItems.ItemsAt(c), Is.Empty);
                int keys = 0;
                foreach (var it in world.FloorItems.All) if (it.ItemId == "key_iron") keys++;
                Assert.That(keys, Is.EqualTo(1));
            }
            Assert.That(carved, Is.GreaterThan(0));
        }

        // 검산: FloorDecorConfig 지하 묘지 값 = BALANCE.md 16번, 지킴이는 적 표에, 갑옷은 ItemCatalog에
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var c = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToCrypt();
            Assert.That((c.GuardianId, c.GuardianCount, c.GuardianPercentPerFloor, c.FloorsPerArmor, c.UpgradeMin, c.UpgradeMax),
                Is.EqualTo(("enemy_wraith", 2, 8, 5, 1, 2)));
            Assert.That(c.Armors, Is.EqualTo(new[] { "armor_cloth", "armor_leather", "armor_mail", "armor_scale", "armor_plate" }));
            var zones = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            Assert.That(zones.TryGetEnemy("enemy_wraith", out var w), Is.True);
            Assert.That((w.MaxHp, w.AttackDamage, w.SightRange, w.Accuracy, w.Evasion, w.ExpReward, w.ExpMaxLevel), Is.EqualTo((6, 2, 8, 8, 6, 4, 30)));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            foreach (var id in c.Armors) Assert.That(items.TryGet(id, out var def) && def.Use == ItemUse.EquipArmor, Is.True, id);
        }
    }
}
