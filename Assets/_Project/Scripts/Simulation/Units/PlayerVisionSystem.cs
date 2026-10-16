using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 플레이어 시야 (1-7). ⑥ 단계 끝(사망 판정 뒤)에 매 틱 실행: 지금 보이는 칸을 계산하고, World.Explored(탐험 기록)에 더하고,
    /// VisibilityChangedEvent를 발행한다. 지금 보이는 칸은 맵 · 플레이어 위치에서 다시 계산할 수 있으므로 저장하지 않는다.
    /// 층 시작 · 불러오기 직후에는 Refresh()를 직접 한 번 부른다.
    /// </summary>
    public sealed class PlayerVisionSystem : ITickSystem
    {
        private readonly World _world;
        private readonly ISightRule _sight;

        public PlayerVisionSystem(World world, ISightRule sight)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _sight = sight ?? throw new ArgumentNullException(nameof(sight));
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
            _world.Events.Publish(new VisibilityChangedEvent(_world.Map.Width, _world.Map.Height, visible, (bool[])_world.Explored.Clone()));
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

        public VisibilityChangedEvent(int width, int height, bool[] visible, bool[] explored)
        {
            Width = width;
            Height = height;
            Visible = visible ?? throw new ArgumentNullException(nameof(visible));
            Explored = explored ?? throw new ArgumentNullException(nameof(explored));
        }

        public bool IsVisible(GridPos pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height && Visible[pos.Y * Width + pos.X];
    }
}
