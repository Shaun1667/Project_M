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
    /// G-2a: 3D 모듈(Varco 프리팹, 원점 = 칸 바닥 중앙, 1 × 1 m) — 모듈이 있는 타입은 3D로, 없으면 Tilemap 타일로 그린다.
    /// 기둥 모듈은 외톨이 벽(상하좌우에 벽이 없는 벽)에 쓴다 (결정 2026-10-18).
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Visuals/Tile Visual Set", fileName = "TileVisualSet")]
    public sealed class TileVisualSet : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TileType Type;
            public TileBase Tile;
            [Tooltip("3D 모듈 프리팹 (G-2a). 비면 Tilemap 타일")] public GameObject Module;
            [Tooltip("칸마다 90° 단위로 돌려 반복 무늬를 줄인다 (위치로 정함 — 난수 안 씀)")] public bool RandomRotation;
            [Tooltip("이 칸에 선 유닛의 발밑 높이 (m, G-2b) — 올라가는 계단 위 등. 표시만")] public float StandHeight;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();
        [Tooltip("외톨이 벽에 세우는 기둥 모듈 (G-2a). 비면 벽 모듈")] [SerializeField] private GameObject _pillarModule;

        public GameObject PillarModule => _pillarModule;

        public TileBase Get(TileType type)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Type == type) return _entries[i].Tile;
            }
            return null;
        }

        /// <summary>3D 모듈 (없으면 false).</summary>
        public bool TryGetModule(TileType type, out GameObject module, out bool randomRotation)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Type != type) continue;
                module = _entries[i].Module;
                randomRotation = _entries[i].RandomRotation;
                return module != null;
            }
            module = null;
            randomRotation = false;
            return false;
        }

        /// <summary>이 타입 칸에 선 유닛의 발밑 높이 (없으면 0).</summary>
        public float StandHeight(TileType type)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].Type == type) return _entries[i].StandHeight;
            return 0f;
        }

        /// <summary>테스트 · 에디터 Setup용 (G-2b).</summary>
        public void SetStandHeight(TileType type, float height)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Type != type) continue;
                var e = _entries[i];
                e.StandHeight = height;
                _entries[i] = e;
                return;
            }
            _entries.Add(new Entry { Type = type, StandHeight = height });
        }

        /// <summary>테스트 · 에디터 Setup용. 모듈은 건드리지 않는다.</summary>
        public void Set(TileType type, TileBase tile)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Type == type)
                {
                    var e = _entries[i];
                    e.Tile = tile;
                    _entries[i] = e;
                    return;
                }
            }
            _entries.Add(new Entry { Type = type, Tile = tile });
        }

        /// <summary>테스트 · 에디터 Setup용 (G-2a): 3D 모듈 연결.</summary>
        public void SetModule(TileType type, GameObject module, bool randomRotation = false)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Type != type) continue;
                var e = _entries[i];
                e.Module = module;
                e.RandomRotation = randomRotation;
                _entries[i] = e;
                return;
            }
            _entries.Add(new Entry { Type = type, Module = module, RandomRotation = randomRotation });
        }

        public void SetPillarModule(GameObject module) => _pillarModule = module;
    }
}
