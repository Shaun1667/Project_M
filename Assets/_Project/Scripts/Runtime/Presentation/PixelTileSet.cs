using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 2D 픽셀 맵 타일 묶음 (P-3a, 결정 2026-10-21 — 64px · 위에서 비스듬히 SPD식). 데이터는 칸 종류만 알고, 어떤 그림인지는 여기서 고른다.
    /// 벽은 아래 칸이 걸을 수 있으면 앞면(윗면 띠 + 벽돌), 아니면 윗면(어두운 덮개). 바닥은 변형 여러 장 중 위치로 고른다(난수 안 씀).
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Pixel Tile Set", fileName = "PixelTileSet")]
    public sealed class PixelTileSet : ScriptableObject
    {
        [Tooltip("바닥 변형 — 칸 위치로 하나를 고름")] [SerializeField] private TileBase[] _floors = new TileBase[0];
        [Tooltip("벽 앞면 (아래 칸이 바닥)")] [SerializeField] private TileBase _wallFront;
        [Tooltip("벽 윗면 (아래 칸도 벽 · 맵 밖)")] [SerializeField] private TileBase _wallTop;
        [SerializeField] private TileBase _stairsDown;
        [SerializeField] private TileBase _stairsUp;

        public TileBase WallFront => _wallFront;
        public TileBase WallTop => _wallTop;
        public TileBase StairsDown => _stairsDown;
        public TileBase StairsUp => _stairsUp;

        /// <summary>칸 위치로 바닥 변형을 고른다 (같은 칸은 늘 같은 그림 — 해시 순서에 기대지 않는 정수 섞기).</summary>
        public TileBase Floor(int x, int y)
        {
            if (_floors == null || _floors.Length == 0) return null;
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return _floors[(int)(h % (uint)_floors.Length)];
            }
        }

        /// <summary>에디터 Setup용.</summary>
        public void Configure(TileBase[] floors, TileBase wallFront, TileBase wallTop, TileBase stairsDown, TileBase stairsUp)
        {
            _floors = floors ?? new TileBase[0];
            _wallFront = wallFront;
            _wallTop = wallTop;
            _stairsDown = stairsDown;
            _stairsUp = stairsUp;
        }
    }
}
