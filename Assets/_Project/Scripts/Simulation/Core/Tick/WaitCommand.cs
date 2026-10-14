using System;

namespace ProjectM.Core
{
    /// <summary>아무것도 하지 않고 차례를 넘긴다 (제자리 대기).</summary>
    public sealed class WaitCommand : ICommand
    {
        public int CostTicks { get; }

        public WaitCommand(int costTicks)
        {
            if (costTicks < 1) throw new ArgumentOutOfRangeException(nameof(costTicks), costTicks, "행동 비용은 1틱 이상이어야 한다.");
            CostTicks = costTicks;
        }

        public void Execute(in TickContext context)
        {
        }
    }
}
