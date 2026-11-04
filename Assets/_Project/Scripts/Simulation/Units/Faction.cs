namespace ProjectM.Units
{
    /// <summary>유닛 진영. 끝에만 추가한다 (스냅샷에 정수로 저장).</summary>
    public enum Faction
    {
        Player = 0,
        Enemy = 1,
        /// <summary>아군 (3-13b 분신) — 플레이어 편. 적과 서로 싸운다.</summary>
        Ally = 2,
    }

    /// <summary>진영 관계 (3-13b): 적 ↔ (플레이어 · 아군)만 적대.</summary>
    public static class Factions
    {
        public static bool AreHostile(Faction a, Faction b) => (a == Faction.Enemy) != (b == Faction.Enemy);
    }
}
