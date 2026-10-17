using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 적 재생성 (3-4a, 결정 2026-10-18). ⑤ 단계, (틱 + 1)이 SpawnConfig.RespawnIntervalTicks의 배수인 틱마다
    /// 지금 층에 살아 있는 적이 RespawnMaxEnemies보다 적으면 1마리를 플레이어 시야 밖 바닥 칸(계단 제외 · 빈 칸) 중 무작위로 놓는다.
    /// 칸은 재생성 난수(RngStream.Spawn) — 맵 생성 수열을 건드리지 않아 다음 층 모양이 바뀌지 않는다.
    /// 마지막 층(보스 층)에서는 만들지 않는다. 떠난 층은 멈춰 있으므로 재생성도 없다. 틱 번호로만 정하므로 저장 상태가 없다.
    /// </summary>
    public sealed class EnemyRespawnSystem : ITickSystem
    {
        private readonly World _world;
        private readonly SpawnConfig _spawn;
        private readonly UnitDef _enemyDef;
        private readonly ICommandSource _ai;
        private readonly ISightRule _sight;
        private readonly int _bossFloor;

        public EnemyRespawnSystem(World world, SpawnConfig spawn, UnitDef enemyDef, ICommandSource ai, ISightRule sight, int bossFloor)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _spawn = spawn ?? throw new ArgumentNullException(nameof(spawn));
            _enemyDef = enemyDef ?? throw new ArgumentNullException(nameof(enemyDef));
            _ai = ai ?? throw new ArgumentNullException(nameof(ai));
            _sight = sight ?? throw new ArgumentNullException(nameof(sight));
            _bossFloor = bossFloor;
        }

        public void Run(in TickContext context)
        {
            int interval = _spawn.RespawnIntervalTicks;
            if (interval <= 0 || (context.Tick + 1) % interval != 0) return;
            if (_world.Floor == _bossFloor) return;
            if (!PlayerActions.TryGetPlayer(_world, out var player) || !player.IsAlive) return;

            int alive = 0;
            foreach (var unit in _world.Units.ToSortedList())
                if (unit.Faction == Faction.Enemy && unit.IsAlive) alive++;
            if (alive >= _spawn.RespawnMaxEnemies) return;

            var cells = Candidates(player);
            if (cells.Count == 0) return;
            var cell = cells[_world.Rng.Get(RngStream.Spawn).Range(0, cells.Count)];
            UnitSpawner.SpawnEnemy(_world, _enemyDef, cell, _ai);
        }

        /// <summary>후보 칸 (y → x 순): 바닥 · 비어 있음 · 지금 플레이어에게 안 보임.</summary>
        public List<GridPos> Candidates(Unit player)
        {
            var map = _world.Map;
            var visible = new bool[map.Width * map.Height];
            _sight.ComputeVisible(map, player.Pos, player.SightRange, visible);
            var list = new List<GridPos>();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var pos = new GridPos(x, y);
                    if (map.Get(pos) != TileType.Floor || visible[y * map.Width + x] || _world.Units.IsOccupied(pos)) continue;
                    list.Add(pos);
                }
            }
            return list;
        }
    }
}
