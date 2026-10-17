using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 인접한 칸으로 한 걸음. 실행 시점에 지형 · 유닛 때문에 못 가면 아무 일 없이 차례만 쓴다
    /// (입력 쪽은 PlayerActions로 미리 검사해 이런 경우 커맨드를 만들지 않는다).
    /// </summary>
    public sealed class MoveCommand : ICommand
    {
        private readonly World _world;

        public GridPos To { get; }
        public int CostTicks { get; }

        public MoveCommand(World world, GridPos to, int costTicks)
        {
            if (costTicks < 1) throw new ArgumentOutOfRangeException(nameof(costTicks), costTicks, "행동 비용은 1틱 이상이어야 한다.");
            _world = world ?? throw new ArgumentNullException(nameof(world));
            To = to;
            CostTicks = costTicks;
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var unit) || !unit.IsAlive) return;
            if (!GridMovement.CanStep(_world.Map, unit.Pos, To) || _world.Units.IsOccupied(To)) return;

            var from = unit.Pos;
            _world.Units.Move(unit.Id, To);
            _world.Events.Publish(new EntityMovedEvent(unit.Id, from, To));

            // 3-4a: 플레이어가 계단 칸에 들어가면 층 이동 요청 — 같은 틱 ⑥ 끝에 FloorTransitionSystem이 처리
            if (unit.Faction == Faction.Player)
            {
                var tile = _world.Map.Get(To);
                if (tile == TileType.StairsDown) _world.RequestFloorChange(1);
                else if (tile == TileType.StairsUp) _world.RequestFloorChange(-1);
            }
        }
    }
}
