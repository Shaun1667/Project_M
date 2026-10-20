using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEngine;
using EntityId = ProjectM.Core.EntityId; // Unity 6의 UnityEngine.EntityId와 구분

namespace ProjectM.Presentation
{
    /// <summary>
    /// 3D 이펙트 (G-3): 시뮬레이션 이벤트만 보고 파티클을 띄운다 — 규칙은 모르고 로직을 기다리지 않는다.
    /// - 지형 효과(TerrainChangedEvent) = 칸 루프, 상태 이상(StatusChangedEvent) = 유닛에 붙는 루프 — 지금 보이는 칸 · 유닛일 때만 켠다.
    /// - 주문(SpellCastEvent) = 지팡이 빛 → 투사체(Bolt · Burst) 또는 번개 선(Beam) → 도착 시 맞는 효과. 투사체가 날아가는 동안
    ///   같은 순간에 온 반응(ReactionTriggeredEvent)도 도착에 맞춰 늦춰 보인다 (최대 0.35초 — 실시간 1초 규칙 안).
    /// - 감전 사슬: 같은 순간의 감전 반응을 차례로 번개 선으로 잇는다.
    /// 무엇을 띄울지는 FxCatalog 표(키 → 프리팹 · 크기 · 색 · 속도 · 길이), 없는 키는 조용히 건너뛴다. 프레임마다 Advance(dt) — 테스트는 직접 부른다.
    /// </summary>
    public sealed class FxPresenter : MonoBehaviour
    {
        [SerializeField] private FxCatalog _catalog;
        [Tooltip("칸 → 월드 · 유닛 모습 (위치 · 보이는지)")] [SerializeField] private EntityPresenter _entities;
        [Tooltip("번개 선 재질 (가산, 흰 텍스처)")] [SerializeField] private Material _lineMaterial;

        [Header("연출 (초 · m)")]
        [Tooltip("투사체 속도 m/s")] [SerializeField] [Min(1f)] private float _projectileSpeed = 18f;
        [Tooltip("투사체 최대 비행 시간 — 이보다 길면 빨라진다")] [SerializeField] [Range(0.05f, 0.6f)] private float _maxTravel = 0.35f;
        [SerializeField] [Range(0.05f, 1f)] private float _lineDuration = 0.25f;
        [SerializeField] [Range(0.01f, 0.4f)] private float _lineWidth = 0.09f;
        [Tooltip("번개 선 꺾임 세기 (m)")] [SerializeField] [Range(0f, 0.5f)] private float _lineJitter = 0.18f;
        [SerializeField] private Color _lightningColor = new Color(1f, 0.92f, 0.35f, 1f);
        [Tooltip("지팡이 끝 · 투사체 높이")] [SerializeField] private float _castHeight = 0.75f;
        [Tooltip("맞는 효과 높이 (몸 가운데)")] [SerializeField] private float _bodyHeight = 0.45f;
        [Tooltip("루프를 끈 뒤 남은 파티클이 사라질 때까지")] [SerializeField] [Range(0f, 3f)] private float _stopFade = 1.2f;

        private const float LoopPrewarm = 0.8f;

        private struct Timed
        {
            public GameObject Go;
            public float Until;
        }

        private sealed class Projectile
        {
            public GameObject Go;
            public Vector3 From, To;
            public float Start, Duration;
        }

        private sealed class Line
        {
            public LineRenderer Renderer;
            public float Start;
        }

        private readonly Dictionary<(GridPos, TerrainEffectType), GameObject> _terrainFx = new Dictionary<(GridPos, TerrainEffectType), GameObject>();
        private readonly Dictionary<(EntityId, StatusType), GameObject> _statusFx = new Dictionary<(EntityId, StatusType), GameObject>();
        private readonly List<(float at, Action action)> _pending = new List<(float, Action)>();
        private readonly List<Projectile> _projectiles = new List<Projectile>();
        private readonly List<Line> _lines = new List<Line>();
        private readonly List<Timed> _timed = new List<Timed>();
        private readonly List<(EntityId, StatusType)> _keyBuffer = new List<(EntityId, StatusType)>();
        private SimEventBus _bus;
        private VisibilityChangedEvent _visibility;
        private bool _revealAll;
        private float _time;
        private float _lastCastTime = -1f, _lastCastDelay;
        private float _lastShockTime = -1f;
        private Vector3 _lastShockPoint;

        public int TerrainFxCount => _terrainFx.Count;
        public int StatusFxCount => _statusFx.Count;
        /// <summary>날아가는 투사체 · 번개 선 · 한 번 재생 중이거나 사라지는 중인 효과 · 대기 중인 효과 수.</summary>
        public int TransientCount => _projectiles.Count + _lines.Count + _timed.Count + _pending.Count;

        /// <summary>디버그: 시야와 상관없이 모든 효과를 보인다 (표시만).</summary>
        public bool RevealAll
        {
            get => _revealAll;
            set
            {
                _revealAll = value;
                RefreshTerrainVisibility();
            }
        }

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(FxCatalog catalog, EntityPresenter entities, Material lineMaterial = null)
        {
            _catalog = catalog;
            _entities = entities;
            _lineMaterial = lineMaterial;
        }

        public bool HasTerrainFx(GridPos pos, TerrainEffectType type) => _terrainFx.ContainsKey((pos, type));
        public bool HasStatusFx(EntityId id, StatusType type) => _statusFx.ContainsKey((id, type));

        public void Bind(SimEventBus bus)
        {
            Unbind();
            ClearAll();
            _bus = bus;
            _bus.Subscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Subscribe<VisibilityChangedEvent>(OnVisibilityChanged);
            _bus.Subscribe<TerrainChangedEvent>(HandleTerrainChanged);
            _bus.Subscribe<StatusChangedEvent>(OnStatusChanged);
            _bus.Subscribe<EntityRemovedEvent>(OnEntityRemoved);
            _bus.Subscribe<SpellCastEvent>(OnSpellCast);
            _bus.Subscribe<ReactionTriggeredEvent>(OnReaction);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<MapLoadedEvent>(OnMapLoaded);
            _bus.Unsubscribe<VisibilityChangedEvent>(OnVisibilityChanged);
            _bus.Unsubscribe<TerrainChangedEvent>(HandleTerrainChanged);
            _bus.Unsubscribe<StatusChangedEvent>(OnStatusChanged);
            _bus.Unsubscribe<EntityRemovedEvent>(OnEntityRemoved);
            _bus.Unsubscribe<SpellCastEvent>(OnSpellCast);
            _bus.Unsubscribe<ReactionTriggeredEvent>(OnReaction);
            _bus = null;
        }

        private void OnDestroy()
        {
            Unbind();
            ClearAll();
        }

        private void Update() => Advance(Time.deltaTime);

        /// <summary>
        /// 주문 연출(날아가는 투사체 · 광선 · 맞을 때까지 기다리는 효과)이 진행 중인지 (2026-10-20 — 입력 대기용).
        /// 터진 뒤 남는 한 번짜리 효과(연기 등) · 지형 · 상태 반복 효과는 넣지 않는다.
        /// </summary>
        public bool IsPlaying => _pending.Count > 0 || _projectiles.Count > 0 || _lines.Count > 0;

        // ── 지형 · 시야 ──

        private void OnMapLoaded(MapLoadedEvent e) => ClearAll(); // 새 판 · 층 이동: 남은 효과를 모두 지운다

        private void OnVisibilityChanged(VisibilityChangedEvent e)
        {
            _visibility = e;
            RefreshTerrainVisibility();
        }

        private bool IsCellVisible(GridPos pos) => _revealAll || _visibility == null || _visibility.IsVisible(pos);

        private void RefreshTerrainVisibility()
        {
            foreach (var pair in _terrainFx)
                if (pair.Value != null) pair.Value.SetActive(IsCellVisible(pair.Key.Item1));
        }

        private void HandleTerrainChanged(TerrainChangedEvent e)
        {
            var key = (e.Pos, e.Type);
            if (e.Remaining <= 0)
            {
                if (_terrainFx.TryGetValue(key, out var old)) StopLoop(old);
                _terrainFx.Remove(key);
                return;
            }
            if (_terrainFx.ContainsKey(key) || _catalog == null || !_catalog.TryGet("terrain." + e.Type, out var entry)) return;
            var go = Spawn(entry, CellCenter(e.Pos), Quaternion.identity, transform, false);
            go.SetActive(IsCellVisible(e.Pos));
            _terrainFx[key] = go;
        }

        // ── 상태 이상 (유닛에 붙음) ──

        private void OnStatusChanged(StatusChangedEvent e)
        {
            var key = (e.Id, e.Type);
            if (e.Remaining <= 0)
            {
                if (_statusFx.TryGetValue(key, out var old)) StopLoop(old);
                _statusFx.Remove(key);
                return;
            }
            if (_statusFx.ContainsKey(key) || _catalog == null || !_catalog.TryGet("status." + e.Type, out var entry)) return;
            if (_entities == null || !_entities.TryGetView(e.Id, out var view) || view.Root == null) return;
            var go = Spawn(entry, view.Root.position, view.Root.rotation, view.Root, false);
            go.SetActive(view.Visible);
            _statusFx[key] = go;
        }

        private void OnEntityRemoved(EntityRemovedEvent e)
        {
            _keyBuffer.Clear();
            foreach (var key in _statusFx.Keys)
                if (key.Item1 == e.Id) _keyBuffer.Add(key);
            foreach (var key in _keyBuffer)
            {
                DestroySafe(_statusFx[key]);
                _statusFx.Remove(key);
            }
        }

        // ── 주문 ──

        private void OnSpellCast(SpellCastEvent e)
        {
            var preview = e.Preview;
            if (_catalog == null || preview == null) return;
            string element = e.Element.ToString();
            var path = preview.Path;
            var affected = preview.AffectedCells;
            var end = path != null && path.Count > 0 ? path[path.Count - 1] : e.Target;

            EntityView caster = null;
            bool hasCaster = _entities != null && _entities.TryGetView(e.Caster, out caster) && caster.Root != null;
            bool casterVisible = hasCaster && caster.Visible;
            var from = (hasCaster ? caster.Root.position : CellCenter(e.Target)) + Vector3.up * _castHeight;
            bool visible = casterVisible || IsCellVisible(end) || IsCellVisible(e.Target);
            if (!visible) return;

            var endPoint = CellCenter(end) + Vector3.up * _bodyHeight;
            var dir = endPoint - from;
            dir.y = 0f;
            var facing = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir.normalized, Vector3.up) : Quaternion.identity;

            if (_catalog.TryGet("cast." + element, out var castFx)) SpawnOnce(castFx, from, facing);

            bool beam = IsBeam(path, affected);
            bool burst = !beam && affected != null && affected.Count > 1;
            float travel = 0f;
            if (beam)
            {
                DrawLightning(from, endPoint, ElementColor(e.Element));
            }
            else if (_catalog.TryGet("projectile." + element, out var projectileFx))
            {
                travel = Mathf.Clamp(Vector3.Distance(from, endPoint) / _projectileSpeed, 0.05f, _maxTravel);
                var go = Spawn(projectileFx, from, facing, transform, false);
                _projectiles.Add(new Projectile { Go = go, From = from, To = endPoint, Start = _time, Duration = travel });
            }

            // 맞는 효과: 융합 전용 키 → 모양별 속성 키
            string impactKey = burst ? "burst." + element : "impact." + element;
            if (_catalog.TryGet("fusion." + e.SpellId, out var impactFx) || _catalog.TryGet(impactKey, out impactFx))
            {
                var at = burst ? CellCenter(end) : endPoint;
                var fx = impactFx;
                Schedule(travel, () => SpawnOnce(fx, at, facing));
            }

            _lastCastTime = _time;
            _lastCastDelay = travel;
        }

        /// <summary>광선(Beam): 맞는 칸이 모두 경로 위 (번개 줄기 · 폭발 마법 융합).</summary>
        private static bool IsBeam(IReadOnlyList<GridPos> path, IReadOnlyList<GridPos> affected)
        {
            if (path == null || affected == null || affected.Count < 2) return false;
            foreach (var cell in affected)
            {
                bool onPath = false;
                foreach (var p in path)
                    if (p == cell) { onPath = true; break; }
                if (!onPath) return false;
            }
            return true;
        }

        // ── 반응 ──

        private void OnReaction(ReactionTriggeredEvent e)
        {
            if (_catalog == null || !IsCellVisible(e.Pos)) return;
            float delay = Mathf.Approximately(_lastCastTime, _time) ? _lastCastDelay : 0f; // 같은 순간의 주문이 도착할 때
            var point = CellCenter(e.Pos) + Vector3.up * _bodyHeight;
            if (e.Target.IsValid && _entities != null && _entities.TryGetView(e.Target, out var view) && view.Root != null)
                point = view.Root.position + Vector3.up * _bodyHeight;

            if (e.ReactionId != null && e.ReactionId.StartsWith("shock"))
            {
                if (Mathf.Approximately(_lastShockTime, _time))
                {
                    var a = _lastShockPoint;
                    var b = point;
                    Schedule(delay, () => DrawLightning(a, b, _lightningColor)); // 감전 사슬: 앞 대상 → 이 대상
                }
                _lastShockTime = _time;
                _lastShockPoint = point;
            }

            if (!_catalog.TryGet("reaction." + e.ReactionId, out var entry)) return;
            var ground = CellCenter(e.Pos);
            var at = entry.Height > 0f ? ground : point; // 바닥 효과(높이 지정)는 칸 바닥 기준
            Schedule(delay, () => SpawnOnce(entry, at, Quaternion.identity));
        }

        // ── 프레임 진행 ──

        /// <summary>연출을 dt초 진행한다 (Update가 부름 · 테스트는 직접). 표시만 바꾼다.</summary>
        public void Advance(float dt)
        {
            if (dt < 0f) dt = 0f;
            _time += dt;

            for (int i = 0; i < _pending.Count; i++)
            {
                if (_pending[i].at > _time) continue;
                var action = _pending[i].action;
                _pending.RemoveAt(i--);
                action();
            }

            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                var p = _projectiles[i];
                float t = p.Duration > 0f ? Mathf.Clamp01((_time - p.Start) / p.Duration) : 1f;
                if (p.Go != null) p.Go.transform.position = Vector3.Lerp(p.From, p.To, t);
                if (t < 1f) continue;
                StopLoop(p.Go);
                _projectiles.RemoveAt(i);
            }

            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                var line = _lines[i];
                float t = (_time - line.Start) / _lineDuration;
                if (line.Renderer == null || t >= 1f)
                {
                    if (line.Renderer != null) DestroySafe(line.Renderer.gameObject);
                    _lines.RemoveAt(i);
                    continue;
                }
                float w = _lineWidth * (1f - t);
                line.Renderer.startWidth = w;
                line.Renderer.endWidth = w;
            }

            for (int i = _timed.Count - 1; i >= 0; i--)
            {
                if (_timed[i].Go != null && _timed[i].Until > _time) continue;
                if (_timed[i].Go != null) DestroySafe(_timed[i].Go);
                _timed.RemoveAt(i);
            }

            if (_entities == null) return;
            foreach (var pair in _statusFx)
            {
                if (pair.Value == null) continue;
                bool show = _entities.TryGetView(pair.Key.Item1, out var view) && view.Visible;
                if (pair.Value.activeSelf != show) pair.Value.SetActive(show);
            }
        }

        /// <summary>대기 중인 효과 · 투사체 · 선 · 한 번 재생을 모두 끝낸다 (테스트 · 디버그). 루프(지형 · 상태)는 남는다.</summary>
        public void FinishTransients()
        {
            Advance(10f); // 도착 · 끄기
            Advance(10f); // 끈 루프 · 한 번 재생 지우기
        }

        // ── 도우미 ──

        private Vector3 CellCenter(GridPos pos) => _entities != null ? _entities.CellCenter(pos) : new Vector3(pos.X + 0.5f, 0f, pos.Y + 0.5f);

        private void Schedule(float delay, Action action)
        {
            if (delay <= 0f) action();
            else _pending.Add((_time + delay, action));
        }

        private void SpawnOnce(FxCatalog.Entry entry, Vector3 position, Quaternion rotation)
        {
            var go = Spawn(entry, position, rotation, transform, true);
            _timed.Add(new Timed { Go = go, Until = _time + (entry.Lifetime > 0f ? entry.Lifetime : 1f) });
        }

        /// <summary>프리팹을 놓고 크기 · 속도 · 색을 맞춘다. once = 반복을 끄고 한 번만 낸다 (팩 효과가 모두 반복 데모 설정).</summary>
        private static GameObject Spawn(FxCatalog.Entry entry, Vector3 position, Quaternion rotation, Transform parent, bool once)
        {
            var go = Instantiate(entry.Prefab, parent);
            go.name = entry.Key;
            go.transform.SetPositionAndRotation(position + Vector3.up * entry.Height, rotation);
            go.transform.localScale = Vector3.one * (entry.Scale > 0f ? entry.Scale : 1f);
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var ps in systems)
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy; // 루트 크기를 따름
                main.simulationSpeed *= entry.Speed > 0f ? entry.Speed : 1f;
                if (once) main.loop = false;
                if (entry.Tint.a > 0f) main.startColor = Tinted(main.startColor, entry.Tint);
            }
            // 루프(지형 · 상태 · 투사체)는 이미 차 있는 모습으로 시작 — 연기 · 불씨가 놓인 순간부터 보이게
            foreach (var ps in systems)
            {
                if (!once) ps.Simulate(LoopPrewarm, false, true);
                ps.Play(false);
            }
            return go;
        }

        private static ParticleSystem.MinMaxGradient Tinted(ParticleSystem.MinMaxGradient color, Color tint)
        {
            switch (color.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(Multiply(color.color, tint));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(Multiply(color.colorMin, tint), Multiply(color.colorMax, tint));
                default:
                    return color; // 그라디언트는 그대로 (팩 효과는 대부분 단색 · 두 색)
            }
        }

        /// <summary>밝기는 원래 색에서, 색조는 틴트에서 — 어두운 회색(가산 재질용) 원색에도 틴트가 살아나게.</summary>
        private static Color Multiply(Color source, Color tint)
        {
            float brightness = Mathf.Max(source.r, Mathf.Max(source.g, source.b));
            return new Color(tint.r * brightness, tint.g * brightness, tint.b * brightness, source.a * tint.a);
        }

        private void StopLoop(GameObject go)
        {
            if (go == null) return;
            go.transform.SetParent(transform, true); // 유닛이 지워져도 남은 파티클이 자연스럽게 사라지게
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            _timed.Add(new Timed { Go = go, Until = _time + _stopFade });
        }

        private void DrawLightning(Vector3 from, Vector3 to, Color color)
        {
            var go = new GameObject("Lightning");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = _lineMaterial;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = _lineWidth;
            line.endWidth = _lineWidth;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            // 꺾인 번개: 칸 1m마다 마디 하나, 끝점은 그대로. 흔들림은 시간 · 위치로 정한 값 (난수 아님 — 규칙 7)
            int segments = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(from, to) * 2f), 2, 24);
            line.positionCount = segments + 1;
            var side = Vector3.Cross((to - from).normalized, Vector3.up);
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                var p = Vector3.Lerp(from, to, t);
                if (i > 0 && i < segments)
                {
                    float wave = Mathf.Sin((i + 1) * 12.9898f + (from.x + to.z) * 78.233f + _time * 37.719f);
                    float lift = Mathf.Sin((i + 3) * 4.1414f + (from.z - to.x) * 21.17f);
                    p += side * (wave * _lineJitter) + Vector3.up * (lift * _lineJitter * 0.5f);
                }
                line.SetPosition(i, p);
            }
            _lines.Add(new Line { Renderer = line, Start = _time });
        }

        private Color ElementColor(Element element)
        {
            if (_catalog != null && _catalog.TryGet("line." + element, out var entry) && entry.Tint.a > 0f) return entry.Tint;
            return _lightningColor;
        }

        private void ClearAll()
        {
            foreach (var go in _terrainFx.Values) DestroySafe(go);
            foreach (var go in _statusFx.Values) DestroySafe(go);
            foreach (var p in _projectiles) DestroySafe(p.Go);
            foreach (var l in _lines) if (l.Renderer != null) DestroySafe(l.Renderer.gameObject);
            foreach (var t in _timed) DestroySafe(t.Go);
            _terrainFx.Clear();
            _statusFx.Clear();
            _projectiles.Clear();
            _lines.Clear();
            _timed.Clear();
            _pending.Clear();
            _visibility = null;
            _lastCastTime = -1f;
            _lastShockTime = -1f;
        }

        private static void DestroySafe(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
