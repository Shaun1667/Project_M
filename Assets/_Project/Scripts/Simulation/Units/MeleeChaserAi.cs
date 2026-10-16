using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 적 1종 AI (결정 2026-10-16): 플레이어가 보이면 다가가고, 닿으면 근접 공격.
    /// 놓치면 마지막으로 본 칸까지 가 본 뒤 멈춘다 (1-7). 기억은 Unit.LastSeenTargetPos(World 상태)에 두고, AI 자체는 상태가 없다 (규칙 6).
    /// 행위자마다 따로 만들 필요 없이 하나를 같이 써도 된다.
    /// </summary>
    public sealed class MeleeChaserAi : ICommandSource
    {
        private readonly World _world;
        private readonly GameConfig _config;
        private readonly ISightRule _sight;

        public MeleeChaserAi(World world, GameConfig config, ISightRule sight)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sight = sight ?? throw new ArgumentNullException(nameof(sight));
        }

        public ICommand NextCommand(in TickContext context)
        {
            int cost = _config.DefaultActionCostTicks;
            if (!_world.Units.TryGet(context.Actor, out var self) || !self.IsAlive) return new WaitCommand(cost);

            if (PlayerActions.TryGetPlayer(_world, out var player) && player.IsAlive
                && _sight.CanSee(_world.Map, self.Pos, player.Pos, self.SightRange))
            {
                self.LastSeenTargetPos = player.Pos;
                if (GridMovement.CanStep(_world.Map, self.Pos, player.Pos))
                    return new AttackCommand(_world, player.Id, cost);
                return StepToward(self, player.Pos, cost) ?? new WaitCommand(cost);
            }

            // 놓침: 마지막으로 본 칸으로. 도착했거나 갈 수 없으면 기억을 지운다.
            if (!self.LastSeenTargetPos.HasValue) return new WaitCommand(cost);
            var target = self.LastSeenTargetPos.Value;
            var step = self.Pos == target ? null : StepToward(self, target, cost);
            if (step == null)
            {
                self.LastSeenTargetPos = null;
                return new WaitCommand(cost);
            }
            return step;
        }

        /// <summary>goal 쪽으로 한 걸음. 길이 없거나 첫 칸이 막혀 있으면 null.</summary>
        private ICommand StepToward(Unit self, GridPos goal, int cost)
        {
            var path = GridPathfinder.FindPath(_world.Map, self.Pos, goal, _world.Units.IsOccupied);
            if (path == null || path.Count == 0 || _world.Units.IsOccupied(path[0])) return null;
            return new MoveCommand(_world, path[0], cost);
        }
    }
}
