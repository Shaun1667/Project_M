using System;

namespace ProjectM.Core
{
    /// <summary>
    /// 실시간 시계 (3-27 — CLAUDE.md 규칙 4 "제한해제는 1초마다 1틱", 결정 2026-11-05): 세계는 GameConfig.RealtimeTickIntervalSeconds마다 1틱,
    /// 플레이어 행동은 즉시 실행되고 틱을 쓰지 않는다 (TickScheduler.ExecutePlayerNow — "플레이어는 무제한 행동").
    /// 규칙은 턴제와 같다 — 누가 언제 Tick()을 부르는지만 다르다. 남은 시간 누적값은 화면 쪽 시계 상태라 저장하지 않는다.
    /// </summary>
    public sealed class RealTimeClock : ITickClock
    {
        /// <summary>한 프레임에 따라잡는 최대 틱 수 (멈췄다 돌아온 프레임에 몰아서 돌지 않게).</summary>
        public const int MaxTicksPerUpdate = 5;

        private readonly TickScheduler _scheduler;
        private readonly float _interval;
        private float _elapsed;

        public RealTimeClock(TickScheduler scheduler, float intervalSeconds)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            if (!(intervalSeconds > 0f)) throw new ArgumentOutOfRangeException(nameof(intervalSeconds), intervalSeconds, "틱 간격은 0보다 커야 한다.");
            _interval = intervalSeconds;
            _scheduler.PlayerActsImmediately = true;
        }

        /// <summary>마지막 Update에서 진행한 틱 수 (저장 시점 판단용).</summary>
        public int LastTicks { get; private set; }

        /// <summary>플레이어 행동 — 바로 실행 (틱 안 씀). 행동 불가면 아무 일 없음.</summary>
        public void SubmitPlayerCommand(ICommand command)
        {
            if (!_scheduler.PlayerId.IsValid) throw new InvalidOperationException("등록된 플레이어가 없다.");
            _scheduler.ExecutePlayerNow(command);
        }

        public void Update(float deltaSeconds)
        {
            LastTicks = 0;
            if (deltaSeconds <= 0f || !_scheduler.PlayerId.IsValid) return;
            _elapsed += deltaSeconds;
            while (_elapsed >= _interval)
            {
                _elapsed -= _interval;
                if (LastTicks >= MaxTicksPerUpdate)
                {
                    _elapsed = 0f;
                    break;
                }
                _scheduler.Tick();
                LastTicks++;
                if (!_scheduler.PlayerId.IsValid) break; // 플레이어 사망 → 멈춤
            }
        }
    }
}
