using System;
using ProjectM.Core;

namespace ProjectM.Dungeon
{
    /// <summary>
    /// ⑤ 풀 밟기 (3-11a, SPD): 높은 풀 칸에 선 살아 있는 유닛(플레이어 · 적 모두)이 있으면 그 칸을 짧은 풀로 바꾼다.
    /// ⑥ 시야 갱신 전에 돌아 같은 틱에 시야가 열린다. 상태를 갖지 않는다.
    /// </summary>
    public sealed class GrassTrampleSystem : IInstantTickSystem // 3-27 실시간 즉시 행동 뒤에도
    {
        private readonly World _world;
        private readonly ProjectM.Status.StatusBook _statuses;

        /// <param name="statuses">부양(3-13a) 판정용. null이면 모두 밟는다.</param>
        public GrassTrampleSystem(World world, ProjectM.Status.StatusBook statuses = null)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _statuses = statuses;
        }

        public void Run(in TickContext context)
        {
            foreach (var unit in _world.Units.ToSortedList())
            {
                if (!unit.IsAlive || !_world.Map.InBounds(unit.Pos) || ProjectM.Status.StatusEffects.Any(_statuses, unit, d => d.Levitates)) continue;
                if (_world.Map.Get(unit.Pos) != TileType.TallGrass) continue;
                _world.SetTile(unit.Pos, TileType.Grass);
                _world.OnGrassTrampled?.Invoke(unit.Pos); // 3-16b: 가끔 씨앗
            }
        }
    }
}
