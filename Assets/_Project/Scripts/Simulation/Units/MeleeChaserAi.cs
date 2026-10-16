using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 적 1종 AI. 우선순위:
    /// ① 플레이어가 보임 — 닿으면 근접 공격. 붙어 있다가 플레이어가 물러나면 플레이어가 떠난 칸(발자국)을 그대로 밟아 따라감 (2026-10-16).
    ///    그 밖에는 GridPathfinder.ChaseStep으로 다가감.
    /// ② 놓침 — 마지막으로 본 칸까지 가 본 뒤 그 턴은 대기하고 잊음 (1-7).
    /// ③ 못 봤고 기억도 없음 — 맵을 배회: 아무 바닥 칸을 목적지로 골라(RngStream.Ai) 걸어가고, 도착하거나 막히면 새로 고름 (2026-10-16).
    /// 기억 · 배회 목적지는 Unit(World 상태)에 두고, AI 자체는 상태가 없다 (규칙 6). 하나를 모든 적이 같이 써도 된다.
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

            // ① 보임
            if (PlayerActions.TryGetPlayer(_world, out var player) && player.IsAlive
                && _sight.CanSee(_world.Map, self.Pos, player.Pos, self.SightRange))
            {
                self.LastSeenTargetPos = player.Pos;
                self.WanderTarget = null;
                if (GridMovement.CanStep(_world.Map, self.Pos, player.Pos))
                    return new AttackCommand(_world, player.Id, cost);
                return FollowTrail(self, player, cost) ?? StepToward(self, player.Pos, cost) ?? new WaitCommand(cost);
            }

            // ② 기억한 칸으로
            if (self.LastSeenTargetPos.HasValue)
            {
                var target = self.LastSeenTargetPos.Value;
                var step = self.Pos == target ? null : StepToward(self, target, cost);
                if (step != null) return step;
                self.LastSeenTargetPos = null; // 도착했거나 갈 수 없음 — 이번 턴은 둘러보고 다음 턴부터 배회
                return new WaitCommand(cost);
            }

            // ③ 배회
            return Wander(self, cost);
        }

        /// <summary>
        /// 발자국 따라가기: 플레이어가 방금 떠난 칸이 플레이어 바로 옆이고, 내가 그 칸으로 한 걸음에 갈 수 있으면(= 붙어 있었음) 그 칸으로.
        /// </summary>
        private ICommand FollowTrail(Unit self, Unit player, int cost)
        {
            if (!player.PreviousPos.HasValue) return null;
            var trail = player.PreviousPos.Value;
            if (trail == self.Pos || GridMovement.Chebyshev(trail, player.Pos) != 1) return null;
            if (!GridMovement.CanStep(_world.Map, self.Pos, trail) || _world.Units.IsOccupied(trail)) return null;
            return new MoveCommand(_world, trail, cost);
        }

        private ICommand Wander(Unit self, int cost)
        {
            if (!self.WanderTarget.HasValue || self.WanderTarget.Value == self.Pos)
            {
                self.WanderTarget = PickWanderTarget(self.Pos);
                if (!self.WanderTarget.HasValue) return new WaitCommand(cost);
            }

            var step = StepToward(self, self.WanderTarget.Value, cost);
            if (step != null) return step;
            self.WanderTarget = null; // 막힘 — 다음 턴에 새 목적지
            return new WaitCommand(cost);
        }

        /// <summary>맵의 바닥 · 계단 칸 중 하나 (지금 칸 제외), 후보는 y → x 순, 난수는 RngStream.Ai.</summary>
        private GridPos? PickWanderTarget(GridPos current)
        {
            var map = _world.Map;
            var candidates = new List<GridPos>();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (p != current && map.IsWalkable(p)) candidates.Add(p);
                }
            }
            if (candidates.Count == 0) return null;
            return candidates[_world.Rng.Get(RngStream.Ai).Range(0, candidates.Count)];
        }

        /// <summary>goal 쪽으로 한 걸음 (GridPathfinder.ChaseStep). 길이 없거나 첫 칸이 막혀 있으면 null.</summary>
        private ICommand StepToward(Unit self, GridPos goal, int cost)
        {
            var step = GridPathfinder.ChaseStep(_world.Map, self.Pos, goal, _world.Units.IsOccupied);
            if (!step.HasValue || _world.Units.IsOccupied(step.Value)) return null;
            return new MoveCommand(_world, step.Value, cost);
        }
    }
}
