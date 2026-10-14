using System;

namespace ProjectM.Core
{
    /// <summary>
    /// 한 층의 기본 지형 그리드. 칸 데이터는 1차원 배열 하나로만 보관해 스냅샷(1-4)에서 그대로 복사할 수 있게 한다.
    /// </summary>
    public sealed class GridMap
    {
        private readonly TileType[] _cells;

        public int Width { get; }
        public int Height { get; }

        public GridMap(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), width, "너비는 1 이상이어야 한다.");
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height), height, "높이는 1 이상이어야 한다.");

            Width = width;
            Height = height;
            _cells = new TileType[width * height];
        }

        public bool InBounds(GridPos pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height;

        public TileType Get(GridPos pos) => _cells[IndexOf(pos)];

        public void Set(GridPos pos, TileType type) => _cells[IndexOf(pos)] = type;

        /// <summary>바닥 칸만 걸을 수 있다. 맵 밖은 걸을 수 없다.</summary>
        public bool IsWalkable(GridPos pos) => InBounds(pos) && _cells[IndexOf(pos)] == TileType.Floor;

        private int IndexOf(GridPos pos)
        {
            if (!InBounds(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), pos, $"맵 {Width}x{Height} 밖의 좌표.");
            return pos.Y * Width + pos.X;
        }
    }
}
