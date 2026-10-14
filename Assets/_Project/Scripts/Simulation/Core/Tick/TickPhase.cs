namespace ProjectM.Core
{
    /// <summary>
    /// 한 틱의 처리 단계. 값 순서가 곧 실행 순서다 (GDD 3-3, 확정 2026-10-14).
    /// 순서를 바꾸면 모든 규칙의 결과가 바뀌므로 GDD 3-3과 함께 결정한다.
    /// </summary>
    public enum TickPhase
    {
        /// <summary>① 행동 실행 — 준비된 행위자가 플레이어 → 적 EntityId 순으로 행동. TickScheduler가 직접 처리.</summary>
        Actions = 0,
        /// <summary>② 반응 규칙 판정 (3-1)</summary>
        Reactions = 1,
        /// <summary>③ 지형 처리 — 확산 · 지속 감소 · 소멸 (2-4)</summary>
        Terrain = 2,
        /// <summary>④ 상태 처리 — 지속 피해 · 지속 감소 · 해제 (2-3)</summary>
        Status = 3,
        /// <summary>⑤ 마나 회복 (2-1)</summary>
        ManaRegen = 4,
        /// <summary>⑥ 사망 · 승패 판정 (1-6)</summary>
        DeathCheck = 5,
        /// <summary>⑦ 이벤트 일괄 발행 → 표현 레이어 (1-3)</summary>
        EventFlush = 6,
    }
}
