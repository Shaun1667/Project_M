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
        /// <summary>신속 (3-13a): To 다음에 이어서 가는 칸 (같은 차례). 없으면 null.</summary>
        public GridPos? Then { get; }
        public int CostTicks { get; }

        public MoveCommand(World world, GridPos to, int costTicks, GridPos? then = null)
        {
            Then = then;
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
            // 신속: 둘째 칸 (막혔으면 첫 칸에서 멈춤). 지나간 첫 칸의 함정 · 풀은 밟지 않는다 (⑤는 차례 끝 칸만 본다)
            if (Then.HasValue && GridMovement.CanStep(_world.Map, To, Then.Value) && !_world.Units.IsOccupied(Then.Value))
            {
                _world.Units.Move(unit.Id, Then.Value);
                _world.Events.Publish(new EntityMovedEvent(unit.Id, To, Then.Value));
            }
        }
    }
}
