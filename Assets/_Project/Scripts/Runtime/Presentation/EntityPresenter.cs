using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEngine;
using EntityId = ProjectM.Core.EntityId; // Unity 6의 UnityEngine.EntityId와 구분

namespace ProjectM.Presentation
{
    /// <summary>엔티티 하나의 3D 모습: 칸 중앙 발밑에 놓이는 루트 + 그 아래 렌더러들 (G-1) + 코드 움직임 상태 (G-2b).</summary>
    public sealed class EntityView
    {
        public Transform Root { get; }
        public Renderer[] Renderers { get; }
        /// <summary>지금 그려지는지 (시야 · 디버그 밝히기).</summary>
        public bool Visible { get; internal set; } = true;

        // ── G-2b 움직임 (표시 전용 — 시뮬레이션 상태가 아님) ──
        internal Vector3 From, To;          // 이동 보간 시작 · 끝 (발밑 월드 좌표)
        internal float MoveT = 1f;          // 0 → 1
        internal Vector3 LungeDir;          // 공격 찌르기 방향 (바닥 평면)
        internal float LungeT = 1f;
        internal float HitT = 1f;           // 피격 번쩍 · 흔들림
        internal float CastT = 1f;          // 주문 시전 통통
        internal float DeathT = -1f;        // 0 이상이면 사망 연출 중
        internal float IdlePhase;
        internal bool Flashing;
        internal float WalkUntil = -1f;     // 이 시각까지 걷는 중 (애니메이션 Walk)

        /// <summary>애니메이션 컨트롤러가 붙은 모델 (2026-10-19 — 플레이어 Idle · Walk). 없으면 코드 움직임만.</summary>
        public Animator Animator { get; internal set; }
        /// <summary>지금 걷는 중으로 보이는지 (Walk 재생 · 테스트 확인용).</summary>
        public bool Walking { get; internal set; }

        public EntityView(Transform root, Renderer[] renderers)
        {
            Root = root;
            Renderers = renderers;
        }

        /// <summary>움직임 연출이 모두 끝났는지 (대기 숨쉬기 제외).</summary>
        public bool IsSettled => MoveT >= 1f && LungeT >= 1f && HitT >= 1f && CastT >= 1f && DeathT < 0f;
    }

    /// <summary>
    /// 엔티티 이벤트(Spawned · Moved · Removed) → 3D 모습 (G-1, 결정 2026-10-18).
    /// VisualId의 프리팹(변형이 있으면 ID로 하나 — G-2a)이 있으면 그것, 없으면 그레이박스 캡슐(카탈로그 색 · 키). 충돌체는 붙이지 않는다 (규칙 2).
    /// 시야(VisibilityChangedEvent)를 받은 뒤로는 지금 보이는 칸에 있는 엔티티만 그린다 (1-7). 시야 이벤트가 없으면 전부 그린다.
    /// 상태 이상은 몸 색 틴트 하나 (우선순위 표) — 3D 이펙트는 G-3.
    /// G-2b 코드 움직임 (결정 2026-10-18 — 클립 대신): 이동 = 칸 사이 보간 + 통통 튐 · 공격 = 대상 쪽으로 찌르기 · 피격 = 흰 번쩍 + 흔들림 ·
    /// 주문 시전 = 대상 쪽을 보고 통통 · 사망 = 가라앉으며 작아진 뒤 지움 · 대기 = 숨쉬기. 로직은 기다리지 않는다 — 이벤트가 오면 바로 목표를 바꾸고
    /// 연출이 따라간다 (모두 0.4초 안). 프레임마다 Advance(dt)가 그린다 (규칙은 돌리지 않음).
    /// </summary>
    public sealed class EntityPresenter : MonoBehaviour
    {
        [SerializeField] private Grid _grid;
        [SerializeField] private EntityVisualCatalog _catalog;
        [Tooltip("칸마다 서는 높이 (계단 위 등, G-2b). 비면 바닥 y = 0")] [SerializeField] private TilemapPresenter _ground;
        [Tooltip("그레이박스 캡슐 재질 (URP Lit, 흰색 — 색은 틴트). 비면 Unity 기본 재질 — 빌드에서 마젠타로 깨진다")] [SerializeField] private Material _greyboxMaterial;

        [Header("코드 움직임 (G-2b, 초 · m) — 실시간 모드 1초 안")]
        [SerializeField] [Range(0.02f, 0.5f)] private float _moveDuration = 0.12f;
        [SerializeField] [Range(0f, 0.5f)] private float _moveHop = 0.08f;
        [SerializeField] [Range(0.02f, 0.5f)] private float _lungeDuration = 0.18f;
        [SerializeField] [Range(0f, 0.6f)] private float _lungeDistance = 0.3f;
        [SerializeField] [Range(0.02f, 0.5f)] private float _hitDuration = 0.22f;
        [SerializeField] [Range(0f, 0.3f)] private float _hitShake = 0.06f;
        [Tooltip("피격 순간 밝기 배율")] [SerializeField] [Range(1f, 4f)] private float _hitFlash = 2.2f;
        [SerializeField] [Range(0.02f, 0.5f)] private float _castDuration = 0.25f;
        [SerializeField] [Range(0f, 0.5f)] private float _castHop = 0.12f;
        [SerializeField] [Range(0.05f, 1f)] private float _deathDuration = 0.4f;
        [SerializeField] [Range(0f, 1f)] private float _deathSink = 0.3f;
        [Tooltip("대기 숨쉬기 세기 (세로 배율)")] [SerializeField] [Range(0f, 0.1f)] private float _idleBreath = 0.03f;
        [SerializeField] [Range(0.1f, 6f)] private float _idleSpeed = 2.4f;
        [Tooltip("이보다 먼 이동(칸)은 보간 없이 옮긴다 — 층 이동 · 순간 이동")] [SerializeField] private float _teleportCells = 1.6f;
        [Tooltip("한 칸 이동이 끝난 뒤에도 Walk를 유지하는 시간 (초) — 이어 걸을 때 Idle로 깜빡이지 않게")] [SerializeField] [Range(0f, 1f)] private float _walkHold = 0.2f;

        /// <summary>애니메이션 컨트롤러의 이동 중 bool 파라미터 (VarcoModelSetup이 만든 컨트롤러와 같은 이름).</summary>
        public const string MovingParameter = "Moving";
        private static readonly int MovingHash = Animator.StringToHash(MovingParameter);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private const float DefaultHeight = 0.9f;
        private const float CapsuleWidth = 0.6f;

        private readonly Dictionary<EntityId, EntityView> _views = new Dictionary<EntityId, EntityView>();
        private readonly Dictionary<EntityId, GridPos> _positions = new Dictionary<EntityId, GridPos>();
        private readonly HashSet<EntityId> _dyingIds = new HashSet<EntityId>();
        private readonly List<EntityView> _dying = new List<EntityView>();
        private VisibilityChangedEvent _visibility;
        private SimEventBus _bus;
        private bool _revealAll;
        private float _time;
        private readonly Dictionary<EntityId, Color> _baseTints = new Dictionary<EntityId, Color>();
        // 3-7a: 엔티티가 든 무기 아이템 ID (표시 기억 — 모습을 다시 만들어도 같은 무기를 보인다)
        private readonly Dictionary<EntityId, string> _heldWeapons = new Dictionary<EntityId, string>();

        /// <summary>프리팹 손 아래 지팡이 · 무기 오브젝트 이름 (메뉴 ProjectM/Setup/Varco Models가 붙임).</summary>
        public const string HeldStaffName = "Staff";
        public const string HeldWeaponPrefix = "Weapon_";
        private readonly Dictionary<EntityId, List<StatusType>> _statuses = new Dictionary<EntityId, List<StatusType>>();
        private MaterialPropertyBlock _block;

        public int ViewCount => _views.Count;
        /// <summary>사망 연출 중인 모습 수 (끝나면 지운다).</summary>
        public int DyingCount => _dying.Count;

        /// <summary>디버그: 시야와 상관없이 모든 엔티티를 그린다. 표시만 바꾸며 게임 상태는 건드리지 않는다.</summary>
        public bool RevealAll
        {
            get => _revealAll;
            set
            {
                _revealAll = value;
                foreach (var pair in _views)
                    ApplyVisibility(pair.Key, pair.Value);
            }
        }

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Grid grid, EntityVisualCatalog catalog, TilemapPresenter ground = null)
        {
            _grid = grid;
            _catalog = catalog;
            _ground = ground;
        }

        public bool TryGetView(EntityId id, out EntityView view) => _views.TryGetValue(id, out view);

        /// <summary>칸 중앙의 월드 좌표 (바닥 높이). Grid가 X축 90° 돌아 있으면 바닥 평면(XZ) 위 (G-1).</summary>
        public Vector3 CellCenter(GridPos pos) => _grid.GetCellCenterWorld(new Vector3Int(pos.X, pos.Y, 0));

        /// <summary>그 칸에 선 유닛의 발밑 (칸 중앙 + 서는 높이 — 계단 위 등).</summary>
        public Vector3 StandPoint(GridPos pos)
        {
            var p = CellCenter(pos);
            if (_ground != null) p.y += _ground.StandHeight(pos);
            return p;
        }

        public void Bind(SimEventBus bus)
        {
            Unbind();
            ClearViews();
            _bus = bus;
            _bus.Subscribe<EntitySpawnedEvent>(OnSpawned);
            _bus.Subscribe<EntityMovedEvent>(OnMoved);
            _bus.Subscribe<EntityRemovedEvent>(OnRemoved);
            _bus.Subscribe<VisibilityChangedEvent>(OnVisibilityChanged);
            _bus.Subscribe<StatusChangedEvent>(OnStatusChanged);
            _bus.Subscribe<UnitDamagedEvent>(OnDamaged);
            _bus.Subscribe<AttackMissedEvent>(OnMissed);
            _bus.Subscribe<UnitDiedEvent>(OnDied);
            _bus.Subscribe<SpellCastEvent>(OnSpellCast);
            _bus.Subscribe<ProjectM.Items.WeaponEquippedEvent>(OnWeaponEquipped);
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<EntitySpawnedEvent>(OnSpawned);
            _bus.Unsubscribe<EntityMovedEvent>(OnMoved);
            _bus.Unsubscribe<EntityRemovedEvent>(OnRemoved);
            _bus.Unsubscribe<VisibilityChangedEvent>(OnVisibilityChanged);
            _bus.Unsubscribe<StatusChangedEvent>(OnStatusChanged);
            _bus.Unsubscribe<UnitDamagedEvent>(OnDamaged);
            _bus.Unsubscribe<AttackMissedEvent>(OnMissed);
            _bus.Unsubscribe<UnitDiedEvent>(OnDied);
            _bus.Unsubscribe<SpellCastEvent>(OnSpellCast);
            _bus.Unsubscribe<ProjectM.Items.WeaponEquippedEvent>(OnWeaponEquipped);
            _bus = null;
        }

        private void OnDestroy() => Unbind();

        private void Update() => Advance(Time.deltaTime);

        /// <summary>남은 모습을 모두 지운다 (새 판 · 새 버스에 연결할 때).</summary>
        private void ClearViews()
        {
            foreach (var view in _views.Values)
                if (view != null && view.Root != null) DestroySafe(view.Root.gameObject);
            foreach (var view in _dying)
                if (view.Root != null) DestroySafe(view.Root.gameObject);
            _views.Clear();
            _dying.Clear();
            _dyingIds.Clear();
            _positions.Clear();
            _baseTints.Clear();
            _statuses.Clear();
            _visibility = null;
        }

        private void OnSpawned(EntitySpawnedEvent e)
        {
            if (_views.ContainsKey(e.Id))
            {
                Debug.LogWarning($"[EntityPresenter] 이미 있는 엔티티 {e.Id} 생성 이벤트 — 무시");
                return;
            }

            var root = new GameObject($"{e.VisualId} {e.Id}").transform;
            root.SetParent(transform, false);
            var tint = Color.white;
            if (_catalog != null && _catalog.TryGet(e.VisualId, out var visual))
            {
                var prefab = EntityVisualCatalog.PickPrefab(visual, e.Id.Value);
                if (prefab != null) Instantiate(prefab, root, false); // 텍스처 그대로 (틴트는 상태만)
                else
                {
                    tint = visual.Tint;
                    CreateCapsule(root, visual.Height > 0f ? visual.Height : DefaultHeight);
                }
            }
            else
            {
                Debug.LogWarning($"[EntityPresenter] VisualId '{e.VisualId}'가 카탈로그에 없다 ({e.Id})");
                CreateCapsule(root, DefaultHeight);
            }

            var at = StandPoint(e.Pos);
            root.position = at;
            var view = new EntityView(root, root.GetComponentsInChildren<Renderer>(true))
            {
                From = at,
                To = at,
                IdlePhase = (e.Id.Value * 0.618f) % 1f * Mathf.PI * 2f, // 엔티티마다 숨쉬기 박자를 어긋나게
                Animator = FindAnimator(root),
            };
            _views.Add(e.Id, view);
            _positions[e.Id] = e.Pos;
            _baseTints[e.Id] = tint;
            ApplyTint(e.Id, view);
            ApplyVisibility(e.Id, view);
            if (_heldWeapons.TryGetValue(e.Id, out var held)) ShowHeld(view, held);
        }

        // ── 3-7a 손에 든 무기 ──

        private void OnWeaponEquipped(ProjectM.Items.WeaponEquippedEvent e)
        {
            _heldWeapons[e.Unit] = e.Weapon.ItemId;
            if (_views.TryGetValue(e.Unit, out var view)) ShowHeld(view, e.Weapon.ItemId);
        }

        /// <summary>
        /// 무기를 들면 손의 지팡이를 숨기고 "Weapon_&lt;아이템 ID&gt;"만 켠다 (결정 2026-10-19 (가)). itemId null = 지팡이.
        /// 프리팹에 그 무기 오브젝트가 없으면(그레이박스 · 적) 아무것도 바꾸지 않는다.
        /// </summary>
        public static void ShowHeld(EntityView view, string itemId)
        {
            if (view?.Root == null) return;
            string wanted = itemId != null ? HeldWeaponPrefix + itemId : null;
            Transform staff = null, weapon = null;
            var held = new List<Transform>();
            foreach (var t in view.Root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == HeldStaffName) staff = t;
                else if (t.name.StartsWith(HeldWeaponPrefix)) { held.Add(t); if (t.name == wanted) weapon = t; }
            }
            if (itemId != null && weapon == null) return;
            foreach (var t in held) t.gameObject.SetActive(t == weapon);
            if (staff != null) staff.gameObject.SetActive(weapon == null);
        }

        /// <summary>지금 손에 보이는 무기 아이템 ID (테스트용), 지팡이 · 맨손이면 null.</summary>
        public string HeldWeaponOf(EntityId id)
        {
            if (!_views.TryGetValue(id, out var view) || view.Root == null) return null;
            foreach (var t in view.Root.GetComponentsInChildren<Transform>(false))
                if (t.name.StartsWith(HeldWeaponPrefix)) return t.name.Substring(HeldWeaponPrefix.Length);
            return null;
        }

        /// <summary>컨트롤러가 붙은 Animator (애니메이션 모델). 없으면 null — 그 엔티티는 코드 움직임만.</summary>
        private static Animator FindAnimator(Transform root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == null) continue;
                animator.applyRootMotion = false; // 위치 · 방향은 여기서
                return animator;
            }
            return null;
        }

        /// <summary>그레이박스: 발밑이 바닥에 닿는 캡슐 (충돌체 제거). 재질은 URP Lit — CreatePrimitive 기본 재질은 빌드에서 깨진다.</summary>
        private void CreateCapsule(Transform root, float height)
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var collider = capsule.GetComponent<Collider>();
            if (collider != null) DestroySafe(collider);
            capsule.name = "Greybox";
            capsule.transform.SetParent(root, false);
            capsule.transform.localScale = new Vector3(CapsuleWidth, height * 0.5f, CapsuleWidth); // 기본 캡슐 높이 2
            capsule.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            if (_greyboxMaterial != null) capsule.GetComponent<Renderer>().sharedMaterial = _greyboxMaterial;
        }

        private void OnMoved(EntityMovedEvent e)
        {
            _positions[e.Id] = e.To;
            if (!_views.TryGetValue(e.Id, out var view)) return;
            var to = StandPoint(e.To);
            var dir = CellCenter(e.To) - CellCenter(e.From);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) view.Root.rotation = Quaternion.LookRotation(dir, Vector3.up); // 이동 방향을 바라봄

            var shown = BasePosition(view);
            // 순간 이동 판정은 칸 사이 거리(From → To)로 — 보이는 위치는 앞 걸음 보간 중이라 뒤처져 있어,
            // 그것으로 재면 꾹 누른 직선 이동 뒤 대각선 한 걸음이 1.6칸을 넘어 툭 끊기고 순간 이동했다 (버그 수정 2026-10-19)
            var step = CellCenter(e.To) - CellCenter(e.From);
            step.y = 0f;
            bool teleport = e.From == e.To || step.magnitude > _teleportCells; // 층 이동 · 먼 이동은 바로
            view.From = teleport ? to : shown;
            view.To = to;
            view.MoveT = teleport ? 1f : 0f;
            if (!teleport) view.WalkUntil = _time + _moveDuration + _walkHold; // 걸어서 옮긴 칸 = Walk
            Pose(view);
            ApplyVisibility(e.Id, view);
        }

        private void OnRemoved(EntityRemovedEvent e)
        {
            if (!_views.TryGetValue(e.Id, out var view)) return;
            _views.Remove(e.Id);
            _positions.Remove(e.Id);
            _baseTints.Remove(e.Id);
            _statuses.Remove(e.Id);
            if (_dyingIds.Remove(e.Id) && view.Root != null && view.Visible)
            {
                view.DeathT = 0f; // 죽었으면 가라앉는 연출 뒤 지운다 (보이는 것만)
                _dying.Add(view);
                return;
            }
            if (view.Root != null) DestroySafe(view.Root.gameObject);
        }

        private void OnDied(UnitDiedEvent e) => _dyingIds.Add(e.Id);

        private void OnDamaged(UnitDamagedEvent e)
        {
            if (_views.TryGetValue(e.Target, out var target))
            {
                target.HitT = 0f;
                target.Flashing = true;
            }
            if (e.Cause.Kind != DamageKind.Attack || !e.Attacker.IsValid) return;
            Lunge(e.Attacker, e.Target);
        }

        /// <summary>3-7: 빗나간 공격도 찌르기는 한다 (피격 번쩍은 없음).</summary>
        private void OnMissed(AttackMissedEvent e) => Lunge(e.Attacker, e.Target);

        private void Lunge(EntityId attackerId, EntityId targetId)
        {
            if (!_views.TryGetValue(attackerId, out var attacker)) return;
            if (!_positions.TryGetValue(attackerId, out var from) || !_positions.TryGetValue(targetId, out var to)) return;
            var dir = CellCenter(to) - CellCenter(from);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            attacker.LungeDir = dir.normalized;
            attacker.LungeT = 0f;
            attacker.Root.rotation = Quaternion.LookRotation(attacker.LungeDir, Vector3.up); // 대상을 바라봄
        }

        private void OnSpellCast(SpellCastEvent e)
        {
            if (!_views.TryGetValue(e.Caster, out var caster)) return;
            caster.CastT = 0f;
            if (!_positions.TryGetValue(e.Caster, out var from)) return;
            var dir = CellCenter(e.Target) - CellCenter(from);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) caster.Root.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        // ── G-2b 프레임 연출 ──

        /// <summary>연출을 dt초 진행한다 (Update가 부름 · 테스트는 직접). 표시만 바꾼다.</summary>
        public void Advance(float dt)
        {
            if (dt < 0f) dt = 0f;
            _time += dt;
            foreach (var pair in _views)
            {
                var view = pair.Value;
                Step(view, dt);
                Pose(view);
                UpdateWalk(view);
                if (view.Flashing) ApplyTint(pair.Key, view);
            }
            for (int i = _dying.Count - 1; i >= 0; i--)
            {
                var view = _dying[i];
                Step(view, dt);
                view.DeathT += dt / _deathDuration;
                if (view.DeathT >= 1f || view.Root == null)
                {
                    if (view.Root != null) DestroySafe(view.Root.gameObject);
                    _dying.RemoveAt(i);
                    continue;
                }
                Pose(view);
                ApplyFlash(view, Color.white);
            }
        }

        /// <summary>
        /// 이동 · 공격 · 맞음 · 시전 · 죽음 연출 중 하나라도 진행 중인지 (2026-10-20 — 입력이 이것이 끝날 때까지 다음 행동을 받지 않음).
        /// 걷기 유지 시간 · 숨쉬기 같은 대기 연출은 넣지 않는다.
        /// </summary>
        public bool IsAnimating
        {
            get
            {
                if (_dying.Count > 0) return true;
                foreach (var pair in _views)
                    if (!pair.Value.IsSettled) return true;
                return false;
            }
        }

        /// <summary>연출을 모두 끝 상태로 (테스트 · 디버그).</summary>
        public void FinishMotions() => Advance(10f);

        private void Step(EntityView view, float dt)
        {
            view.MoveT = Mathf.Min(1f, view.MoveT + dt / _moveDuration);
            view.LungeT = Mathf.Min(1f, view.LungeT + dt / _lungeDuration);
            view.HitT = Mathf.Min(1f, view.HitT + dt / _hitDuration);
            view.CastT = Mathf.Min(1f, view.CastT + dt / _castDuration);
        }

        /// <summary>이동 직후 · 이어 걷는 동안 Walk, 멈추면 Idle (애니메이션 모델만 — 컨트롤러 bool Moving).</summary>
        private void UpdateWalk(EntityView view)
        {
            bool walking = _time < view.WalkUntil;
            if (view.Walking == walking) return;
            view.Walking = walking;
            if (view.Animator != null && view.Animator.isActiveAndEnabled) view.Animator.SetBool(MovingHash, walking);
        }

        private static Vector3 BasePosition(EntityView view) => Vector3.Lerp(view.From, view.To, Smooth(view.MoveT));

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        /// <summary>지금 연출 상태로 루트 위치 · 크기를 정한다.</summary>
        private void Pose(EntityView view)
        {
            if (view.Root == null) return;
            var pos = BasePosition(view);
            bool animated = view.Animator != null; // 애니메이션 모델은 통통 튐 · 숨쉬기를 클립(Walk · Idle)이 맡는다
            if (!animated) pos.y += _moveHop * Mathf.Sin(Mathf.PI * view.MoveT);
            pos += view.LungeDir * (_lungeDistance * Mathf.Sin(Mathf.PI * view.LungeT));
            pos.y += _castHop * Mathf.Sin(Mathf.PI * view.CastT);
            if (view.HitT < 1f)
                pos += view.Root.right * (_hitShake * (1f - view.HitT) * Mathf.Sin(view.HitT * 40f));

            float breath = animated ? 0f : _idleBreath * Mathf.Sin(_time * _idleSpeed + view.IdlePhase);
            var scale = new Vector3(1f - breath * 0.5f, 1f + breath, 1f - breath * 0.5f);
            if (view.DeathT >= 0f)
            {
                float t = Smooth(Mathf.Clamp01(view.DeathT));
                scale *= Mathf.Max(0.01f, 1f - t);
                pos.y -= _deathSink * t;
            }
            view.Root.position = pos;
            view.Root.localScale = scale;
        }

        private void OnVisibilityChanged(VisibilityChangedEvent e)
        {
            _visibility = e;
            foreach (var pair in _views)
                ApplyVisibility(pair.Key, pair.Value);
        }

        private void ApplyVisibility(EntityId id, EntityView view)
        {
            if (view == null) return;
            view.Visible = _revealAll || _visibility == null || !_positions.TryGetValue(id, out var pos) || _visibility.IsVisible(pos);
            foreach (var r in view.Renderers)
                if (r != null) r.enabled = view.Visible;
        }

        private static void DestroySafe(Object obj)
        {
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        // ── 상태 이상 틴트 (2-3 그레이박스 표시 — 3D 이펙트는 G-3) ──

        /// <summary>틴트 우선순위: 빙결(3-3a) > 마비 > 화상 > 중독 > 둔화 > 젖음(3-2). 여러 개면 앞의 것 하나만.</summary>
        private static readonly (StatusType type, Color color)[] StatusTints =
        {
            (StatusType.Frozen, new Color(0.85f, 0.97f, 1f)),
            (StatusType.Paralyze, new Color(1f, 0.95f, 0.25f)),
            (StatusType.Burn, new Color(1f, 0.5f, 0.1f)),
            (StatusType.Poison, new Color(0.7f, 0.3f, 1f)),
            (StatusType.Slow, new Color(0.35f, 0.85f, 1f)),
            (StatusType.Wet, new Color(0.25f, 0.45f, 1f)),
        };

        private const float StatusTintStrength = 0.65f;

        public static Color StatusTint(StatusType type)
        {
            foreach (var (t, c) in StatusTints)
                if (t == type) return c;
            return Color.white;
        }

        /// <summary>지금 몸 색 (기본 색 + 상태 틴트) — 테스트 · 디버그용. 피격 번쩍은 포함하지 않는다.</summary>
        public Color CurrentTint(EntityId id)
        {
            var baseTint = _baseTints.TryGetValue(id, out var b) ? b : Color.white;
            if (!_statuses.TryGetValue(id, out var list)) return baseTint;
            foreach (var (type, color) in StatusTints)
                if (list.Contains(type)) return Color.Lerp(baseTint, color, StatusTintStrength);
            return baseTint;
        }

        private void OnStatusChanged(StatusChangedEvent e)
        {
            if (!_statuses.TryGetValue(e.Id, out var list))
            {
                list = new List<StatusType>();
                _statuses[e.Id] = list;
            }
            if (e.Remaining > 0) { if (!list.Contains(e.Type)) list.Add(e.Type); }
            else list.Remove(e.Type);

            if (_views.TryGetValue(e.Id, out var view)) ApplyTint(e.Id, view);
        }

        /// <summary>재질을 복제하지 않고 MaterialPropertyBlock으로 색만 바꾼다 (URP Lit = _BaseColor, 그 밖 = _Color). 피격 중이면 밝게.</summary>
        private void ApplyTint(EntityId id, EntityView view)
        {
            ApplyFlash(view, CurrentTint(id));
            if (view.HitT >= 1f) view.Flashing = false;
        }

        private void ApplyFlash(EntityView view, Color tint)
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            float flash = view.HitT < 1f ? Mathf.Lerp(_hitFlash, 1f, view.HitT) : 1f;
            var color = new Color(tint.r * flash, tint.g * flash, tint.b * flash, tint.a);
            foreach (var r in view.Renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, color);
                _block.SetColor(ColorId, color);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
