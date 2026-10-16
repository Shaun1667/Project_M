using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// "from에서 to가 보이는가" 판정 (CLAUDE.md: 바뀔 수 있는 판정은 인터페이스 뒤에). 그리드 계산만 쓴다 (규칙 2).
    /// 상태를 갖지 않는다.
    /// </summary>
    public interface ISightRule
    {
        bool CanSee(GridMap map, GridPos from, GridPos to, int range);
    }

    /// <summary>
    /// 1-6 임시 판정: 체비셰프 거리만 본다 (벽에 가려지는 것은 무시). 1-7 시야 계산으로 바꾼다.
    /// </summary>
    public sealed class RangeOnlySightRule : ISightRule
    {
        public bool CanSee(GridMap map, GridPos from, GridPos to, int range) => GridMovement.Chebyshev(from, to) <= range;
    }
}
