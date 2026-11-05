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
    /// <summary>3-28b 마법 제작대 · 에테르 (결정 2026-11-05): 만들기 12 · 분해 6 · 강화 6/9/12 (최대 +3) · 작업실 구역마다 한 층 · 스냅샷 v29 (BALANCE 24번).</summary>
    public class WorkbenchTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);
        private static readonly WorkbenchConfig Config = new WorkbenchConfig(12, 6, new[] { 6, 9, 12 });

        private static ItemDef RelicDef(string id, TalentEffect effect, params int[] values) =>
            new ItemDef(id, ItemUse.Relic, relicEffects: new[] { new TalentEffectDef(effect, values) });

        private static readonly ItemBook Items = new ItemBook(new[]
        {
            RelicDef("relic_life_stone", TalentEffect.MaxHp, 10, 15, 20, 25),
            RelicDef("relic_predator_fang", TalentEffect.HitManaChance, 25, 35, 45, 55),
            new ItemDef("food_ration", ItemUse.Food, food: 300),
        });

        private static (World world, Unit player) Start()
        {
            var world = new World(GridMovementTests.Parse("#######", "#.....#", "#.....#", "#######"), 3UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            world.SetRelicRules(Items);
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(2, 1), Magic.QuickSlotCount);
            world.Events.Flush();
            return (world, player);
        }

        private static int Give(World world, Unit player, string id, int upgrade = 0) =>
            Inventory.TryAdd(world, null, Items, player, InventoryRef.Item(id, upgrade));

        private static int IndexOf(Unit player, string id) => System.Array.FindIndex(player.InventorySlots, e => e.Id == id);

        // 분해 = +6 (강화 단계와 무관), 두 개 분해 = 12 = 만들기 하나. 에테르가 모자라면 안 됨, 유물이 아니면 안 됨
        [Test]
        public void DisassembleTwice_ThenCraftOne()
        {
            var (world, player) = Start();
            Assert.That(Workbench.Craft(world, Items, Config, player, "relic_life_stone"), Is.EqualTo(WorkbenchResult.NotEnoughEther));
            Give(world, player, "relic_predator_fang", upgrade: 2);
            Give(world, player, "relic_life_stone");
            Give(world, player, "food_ration");
            Assert.That(Workbench.Disassemble(world, Items, Config, player, IndexOf(player, "food_ration")), Is.EqualTo(WorkbenchResult.NotRelic));
            Assert.That(Workbench.Disassemble(world, Items, Config, player, IndexOf(player, "relic_predator_fang")), Is.EqualTo(WorkbenchResult.Done));
            Assert.That(world.Run.Ether, Is.EqualTo(6), "강화한 유물도 6");
            Workbench.Disassemble(world, Items, Config, player, IndexOf(player, "relic_life_stone"));
            Assert.That(world.Run.Ether, Is.EqualTo(12));
            Assert.That(Workbench.Craft(world, Items, Config, player, "food_ration"), Is.EqualTo(WorkbenchResult.NotRelic));
            Assert.That(Workbench.Craft(world, Items, Config, player, "relic_predator_fang"), Is.EqualTo(WorkbenchResult.Done));
            Assert.That(world.Run.Ether, Is.EqualTo(0));
            Assert.That(IndexOf(player, "relic_predator_fang"), Is.GreaterThanOrEqualTo(0), "배낭에");
        }

        // 강화: 배낭 유물 +1 = 6 · +2 = 9 · +3 = 12, 그다음은 최대
        [Test]
        public void Upgrade_CostsAndMax()
        {
            var (world, player) = Start();
            for (int i = 0; i < 6; i++) Give(world, player, "relic_predator_fang");
            for (int i = 0; i < 5; i++) Workbench.Disassemble(world, Items, Config, player, IndexOf(player, "relic_predator_fang"));
            Assert.That(world.Run.Ether, Is.EqualTo(30));
            int idx = IndexOf(player, "relic_predator_fang");
            Assert.That(Workbench.Upgrade(world, Items, Config, player, -1, idx), Is.EqualTo(WorkbenchResult.Done));
            Assert.That(world.Run.Ether, Is.EqualTo(24));
            Workbench.Upgrade(world, Items, Config, player, -1, IndexOf(player, "relic_predator_fang"));
            Workbench.Upgrade(world, Items, Config, player, -1, IndexOf(player, "relic_predator_fang"));
            Assert.That(world.Run.Ether, Is.EqualTo(30 - 6 - 9 - 12));
            var relic = player.InventorySlots[IndexOf(player, "relic_predator_fang")];
            Assert.That(relic.Upgrade, Is.EqualTo(3));
            Assert.That(Workbench.Upgrade(world, Items, Config, player, -1, IndexOf(player, "relic_predator_fang")), Is.EqualTo(WorkbenchResult.MaxUpgrade));
            Relics.Equip(world, Items, player, IndexOf(player, "relic_predator_fang"));
            Assert.That(Talents.MeleeHitManaPercent(world, player), Is.EqualTo(55), "+3 값");
        }

        // 낀 생명석을 강화하면 최대 체력이 차이만큼 (+10 → +15)
        [Test]
        public void Upgrade_WornLifeStone_RaisesMaxHp()
        {
            var (world, player) = Start();
            Give(world, player, "relic_predator_fang");
            Workbench.Disassemble(world, Items, Config, player, IndexOf(player, "relic_predator_fang"));
            Give(world, player, "relic_life_stone");
            Relics.Equip(world, Items, player, IndexOf(player, "relic_life_stone"));
            Assert.That(player.MaxHp, Is.EqualTo(30));
            Assert.That(Workbench.Upgrade(world, Items, Config, player, 0, -1), Is.EqualTo(WorkbenchResult.Done));
            Assert.That((player.Relics[0].Upgrade, player.MaxHp), Is.EqualTo((1, 35)));
        }

        // 작업실 층: 구역마다 정확히 한 층 (1층 · 보스 층 · 제단 구역 제외), 같은 시드 = 같은 층
        [Test]
        public void WorkshopFloor_OnePerZone_Deterministic()
        {
            var zones = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset").ToBook();
            foreach (ulong seed in new[] { 1UL, 77UL, 123456789UL })
            {
                foreach (var zone in zones.Zones)
                {
                    int count = 0;
                    for (int f = zone.FirstFloor; f <= zone.LastFloor; f++)
                    {
                        if (!Workbench.IsWorkshopFloor(zones, seed, f)) continue;
                        count++;
                        Assert.That(f >= 2 && !zones.IsBossFloor(f), Is.True, $"시드 {seed} 층 {f}");
                        Assert.That(Workbench.IsWorkshopFloor(zones, seed, f), Is.True, "같은 시드 = 같은 결과");
                    }
                    Assert.That(count, Is.EqualTo(zone.IsAltar ? 0 : 1), $"시드 {seed} 구역 {zone.Name}");
                }
            }
        }

        // 작업실 방: 가운데 칸 = 마법 제작대 (걸을 수 없음 · 시야는 안 막음)
        [Test]
        public void BuildRoom_PlacesBench()
        {
            var gen = new DungeonGenConfig(width: 50, height: 50, roomCount: 10, roomSizeRoll: 20, roomMinSize: 7,
                wallBlockCount: 20, wallBlockSizeRoll: 10, wallBlockMinSize: 3, wallTunnelCostMultiplier: 5, placementAttemptLimit: 1000, deadEndLoopMinWalkDistance: 10);
            DungeonFloor floor = null;
            var world = World.Create(4UL, rng => (floor = new DungeonGenerator(gen).Generate(rng)).Map);
            Assert.That(Workbench.BuildRoom(world, floor, Config, world.Rng.Get(RngStream.Decor)), Is.True);
            int benches = 0;
            for (int y = 0; y < world.Map.Height; y++)
                for (int x = 0; x < world.Map.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (world.Map.Get(p) != TileType.ArtifactBench) continue;
                    benches++;
                    Assert.That(world.Map.IsWalkable(p), Is.False);
                    Assert.That(world.Map.BlocksSight(p), Is.False);
                }
            Assert.That(benches, Is.EqualTo(1));
        }

        // 스냅샷 v29: 에테르 왕복
        [Test]
        public void Snapshot_RoundTripsEther()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(29));
            var (world, player) = Start();
            Give(world, player, "relic_life_stone");
            Workbench.Disassemble(world, Items, Config, player, IndexOf(player, "relic_life_stone"));
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => null);
            Assert.That(restored.Run.Ether, Is.EqualTo(6));
        }

        // 화면 글
        [Test]
        public void HudText_WorkbenchLines()
        {
            var text = new HudText();
            Assert.That(HudText.WorkbenchTitle(18), Is.EqualTo("마법 제작대 — 에테르 18"));
            Assert.That(HudText.EtherLabel(0), Is.Empty, "0이면 안 보임");
            Assert.That(HudText.WorkbenchFailed(WorkbenchResult.NotEnoughEther), Is.EqualTo("에테르가 모자라다"));
            Assert.That(text.WorkbenchUsed(WorkbenchAction.Upgrade, "relic_x", 2, 3), Does.Contain("+2").And.Contain("에테르 3"));
            Assert.That(HudText.Tile(TileType.ArtifactBench), Is.EqualTo("마법 제작대"));
        }

        // 프로젝트 에셋 = BALANCE 24번 (제작대 수치 · 유물 강화 값)
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var w = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToWorkbench();
            Assert.That((w.CraftCost, w.DisassembleGain, w.MaxUpgrade), Is.EqualTo((12, 6, 3)));
            Assert.That(w.UpgradeCosts, Is.EqualTo(new[] { 6, 9, 12 }));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            void Check(string id, params int[] values)
            {
                Assert.That(items.TryGet(id, out var def), Is.True, id);
                Assert.That(def.RelicEffects[0].Values, Is.EqualTo(values), id);
            }
            Check("relic_elemental_crystal", 2, 3, 4, 5);
            Check("relic_life_stone", 10, 15, 20, 25);
            Check("relic_mana_spring", -1, -1, -2, -2);
            Check("relic_predator_fang", 25, 35, 45, 55);
            Check("relic_shadow_cloak", 30, 45, 60, 75);
        }
    }
}
