using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 픽셀 그래픽 시험 (P-0, 2026-10-20 — 64px · 위에서 비스듬히). 게임 규칙과 연결하지 않은 보기 전용 장면:
    /// 실제 던전 생성기로 만든 맵을 64px 타일로 칠하고(바닥 · 벽 앞면 · 벽 윗면 — SPD처럼 아래가 바닥인 벽만 앞면),
    /// 플레이어 · 적 · 바닥 아이템 스프라이트를 놓는다. 방향키 · WASD = 플레이어 한 칸 이동(벽 막힘만), 휠 = 확대 배율, R = 다른 시드.
    /// 카메라는 정사영이고 1 텍셀 = 정수 화면 픽셀 (픽셀이 번지지 않게). 씬은 메뉴 ProjectM/Setup/Pixel Test Scene이 만든다.
    /// </summary>
    public sealed class PixelTestView : MonoBehaviour
    {
        [SerializeField] private DungeonGenConfigAsset _dungeonConfig;
        [SerializeField] private Camera _camera;
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private Material _spriteMaterial;
        [SerializeField] private Sprite _floor;
        [SerializeField] private Sprite _wall;
        [SerializeField] private Sprite _wallTop;
        [SerializeField] private Sprite _player;
        [SerializeField] private Sprite _enemy;
        [SerializeField] private Sprite[] _items = new Sprite[0];
        [SerializeField] private int _enemyCount = 8;
        [SerializeField] private int _itemCount = 10;
        [SerializeField] private ulong _seed = 1;
        [Tooltip("1 텍셀 = 몇 화면 픽셀 (0 = 화면 높이에 약 9칸이 보이게 자동)")] [SerializeField] private int _zoom;
        [SerializeField] private int _pixelsPerUnit = 64;

        private GridMap _map;
        private Transform _playerView;
        private GridPos _playerPos;
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private Tile _floorTile, _wallTile, _wallTopTile;

        private void Start() => Build();

        /// <summary>맵 · 스프라이트를 다시 만든다 (에디터 미리보기에서도 부름 — 플레이 중이 아니면 즉시 지움).</summary>
        public void Build()
        {
            foreach (var go in _spawned)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            _spawned.Clear();
            _tilemap.ClearAllTiles();
            _floorTile = MakeTile(_floor);
            _wallTile = MakeTile(_wall);
            _wallTopTile = MakeTile(_wallTop);

            var generator = new DungeonGenerator(_dungeonConfig.ToConfig());
            DungeonFloor floor = null;
            var world = World.Create(_seed, rng =>
            {
                floor = generator.Generate(rng, stairsUp: false, stairsDown: true);
                return floor.Map;
            });
            _map = floor.Map;

            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                {
                    var pos = new GridPos(x, y);
                    var cell = new Vector3Int(x, y, 0);
                    if (_map.IsWalkable(pos)) _tilemap.SetTile(cell, _floorTile);
                    else if (_map.Get(pos) == TileType.Wall)
                        _tilemap.SetTile(cell, _map.IsWalkable(new GridPos(x, y - 1)) ? _wallTile : _wallTopTile); // 아래가 바닥이면 앞면이 보임
                }

            var free = new List<GridPos>();
            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                    if (_map.Get(new GridPos(x, y)) == TileType.Floor && new GridPos(x, y) != floor.Start) free.Add(new GridPos(x, y));
            var rngLoot = world.Rng.Get(RngStream.Loot);
            for (int i = 0; i < _itemCount && free.Count > 0 && _items.Length > 0; i++)
                Spawn(_items[i % _items.Length], Take(free, rngLoot), 0.5f, "item");
            for (int i = 0; i < _enemyCount && free.Count > 0; i++)
                Spawn(_enemy, Take(free, rngLoot), 0.04f, "enemy");

            _playerPos = floor.Start;
            _playerView = Spawn(_player, _playerPos, 0.04f, "player").transform;
            ApplyZoom();
            Follow();
        }

        private static GridPos Take(List<GridPos> cells, IRng rng)
        {
            int i = rng.Range(0, cells.Count);
            var p = cells[i];
            cells.RemoveAt(i);
            return p;
        }

        private Tile MakeTile(Sprite sprite)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            return tile;
        }

        /// <summary>스프라이트 하나 (발밑 기준 — 아이템은 칸 가운데). 아래 칸일수록 앞에 그린다.</summary>
        private GameObject Spawn(Sprite sprite, GridPos pos, float yOffset, string name)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (_spriteMaterial != null) sr.sharedMaterial = _spriteMaterial;
            go.transform.position = CellPoint(pos, yOffset);
            sr.sortingOrder = 1000 - pos.Y * 10 + (name == "item" ? 0 : 5);
            _spawned.Add(go);
            return go;
        }

        private static Vector3 CellPoint(GridPos pos, float yOffset) => new Vector3(pos.X + 0.5f, pos.Y + yOffset, 0f);

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _map == null) return;
            if (keyboard.rKey.wasPressedThisFrame)
            {
                _seed++;
                Build();
                return;
            }
            var dir = new GridPos(0, 0);
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) dir = new GridPos(0, 1);
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) dir = new GridPos(0, -1);
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) dir = new GridPos(-1, 0);
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) dir = new GridPos(1, 0);
            if (dir != new GridPos(0, 0))
            {
                var to = _playerPos + dir;
                if (GridMovement.CanStep(_map, _playerPos, to))
                {
                    _playerPos = to;
                    _playerView.position = CellPoint(to, 0.04f);
                    _playerView.GetComponent<SpriteRenderer>().sortingOrder = 1005 - to.Y * 10;
                    if (dir.X != 0) _playerView.GetComponent<SpriteRenderer>().flipX = dir.X < 0;
                }
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0f)
                {
                    _zoom = Mathf.Clamp(CurrentZoom() + (scroll > 0 ? 1 : -1), 1, 8);
                    ApplyZoom();
                }
            }
            ApplyZoom();
            Follow();
        }

        private int CurrentZoom() => _zoom > 0 ? _zoom : Mathf.Max(1, Mathf.RoundToInt(Screen.height / (9f * _pixelsPerUnit)));

        /// <summary>정수 배율: 화면 높이 = 2 × size × PPU × 배율.</summary>
        private void ApplyZoom() => ApplyZoom(Screen.height);

        /// <summary>화면 높이 screenHeight 픽셀 기준 (에디터 미리보기 렌더는 렌더 텍스처 높이).</summary>
        public void ApplyZoom(int screenHeight) => _camera.orthographicSize = screenHeight / (2f * _pixelsPerUnit * CurrentZoom());

        /// <summary>카메라를 플레이어에 — 화면 픽셀 격자에 맞춰 반올림 (픽셀 흔들림 방지).</summary>
        private void Follow()
        {
            if (_playerView == null) return;
            float unitsPerScreenPixel = 1f / (_pixelsPerUnit * CurrentZoom());
            var p = _playerView.position + new Vector3(0f, 0.5f, 0f);
            _camera.transform.position = new Vector3(Mathf.Round(p.x / unitsPerScreenPixel) * unitsPerScreenPixel,
                Mathf.Round(p.y / unitsPerScreenPixel) * unitsPerScreenPixel, -10f);
        }
    }
}
