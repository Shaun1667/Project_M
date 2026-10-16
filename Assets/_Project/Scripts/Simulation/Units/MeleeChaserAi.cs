using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 적 1종 AI (결정 2026-10-16): 플레이어가 보이면 다가가고, 닿으면 근접 공격. 안 보이면 제자리 대기.
    /// 상태를 갖지 않는다 (규칙 6) — 마지막으로 본 위치를 기억하지 않으므로 시야를 벗어나면 멈춘다.
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
            if (!PlayerActions.TryGetPlayer(_world, out var player) || !player.IsAlive) return new WaitCommand(cost);
            if (!_sight.CanSee(_world.Map, self.Pos, player.Pos, self.SightRange)) return new WaitCommand(cost);

            if (GridMovement.CanStep(_world.Map, self.Pos, player.Pos))
                return new AttackCommand(_world, player.Id, cost);

            var path = GridPathfinder.FindPath(_world.Map, self.Pos, player.Pos, _world.Units.IsOccupied);
            if (path == null || path.Count == 0 || _world.Units.IsOccupied(path[0])) return new WaitCommand(cost);
            return new MoveCommand(_world, path[0], cost);
        }
    }
}
