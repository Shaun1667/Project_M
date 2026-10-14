using System;
using System.Collections.Generic;
using ProjectM.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// TileType → 화면 타일. 그래픽 방식(2.5D / 2D 픽셀)을 바꿀 때는 이 에셋만 바꾼다.
    /// 등록되지 않은 타입(Empty 포함)은 그리지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Visuals/Tile Visual Set", fileName = "TileVisualSet")]
    public sealed class TileVisualSet : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TileType Type;
            public TileBase Tile;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public TileBase Get(TileType type)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Type == type) return _entries[i].Tile;
            }
            return null;
        }

        /// <summary>테스트 · 에디터 Setup용.</summary>
        public void Set(TileType type, TileBase tile)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Type == type)
                {
                    _entries[i] = new Entry { Type = type, Tile = tile };
                    return;
                }
            }
            _entries.Add(new Entry { Type = type, Tile = tile });
        }
    }
}
