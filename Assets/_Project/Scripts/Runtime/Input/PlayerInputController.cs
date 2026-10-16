using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Units;
using UnityEngine;
using UnityEngine.InputSystem;
using EntityId = ProjectM.Core.EntityId; // Unity 6의 UnityEngine.EntityId와 구분

namespace ProjectM.Input
{
    /// <summary>
    /// 키보드 · 마우스 → 플레이어 커맨드 → 시계 (결정 2026-10-16: 8방향, 키보드 + 클릭 이동 둘 다).
    /// 규칙은 PlayerActions가 정하고, 여기서는 입력을 읽어 넘기기만 한다.
    /// - 키보드: 방향키 · WASD(4방향), 넘패드 1~9 · Q/E/Z/C(대각선 포함 8방향). 누르고 있으면 반복. 넘패드 5 · 스페이스 · 마침표 = 제자리 대기.
    /// - 클릭: 그 칸까지 경로를 따라 한 칸씩 여러 턴 진행. 적 칸을 누르면 다가가서 한 번 공격.
    ///   키 입력 · 새 클릭 · 길이 막힘 · 새 적이 보임 → 멈춤. 자기 칸 클릭 = 제자리 대기.
    /// 클릭 경로는 입력 상태일 뿐 World에 저장하지 않는다.
    /// </summary>
    public sealed class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Grid _grid;

        [Header("입력 반복 (연출 시간, 규칙과 무관)")]
        [Tooltip("키를 누르고 있을 때 첫 반복까지 걸리는 시간, 초")]
        [SerializeField] private float _keyRepeatDelay = 0.3f;
        [Tooltip("키 반복 간격, 초")]
        [SerializeField] private float _keyRepeatInterval = 0.12f;
        [Tooltip("클릭 이동 한 칸 간격, 초")]
        [SerializeField] private float _pathStepInterval = 0.08f;

        private static readonly (Key key, GridPos dir)[] DirectionKeys =
        {
            (Key.UpArrow, new GridPos(0, 1)), (Key.W, new GridPos(0, 1)), (Key.Numpad8, new GridPos(0, 1)),
            (Key.Numpad9, new GridPos(1, 1)), (Key.E, new GridPos(1, 1)),
            (Key.RightArrow, new GridPos(1, 0)), (Key.D, new GridPos(1, 0)), (Key.Numpad6, new GridPos(1, 0)),
            (Key.Numpad3, new GridPos(1, -1)), (Key.C, new GridPos(1, -1)),
            (Key.DownArrow, new GridPos(0, -1)), (Key.S, new GridPos(0, -1)), (Key.Numpad2, new GridPos(0, -1)),
            (Key.Numpad1, new GridPos(-1, -1)), (Key.Z, new GridPos(-1, -1)),
            (Key.LeftArrow, new GridPos(-1, 0)), (Key.A, new GridPos(-1, 0)), (Key.Numpad4, new GridPos(-1, 0)),
            (Key.Numpad7, new GridPos(-1, 1)), (Key.Q, new GridPos(-1, 1)),
        };

        private static readonly Key[] WaitKeys = { Key.Numpad5, Key.Space, Key.Period };

        private World _world;
        private GameConfig _config;
        private ITickClock _clock;
        private ISightRule _sight;

        private GridPos? _heldDirection;
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

        public void Bind(World world, GameConfig config, ITickClock clock, ISightRule sight)
        {
            _world = world;
            _config = config;
            _clock = clock;
            _sight = sight;
            _heldDirection = null;
            CancelPath();
        }

        public void Unbind()
        {
            _world = null;
            _clock = null;
            CancelPath();
        }

        private void Update()
        {
            if (_world == null) return;
            if (!PlayerActions.TryGetPlayer(_world, out var player))
            {
                CancelPath();
                return;
            }

            var keyboard = Keyboard.current;
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

            var direction = ReadDirection(keyboard, out bool pressedNow);
            if (!direction.HasValue)
            {
                _heldDirection = null;
                return false;
            }

            CancelPath();
            float now = Time.time;
            if (pressedNow || _heldDirection != direction)
            {
                Step(direction.Value);
                _nextRepeatTime = now + _keyRepeatDelay;
            }
            else if (now >= _nextRepeatTime)
            {
                Step(direction.Value);
                _nextRepeatTime = now + _keyRepeatInterval;
            }
            _heldDirection = direction;
            return true;
        }

        /// <summary>이번 프레임에 새로 누른 방향이 있으면 그것, 없으면 누르고 있는 방향 (표 순서대로 첫 번째).</summary>
        private static GridPos? ReadDirection(Keyboard keyboard, out bool pressedNow)
        {
            foreach (var (key, dir) in DirectionKeys)
            {
                if (keyboard[key].wasPressedThisFrame)
                {
                    pressedNow = true;
                    return dir;
                }
            }

            pressedNow = false;
            foreach (var (key, dir) in DirectionKeys)
            {
                if (keyboard[key].isPressed) return dir;
            }
            return null;
        }

        private void Step(GridPos direction)
        {
            if (PlayerActions.TryStep(_world, _config, direction, out var command))
                _clock.SubmitPlayerCommand(command);
        }

        private void OnClick(Unit player, Vector2 screenPos)
        {
            CancelPath();

            var world = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_camera.transform.position.z));
            var cell = _grid.WorldToCell(world);
            var target = new GridPos(cell.x, cell.y);
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
