namespace ProjectM.Core
{
    /// <summary>
    /// 시뮬레이션 난수 (CLAUDE.md 핵심 설계 규칙 7). 규칙 코드는 이 인터페이스만 주입받는다.
    /// 엔진 · .NET 기본 난수 클래스는 직접 쓰지 않는다 (RngTests.Simulation_DoesNotUseDirectRandom이 검사).
    /// </summary>
    public interface IRng
    {
        /// <summary>32비트 무작위 정수.</summary>
        uint NextUInt();

        /// <summary>[minInclusive, maxExclusive) 범위의 정수. 치우침 없음.</summary>
        int Range(int minInclusive, int maxExclusive);

        /// <summary>[0, 1) 범위의 실수.</summary>
        float NextFloat();
    }
}
