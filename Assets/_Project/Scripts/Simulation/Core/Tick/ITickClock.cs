namespace ProjectM.Core
{
    /// <summary>
    /// 틱을 언제 진행할지 정하는 시계 (GDD 3-1 시계 교체 방식). 규칙은 시계를 모르고 Tick()만 받는다.
    /// 턴제: TurnClock. 제한해제(실시간): RealtimeClock — Phase 9.
    /// </summary>
    public interface ITickClock
    {
        /// <summary>플레이어 행동을 넣는다.</summary>
        void SubmitPlayerCommand(ICommand command);

        /// <summary>프레임마다 경과 시간을 넘긴다. 턴제 시계는 무시한다.</summary>
        void Update(float deltaSeconds);
    }
}
