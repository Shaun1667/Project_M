using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Magic
{
    /// <summary>
    /// 지형 흡수 (3-58, 결정 2026-11-25 — 내 칸 + 둘레 8칸): 칸의 지형 효과를 없애고 그 마나를 얻는다. 칸마다 얼마인지는 TerrainDef.AbsorbMana
    /// (TerrainCatalog.asset — BALANCE.md 7번, 0 = 흡수 안 됨). 영구 지형(웅덩이 · TerrainLayer.Permanent)은 흡수하지 않는다.
    /// 칸 순서: y → x. 마나가 가득해도 지형은 없앤다 (위험 지형에서 빠져나오기). 상태를 갖지 않는다.
    /// </summary>
    public static class Absorption
    {
        /// <summary>둘레 반지름 (1 = 3×3).</summary>
        public const int Radius = 1;

        /// <summary>center 둘레에서 흡수할 수 있는 지형과 마나 합. 없으면 0.</summary>
        public static int Preview(World world, TerrainBook terrains, GridPos center, List<(GridPos pos, TerrainEffectType type)> found = null)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (terrains == null) return 0;
            int total = 0;
            for (int y = center.Y - Radius; y <= center.Y + Radius; y++)
                for (int x = center.X - Radius; x <= center.X + Radius; x++)
                {
                    var p = new GridPos(x, y);
                    if (!world.Map.InBounds(p)) continue;
                    foreach (var t in world.Terrain.At(p))
                    {
                        if (t.Remaining == TerrainLayer.Permanent || !terrains.TryGet(t.Type, out var def) || def.AbsorbMana <= 0) continue;
                        total += def.AbsorbMana;
                        found?.Add((p, t.Type));
                    }
                }
            return total;
        }

        /// <summary>흡수한다: 지형을 없애고 마나 회복(최대치까지) · TerrainAbsorbedEvent. 돌려주는 값 = 흡수한 마나 합 (회복량이 아님).</summary>
        public static int Absorb(World world, TerrainBook terrains, Unit unit)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (unit == null || !unit.IsAlive) return 0;
            var found = new List<(GridPos pos, TerrainEffectType type)>();
            int total = Preview(world, terrains, unit.Pos, found);
            if (total == 0) return 0;
            var cells = new List<GridPos>();
            foreach (var (pos, type) in found)
            {
                TerrainEffects.Remove(world, pos, type);
                if (!cells.Contains(pos)) cells.Add(pos);
            }
            int gained = Mana.Restore(world, unit, total);
            world.Events.Publish(new TerrainAbsorbedEvent(unit.Id, cells, total, gained));
            return total;
        }
    }

    /// <summary>흡수 행동 (3-58, R). 비용 = 기본 행동 1회.</summary>
    public sealed class AbsorbCommand : ICommand
    {
        private readonly World _world;
        private readonly TerrainBook _terrains;
        public int CostTicks { get; }

        public AbsorbCommand(World world, TerrainBook terrains, int costTicks)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _terrains = terrains;
            CostTicks = Math.Max(1, costTicks);
        }

        public void Execute(in TickContext context)
        {
            if (_world.Units.TryGet(context.Actor, out var actor)) Absorption.Absorb(_world, _terrains, actor);
        }
    }

    /// <summary>지형을 흡수했다 (3-58). Amount = 흡수한 마나 합, Gained = 실제로 늘어난 마나 (최대치에 걸리면 작음).</summary>
    public sealed class TerrainAbsorbedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public IReadOnlyList<GridPos> Cells { get; }
        public int Amount { get; }
        public int Gained { get; }

        public TerrainAbsorbedEvent(EntityId unit, IReadOnlyList<GridPos> cells, int amount, int gained)
        {
            Unit = unit;
            Cells = cells;
            Amount = amount;
            Gained = gained;
        }
    }
}
