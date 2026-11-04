using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Status
{
    /// <summary>불 번짐 수치 (3-11b). 값은 TerrainCatalogAsset에서 온다 — 기준은 BALANCE.md 7번.</summary>
    public sealed class FireSpreadConfig
    {
        /// <summary>풀이 타는 동안 놓이는 화염 지대 지속 (틱). 풀 점화 반응(ReactionCatalog)의 지속과 같은 값.</summary>
        public int BurnTicks { get; }
        /// <summary>한 틱에 새로 불이 붙는 최대 칸 수 (규칙 10 안전장치 — 넘는 칸은 다음 틱에).</summary>
        public int MaxIgnitionsPerTick { get; }

        public FireSpreadConfig(int burnTicks, int maxIgnitionsPerTick)
        {
            if (burnTicks < 1) throw new ArgumentOutOfRangeException(nameof(burnTicks));
            if (maxIgnitionsPerTick < 1) throw new ArgumentOutOfRangeException(nameof(maxIgnitionsPerTick));
            BurnTicks = burnTicks;
            MaxIgnitionsPerTick = maxIgnitionsPerTick;
        }
    }

    /// <summary>
    /// ③ 불 번짐 (3-11b, 결정 2026-11-04 — SPD): 지형 처리(TerrainSystem) 다음에, 화염 지대가 있는 칸마다
    /// 그 칸의 탈 것(풀)을 태우고(→ 바닥), 상하좌우 이웃의 탈 것에 불을 붙인다(→ 바닥 + 화염 지대 BurnTicks).
    /// 이번 틱에 붙은 불은 다음 틱부터 번진다. 젖게 하는 지형(물 · 오염된 물)이 있는 칸은 타지 않고,
    /// 화염 지대와 같은 칸에 그런 지형이 있으면 불이 꺼진다 (번지지 않음 — 오염 물결 융합을 불 위에 쏜 경우 등).
    /// 한 틱에 MaxIgnitionsPerTick칸까지 (칸 인덱스 순). 상태를 갖지 않는다.
    /// </summary>
    public sealed class FireSpreadSystem : ITickSystem
    {
        private static readonly GridPos[] Neighbors =
        {
            new GridPos(0, 1), new GridPos(1, 0), new GridPos(0, -1), new GridPos(-1, 0),
        };

        private readonly World _world;
        private readonly TerrainBook _terrains;
        private readonly FireSpreadConfig _config;

        public FireSpreadSystem(World world, TerrainBook terrains, FireSpreadConfig config)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _terrains = terrains ?? throw new ArgumentNullException(nameof(terrains));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Run(in TickContext context)
        {
            var map = _world.Map;
            var ignite = new SortedSet<int>();
            foreach (var pair in _world.Terrain.ToSortedList())
            {
                if (!pair.Value.Exists(t => t.Type == TerrainEffectType.FireZone)) continue;
                var pos = _world.Terrain.PosOf(pair.Key);
                if (IsDamp(pos))
                {
                    TerrainEffects.Remove(_world, pos, TerrainEffectType.FireZone); // 물 · 오염된 물이 닿은 불은 꺼진다 (2026-11-04 사용자 요청)
                    continue;
                }
                if (GridMap.IsFlammableType(map.Get(pos))) _world.SetTile(pos, TileType.Floor); // 화염 지대 아래 풀은 탄다
                foreach (var dir in Neighbors)
                {
                    var n = pos + dir;
                    if (map.InBounds(n) && GridMap.IsFlammableType(map.Get(n)) && !IsDamp(n) && !_world.Terrain.Has(n, TerrainEffectType.FireZone))
                        ignite.Add(n.Y * map.Width + n.X);
                }
            }

            int count = 0;
            foreach (int index in ignite)
            {
                if (count++ >= _config.MaxIgnitionsPerTick) break;
                var pos = _world.Terrain.PosOf(index);
                _world.SetTile(pos, TileType.Floor);
                TerrainEffects.Place(_world, pos, TerrainEffectType.FireZone, _config.BurnTicks);
            }
        }

        /// <summary>그 칸에 유닛을 젖게 하는 지형이 있는지 (물 · 오염된 물 — 데이터의 AppliesStatus로 판정).</summary>
        private bool IsDamp(GridPos pos)
        {
            foreach (var t in _world.Terrain.At(pos))
                if (_terrains.TryGet(t.Type, out var def) && def.AppliesStatus == StatusType.Wet) return true;
            return false;
        }
    }
}
