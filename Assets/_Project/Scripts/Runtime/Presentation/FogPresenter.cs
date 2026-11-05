using ProjectM.Core;
using ProjectM.Units;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 시야 안개 (1-7): 지형 위 Tilemap 한 장. 안 본 칸 = 검정, 본 적 있는 칸 = 어둡게, 지금 보이는 칸 = 안개 없음.
    /// VisibilityChangedEvent만 보고 그리며, 바뀐 칸만 다시 칠한다. 안개 타일은 흰 사각형 + 칸별 색(TileFlags.None).
    /// 부드러운 경계 (2026-10-23 사용자 "SPD처럼 흐릿하게"): _soft가 있으면 Tilemap 대신 칸 하나 = 텍셀 하나인 작은 텍스처를
    /// 이중 선형(Bilinear)으로 늘려 그린다 — 텍셀 가운데 = 칸 가운데라 이웃 칸 사이(칸 반 + 반)에서 안개가 서서히 바뀐다(SPD 방식). 맵 밖 한 줄은 안 본 칸.
    /// 안 본 칸은 번짐 없이 완전히 검정 (2026-10-23 사용자 — 번진 빛에 안 본 벽이 비쳤음): 그 위에 Tilemap이 안 본 칸만 검정으로 덮는다(정렬이 부드러운 안개보다 위).
    /// 그래서 경계의 그러데이션은 보이는 · 본 칸 안쪽에서만 생긴다. → 2026-11-05 사용자 "검정 부분도 SPD처럼 경계선이 흐릿하게": 이 덮개는 _hardUnexplored로만 (기본 끔), 3D에서도 부드러운 안개.
    /// </summary>
    public sealed class FogPresenter : MonoBehaviour
    {
        private enum CellState : byte { Unknown = 0, Unexplored, Explored, Visible }

        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TileBase _fogTile;
        [SerializeField] private Color _unexploredColor = Color.black;
        [SerializeField] private Color _exploredColor = new Color(0f, 0f, 0f, 0.6f);
        [Tooltip("부드러운 안개 (2D · 3D) — 있으면 Tilemap 대신 이 스프라이트에 칸별 텍스처를 늘려 그린다")] [SerializeField] private SpriteRenderer _soft;
        [Tooltip("켜면 안 본 칸은 번짐 없이 Tilemap 검정으로 덮는다 (2026-10-23 2D). 끄면 검정 경계도 흐릿하게 (2026-11-05 사용자 \"SPD처럼 경계선이 흐릿하게\")")]
        [SerializeField] private bool _hardUnexplored;
        [Tooltip("부드러운 안개: 칸 하나를 몇 텍셀로 (클수록 매끈)")] [Range(1, 8)] [SerializeField] private int _softResolution = 4;
        [Tooltip("부드러운 안개: 어두운 칸 경계에서 밝은 칸 안쪽으로 번지는 폭 (칸)")] [Range(0.1f, 2f)] [SerializeField] private float _softFade = 1f;

        private Texture2D _softTexture;
        private Color32[] _softPixels;

        private SimEventBus _bus;
        private CellState[] _states = System.Array.Empty<CellState>();
        private int _width;
        private bool _revealAll;

        /// <summary>
        /// 디버그: 안개를 숨겨 맵 전체를 밝힌다. 안개 타일은 계속 갱신하므로 끄면 지금 시야가 그대로 돌아온다.
        /// 표시만 바꾸며 게임 상태(탐험 기록 등)는 건드리지 않는다.
        /// </summary>
        public bool RevealAll
        {
            get => _revealAll;
            set
            {
                _revealAll = value;
                var renderer = _tilemap != null ? _tilemap.GetComponent<TilemapRenderer>() : null;
                if (renderer != null) renderer.enabled = !value;
                if (_soft != null) _soft.enabled = !value;
            }
        }

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Tilemap tilemap, TileBase fogTile, SpriteRenderer soft = null)
        {
            _tilemap = tilemap;
            _fogTile = fogTile;
            _soft = soft;
        }

        public void Bind(SimEventBus bus)
        {
            Unbind();
            _bus = bus;
            _bus.Subscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Subscribe<VisibilityChangedEvent>(OnVisibilityChanged);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Unsubscribe<VisibilityChangedEvent>(OnVisibilityChanged);
            _bus = null;
        }

        private void OnDestroy()
        {
            Unbind();
            DestroySoft();
        }

        private void OnMapLoaded(MapLoadedEvent e)
        {
            _tilemap.ClearAllTiles();
            _width = e.Width;
            _states = new CellState[e.Width * e.Height];
            if (_soft != null) BuildSoft(e.Width, e.Height);
            for (int i = 0; i < _states.Length; i++)
                Paint(i, CellState.Unexplored);
            ApplySoft();
        }

        private void OnVisibilityChanged(VisibilityChangedEvent e)
        {
            if (e.Width * e.Height != _states.Length) return; // 맵보다 먼저 온 이벤트
            for (int i = 0; i < _states.Length; i++)
            {
                var state = e.Visible[i] ? CellState.Visible : e.Explored[i] ? CellState.Explored : CellState.Unexplored;
                if (state != _states[i]) Paint(i, state);
            }
            ApplySoft();
        }

        private void Paint(int index, CellState state)
        {
            _states[index] = state;
            var pos = new Vector3Int(index % _width, index / _width, 0);
            if (_softPixels != null)
            {
                _softDirty = true; // 텍셀은 ApplySoft가 칸 상태에서 다시 계산
                if (state != CellState.Unexplored || !_hardUnexplored) // _hardUnexplored면 안 본 칸만 Tilemap 검정으로 (부드러운 안개 위)
                {
                    _tilemap.SetTile(pos, null);
                    return;
                }
            }
            if (state == CellState.Visible)
            {
                _tilemap.SetTile(pos, null);
                return;
            }
            _tilemap.SetTile(pos, _fogTile);
            _tilemap.SetTileFlags(pos, TileFlags.None);
            _tilemap.SetColor(pos, state == CellState.Explored ? _exploredColor : _unexploredColor);
        }

        private bool _softDirty;
        private int _softHeight;

        /// <summary>
        /// 맵 크기 + 둘레 한 칸짜리 텍스처 (칸 하나 = _softResolution 텍셀, 이중 선형). 스프라이트 왼쪽 아래 = 칸 (−1, −1)의 왼쪽 아래 (1 칸 = 1 단위).
        /// 2026-11-05: 텍셀 하나 = 칸 하나일 때는 경계가 칸 가운데 ~ 가운데로 번져, 그리지 않는 안 본 칸(3D 바닥 모듈 없음) 앞에서 반쯤 어두운 채 끊겼다 →
        /// 더 잘게 나눠 "어두운 칸까지의 거리"로 칠한다 — 경계에 닿으면 그 칸 어둡기 그대로, 밝은 칸 안쪽 _softFade칸 동안 서서히 밝아짐.
        /// </summary>
        private void BuildSoft(int width, int height)
        {
            DestroySoft();
            int r = Mathf.Max(1, _softResolution);
            int w = (width + 2) * r, h = (height + 2) * r;
            _softHeight = height;
            _softTexture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "SoftFog",
            };
            _softPixels = new Color32[w * h];
            Color32 unexplored = _unexploredColor;
            for (int i = 0; i < _softPixels.Length; i++) _softPixels[i] = unexplored;
            _soft.sprite = Sprite.Create(_softTexture, new Rect(0, 0, w, h), Vector2.zero, r, 0, SpriteMeshType.FullRect);
            _soft.sprite.name = "SoftFog";
            _soft.transform.localPosition = new Vector3(-1f, -1f, _soft.transform.localPosition.z);
            _softDirty = true;
        }

        /// <summary>칸의 어둡기 (0 = 보임 · 본 칸 = _exploredColor.a · 안 본 칸 · 맵 밖 = _unexploredColor.a).</summary>
        private float Darkness(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _softHeight) return _unexploredColor.a;
            var state = _states[y * _width + x];
            return state == CellState.Visible ? 0f : state == CellState.Explored ? _exploredColor.a : _unexploredColor.a;
        }

        private void ApplySoft()
        {
            if (_softTexture == null || !_softDirty) return;
            _softDirty = false;
            int r = Mathf.Max(1, _softResolution);
            int w = (_width + 2) * r, h = (_softHeight + 2) * r;
            float fade = Mathf.Max(0.01f, _softFade);
            Color32 baseColor = _unexploredColor;
            for (int ty = 0; ty < h; ty++)
            {
                float py = (ty + 0.5f) / r - 1f; // 칸 좌표 (칸 y는 [y, y + 1))
                int cy = Mathf.FloorToInt(py);
                for (int tx = 0; tx < w; tx++)
                {
                    float px = (tx + 0.5f) / r - 1f;
                    int cx = Mathf.FloorToInt(px);
                    float d = Darkness(cx, cy);
                    for (int oy = -1; oy <= 1; oy++)
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oy == 0) continue;
                            float v = Darkness(cx + ox, cy + oy);
                            if (v <= d) continue;
                            float dx = Mathf.Max(0f, Mathf.Max(cx + ox - px, px - (cx + ox + 1)));
                            float dy = Mathf.Max(0f, Mathf.Max(cy + oy - py, py - (cy + oy + 1)));
                            float k = 1f - Mathf.Sqrt(dx * dx + dy * dy) / fade;
                            if (k > 0f) d = Mathf.Max(d, v * k);
                        }
                    _softPixels[ty * w + tx] = new Color32(baseColor.r, baseColor.g, baseColor.b, (byte)Mathf.RoundToInt(Mathf.Clamp01(d) * 255f));
                }
            }
            _softTexture.SetPixels32(_softPixels);
            _softTexture.Apply(false);
        }

        private void DestroySoft()
        {
            if (_soft != null && _soft.sprite != null) DestroySafe(_soft.sprite);
            if (_softTexture != null) DestroySafe(_softTexture);
            _softTexture = null;
            _softPixels = null;
        }

        private static void DestroySafe(Object obj)
        {
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
