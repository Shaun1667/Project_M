namespace ProjectM.Core
{
    /// <summary>
    /// 행위자의 행동을 정하는 쪽. 적 AI는 항상 행동을 내야 한다 (할 일이 없으면 WaitCommand).
    /// 플레이어는 PlayerCommandSource가 입력 큐 역할을 한다.
    /// </summary>
    public interface ICommandSource
    {
        ICommand NextCommand(in TickContext context);
    }
}
