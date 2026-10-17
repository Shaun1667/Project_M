using ProjectM.Core;

namespace ProjectM.Dungeon
{
    /// <summary>생성된 층 하나. Exit 칸은 Map에 StairsDown으로 (마지막 층은 바닥), 2층부터 Start 칸은 StairsUp으로 들어 있다 (3-4a).</summary>
    public sealed class DungeonFloor
    {
        public GridMap Map { get; }
        /// <summary>플레이어 시작 칸 (첫 방의 기준점).</summary>
        public GridPos Start { get; }
        /// <summary>내려가는 계단 칸 (마지막 방의 기준점).</summary>
        public GridPos Exit { get; }

        public DungeonFloor(GridMap map, GridPos start, GridPos exit)
        {
            Map = map;
            Start = start;
            Exit = exit;
        }
    }
}
