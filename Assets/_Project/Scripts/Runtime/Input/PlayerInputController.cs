using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Units;
using UnityEngine;
using UnityEngine.InputSystem;
using EntityId = ProjectM.Core.EntityId; // Unity 6의 UnityEngine.EntityId와 구분

namespace ProjectM.Input
{
    /// <summary>
    /// 키보드 · 마우스 → 플레이어 커맨드 → 시계 (결정 2026-10-16: 8방향, 키보드 + 클릭 이동 둘 다).
    /// 규칙은 PlayerActions가 정하고, 여기서는 입력을 읽어 넘기기만 한다.
    /// - 키보드: 방향키 · WASD · 넘패드. 대각선은 두 방향을 함께 누른다 (W+D = 오른쪽 위, 변경 2026-10-16) — 넘패드 7 · 9 · 1 · 3도 대각선.
    ///   첫 키를 누르면 _comboWindow 동안 두 번째 키를 기다렸다가 합쳐서 한 걸음 (이미 대각선이면 바로).
    ///   누르고 있으면 반복하며, 반복 때마다 지금 눌린 키 조합으로 방향을 다시 정한다. 넘패드 5 · 스페이스 · 마침표 = 제자리 대기.
    /// - 클릭: 그 칸까지 경로를 따라 한 칸씩 여러 턴 진행. 적 칸을 누르면 다가가서 한 번 공격.
    ///   키 입력 · 새 클릭 · 길이 막힘 · 새 적이 보임 → 멈춤. 자기 칸 클릭 = 제자리 대기.
    /// - 주문 (2-2): 숫자 키 1~N = 그 칸의 주문으로 조준 모드. 보이는 가장 가까운 적이 자동 선택, Tab = 다음 적,
    ///   방향키 · WASD · 넘패드 = 조준 칸 한 칸씩, 마우스 = 가리킨 칸, Space · Enter · 클릭 = 시전, Esc · 같은 숫자 키 = 취소.
    ///   조준 중에는 경로 · 맞는 칸을 미리보기로 칠한다 (AimPreviewPresenter). 쓸 수 없으면 이유를 힌트로 보낸다.
    /// 클릭 경로 · 조준 상태는 입력 상태일 뿐 World에 저장하지 않는다.
    /// </summary>
    public sealed class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Grid _grid;
        [SerializeField] private ProjectM.Presentation.AimPreviewPresenter _aimPreview;

        [Header("입력 반복 (연출 시간, 규칙과 무관)")]
        [Tooltip("키를 누르고 있을 때 첫 반복까지 걸리는 시간, 초")]
        [SerializeField] private float _keyRepeatDelay = 0.3f;
        [Tooltip("키 반복 간격, 초")]
        [SerializeField] private float _keyRepeatInterval = 0.12f;
        [Tooltip("클릭 이동 한 칸 간격, 초")]
        [SerializeField] private float _pathStepInterval = 0.08f;
        [Tooltip("첫 방향키 뒤 두 번째 키(대각선)를 기다리는 시간, 초")]
        [SerializeField] private float _comboWindow = 0.07f;

        private static readonly (Key key, GridPos dir)[] DirectionKeys =
        {
            (Key.UpArrow, new GridPos(0, 1)), (Key.W, new GridPos(0, 1)), (Key.Numpad8, new GridPos(0, 1)),
            (Key.RightArrow, new GridPos(1, 0)), (Key.D, new GridPos(1, 0)), (Key.Numpad6, new GridPos(1, 0)),
            (Key.DownArrow, new GridPos(0, -1)), (Key.S, new GridPos(0, -1)), (Key.Numpad2, new GridPos(0, -1)),
            (Key.LeftArrow, new GridPos(-1, 0)), (Key.A, new GridPos(-1, 0)), (Key.Numpad4, new GridPos(-1, 0)),
            (Key.Numpad9, new GridPos(1, 1)), (Key.Numpad3, new GridPos(1, -1)),
            (Key.Numpad1, new GridPos(-1, -1)), (Key.Numpad7, new GridPos(-1, 1)),
        };

        private static readonly Key[] WaitKeys = { Key.Numpad5, Key.Space, Key.Period };

        private static readonly Key[] CastKeys = { Key.Space, Key.Enter, Key.NumpadEnter };

        private World _world;
        private GameConfig _config;
        private ITickClock _clock;
        private ISightRule _sight;
        private SpellBook _spells;
        private ITargetingRule _targeting;

        // 조준 모드 (주문 칸 번호, -1 = 조준 아님)
        private int _aimSlot = -1;
        private GridPos _aimCursor;
        private Vector2 _lastMousePos;

        /// <summary>HUD에 보일 안내 한 줄 (조준 안내 · 시전 불가 이유). 빈 문자열 = 지움.</summary>
        public event System.Action<string> HintChanged;

        public bool IsAiming => _aimSlot >= 0;

        private bool _comboPending;
        private float _comboEndTime;
        private GridPos _comboDirection;
        private float _nextRepeatTime;

        private List<GridPos> _path;
        private int _pathIndex;
        private float _nextPathStepTime;
        private readonly HashSet<EntityId> _enemiesSeenAtPathStart = new HashSet<EntityId>();

        public bool IsFollowingPath => _path != null;

        /// <summary>테스트 · 에디터 Setup용. 씬에서는 인스펙터로 연결한다.</summary>
        public void Configure(Camera camera, Grid grid)
        {
            _camera = camera;
            _grid = grid;
        }

        public void Bind(World world, GameConfig config, ITickClock clock, ISightRule sight, SpellBook spells, ITargetingRule targeting)
        {
            _world = world;
            _config = config;
            _clock = clock;
            _sight = sight;
            _spells = spells;
            _targeting = targeting;
            _comboPending = false;
            CancelPath();
            CancelAim();
        }

        public void Unbind()
        {
            _world = null;
            _clock = null;
            CancelPath();
            CancelAim();
        }

        private void Update()
        {
            if (_world == null) return;
            if (!PlayerActions.TryGetPlayer(_world, out var player))
            {
                CancelPath();
                CancelAim();
                return;
            }

            var keyboard = Keyboard.current;
            var mouseDevice = Mouse.current;
            if (IsAiming)
            {
                HandleAim(player, keyboard, mouseDevice);
                return;
            }
            if (keyboard != null && TryStartAim(player, keyboard)) return;
            if (keyboard != null && HandleKeyboard(keyboard)) return;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && _camera != null && _grid != null)
            {
                OnClick(player, mouse.position.ReadValue());
                return;
            }

            if (_path != null && Time.time >= _nextPathStepTime)
                StepPath();
        }

        // ── 주문 조준 (2-2) ──

        /// <summary>숫자 키로 조준 시작. 칸이 비었거나 마나가 모자라면 힌트만.</summary>
        private bool TryStartAim(Unit player, Keyboard keyboard)
        {
            if (_spells == null) return false;
            int slot = PressedSlotKey(keyboard, player.SpellSlots.Length);
            if (slot < 0) return false;

            CancelPath();
            _comboPending = false;
            var id = player.SpellSlots[slot];
            if (id == null || !_spells.TryGet(id, out var spell))
            {
                Hint($"[{slot + 1}] empty slot");
                return true;
            }
            if (player.Mana < spell.ManaCost)
            {
                Hint($"[{slot + 1}] {spell.Id}: not enough mana ({player.Mana}/{spell.ManaCost})");
                return true;
            }

            _aimSlot = slot;
            _aimCursor = NearestVisibleEnemy(player, spell.Range) ?? player.Pos;
            if (Mouse.current != null) _lastMousePos = Mouse.current.position.ReadValue();
            _nextRepeatTime = Time.time + _keyRepeatDelay;
            RefreshAim(player);
            return true;
        }

        private void HandleAim(Unit player, Keyboard keyboard, Mouse mouse)
        {
            if (keyboard != null)
            {
                int slot = PressedSlotKey(keyboard, player.SpellSlots.Length);
                if (keyboard[Key.Escape].wasPressedThisFrame || slot == _aimSlot)
                {
                    CancelAim();
                    return;
                }
                if (slot >= 0)
                {
                    // 다른 주문으로 바꿔 조준
                    CancelAim();
                    TryStartAim(player, keyboard);
                    return;
                }
                foreach (var key in CastKeys)
                {
                    if (keyboard[key].wasPressedThisFrame)
                    {
                        TryCast(player);
                        return;
                    }
                }
                if (keyboard[Key.Tab].wasPressedThisFrame)
                {
                    _aimCursor = NextVisibleEnemy(player, _aimCursor) ?? _aimCursor;
                    RefreshAim(player);
                    return;
                }

                // 방향키: 조준 칸 한 칸씩 (누르고 있으면 반복)
                var dir = ReadHeldDirection(keyboard, out bool pressedNow);
                if (dir != default && (pressedNow || Time.time >= _nextRepeatTime))
                {
                    var moved = _aimCursor + dir;
                    if (_world.Map.InBounds(moved)) _aimCursor = moved;
                    _nextRepeatTime = Time.time + (pressedNow ? _keyRepeatDelay : _keyRepeatInterval);
                    RefreshAim(player);
                    return;
                }
            }

            if (mouse != null && _camera != null && _grid != null)
            {
                var screen = mouse.position.ReadValue();
                bool clicked = mouse.leftButton.wasPressedThisFrame;
                if (clicked || (screen - _lastMousePos).sqrMagnitude > 0.01f)
                {
                    _lastMousePos = screen;
                    var cell = ScreenToCell(screen);
                    if (_world.Map.InBounds(cell) && cell != _aimCursor)
                    {
                        _aimCursor = cell;
                        RefreshAim(player);
                    }
                    if (clicked) TryCast(player);
                }
            }
        }

        private void TryCast(Unit player)
        {
            var block = SpellCasting.TryCreateCast(_world, _config, _spells, _targeting, _sight, _aimSlot, _aimCursor, out var command);
            if (block != CastBlock.None)
            {
                Hint(AimHint(player) + "  — " + Describe(block));
                return;
            }
            CancelAim();
            _clock.SubmitPlayerCommand(command);
        }

        private void RefreshAim(Unit player)
        {
            var block = SpellCasting.Check(_world, _spells, _targeting, _sight, player, _aimSlot, _aimCursor, out _, out var preview);
            if (_aimPreview != null) _aimPreview.Show(_aimCursor, preview, block == CastBlock.None);
            Hint(block == CastBlock.None ? AimHint(player) : AimHint(player) + "  — " + Describe(block));
        }

        private void CancelAim()
        {
            bool wasAiming = _aimSlot >= 0;
            _aimSlot = -1;
            if (_aimPreview != null) _aimPreview.Hide();
            if (wasAiming) Hint(string.Empty);
        }

        private string AimHint(Unit player)
        {
            var id = player.SpellSlots[_aimSlot];
            string cost = _spells.TryGet(id, out var spell) ? $" ({spell.ManaCost} MP)" : string.Empty;
            return $"[{_aimSlot + 1}] {id}{cost}: Space/Click cast, Tab next target, Esc cancel";
        }

        private static string Describe(CastBlock block)
        {
            switch (block)
            {
                case CastBlock.NotEnoughMana: return "not enough mana";
                case CastBlock.OutOfRange: return "out of range";
                case CastBlock.NotVisible: return "can't see there";
                case CastBlock.TargetIsSelf: return "pick a target";
                case CastBlock.Blocked: return "blocked";
                case CastBlock.EmptySlot: return "empty slot";
                case CastBlock.UnknownSpell: return "unknown spell";
                default: return block.ToString();
            }
        }

        private void Hint(string text) => HintChanged?.Invoke(text);

        /// <summary>이번 프레임에 누른 숫자 키(1~count)의 칸 번호, 없으면 -1.</summary>
        private static int PressedSlotKey(Keyboard keyboard, int count)
        {
            for (int i = 0; i < count && i < 9; i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame) return i;
            }
            return -1;
        }

        /// <summary>보이는 적 목록: 사거리 안 먼저, 그다음 가까운 순, 같으면 EntityId 순.</summary>
        private List<Unit> VisibleEnemies(Unit player, int range)
        {
            var list = new List<Unit>();
            foreach (var unit in _world.Units.ToSortedList())
            {
                if (unit.Faction == player.Faction || !unit.IsAlive) continue;
                if (_sight.CanSee(_world.Map, player.Pos, unit.Pos, player.SightRange)) list.Add(unit);
            }
            list.Sort((a, b) =>
            {
                bool ar = _targeting.InRange(player.Pos, a.Pos, range), br = _targeting.InRange(player.Pos, b.Pos, range);
                if (ar != br) return ar ? -1 : 1;
                int da = GridMovement.Chebyshev(player.Pos, a.Pos), db = GridMovement.Chebyshev(player.Pos, b.Pos);
                if (da != db) return da.CompareTo(db);
                return a.Id.Value.CompareTo(b.Id.Value);
            });
            return list;
        }

        private GridPos? NearestVisibleEnemy(Unit player, int range)
        {
            var list = VisibleEnemies(player, range);
            return list.Count > 0 ? list[0].Pos : (GridPos?)null;
        }

        private GridPos? NextVisibleEnemy(Unit player, GridPos current)
        {
            _spells.TryGet(player.SpellSlots[_aimSlot], out var spell);
            var list = VisibleEnemies(player, spell != null ? spell.Range : 0);
            if (list.Count == 0) return null;
            int index = list.FindIndex(u => u.Pos == current);
            return list[(index + 1) % list.Count].Pos;
        }

        private GridPos ScreenToCell(Vector2 screenPos)
        {
            var world = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_camera.transform.position.z));
            var cell = _grid.WorldToCell(world);
            return new GridPos(cell.x, cell.y);
        }

        /// <returns>키 입력을 처리했으면 true.</returns>
        private bool HandleKeyboard(Keyboard keyboard)
        {
            foreach (var key in WaitKeys)
            {
                if (!keyboard[key].wasPressedThisFrame) continue;
                CancelPath();
                _clock.SubmitPlayerCommand(PlayerActions.Wait(_config));
                return true;
            }

            var held = ReadHeldDirection(keyboard, out bool pressedNow);
            float now = Time.time;

            // 새 키 → 잠깐 기다리며 함께 누른 키를 모은다 (W 다음 D = 오른쪽 위)
            if (pressedNow && !_comboPending)
            {
                _comboPending = true;
                _comboEndTime = now + _comboWindow;
                _comboDirection = default;
                CancelPath();
            }

            if (_comboPending)
            {
                // 기다리는 사이 먼저 뗀 키도 놓치지 않게, 눌린 축만 덮어쓴다
                _comboDirection = new GridPos(held.X != 0 ? held.X : _comboDirection.X, held.Y != 0 ? held.Y : _comboDirection.Y);
                if (now < _comboEndTime && !GridMovement.IsDiagonal(_comboDirection)) return true;

                _comboPending = false;
                if (_comboDirection != default) Step(_comboDirection);
                _nextRepeatTime = now + _keyRepeatDelay;
                return true;
            }

            if (held == default) return false;

            CancelPath();
            if (now >= _nextRepeatTime)
            {
                Step(held);
                _nextRepeatTime = now + _keyRepeatInterval;
            }
            return true;
        }

        /// <summary>눌린 방향키를 모두 합친 방향 (축마다 -1 · 0 · 1, 반대 키끼리는 상쇄). 이번 프레임에 새로 누른 키가 있으면 pressedNow.</summary>
        private static GridPos ReadHeldDirection(Keyboard keyboard, out bool pressedNow)
        {
            pressedNow = false;
            int x = 0, y = 0;
            foreach (var (key, dir) in DirectionKeys)
            {
                var control = keyboard[key];
                if (control.wasPressedThisFrame) pressedNow = true;
                if (!control.isPressed && !control.wasPressedThisFrame) continue;
                x += dir.X;
                y += dir.Y;
            }
            return new GridPos(System.Math.Sign(x), System.Math.Sign(y));
        }

        private void Step(GridPos direction)
        {
            if (PlayerActions.TryStep(_world, _config, direction, out var command))
                _clock.SubmitPlayerCommand(command);
        }

        private void OnClick(Unit player, Vector2 screenPos)
        {
            CancelPath();

            var target = ScreenToCell(screenPos);
            if (!_world.Map.InBounds(target)) return;

            if (target == player.Pos)
            {
                _clock.SubmitPlayerCommand(PlayerActions.Wait(_config));
                return;
            }

            var path = GridPathfinder.FindPath(_world.Map, player.Pos, target, _world.Units.IsOccupied);
            if (path == null || path.Count == 0) return;

            _path = path;
            _pathIndex = 0;
            CollectVisibleEnemies(player, _enemiesSeenAtPathStart);
            StepPath();
        }

        private void StepPath()
        {
            if (!PlayerActions.TryGetPlayer(_world, out var player))
            {
                CancelPath();
                return;
            }

            // 출발 때 없던 적이 보이면 멈춘다.
            var visible = new HashSet<EntityId>();
            CollectVisibleEnemies(player, visible);
            if (!_enemiesSeenAtPathStart.IsSupersetOf(visible))
            {
                CancelPath();
                return;
            }

            bool isLast = _pathIndex == _path.Count - 1;
            if (!PlayerActions.TryStepTo(_world, _config, player, _path[_pathIndex], out var command)
                || (command is AttackCommand && !isLast))
            {
                CancelPath();
                return;
            }

            _clock.SubmitPlayerCommand(command);
            _pathIndex++;
            if (isLast || command is AttackCommand) CancelPath();
            else _nextPathStepTime = Time.time + _pathStepInterval;
        }

        private void CollectVisibleEnemies(Unit player, HashSet<EntityId> result)
        {
            result.Clear();
            foreach (var unit in _world.Units.ToSortedList())
            {
                if (unit.Faction == player.Faction || !unit.IsAlive) continue;
                if (_sight.CanSee(_world.Map, player.Pos, unit.Pos, player.SightRange))
                    result.Add(unit.Id);
            }
        }

        private void CancelPath()
        {
            _path = null;
            _pathIndex = 0;
        }
    }
}
