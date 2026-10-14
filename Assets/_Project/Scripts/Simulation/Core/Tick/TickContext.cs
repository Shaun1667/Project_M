namespace ProjectM.Core
{
    /// <summary>지금 처리 중인 틱 정보. 커맨드 · 단계 시스템에 넘겨진다.</summary>
    public readonly struct TickContext
    {
        /// <summary>처리 중인 틱 번호 (0부터).</summary>
        public readonly long Tick;

        /// <summary>지금 행동 중인 행위자. 행동 단계 밖에서는 None.</summary>
        public readonly EntityId Actor;

        public TickContext(long tick, EntityId actor)
        {
            Tick = tick;
            Actor = actor;
        }
    }
}
