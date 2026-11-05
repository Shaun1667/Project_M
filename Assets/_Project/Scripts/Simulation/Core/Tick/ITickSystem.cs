namespace ProjectM.Core
{
    /// <summary>틱 단계(②~⑦)에 등록되어 매 틱 한 번 실행되는 시스템. 예: 상태 처리, 마나 회복.</summary>
    public interface ITickSystem
    {
        void Run(in TickContext context);
    }

    /// <summary>
    /// 실시간 모드(3-27, 결정 2026-11-05)에서 플레이어의 즉시 행동 뒤에도 바로 도는 시스템 — 시간이 아니라 위치 · 결과에 반응하는 것
    /// (줍기 · 함정 · 문 · 풀 밟기 · 사망 판정 · 시야 · 이벤트 발행). 여러 번 돌아도 결과가 같아야 한다. 매 틱에도 평소대로 돈다.
    /// 지속 감소 · 회복 · 허기 · 재생성처럼 시간에 따른 시스템은 붙이지 않는다.
    /// </summary>
    public interface IInstantTickSystem : ITickSystem
    {
    }
}
