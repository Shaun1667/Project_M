using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Dungeon
{
    /// <summary>
    /// ⑤ 문 열고 닫기 (3-11c, SPD): 살아 있는 유닛이 선 닫힌 문은 열린 문, 아무도 없는 열린 문은 닫힌 문으로.
    /// ⑥ 시야 갱신 전에 돌아 같은 틱에 문 너머가 보인다. 상태를 갖지 않는다 (문 상태는 칸 종류 = 맵에 저장).
    /// </summary>
    public sealed class DoorSystem : IInstantTickSystem // 3-27 실시간 즉시 행동 뒤에도
    {
        private readonly World _world;

        public DoorSystem(World world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public void Run(in TickContext context)
        {
            var map = _world.Map;
            var occupied = new HashSet<GridPos>();
            foreach (var unit in _world.Units.ToSortedList())
            {
                if (!unit.IsAlive || !map.InBounds(unit.Pos)) continue;
                occupied.Add(unit.Pos);
                if (map.Get(unit.Pos) == TileType.Door) _world.SetTile(unit.Pos, TileType.OpenDoor);
            }
            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var pos = new GridPos(x, y);
                if (map.Get(pos) == TileType.OpenDoor && !occupied.Contains(pos)) _world.SetTile(pos, TileType.Door);
            }
        }
    }
}
