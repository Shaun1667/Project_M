using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 얇은 벽 RuleTile (2026-10-22 사용자 요청 "벽을 RuleTile로 얇게" — SPD식). 규칙 · 스프라이트는 Setup(MainSceneSetup)이 Tools/tiles_wall.py 그림으로 만든다.
    /// 같은 Tilemap의 이웃만 본다: 이 타일 = 벽, 다른 타일 = 바닥(걸을 수 있는 칸), 빈 칸 = 맵 밖.
    /// 이웃 종류: Floor(바닥) · NotFloor · Shown(바닥 또는 벽 앞면 — 아래가 바닥인 벽) · Hidden. 규칙은 회전 · 뒤집기 없이 위에서부터 첫 일치.
    /// 홀로 선 벽(위 칸도 바닥)의 솟은 윗면은 칸 밖(위 칸)에 그려야 해서 PixelMapPresenter의 덮개 Tilemap이 맡는다.
    /// 스프라이트가 여러 장인 규칙은 칸 x % 장 수로 고른다(벽 앞면 벽돌이 옆 칸과 이어짐 — 난수 · 펄린 안 씀).
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Pixel Wall Tile", fileName = "PixelWallTile")]
    public sealed class PixelWallTile : RuleTile<PixelWallTile.Neighbor>
    {
        public sealed class Neighbor : RuleTile.TilingRuleOutput.Neighbor
        {
            public const int Floor = 3;
            public const int NotFloor = 4;
            public const int Shown = 5;
            public const int Hidden = 6;
        }

        public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
        {
            transform = Matrix4x4.identity;
            int count = Mathf.Min(rule.m_Neighbors.Count, rule.m_NeighborPositions.Count);
            for (int i = 0; i < count; i++)
                if (!Matches(rule.m_Neighbors[i], position + rule.m_NeighborPositions[i], tilemap)) return false;
            return true;
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = m_DefaultSprite;
            tileData.gameObject = null;
            tileData.colliderType = Tile.ColliderType.None;
            tileData.flags = TileFlags.LockTransform;
            tileData.transform = Matrix4x4.identity;
            var transform = Matrix4x4.identity;
            foreach (var rule in m_TilingRules)
            {
                if (!RuleMatches(rule, position, tilemap, ref transform)) continue;
                int n = rule.m_Sprites.Length;
                tileData.sprite = n == 0 ? null : rule.m_Sprites[((position.x % n) + n) % n];
                return;
            }
        }

        private bool Matches(int neighbor, Vector3Int pos, ITilemap tilemap)
        {
            switch (neighbor)
            {
                case Neighbor.This: return IsWall(tilemap.GetTile(pos));
                case Neighbor.NotThis: return !IsWall(tilemap.GetTile(pos));
                case Neighbor.Floor: return IsFloor(tilemap.GetTile(pos));
                case Neighbor.NotFloor: return !IsFloor(tilemap.GetTile(pos));
                case Neighbor.Shown: return IsShown(pos, tilemap);
                case Neighbor.Hidden: return !IsShown(pos, tilemap);
                default: return true;
            }
        }

        /// <summary>벽 윗면에서 보이는 쪽: 바닥, 또는 아래가 바닥인 벽(앞면).</summary>
        private bool IsShown(Vector3Int pos, ITilemap tilemap)
        {
            var tile = tilemap.GetTile(pos);
            return IsFloor(tile) || (IsWall(tile) && IsFloor(tilemap.GetTile(pos + Vector3Int.down)));
        }

        private bool IsWall(TileBase tile) => tile == this;
        private bool IsFloor(TileBase tile) => tile != null && tile != this;
    }
}
