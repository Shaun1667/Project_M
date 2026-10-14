using System;

namespace ProjectM.Core
{
    /// <summary>
    /// 턴제 시계. 플레이어가 행동을 확정하면, 다시 플레이어 입력이 필요해질 때까지 틱을 진행한다.
    /// 플레이어 행동 비용이 N틱이면 그 사이 N틱 동안 적들이 움직인다.
    /// </summary>
    public sealed class TurnClock : ITickClock
    {
        /// <summary>한 번의 입력으로 진행할 수 있는 최대 틱 수. 플레이어가 없는 등 끝나지 않는 진행을 막는 안전장치.</summary>
        public const int MaxTicksPerSubmit = 10000;

        private readonly TickScheduler _scheduler;
        private readonly PlayerCommandSource _playerSource;

        public TurnClock(TickScheduler scheduler, PlayerCommandSource playerSource)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _playerSource = playerSource ?? throw new ArgumentNullException(nameof(playerSource));
        }

        public void SubmitPlayerCommand(ICommand command)
        {
            if (!_scheduler.PlayerId.IsValid) throw new InvalidOperationException("등록된 플레이어가 없다.");
            _playerSource.Enqueue(command);

            int ticks = 0;
            while (!_scheduler.IsWaitingForPlayer)
            {
                if (!_scheduler.PlayerId.IsValid) return; // 진행 중 플레이어 사망 → 멈춤 (승패 처리는 1-6)
                if (++ticks > MaxTicksPerSubmit)
                    throw new InvalidOperationException($"입력 하나로 {MaxTicksPerSubmit}틱을 넘게 진행했다.");
                _scheduler.Tick();
            }
        }

        public void Update(float deltaSeconds)
        {
        }
    }
}
