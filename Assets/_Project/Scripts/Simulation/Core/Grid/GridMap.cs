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

        /// <summary>걸을 수 있는 지형: 바닥 · 계단(내려가는 · 올라가는) · 풀(3-11a) · 문(3-11c, 닫힌 문도 들어서면 열림).</summary>
        public static bool IsWalkableType(TileType type) => IsGroundType(type) || type == TileType.StairsDown || type == TileType.StairsUp
            || IsDoorType(type);

        /// <summary>문 (3-11c): 닫힌 문 · 열린 문.</summary>
        public static bool IsDoorType(TileType type) => type == TileType.Door || type == TileType.OpenDoor;

        /// <summary>맨땅 — 유닛 · 아이템을 놓을 수 있는 보통 칸: 바닥 · 짧은 풀 · 높은 풀 (계단 제외, 3-11a).</summary>
        public static bool IsGroundType(TileType type) => type == TileType.Floor || type == TileType.Grass || type == TileType.TallGrass;

        /// <summary>그 종류의 첫 칸 (y → x 순), 없으면 null — 계단 찾기 (3-4a).</summary>
        public GridPos? Find(TileType type)
        {
            for (int i = 0; i < _cells.Length; i++)
                if (_cells[i] == type) return new GridPos(i % Width, i / Width);
            return null;
        }

        /// <summary>불에 타는 칸 (3-11b): 짧은 풀 · 높은 풀 · 문(3-11c). 타면 바닥이 된다 (FireSpreadSystem).</summary>
        public static bool IsFlammableType(TileType type) => type == TileType.Grass || type == TileType.TallGrass || IsDoorType(type);

        /// <summary>시야를 막는 칸인지: 벽 · 빈칸 · 맵 밖 · 높은 풀(3-11a) · 닫힌 문(3-11c). 바닥 · 계단 · 짧은 풀 · 열린 문은 보인다.</summary>
        public bool BlocksSight(GridPos pos)
        {
            if (!InBounds(pos)) return true;
            var type = _cells[IndexOf(pos)];
            return !IsWalkableType(type) || type == TileType.TallGrass || type == TileType.Door;
        }

        /// <summary>주문 · 투척 경로를 막는 칸인지: 벽 · 빈칸 · 맵 밖. 높은 풀 · 닫힌 문은 시야만 가리고 막지 않는다 (3-11a · 3-11c, SPD).</summary>
        public bool BlocksProjectile(GridPos pos) => !InBounds(pos) || !IsWalkableType(_cells[IndexOf(pos)]);

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
