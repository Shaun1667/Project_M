using System;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>찾기 수치 (3-12b). 값은 TrapCatalogAsset에서 — 기준은 BALANCE.md 17번.</summary>
    public sealed class SearchConfig
    {
        /// <summary>찾기 행동이 확실히 찾는 반경 (체비셰프).</summary>
        public int Radius { get; }
        /// <summary>찾기 행동 비용 (틱).</summary>
        public int CostTicks { get; }
        /// <summary>저절로 발견: 반경 안의 숨겨진 것마다 매 틱 이 확률(%)로 찾는다. 0이면 없음.</summary>
        public int PassivePercent { get; }
        public int PassiveRadius { get; }
        /// <summary>빠른 찾기 (3-24, SPD — 정보 모드에서 E를 한 번 더): 둘레 QuickRadius 칸, 비용 QuickCostTicks.</summary>
        public int QuickRadius { get; }
        public int QuickCostTicks { get; }

        public SearchConfig(int radius, int costTicks, int passivePercent, int passiveRadius, int quickRadius = 1, int quickCostTicks = 1)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
            if (costTicks < 1) throw new ArgumentOutOfRangeException(nameof(costTicks));
            if (passivePercent < 0 || passivePercent > 100) throw new ArgumentOutOfRangeException(nameof(passivePercent));
            if (passiveRadius < 0) throw new ArgumentOutOfRangeException(nameof(passiveRadius));
            if (quickRadius < 0) throw new ArgumentOutOfRangeException(nameof(quickRadius));
            if (quickCostTicks < 1) throw new ArgumentOutOfRangeException(nameof(quickCostTicks));
            QuickRadius = quickRadius;
            QuickCostTicks = quickCostTicks;
            Radius = radius;
            CostTicks = costTicks;
            PassivePercent = passivePercent;
            PassiveRadius = passiveRadius;
        }
    }

    /// <summary>
    /// 숨겨진 함정 · 숨겨진 문 찾기는 여기 하나 (3-12b, SPD): 찾기 행동(반경 확실히) · 저절로 발견(이웃 칸 확률) · 지도 주문서(층 전체).
    /// 숨겨진 문은 칸 종류 HiddenDoor(벽처럼 보이고 못 지나감) → 찾으면 닫힌 문.
    /// </summary>
    public static class Searching
    {
        /// <summary>그 칸에 숨겨진 것이 있으면 드러낸다. 드러냈으면 true.</summary>
        public static bool RevealAt(World world, GridPos pos)
        {
            if (!world.Map.InBounds(pos)) return false;
            if (world.Map.Get(pos) == TileType.HiddenDoor)
            {
                world.SetTile(pos, TileType.Door);
                world.Events.Publish(new HiddenDoorFoundEvent(pos));
                return true;
            }
            return Traps.Reveal(world, pos);
        }

        /// <summary>찾기 행동: center에서 체비셰프 radius 안의 숨겨진 것을 모두 드러낸다 (y → x 순). SearchedEvent를 낸다. 찾은 수.</summary>
        public static int Search(World world, EntityId actor, GridPos center, int radius)
        {
            int found = 0;
            for (int y = center.Y - radius; y <= center.Y + radius; y++)
                for (int x = center.X - radius; x <= center.X + radius; x++)
                    if (RevealAt(world, new GridPos(x, y))) found++;
            world.Events.Publish(new SearchedEvent(actor, center, found));
            return found;
        }

        /// <summary>지도 주문서 (3-9 MagicMapping): 층의 숨겨진 것을 모두 드러낸다.</summary>
        public static void RevealAll(World world)
        {
            var map = world.Map;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    RevealAt(world, new GridPos(x, y));
        }

        /// <summary>숨겨진 것이 있는 칸인지 (저절로 발견 후보).</summary>
        public static bool HasHidden(World world, GridPos pos) =>
            world.Map.InBounds(pos) && (world.Map.Get(pos) == TileType.HiddenDoor || (world.Traps.TryGet(pos, out var trap) && trap.Hidden));
    }

    /// <summary>찾기 행동 (3-12b): 비용 CostTicks, 행위자 둘레 Radius 칸을 확실히 찾는다. 상태를 갖지 않는다.</summary>
    public sealed class SearchCommand : ICommand
    {
        private readonly World _world;
        private readonly SearchConfig _config;

        public int CostTicks => _quick ? _config.QuickCostTicks : _config.CostTicks;
        private readonly bool _quick;

        /// <param name="quick">true면 빠른 찾기 (QuickRadius · QuickCostTicks, 3-24).</param>
        public SearchCommand(World world, SearchConfig config, bool quick = false)
        {
            _quick = quick;
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var actor) || !actor.IsAlive) return;
            Searching.Search(_world, actor.Id, actor.Pos, _quick ? _config.QuickRadius : _config.Radius);
        }
    }

    /// <summary>
    /// ⑤ 저절로 발견 (3-12b, SPD): 살아 있는 플레이어 둘레 PassiveRadius 칸의 숨겨진 것마다 PassivePercent 확률 (RngStream.Search, y → x 순).
    /// 함정 발동(TrapSystem)보다 먼저 등록한다. 상태를 갖지 않는다.
    /// </summary>
    public sealed class PassiveSearchSystem : ITickSystem
    {
        private readonly World _world;
        private readonly SearchConfig _config;

        public PassiveSearchSystem(World world, SearchConfig config)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Run(in TickContext context)
        {
            if (_config.PassivePercent == 0 || !PlayerActions.TryGetPlayer(_world, out var player) || !player.IsAlive) return;
            int r = _config.PassiveRadius;
            for (int y = player.Pos.Y - r; y <= player.Pos.Y + r; y++)
                for (int x = player.Pos.X - r; x <= player.Pos.X + r; x++)
                {
                    var pos = new GridPos(x, y);
                    if (!Searching.HasHidden(_world, pos)) continue;
                    if (_world.Rng.Get(RngStream.Search).Range(0, 100) < _config.PassivePercent) Searching.RevealAt(_world, pos);
                }
        }
    }

    /// <summary>찾기 행동을 했다 (로그). Found = 드러낸 수.</summary>
    public sealed class SearchedEvent : ISimEvent
    {
        public EntityId Actor { get; }
        public GridPos Center { get; }
        public int Found { get; }

        public SearchedEvent(EntityId actor, GridPos center, int found)
        {
            Actor = actor;
            Center = center;
            Found = found;
        }
    }

    /// <summary>숨겨진 문을 찾았다 (로그). 칸은 이미 닫힌 문(TileChangedEvent)으로 바뀌었다.</summary>
    public sealed class HiddenDoorFoundEvent : ISimEvent
    {
        public GridPos Pos { get; }

        public HiddenDoorFoundEvent(GridPos pos)
        {
            Pos = pos;
        }
    }
}
