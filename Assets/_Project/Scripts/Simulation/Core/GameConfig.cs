using System;

namespace ProjectM.Core
{
    /// <summary>
    /// 게임 전역 수치. 값은 데이터 에셋(GameConfigAsset)에서 넘겨받고 코드에 기본값을 두지 않는다.
    /// 기준값과 근거는 BALANCE.md 1번.
    /// </summary>
    public sealed class GameConfig
    {
        /// <summary>행동 비용을 따로 지정하지 않은 행동이 쓰는 틱 수 (GDD 2-4).</summary>
        public int DefaultActionCostTicks { get; }

        /// <summary>제한해제(실시간) 모드에서 1틱의 길이, 초 (GDD 3-1).</summary>
        public float RealtimeTickIntervalSeconds { get; }

        public GameConfig(int defaultActionCostTicks, float realtimeTickIntervalSeconds)
        {
            if (defaultActionCostTicks < 1)
                throw new ArgumentOutOfRangeException(nameof(defaultActionCostTicks), defaultActionCostTicks, "행동 비용은 1틱 이상이어야 한다.");
            if (!(realtimeTickIntervalSeconds > 0f))
                throw new ArgumentOutOfRangeException(nameof(realtimeTickIntervalSeconds), realtimeTickIntervalSeconds, "틱 간격은 0보다 커야 한다.");

            DefaultActionCostTicks = defaultActionCostTicks;
            RealtimeTickIntervalSeconds = realtimeTickIntervalSeconds;
        }
    }
}
