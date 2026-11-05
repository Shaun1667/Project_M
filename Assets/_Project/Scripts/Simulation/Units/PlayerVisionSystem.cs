using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 플레이어 시야 (1-7). ⑥ 단계 끝(사망 판정 뒤)에 매 틱 실행: 지금 보이는 칸을 계산하고, World.Explored(탐험 기록)에 더하고,
    /// VisibilityChangedEvent를 발행한다. 지금 보이는 칸은 맵 · 플레이어 위치에서 다시 계산할 수 있으므로 저장하지 않는다.
    /// 층 시작 · 불러오기 직후에는 Refresh()를 직접 한 번 부른다.
    /// </summary>
    public sealed class PlayerVisionSystem : IInstantTickSystem // 3-27 실시간 즉시 행동 뒤에도
    {
        private readonly World _world;
        private readonly ISightRule _sight;
        private readonly ProjectM.Status.StatusBook _statuses;

        /// <param name="statuses">정신 시야(3-13a) 판정용. null이면 감지 없음.</param>
        public PlayerVisionSystem(World world, ISightRule sight, ProjectM.Status.StatusBook statuses = null)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _sight = sight ?? throw new ArgumentNullException(nameof(sight));
            _statuses = statuses;
        }

        public void Run(in TickContext context) => Refresh();

        /// <summary>플레이어가 없으면(사망) 아무것도 하지 않는다 — 마지막 화면이 그대로 남는다.</summary>
        public void Refresh()
        {
            if (!PlayerActions.TryGetPlayer(_world, out var player)) return;

            var visible = new bool[_world.Map.Width * _world.Map.Height];
            _sight.ComputeVisible(_world.Map, player.Pos, player.SightRange, visible);
            for (int i = 0; i < visible.Length; i++)
            {
                if (visible[i]) _world.Explored[i] = true;
            }
            // 3-13a 정신 시야: 층의 살아 있는 적 칸 (지도는 밝히지 않음 — 유닛만 보임)
            bool[] sensed = null;
            if (ProjectM.Status.StatusEffects.Any(_statuses, player, d => d.SensesEnemies))
            {
                sensed = new bool[visible.Length];
                foreach (var unit in _world.Units.ToSortedList())
                    if (unit.IsAlive && Factions.AreHostile(unit.Faction, player.Faction) && _world.Map.InBounds(unit.Pos)) sensed[unit.Pos.Y * _world.Map.Width + unit.Pos.X] = true;
            }
            _world.Events.Publish(new VisibilityChangedEvent(_world.Map.Width, _world.Map.Height, visible, (bool[])_world.Explored.Clone(), sensed));
        }
    }

    /// <summary>플레이어 시야가 갱신됐다. 배열은 복사본 (인덱스 = y * Width + x).</summary>
    public sealed class VisibilityChangedEvent : ISimEvent
    {
        public int Width { get; }
        public int Height { get; }
        /// <summary>지금 보이는 칸.</summary>
        public bool[] Visible { get; }
        /// <summary>한 번이라도 본 칸 (Visible 포함).</summary>
        public bool[] Explored { get; }

        /// <summary>정신 시야로 감지한 적 칸 (3-13a). 없으면 null.</summary>
        public bool[] Sensed { get; }

        public VisibilityChangedEvent(int width, int height, bool[] visible, bool[] explored, bool[] sensed = null)
        {
            Sensed = sensed;
            Width = width;
            Height = height;
            Visible = visible ?? throw new ArgumentNullException(nameof(visible));
            Explored = explored ?? throw new ArgumentNullException(nameof(explored));
        }

        public bool IsVisible(GridPos pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height && Visible[pos.Y * Width + pos.X];

        /// <summary>정신 시야로 감지한 칸인지 (3-13a — 유닛 표시용).</summary>
        public bool IsSensed(GridPos pos) => Sensed != null && pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height && Sensed[pos.Y * Width + pos.X];
    }
}
