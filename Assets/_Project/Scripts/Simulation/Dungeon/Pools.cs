using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>피라냐 연못 수치 (3-50, SPD Pool). 값은 FloorDecorConfigAsset에서 — 기준은 BALANCE.md 16번.</summary>
    public sealed class PoolConfig
    {
        /// <summary>피라냐 (적 표 ID = 모습 ID, 적 줄 WaterOnly).</summary>
        public string PiranhaId { get; }
        public int PiranhaCount { get; }
        /// <summary>피라냐 세기 % = 100 + 층마다 × (층 − 1) — UnitDef.Scaled.</summary>
        public int PercentPerFloor { get; }

        public PoolConfig(string piranhaId, int piranhaCount, int percentPerFloor)
        {
            if (string.IsNullOrEmpty(piranhaId)) throw new ArgumentException("피라냐 ID가 비어 있다", nameof(piranhaId));
            if (piranhaCount < 0) throw new ArgumentOutOfRangeException(nameof(piranhaCount));
            if (percentPerFloor < 0) throw new ArgumentOutOfRangeException(nameof(percentPerFloor));
            PiranhaId = piranhaId;
            PiranhaCount = piranhaCount;
            PercentPerFloor = percentPerFloor;
        }

        public int PercentFor(int floor) => 100 + PercentPerFloor * Math.Max(0, floor - 1);
    }

    /// <summary>피라냐 연못 규칙 연결 (3-50): 수치 · 피라냐 기본 수치 · 적 AI. World.SetPoolRules — 규칙 연결이라 저장하지 않는다.</summary>
    public sealed class PoolRules
    {
        public PoolConfig Config { get; }
        public UnitDef Piranha { get; }
        public ICommandSource Ai { get; }

        public PoolRules(PoolConfig config, UnitDef piranha, ICommandSource ai)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Piranha = piranha ?? throw new ArgumentNullException(nameof(piranha));
            Ai = ai ?? throw new ArgumentNullException(nameof(ai));
        }
    }

    /// <summary>
    /// 피라냐 연못 (3-50, 결정 2026-11-18 — SPD Pool): 가운데 칸(받침대)만 빼고 방 전체에 영구 물 지형, 물 칸 중 PiranhaCount곳에 피라냐(층 세기).
    /// 피라냐는 물 칸으로만 움직이고(EnemyTraits.WaterOnly — MeleeChaserAi), 자기 칸에 물이 없어지면(얼음 · 증발 · 밀려남) 그 차례에 죽는다(StrandedCommand).
    /// </summary>
    public static class Pools
    {
        /// <summary>물을 채우고 피라냐를 놓는다. 돌려주는 칸 = 가운데 받침대 (보상 자리). 피라냐 칸 난수 = rng (꾸미기 흐름).</summary>
        public static List<GridPos> Fill(World world, IRng rng, List<GridPos> cells)
        {
            var center = cells[cells.Count / 2];
            var water = new List<GridPos>();
            foreach (var c in cells)
            {
                if (c == center) continue;
                TerrainEffects.Place(world, c, TerrainEffectType.Water, TerrainLayer.Permanent);
                water.Add(c);
            }
            var rules = world.PoolRules;
            if (rules != null)
            {
                var def = rules.Piranha.Scaled(rules.Config.PercentFor(world.Floor));
                for (int i = 0; i < rules.Config.PiranhaCount && water.Count > 0; i++)
                {
                    int k = rng.Range(0, water.Count);
                    if (!world.Units.TryGetAt(water[k], out _)) UnitSpawner.SpawnEnemy(world, def, water[k], rules.Ai);
                    water.RemoveAt(k);
                }
            }
            return new List<GridPos> { center };
        }
    }

    /// <summary>물 밖의 피라냐: 숨이 막혀 죽는다 (3-50, 차례 1). 남은 체력만큼 피해 — 사망 판정은 ⑥.</summary>
    public sealed class StrandedCommand : ICommand
    {
        private readonly World _world;
        public int CostTicks { get; }

        public StrandedCommand(World world, int costTicks)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            CostTicks = Math.Max(1, costTicks);
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var self) || !self.IsAlive) return;
            _world.Events.Publish(new StrandedEvent(self.Id));
            Combat.ApplyDamage(_world, self.Id, self, self.Hp, DamageCause.FromTerrain(TerrainEffectType.None));
        }
    }

    /// <summary>물 밖의 피라냐가 숨이 막혔다 (로그).</summary>
    public sealed class StrandedEvent : ISimEvent
    {
        public EntityId Unit { get; }

        public StrandedEvent(EntityId unit)
        {
            Unit = unit;
        }
    }
}
