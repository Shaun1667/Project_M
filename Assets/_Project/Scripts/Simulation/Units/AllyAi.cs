using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 아군 AI (3-13b 분신, SPD MirrorImage): ① 보이는 적 중 가장 가까운 것 — 닿으면 공격, 아니면 다가감 ② 없으면 플레이어 곁(2칸 안)으로.
    /// 상태가 없다 (규칙 6). 모든 아군이 같이 써도 된다.
    /// </summary>
    public sealed class AllyAi : ICommandSource
    {
        private readonly World _world;
        private readonly GameConfig _config;
        private readonly ISightRule _sight;

        public AllyAi(World world, GameConfig config, ISightRule sight)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sight = sight ?? throw new ArgumentNullException(nameof(sight));
        }

        public ICommand NextCommand(in TickContext context)
        {
            int cost = _config.DefaultActionCostTicks;
            if (!_world.Units.TryGet(context.Actor, out var self) || !self.IsAlive) return new WaitCommand(cost);

            Unit target = null;
            foreach (var u in _world.Units.ToSortedList())
            {
                if (!u.IsAlive || !Factions.AreHostile(u.Faction, self.Faction) || !_sight.CanSee(_world.Map, self.Pos, u.Pos, self.SightRange)) continue;
                if (target == null || GridMovement.Chebyshev(self.Pos, u.Pos) < GridMovement.Chebyshev(self.Pos, target.Pos)) target = u;
            }
            if (target != null)
            {
                if (GridMovement.CanStep(_world.Map, self.Pos, target.Pos)) return new AttackCommand(_world, target.Id, cost);
                return StepToward(self, target.Pos, cost) ?? new WaitCommand(cost);
            }
            if (PlayerActions.TryGetPlayer(_world, out var player) && GridMovement.Chebyshev(self.Pos, player.Pos) > 2)
                return StepToward(self, player.Pos, cost) ?? new WaitCommand(cost);
            return new WaitCommand(cost);
        }

        private ICommand StepToward(Unit self, GridPos goal, int cost)
        {
            var step = GridPathfinder.ChaseStep(_world.Map, self.Pos, goal, _world.Units.IsOccupied);
            if (!step.HasValue || _world.Units.IsOccupied(step.Value)) return null;
            return new MoveCommand(_world, step.Value, cost);
        }
    }
}
