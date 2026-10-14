namespace ProjectM.Core
{
    /// <summary>틱 단계(②~⑦)에 등록되어 매 틱 한 번 실행되는 시스템. 예: 상태 처리, 마나 회복.</summary>
    public interface ITickSystem
    {
        void Run(in TickContext context);
    }
}
