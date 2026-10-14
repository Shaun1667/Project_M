namespace ProjectM.Core
{
    /// <summary>
    /// 행위자의 행동 하나. 실행은 즉시 일어나고, 행위자는 CostTicks만큼 기다린 뒤 다시 행동한다
    /// (결정 2026-10-14: "즉시 실행 + N틱 대기"). 비용 값은 데이터에서 온다 — 기본값은 GameConfig.DefaultActionCostTicks.
    /// </summary>
    public interface ICommand
    {
        /// <summary>행동 비용(틱). 1 이상.</summary>
        int CostTicks { get; }

        void Execute(in TickContext context);
    }
}
