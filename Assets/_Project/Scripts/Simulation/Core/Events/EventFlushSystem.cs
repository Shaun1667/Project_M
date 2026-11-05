using System;

namespace ProjectM.Core
{
    /// <summary>⑦ 이벤트 발행 단계 (GDD 3-3). TickScheduler.AddSystem(TickPhase.EventFlush, ...)로 등록한다.</summary>
    public sealed class EventFlushSystem : IInstantTickSystem // 3-27 실시간 즉시 행동 뒤에도
    {
        private readonly SimEventBus _bus;

        public EventFlushSystem(SimEventBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public void Run(in TickContext context) => _bus.Flush();
    }
}
