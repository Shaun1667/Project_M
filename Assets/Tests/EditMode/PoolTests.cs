using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-50 피라냐 연못 (SPD Pool — 결정 2026-11-18): 받침대만 빼고 영구 물 · 물에서만 움직이는 피라냐 3마리(층 세기) · 물이 없어지면 죽음 · 보물 방 목록 1개 (BALANCE.md 16번).</summary>
    public class PoolTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly PoolConfig PoolCfg = new PoolConfig("enemy_piranha", 3, 8);
        private static readonly UnitDef PiranhaDef = new UnitDef("enemy_piranha", 6, 3, 6);
        private static readonly EnemyBook Book = new EnemyBook(new[] { new EnemyTraits("enemy_piranha", moveSteps: 2, waterOnly: true) });

        private sealed class Run
        {
            public World World;
            public Unit Player;
            public Unit Piranha;
            public MeleeChaserAi Ai;
        }

        // 물 = (1..3, 1..2), 나머지 맨땅
        private static Run Start(GridPos piranhaAt, GridPos playerAt)
        {
            var world = new World(GridMovementTests.Parse("#########", "#.......#", "#.......#", "#########"), 5UL);
            world.InstallCoreSystems(Sight, new ProjectM.Magic.MagicConfig(3, 1, 4), SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            for (int y = 1; y <= 2; y++)
                for (int x = 1; x <= 3; x++)
                    TerrainEffects.Place(world, new GridPos(x, y), TerrainEffectType.Water, TerrainLayer.Permanent);
            var ai = new MeleeChaserAi(world, Config, Sight, null, Book);
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 40, 3, 8), playerAt);
            var piranha = UnitSpawner.SpawnEnemy(world, PiranhaDef, piranhaAt, ai);
            world.Events.Flush();
            return new Run { World = world, Player = player, Piranha = piranha, Ai = ai };
        }

        private static void Act(Run r)
        {
            r.Ai.NextCommand(new TickContext(0, r.Piranha.Id)).Execute(new TickContext(0, r.Piranha.Id));
            r.World.Events.Flush();
        }

        // 보이는 플레이어가 맨땅에 있어도 물 밖으로는 나가지 않는다 (물 끝까지만 다가감)
        [Test]
        public void StaysInWater_WhileChasing()
        {
            var r = Start(new GridPos(1, 1), new GridPos(6, 1));
            for (int i = 0; i < 5; i++)
            {
                Act(r);
                Assert.That(r.World.Terrain.Has(r.Piranha.Pos, TerrainEffectType.Water), Is.True, $"{i}번째 차례 {r.Piranha.Pos}");
            }
            Assert.That(r.Piranha.Pos.X, Is.EqualTo(3), "물 끝까지는 다가감");
            Assert.That(r.Piranha.IsAlive, Is.True);
        }

        // 물 가장자리에서 바로 옆(맨땅)의 플레이어는 문다
        [Test]
        public void BitesAdjacentPlayer_FromWater()
        {
            var r = Start(new GridPos(3, 1), new GridPos(4, 1));
            Assert.That(r.Ai.NextCommand(new TickContext(0, r.Piranha.Id)), Is.InstanceOf<AttackCommand>());
        }

        // 자기 칸의 물이 없어지면 그 차례에 숨이 막혀 죽는다 (로그 이벤트)
        [Test]
        public void DiesWhenWaterGone()
        {
            var r = Start(new GridPos(2, 1), new GridPos(6, 2));
            var stranded = new List<StrandedEvent>();
            r.World.Events.Subscribe<StrandedEvent>(stranded.Add);
            TerrainEffects.Remove(r.World, new GridPos(2, 1), TerrainEffectType.Water);
            Act(r);
            Assert.That(stranded.Count, Is.EqualTo(1));
            Assert.That(r.Piranha.Hp, Is.EqualTo(0));
            Assert.That(r.Piranha.Pos, Is.EqualTo(new GridPos(2, 1)), "움직이지 않고");
        }

        // 물에서만 사는 종류가 아니면 예전처럼 맨땅으로 다닌다
        [Test]
        public void OrdinaryEnemy_LeavesWater()
        {
            var world = new World(GridMovementTests.Parse("#########", "#.......#", "#########"), 5UL);
            world.InstallCoreSystems(Sight, new ProjectM.Magic.MagicConfig(3, 1, 4), SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            TerrainEffects.Place(world, new GridPos(1, 1), TerrainEffectType.Water, TerrainLayer.Permanent);
            var ai = new MeleeChaserAi(world, Config, Sight, null, Book);
            UnitSpawner.SpawnPlayer(world, new UnitDef("player", 40, 3, 8), new GridPos(6, 1));
            var rat = UnitSpawner.SpawnEnemy(world, new UnitDef("rat", 6, 2, 6), new GridPos(1, 1), ai);
            ai.NextCommand(new TickContext(0, rat.Id)).Execute(new TickContext(0, rat.Id));
            Assert.That(rat.Pos, Is.EqualTo(new GridPos(2, 1)));
        }

        // 세기: 100 + 8 × (층 − 1)
        [Test]
        public void Config_Percent()
        {
            Assert.That(PoolCfg.PercentFor(1), Is.EqualTo(100));
            Assert.That(PoolCfg.PercentFor(11), Is.EqualTo(180));
        }

        // 피라냐 연못: 입구 = 문, 가운데 받침대만 물 없음 · 보물 방 목록 1개, 나머지 칸 모두 물, 물 칸에 피라냐 3마리
        [Test]
        public void Room_WaterPiranhasAndPedestal()
        {
            int carved = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var (world, _, room) = SpecialRoomTests.CarveFor(seed, SpecialRoomType.Pool, w =>
                    w.SetPoolRules(new PoolRules(PoolCfg, PiranhaDef, new MeleeChaserAi(w, Config, Sight, null, Book))));
                if (room == null) continue;
                carved++;
                var center = room.Cells[room.Cells.Count / 2];
                Assert.That(world.Map.Get(room.Entrance), Is.EqualTo(TileType.Door), $"시드 {seed}");
                Assert.That(world.Terrain.Has(center, TerrainEffectType.Water), Is.False, $"시드 {seed}: 받침대");
                Assert.That(world.FloorItems.ItemsAt(center).Count, Is.EqualTo(1), $"시드 {seed}");
                int piranhas = 0;
                foreach (var c in room.Cells)
                {
                    if (c != center) Assert.That(world.Terrain.Has(c, TerrainEffectType.Water), Is.True, $"시드 {seed} {c}");
                    if (world.Units.TryGetAt(c, out var u) && u.VisualId == "enemy_piranha")
                    {
                        piranhas++;
                        Assert.That(c, Is.Not.EqualTo(center));
                    }
                }
                Assert.That(piranhas, Is.EqualTo(3), $"시드 {seed}");
            }
            Assert.That(carved, Is.GreaterThan(0));
        }

        // 검산: FloorDecorConfig 피라냐 연못 값 = BALANCE.md 16번, 피라냐 적 줄
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var c = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToPool();
            Assert.That((c.PiranhaId, c.PiranhaCount, c.PercentPerFloor), Is.EqualTo(("enemy_piranha", 3, 8)));
            var zones = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            Assert.That(zones.TryGetEnemy("enemy_piranha", out var p), Is.True);
            Assert.That((p.MaxHp, p.AttackDamage, p.SightRange, p.Accuracy, p.Evasion, p.ExpReward, p.MoveSteps, p.WaterOnly), Is.EqualTo((6, 3, 6, 12, 8, 3, 2, true)));
            Assert.That(zones.ToEnemyBook().TryGet("enemy_piranha", out var traits) && traits.WaterOnly && traits.MoveSteps == 2, Is.True);
        }
    }
}
