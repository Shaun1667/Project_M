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
    /// <summary>3-48 석상 방 (SPD Statue — 결정 2026-11-18): 가운데 잠든 석상, 맞거나 옆에 오면 깨어남, 쓰러지면 그 층 티어 무기(+1~2) (BALANCE.md 16번).</summary>
    public class StatueTests
    {
        private sealed class WaitAi : ICommandSource { public ICommand NextCommand(in TickContext context) => new WaitCommand(1); }

        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly StatueConfig Config = new StatueConfig("enemy_statue", 8, new[] { 2, 3, 4, 5, 5 }, 5, 1, 2);
        private static readonly UnitDef Statue = new UnitDef("enemy_statue", 20, 4, 6, 0, accuracy: 10, evasion: 4, expReward: 8, expMaxLevel: 30);
        private static readonly ItemBook Items = new ItemBook(new[]
        {
            new ItemDef("knuckle", ItemUse.EquipWeapon, weapon: new WeaponDef(10, 2, 100, 1, 1, tier: 1)),
            new ItemDef("shortsword", ItemUse.EquipWeapon, weapon: new WeaponDef(10, 2, 100, 1, 1, tier: 2)),
            new ItemDef("spear", ItemUse.EquipWeapon, weapon: new WeaponDef(10, 2, 100, 1, 1, tier: 2)),
            new ItemDef("greataxe", ItemUse.EquipWeapon, weapon: new WeaponDef(10, 2, 100, 1, 1, tier: 5)),
        });

        private static (World World, Unit Player, Unit Statue, TurnClock Clock) StatueWorld()
        {
            var map = GridMovementTests.Parse("#########", "#.......#", "#.......#", "#.......#", "#########");
            var world = new World(map, 3UL);
            world.InstallCoreSystems(new ShadowcastSightRule(), Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var ai = new MeleeChaserAi(world, StepConfig, new ShadowcastSightRule());
            world.SetStatueRules(new StatueRules(Config, Statue, ai, Items));
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 20, 3, 8, 10), new GridPos(1, 2), Magic.QuickSlotCount);
            var statue = Statues.Place(world, new GridPos(5, 2));
            world.Events.Flush();
            return (world, player, statue, new TurnClock(world.Scheduler, world.PlayerInput));
        }

        // 잠든 석상은 플레이어가 보여도 움직이지 않는다
        [Test]
        public void Dormant_DoesNotMove_WhilePlayerVisibleButFar()
        {
            var (world, _, statue, clock) = StatueWorld();
            Assert.That(statue.Dormant, Is.True);
            for (int i = 0; i < 5; i++) clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
            Assert.That(statue.Pos, Is.EqualTo(new GridPos(5, 2)));
            Assert.That(statue.Dormant, Is.True);
        }

        // 바로 옆에 오면 깨어난다
        [Test]
        public void WakesWhenPlayerAdjacent()
        {
            var (world, player, statue, clock) = StatueWorld();
            world.Units.Teleport(player.Id, new GridPos(4, 2));
            clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
            Assert.That(statue.Dormant, Is.False);
        }

        // 피해를 입으면 깨어난다 (멀리서 주문)
        [Test]
        public void WakesWhenDamaged()
        {
            var (world, player, statue, _) = StatueWorld();
            int woke = 0;
            world.Events.Subscribe<StatueAwokenEvent>(_ => woke++);
            Combat.ApplyDamage(world, player.Id, statue, 1, DamageCause.Spell("fire_basic"));
            world.Events.Flush();
            Assert.That(statue.Dormant, Is.False);
            Assert.That(woke, Is.EqualTo(1));
            Combat.ApplyDamage(world, player.Id, statue, 1, DamageCause.Spell("fire_basic"));
            world.Events.Flush();
            Assert.That(woke, Is.EqualTo(1), "한 번만");
        }

        // 쓰러지면 그 층 티어 무기 하나(+1~2)를 그 칸에 떨어뜨린다 (1층 = 티어 2)
        [Test]
        public void DropsTierWeaponWithUpgrade()
        {
            var (world, player, statue, clock) = StatueWorld();
            Combat.ApplyDamage(world, player.Id, statue, statue.Hp, DamageCause.Attack);
            clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
            var drops = world.FloorItems.ItemsAt(new GridPos(5, 2));
            Assert.That(drops.Count, Is.EqualTo(1));
            Assert.That(new[] { "shortsword", "spear" }, Has.Member(drops[0].ItemId));
            Assert.That(drops[0].Upgrade, Is.InRange(1, 2));
        }

        // 티어 · 세기 계산
        [Test]
        public void Config_TierAndPercent()
        {
            Assert.That(Config.TierFor(1), Is.EqualTo(2));
            Assert.That(Config.TierFor(6), Is.EqualTo(3));
            Assert.That(Config.TierFor(16), Is.EqualTo(5));
            Assert.That(Config.TierFor(25), Is.EqualTo(5));
            Assert.That(Config.PercentFor(1), Is.EqualTo(100));
            Assert.That(Config.PercentFor(11), Is.EqualTo(180));
        }

        // 잠듦은 스냅샷에 남는다 (v34)
        [Test]
        public void Dormant_RoundTrips()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(34));
            var (world, _, statue, _) = StatueWorld();
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => new WaitAi());
            Assert.That(restored.Units.TryGet(statue.Id, out var back) && back.Dormant, Is.True);
        }

        // 석상 방: 입구 = 문, 가운데 칸에 잠든 석상, 아이템 없음
        [Test]
        public void Room_DormantStatueInCenter()
        {
            int carved = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var (world, _, room) = SpecialRoomTests.CarveFor(seed, SpecialRoomType.Statue,
                    w => w.SetStatueRules(new StatueRules(Config, Statue, new MeleeChaserAi(w, StepConfig, new ShadowcastSightRule()), Items)));
                if (room == null) continue;
                carved++;
                Assert.That(world.Map.Get(room.Entrance), Is.EqualTo(TileType.Door), $"시드 {seed}");
                Assert.That(world.Units.TryGetAt(room.Cells[room.Cells.Count / 2], out var statue), Is.True, $"시드 {seed}");
                Assert.That(statue.VisualId, Is.EqualTo("enemy_statue"));
                Assert.That(statue.Dormant, Is.True);
                foreach (var c in room.Cells) Assert.That(world.FloorItems.ItemsAt(c), Is.Empty);
            }
            Assert.That(carved, Is.GreaterThan(0));
        }

        // 검산: FloorDecorConfig 석상 값 = BALANCE.md 16번, 석상 적 줄, 티어마다 무기가 있다
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var c = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToStatue();
            Assert.That((c.StatueId, c.PercentPerFloor, c.FloorsPerTier, c.UpgradeMin, c.UpgradeMax), Is.EqualTo(("enemy_statue", 8, 5, 1, 2)));
            Assert.That(c.WeaponTiers, Is.EqualTo(new[] { 2, 3, 4, 5, 5 }));
            var zones = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            Assert.That(zones.TryGetEnemy("enemy_statue", out var s), Is.True);
            Assert.That((s.MaxHp, s.AttackDamage, s.SightRange, s.Accuracy, s.Evasion, s.ExpReward, s.ExpMaxLevel, s.MoveCostTicks), Is.EqualTo((20, 4, 6, 10, 4, 8, 30, 2)));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            foreach (int tier in c.WeaponTiers)
            {
                bool any = false;
                foreach (var id in items.Ids) if (items.TryGet(id, out var d) && d.Use == ItemUse.EquipWeapon && d.Weapon != null && d.Weapon.Tier == tier) any = true;
                Assert.That(any, Is.True, $"티어 {tier} 무기");
            }
        }
    }
}
