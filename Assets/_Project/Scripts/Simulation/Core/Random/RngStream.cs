namespace ProjectM.Core
{
    /// <summary>
    /// 난수 용도. 용도마다 독립된 수열을 써서, 한 용도의 소비량이 다른 용도의 결과를 바꾸지 않게 한다.
    /// 값은 끝에만 추가한다 — 수열이 값(정수)에서 만들어지므로 순서를 바꾸면 기존 시드의 결과가 바뀐다.
    /// 연출용 난수는 표현 레이어 몫이라 여기에 두지 않는다.
    /// </summary>
    public enum RngStream
    {
        MapGen = 0,
        Combat = 1,
        Ai = 2,
        Loot = 3,
    }
}
