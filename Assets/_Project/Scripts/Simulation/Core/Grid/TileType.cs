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
        /// <summary>내려가는 계단 — 층의 도착점. 걸을 수 있다. 플레이어가 들어가면 다음 층 (3-4a).</summary>
        StairsDown = 3,
        /// <summary>올라가는 계단 (3-4a) — 2층부터 시작 칸. 플레이어가 들어가면 이전 층 (떠날 때 그대로 남아 있음).</summary>
        StairsUp = 4,
        /// <summary>짧은 풀 (3-11a) — 장식 바닥. 걸을 수 있고 보인다.</summary>
        Grass = 5,
        /// <summary>높은 풀 (3-11a, SPD) — 걸을 수 있지만 시야를 가린다. 유닛이 들어서면 짧은 풀이 된다 (GrassTrampleSystem).</summary>
        TallGrass = 6,
        /// <summary>닫힌 문 (3-11c, SPD) — 걸을 수 있지만 시야를 가린다. 유닛이 서면 열린 문이 된다 (DoorSystem). 불에 탄다.</summary>
        Door = 7,
        /// <summary>열린 문 (3-11c) — 유닛이 서 있는 동안. 비면 닫힌 문으로 돌아간다.</summary>
        OpenDoor = 8,
        /// <summary>숨겨진 문 (3-12b, SPD) — 찾기 전엔 벽과 같다 (못 지나감 · 시야 막음 · 벽으로 그림). 찾으면 닫힌 문.</summary>
        HiddenDoor = 9,
        /// <summary>잠긴 문 (3-15b 보물 방) — 걸을 수 없고 시야를 막는다. 열쇠를 가지고 부딪히면 닫힌 문이 된다 (UnlockCommand).</summary>
        LockedDoor = 10,
    }
}
