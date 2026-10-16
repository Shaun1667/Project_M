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

        /// <summary>스냅샷 복원용. cells는 복사해서 쓴다.</summary>
        public static GridMap FromCells(int width, int height, TileType[] cells)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            var map = new GridMap(width, height);
            if (cells.Length != map._cells.Length)
                throw new ArgumentException($"칸 수 {cells.Length}가 {width}x{height}와 맞지 않는다.", nameof(cells));
            Array.Copy(cells, map._cells, cells.Length);
            return map;
        }

        public bool InBounds(GridPos pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height;

        public TileType Get(GridPos pos) => _cells[IndexOf(pos)];

        public void Set(GridPos pos, TileType type) => _cells[IndexOf(pos)] = type;

        /// <summary>칸 배열의 복사본. 인덱스 = y * Width + x.</summary>
        public TileType[] CopyCells() => (TileType[])_cells.Clone();

        /// <summary>걸을 수 있는 칸인지. 맵 밖은 걸을 수 없다.</summary>
        public bool IsWalkable(GridPos pos) => InBounds(pos) && IsWalkableType(_cells[IndexOf(pos)]);

        /// <summary>걸을 수 있는 지형: 바닥 · 계단.</summary>
        public static bool IsWalkableType(TileType type) => type == TileType.Floor || type == TileType.StairsDown;

        /// <summary>맵 가장자리 한 줄인지.</summary>
        public bool IsBorder(GridPos pos) => pos.X == 0 || pos.Y == 0 || pos.X == Width - 1 || pos.Y == Height - 1;

        private int IndexOf(GridPos pos)
        {
            if (!InBounds(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), pos, $"맵 {Width}x{Height} 밖의 좌표.");
            return pos.Y * Width + pos.X;
        }
    }
}
