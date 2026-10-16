using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 시야 판정 (CLAUDE.md: 바뀔 수 있는 판정은 인터페이스 뒤에). 그리드 계산만 쓴다 (규칙 2).
    /// 상태를 갖지 않는다.
    /// </summary>
    public interface ISightRule
    {
        /// <summary>from에서 to가 보이는지.</summary>
        bool CanSee(GridMap map, GridPos from, GridPos to, int range);

        /// <summary>origin에서 보이는 칸을 visible(인덱스 = y * Width + x)에 표시한다.</summary>
        void ComputeVisible(GridMap map, GridPos origin, int range, bool[] visible);
    }

    /// <summary>1-7 판정 (결정 2026-10-16): 대칭 그림자 투사, 원형 범위 — FieldOfView.</summary>
    public sealed class ShadowcastSightRule : ISightRule
    {
        public bool CanSee(GridMap map, GridPos from, GridPos to, int range) => FieldOfView.CanSee(map, from, to, range);

        public void ComputeVisible(GridMap map, GridPos origin, int range, bool[] visible) => FieldOfView.Compute(map, origin, range, visible);
    }
}
