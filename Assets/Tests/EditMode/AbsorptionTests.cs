using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>
    /// 3-58 지형 흡수 (결정 2026-11-25 — 내 칸 + 둘레 8칸, R · 1턴): 지형을 없애고 TerrainDef.AbsorbMana만큼 마나.
    /// 영구 지형 · 흡수 마나 0(예고 칸)은 그대로. 흡수할 것이 없으면 커맨드 없음 (BALANCE.md 7번).
    /// </summary>
    public class AbsorptionTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);

        private static TerrainBook Terrains() => new TerrainBook(new[]
        {
            new TerrainDef(TerrainEffectType.FireZone, 4, absorbMana: 2),
            new TerrainDef(TerrainEffectType.Water, 12, absorbMana: 1),
            new TerrainDef(TerrainEffectType.Warning, 2),
        }, SpellTests.BalanceStatuses());

        private static (World world, Unit player) Start(int mana)
        {
            var world = new World(GridMovementTests.Parse("#######", "#.....#", "#.....#", "#.....#", "#######"), 3UL);
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 20, 2, 6, maxMana: 10), new GridPos(2, 2));
            Mana.TrySpend(world, player, player.Mana - mana);
            world.Events.Flush();
            return (world, player);
        }

        // 둘레 3×3: 화염 지대 2칸(2씩) + 물 1칸(1) = 마나 5. 둘레 밖 · 영구 물 · 예고 칸은 그대로
        [Test]
        public void Absorb_RemovesNearbyTerrain_AndRestoresMana()
        {
            var terrains = Terrains();
            var (world, player) = Start(2);
            TerrainEffects.Place(world, new GridPos(2, 2), TerrainEffectType.FireZone, 4); // 내 칸
            TerrainEffects.Place(world, new GridPos(3, 3), TerrainEffectType.FireZone, 4);
            TerrainEffects.Place(world, new GridPos(1, 1), TerrainEffectType.Water, 12);
            TerrainEffects.Place(world, new GridPos(1, 3), TerrainEffectType.Water, TerrainLayer.Permanent); // 웅덩이
            TerrainEffects.Place(world, new GridPos(3, 1), TerrainEffectType.Warning, 2);
            TerrainEffects.Place(world, new GridPos(5, 2), TerrainEffectType.FireZone, 4); // 둘레 밖
            var events = new List<TerrainAbsorbedEvent>();
            world.Events.Subscribe<TerrainAbsorbedEvent>(events.Add);

            Assert.That(Absorption.Preview(world, terrains, player.Pos), Is.EqualTo(5));
            var command = PlayerActions.Absorb(world, Config, terrains);
            Assert.That(command, Is.Not.Null);
            command.Execute(new TickContext(0, player.Id));
            world.Events.Flush();

            Assert.That(player.Mana, Is.EqualTo(7));
            Assert.That(world.Terrain.Has(new GridPos(2, 2), TerrainEffectType.FireZone), Is.False);
            Assert.That(world.Terrain.Has(new GridPos(3, 3), TerrainEffectType.FireZone), Is.False);
            Assert.That(world.Terrain.Has(new GridPos(1, 1), TerrainEffectType.Water), Is.False);
            Assert.That(world.Terrain.Has(new GridPos(1, 3), TerrainEffectType.Water), Is.True, "영구 웅덩이");
            Assert.That(world.Terrain.Has(new GridPos(3, 1), TerrainEffectType.Warning), Is.True, "예고 칸");
            Assert.That(world.Terrain.Has(new GridPos(5, 2), TerrainEffectType.FireZone), Is.True, "둘레 밖");
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That((events[0].Amount, events[0].Gained, events[0].Cells.Count), Is.EqualTo((5, 5, 3)));
        }

        // 마나가 가득해도 지형은 없앤다 (위험 지형에서 빠져나오기) — 늘어난 양은 최대치까지
        [Test]
        public void Absorb_AtFullMana_StillClearsTerrain()
        {
            var terrains = Terrains();
            var (world, player) = Start(9);
            TerrainEffects.Place(world, new GridPos(2, 2), TerrainEffectType.FireZone, 4);
            TerrainEffects.Place(world, new GridPos(2, 1), TerrainEffectType.FireZone, 4);
            var events = new List<TerrainAbsorbedEvent>();
            world.Events.Subscribe<TerrainAbsorbedEvent>(events.Add);
            PlayerActions.Absorb(world, Config, terrains).Execute(new TickContext(0, player.Id));
            world.Events.Flush();
            Assert.That(player.Mana, Is.EqualTo(10));
            Assert.That(world.Terrain.CellCount, Is.EqualTo(0));
            Assert.That((events[0].Amount, events[0].Gained), Is.EqualTo((4, 1)));
        }

        // 흡수할 것이 없으면(예고 칸 · 영구 물뿐) 커맨드 없음 = 차례를 쓰지 않음
        [Test]
        public void NothingToAbsorb_ReturnsNoCommand()
        {
            var terrains = Terrains();
            var (world, _) = Start(0);
            TerrainEffects.Place(world, new GridPos(2, 2), TerrainEffectType.Warning, 2);
            TerrainEffects.Place(world, new GridPos(3, 2), TerrainEffectType.Water, TerrainLayer.Permanent);
            Assert.That(PlayerActions.Absorb(world, Config, terrains), Is.Null);
            Assert.That(PlayerActions.Absorb(world, Config, null), Is.Null);
        }

        // 검산: TerrainCatalog 흡수 마나 = BALANCE.md 7번 (화염 지대 · 독구름 · 마비 가스 2, 물 · 오염된 물 · 빙판 1, 예고 칸 0)
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var expected = new Dictionary<TerrainEffectType, int>
            {
                { TerrainEffectType.FireZone, 2 }, { TerrainEffectType.PoisonCloud, 2 }, { TerrainEffectType.ParalyticGas, 2 },
                { TerrainEffectType.Water, 1 }, { TerrainEffectType.ContaminatedWater, 1 }, { TerrainEffectType.IcePatch, 1 },
                { TerrainEffectType.Warning, 0 },
            };
            foreach (var pair in expected)
            {
                Assert.That(terrains.TryGet(pair.Key, out var def), Is.True, pair.Key.ToString());
                Assert.That(def.AbsorbMana, Is.EqualTo(pair.Value), pair.Key.ToString());
            }
        }
    }
}
