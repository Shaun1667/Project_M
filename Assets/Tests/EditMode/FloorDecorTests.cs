using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-11a 층 꾸미기: 물 웅덩이(영구 물 지형) · 짧은 풀 · 높은 풀(시야 가림 · 밟으면 짧은 풀) — BALANCE.md 16번.</summary>
    public class FloorDecorTests
    {
        private const string AssetPath = "Assets/_Project/Data/Dungeon/FloorDecorConfig.asset";
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);

        /// <summary>BALANCE.md 16번 표와 같은 값.</summary>
        private static FloorDecorConfig BalanceConfig() => new FloorDecorConfig(
            grassPatchCount: 6, grassPatchMinSize: 4, grassPatchMaxSize: 14, tallGrassPercent: 50,
            puddleCount: 4, puddleMinSize: 3, puddleMaxSize: 9);

        private static DungeonGenConfig GenConfig() => new DungeonGenConfig(
            width: 50, height: 50,
            roomCount: 10, roomSizeRoll: 20, roomMinSize: 7,
            wallBlockCount: 20, wallBlockSizeRoll: 10, wallBlockMinSize: 3,
            wallTunnelCostMultiplier: 5, placementAttemptLimit: 1000,
            deadEndLoopMinWalkDistance: 10);

        private static TerrainDef WaterDef()
        {
            SpellTests.BalanceTerrains().TryGet(TerrainEffectType.Water, out var water);
            return water;
        }

        /// <summary>게임과 같은 순서: 맵 생성 → (꾸미기). decorate = false면 꾸미지 않은 같은 시드의 층.</summary>
        private static (World World, DungeonFloor Floor) Build(ulong seed, bool decorate = true)
        {
            DungeonFloor floor = null;
            var world = World.Create(seed, rng => (floor = new DungeonGenerator(GenConfig()).Generate(rng)).Map);
            if (decorate) FloorDecor.Apply(world, floor, BalanceConfig(), WaterDef());
            world.Events.Flush();
            return (world, floor);
        }

        private static int Count(GridMap map, TileType type)
        {
            int n = 0;
            foreach (var t in map.CopyCells())
                if (t == type) n++;
            return n;
        }

        [Test]
        public void SameSeed_SameDecor()
        {
            var a = Build(42UL);
            var b = Build(42UL);
            Assert.That(b.World.Map.CopyCells(), Is.EqualTo(a.World.Map.CopyCells()));
            var ta = a.World.Terrain.ToSortedList();
            var tb = b.World.Terrain.ToSortedList();
            Assert.That(tb.ConvertAll(p => p.Key), Is.EqualTo(ta.ConvertAll(p => p.Key)), "웅덩이 칸");
        }

        // 꾸미기는 Decor 난수만 — 걸을 수 있는 칸 · 계단 · 시작점은 꾸미지 않은 층과 같다
        [Test]
        public void Decor_KeepsLayout_AndOnlyReplacesFloor()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var plain = Build(seed, decorate: false);
                var decorated = Build(seed);
                Assert.That(decorated.Floor.Start, Is.EqualTo(plain.Floor.Start));
                var before = plain.World.Map.CopyCells();
                var after = decorated.World.Map.CopyCells();
                for (int i = 0; i < before.Length; i++)
                {
                    if (after[i] == before[i]) continue;
                    Assert.That(before[i], Is.EqualTo(TileType.Floor), $"시드 {seed} 칸 {i}: 바닥만 풀이 된다");
                    Assert.That(after[i], Is.EqualTo(TileType.Grass).Or.EqualTo(TileType.TallGrass));
                }
                Assert.That(decorated.World.Map.Get(decorated.Floor.Start), Is.Not.EqualTo(TileType.TallGrass), "시작 칸은 비움");
                Assert.That(decorated.World.Terrain.At(decorated.Floor.Start), Is.Empty, "시작 칸에 웅덩이 없음");
            }
        }

        [Test]
        public void Decor_PlacesPuddlesAndGrass_WithinSizeLimits()
        {
            var c = BalanceConfig();
            var (world, _) = Build(7UL);
            int water = 0;
            foreach (var pair in world.Terrain.ToSortedList())
            {
                foreach (var t in pair.Value)
                {
                    Assert.That(t.Type, Is.EqualTo(TerrainEffectType.Water));
                    Assert.That(t.Remaining, Is.EqualTo(TerrainLayer.Permanent));
                    water++;
                }
                Assert.That(world.Map.Get(world.Terrain.PosOf(pair.Key)), Is.EqualTo(TileType.Floor), "웅덩이 칸에는 풀이 없다");
            }
            int grass = Count(world.Map, TileType.Grass) + Count(world.Map, TileType.TallGrass);
            Assert.That(water, Is.InRange(1, c.PuddleCount * c.PuddleMaxSize));
            Assert.That(grass, Is.InRange(1, c.GrassPatchCount * c.GrassPatchMaxSize));
            Assert.That(Count(world.Map, TileType.TallGrass), Is.GreaterThan(0));
        }

        // 영구 물: ③에서 줄지 않고 위 유닛은 계속 젖는다
        [Test]
        public void PermanentPuddle_NeverExpires_AndWets()
        {
            var world = new World(GridMovementTests.Parse("#####", "#...#", "#####"), 3UL);
            var book = SpellTests.BalanceStatuses();
            world.InstallCoreSystems(Sight, Magic, book, SpellTests.BalanceTerrains());
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount);
            TerrainEffects.Place(world, new GridPos(1, 1), WaterDef(), TerrainLayer.Permanent);
            var clock = new TurnClock(world.Scheduler, world.PlayerInput);
            for (int i = 0; i < 50; i++) clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
            world.Events.Flush();
            Assert.That(world.Terrain.At(new GridPos(1, 1))[0].Remaining, Is.EqualTo(TerrainLayer.Permanent));
            Assert.That(player.HasStatus(StatusType.Wet), Is.True);
        }

        // 물 주문(12틱)을 웅덩이에 다시 깔아도 영구 그대로 (긴 쪽)
        [Test]
        public void ShorterWaterOnPuddle_StaysPermanent()
        {
            var world = new World(GridMovementTests.Parse("#####", "#...#", "#####"), 3UL);
            TerrainEffects.Place(world, new GridPos(2, 1), WaterDef(), TerrainLayer.Permanent);
            TerrainEffects.Place(world, new GridPos(2, 1), WaterDef());
            Assert.That(world.Terrain.At(new GridPos(2, 1))[0].Remaining, Is.EqualTo(TerrainLayer.Permanent));
        }

        // 높은 풀은 시야를 가리고(풀 칸 자체는 보임), 짧은 풀은 가리지 않는다. 주문 경로는 막지 않는다 (SPD)
        [Test]
        public void TallGrass_BlocksSight_NotProjectiles()
        {
            var map = GridMovementTests.Parse("#########", "#.......#", "#########");
            map.Set(new GridPos(3, 1), TileType.TallGrass);
            Assert.That(Sight.CanSee(map, new GridPos(1, 1), new GridPos(3, 1), 8), Is.True, "풀 칸 자체는 보인다");
            Assert.That(Sight.CanSee(map, new GridPos(1, 1), new GridPos(5, 1), 8), Is.False, "풀 너머는 안 보인다");
            var path = new BresenhamTargetingRule().Trace(map, new GridPos(1, 1), new GridPos(5, 1), 8, null);
            Assert.That(path, Does.Contain(new GridPos(5, 1)), "주문 경로는 풀을 지난다");

            map.Set(new GridPos(3, 1), TileType.Grass);
            Assert.That(Sight.CanSee(map, new GridPos(1, 1), new GridPos(5, 1), 8), Is.True, "짧은 풀은 안 가림");
            Assert.That(map.IsWalkable(new GridPos(3, 1)), Is.True);
        }

        // 들어서면 짧은 풀이 되고 같은 틱에 시야가 열린다
        [Test]
        public void SteppingIntoTallGrass_TramplesIt()
        {
            var map = GridMovementTests.Parse("#######", "#.....#", "#######");
            map.Set(new GridPos(2, 1), TileType.TallGrass);
            var world = new World(map, 3UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount);
            var changed = new List<TileChangedEvent>();
            world.Events.Subscribe<TileChangedEvent>(changed.Add);
            world.Events.Flush();

            Assert.That(PlayerActions.TryStep(world, StepConfig, new GridPos(1, 0), out var step), Is.True);
            new TurnClock(world.Scheduler, world.PlayerInput).SubmitPlayerCommand(step);
            world.Events.Flush();

            Assert.That(player.Pos, Is.EqualTo(new GridPos(2, 1)));
            Assert.That(world.Map.Get(new GridPos(2, 1)), Is.EqualTo(TileType.Grass));
            Assert.That(changed.Exists(e => e.Pos == new GridPos(2, 1) && e.Type == TileType.Grass), Is.True);
        }

        // 풀 칸 · 영구 웅덩이도 스냅샷으로 그대로 저장 · 복원된다
        [Test]
        public void Snapshot_RoundTripsGrassAndPuddles()
        {
            var (world, _) = Build(11UL);
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => null);
            Assert.That(restored.Map.CopyCells(), Is.EqualTo(world.Map.CopyCells()));
            var a = world.Terrain.ToSortedList();
            var b = restored.Terrain.ToSortedList();
            Assert.That(b.Count, Is.EqualTo(a.Count));
            for (int i = 0; i < a.Count; i++)
                Assert.That(b[i].Value[0].Remaining, Is.EqualTo(TerrainLayer.Permanent));
        }

        // 검산: 프로젝트의 FloorDecorConfig 에셋이 BALANCE.md 16번 표와 같은 값인지
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>(AssetPath);
            Assert.That(asset, Is.Not.Null, AssetPath + " 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var c = asset.ToConfig();
            var e = BalanceConfig();
            Assert.That(c.GrassPatchCount, Is.EqualTo(e.GrassPatchCount));
            Assert.That(c.GrassPatchMinSize, Is.EqualTo(e.GrassPatchMinSize));
            Assert.That(c.GrassPatchMaxSize, Is.EqualTo(e.GrassPatchMaxSize));
            Assert.That(c.TallGrassPercent, Is.EqualTo(e.TallGrassPercent));
            Assert.That(c.PuddleCount, Is.EqualTo(e.PuddleCount));
            Assert.That(c.PuddleMinSize, Is.EqualTo(e.PuddleMinSize));
            Assert.That(c.PuddleMaxSize, Is.EqualTo(e.PuddleMaxSize));
        }
    }
}
