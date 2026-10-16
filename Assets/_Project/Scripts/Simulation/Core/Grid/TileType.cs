namespace ProjectM.Core
{
    /// <summary>
    /// 칸의 기본 지형. 값은 끝에만 추가한다 (에셋 · 세이브가 정수로 저장).
    /// 상태 · 지형 효과 레이어(물, 화염 지대 등)는 별도 구조로 2-4에서 정한다.
    /// </summary>
    public enum TileType
    {
        Empty = 0,
        Floor = 1,
        Wall = 2,
        /// <summary>내려가는 계단 — 층의 도착점. 걸을 수 있다 (밟았을 때 동작은 3-4).</summary>
        StairsDown = 3,
    }
}
