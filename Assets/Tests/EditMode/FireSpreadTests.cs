using System;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-11b 번지는 불: 풀 × 불 = 점화(칸 반응) · 화염 지대에서 이웃 풀로 번짐 (BALANCE.md 7번 · 8번).</summary>
    public class FireSpreadTests
    {
        private const int Fire = 0;
        private static readonly GameConfig WaitConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);

        /// <summary>BALANCE.md 7번 "불 번짐" 값.</summary>
        private static FireSpreadConfig BalanceSpread() => new FireSpreadConfig(burnTicks: 3, maxIgnitionsPerTick: 8);

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;

            public void Wait(int turns)
            {
                for (int i = 0; i < turns; i++) Clock.SubmitPlayerCommand(PlayerActions.Wait(WaitConfig));
                World.Events.Flush();
            }

            public bool Burning(int x, int y) => World.Terrain.Has(new GridPos(x, y), TerrainEffectType.FireZone);
            public TileType Tile(int x, int y) => World.Map.Get(new GridPos(x, y));
        }

        /// <summary>한 줄 방 (1,1)~(10,1). grassFrom ~ grassTo 칸은 짧은 풀. 플레이어는 (1,1).</summary>
        private static Session Start(int grassFrom, int grassTo, FireSpreadConfig spread = null)
        {
            var map = GridMovementTests.Parse("############", "#..........#", "############");
            for (int x = grassFrom; x <= grassTo; x++) map.Set(new GridPos(x, 1), TileType.Grass);
            var world = new World(map, 5UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains(), fireSpread: spread ?? BalanceSpread());
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 1), Magic.QuickSlotCount);
            world.Events.Flush();
            return s;
        }

        // 불이 붙은 칸에서 매 틱 한 칸씩 이웃 풀로 번지고, 탄 풀은 바닥이 된다
        [Test]
        public void Fire_SpreadsOneCellPerTick_AndBurnsGrass()
        {
            var s = Start(4, 8);
            TerrainEffects.Place(s.World, new GridPos(4, 1), TerrainEffectType.FireZone, 3);
            s.Wait(1);
            Assert.That(s.Tile(4, 1), Is.EqualTo(TileType.Floor), "불 아래 풀은 탄다");
            Assert.That(s.Burning(5, 1), Is.True, "이웃 풀에 붙음");
            Assert.That(s.Tile(5, 1), Is.EqualTo(TileType.Floor));
            Assert.That(s.Burning(6, 1), Is.False, "이번 틱에 붙은 불은 다음 틱부터 번진다");
            s.Wait(1);
            Assert.That(s.Burning(6, 1), Is.True);
            s.Wait(10);
            for (int x = 4; x <= 8; x++) Assert.That(s.Tile(x, 1), Is.EqualTo(TileType.Floor), $"({x},1) 다 탐");
            for (int x = 1; x <= 10; x++) Assert.That(s.Burning(x, 1), Is.False, "풀이 다 타면 꺼진다");
        }

        // 물이 있는 풀 칸은 타지 않는다 — 불이 거기서 멈춤
        [Test]
        public void DampGrass_DoesNotBurn()
        {
            var s = Start(4, 8);
            SpellTests.BalanceTerrains().TryGet(TerrainEffectType.Water, out var water);
            TerrainEffects.Place(s.World, new GridPos(6, 1), water, TerrainLayer.Permanent);
            TerrainEffects.Place(s.World, new GridPos(4, 1), TerrainEffectType.FireZone, 3);
            s.Wait(5);
            Assert.That(s.Tile(5, 1), Is.EqualTo(TileType.Floor));
            Assert.That(s.Tile(6, 1), Is.EqualTo(TileType.Grass), "젖은 칸");
            Assert.That(s.Tile(7, 1), Is.EqualTo(TileType.Grass), "그 너머로 못 감");
        }

        // 한 틱에 붙는 칸 수 상한 (넘는 칸은 다음 틱)
        [Test]
        public void Ignitions_CappedPerTick()
        {
            var s = Start(2, 10, new FireSpreadConfig(burnTicks: 3, maxIgnitionsPerTick: 1));
            TerrainEffects.Place(s.World, new GridPos(2, 1), TerrainEffectType.FireZone, 3);
            TerrainEffects.Place(s.World, new GridPos(6, 1), TerrainEffectType.FireZone, 3);
            s.Wait(1);
            int burning = 0;
            for (int x = 1; x <= 10; x++) if (s.Burning(x, 1)) burning++;
            Assert.That(burning, Is.EqualTo(3), "처음 2곳 + 새로 1칸 (상한 1)");
            Assert.That(s.Burning(3, 1), Is.True, "칸 인덱스 순으로 먼저");
        }

        // 번지는 불 위에 있으면 플레이어도 화상 (규칙 10 — 플레이어도 지형 영향)
        [Test]
        public void SpreadingFire_BurnsPlayer()
        {
            var s = Start(1, 4);
            TerrainEffects.Place(s.World, new GridPos(3, 1), TerrainEffectType.FireZone, 3);
            s.Wait(3);
            Assert.That(s.Player.HasStatus(StatusType.Burn), Is.True);
            Assert.That(s.Player.Hp, Is.LessThan(PlayerDef.MaxHp));
        }

        // 불 주문이 풀에 맞으면 점화 반응 — 바닥 + 화염 지대, 반응 상한(4회)에 세지 않음
        [Test]
        public void FireSpellOnGrass_Ignites()
        {
            var s = ReactionTests.Start(ReactionTests.Book(), new GridPos(1, 2), new GridPos(6, 2));
            var target = new GridPos(4, 2);
            s.World.Map.Set(target, TileType.TallGrass);
            s.World.Map.Set(new GridPos(3, 2), TileType.Grass);
            TestInventory.Equip(s.World, s.Book, s.Player, Fire, "fire_basic");
            s.World.Events.Flush();
            s.Cast(Fire, target); // 투사체는 (3,2)를 지나 (4,2)에 — 이 세션은 번짐 없음
            s.World.Events.Flush();
            Assert.That(s.Reactions, Does.Contain("tallgrass_ignite"));
            Assert.That(s.World.Map.Get(new GridPos(3, 2)), Is.EqualTo(TileType.Grass), "지나간 칸은 점화하지 않는다 — 맞은 칸만");
            Assert.That(s.World.Map.Get(target), Is.EqualTo(TileType.Floor));
            Assert.That(s.World.Terrain.Has(target, TerrainEffectType.FireZone), Is.True);
        }

        [Test]
        public void TileReaction_Validation()
        {
            Assert.Throws<ArgumentException>(() => new ReactionDef("x", ReactionTrigger.Tile, StatusType.None, TerrainEffectType.None, Element.Fire, 0),
                "칸 종류 없음");
            Assert.Throws<ArgumentException>(() => new ReactionDef("x", ReactionTrigger.Terrain, StatusType.None, TerrainEffectType.Water, Element.Fire, 0,
                transformTileTo: TileType.Floor), "칸 바꾸기는 칸 반응에서만");
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReactionDef("x", ReactionTrigger.Tile, StatusType.None, TerrainEffectType.None, Element.Fire, 0,
                tile: TileType.Grass, placeTerrain: TerrainEffectType.FireZone), "놓는 지형은 지속 1 이상");
        }

        // 검산: TerrainCatalog의 불 번짐 값 = BALANCE.md 7번, 점화 반응 지속과 같음
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset");
            Assert.That(asset, Is.Not.Null);
            var c = asset.ToFireSpread();
            var e = BalanceSpread();
            Assert.That((c.BurnTicks, c.MaxIgnitionsPerTick), Is.EqualTo((e.BurnTicks, e.MaxIgnitionsPerTick)));
            var reactions = ReactionTests.BalanceReactions(SpellTests.BalanceStatuses());
            Assert.That(reactions.FindTile(TileType.Grass, Element.Fire).TransformDurationTicks, Is.EqualTo(e.BurnTicks), "점화 = 번짐 지속");
        }
    }
}
