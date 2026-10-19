using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.UI;
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
    /// - 빠른 칸 (3-2a): 숫자 키 1~N · HUD 칸 클릭. 주문 = 조준 모드, 아이템 = 사용, 빈 칸 = 인벤토리가 열려 그 칸에 등록할 항목 고르기.
    /// - 조준 (2-2): 보이는 가장 가까운 적이 자동 선택, Tab = 다음 적, 방향키 · WASD · 넘패드 = 조준 칸 한 칸씩, 마우스 = 가리킨 칸,
    ///   Space · Enter · 클릭 = 시전, Esc · 같은 숫자 키 = 취소. 미리보기는 AimPreviewPresenter. 쓸 수 없으면 이유를 힌트로 보낸다.
    /// - 인벤토리 (3-2a): I 키로 열고 닫는다. 열려 있는 동안 다른 입력은 받지 않는다 (차례도 안 감) — 규칙은 InventoryMenu.
    /// - 계단 (3-4a): 계단 칸으로는 걸어가기만 한다. 계단 위에서 Enter · 자기 칸 클릭 = 층 이동 (틱 안 씀, FloorTransition).
    /// - 마법 융합 (3-2b): 융합기 사용(빠른 칸 · 인벤토리 "사용") → 인벤토리에서 주문 두 개 → 융합 조준. 판정 · 시전은 FusionCasting.
    /// 클릭 경로 · 조준 · 인벤토리 창 상태는 입력 상태일 뿐 World에 저장하지 않는다.
    /// </summary>
    public sealed class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Grid _grid;
        [SerializeField] private ProjectM.Presentation.AimPreviewPresenter _aimPreview;
        [Tooltip("빠른 칸 클릭 판정 · 강조 (3-2a)")] [SerializeField] private HudPresenter _hud;
        [Tooltip("인벤토리 창 (3-2a)")] [SerializeField] private InventoryPresenter _inventoryView;

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
        private ItemBook _items;
        private FusionBook _fusions;
        private ProjectM.Dungeon.FloorTransition _stairs;
        private bool _stairsHintShown;

        private static readonly Key[] StairsKeys = { Key.Enter, Key.NumpadEnter };

        // 조준 모드: 주문 ID (null = 조준 아님) · 빠른 칸 번호 (-1 = 인벤토리에서 바로)
        // 융합 조준 (3-2b): _aimSpellId = 첫 주문, _aimFusionSecond = 둘째 주문 (null = 일반 주문)
        private string _aimSpellId;
        private string _aimFusionSecond;
        private int _aimSlot = -1;
        private GridPos _aimCursor;
        private Vector2 _lastMousePos;

        private readonly InventoryMenu _inventory = new InventoryMenu();

        /// <summary>HUD에 보일 안내 한 줄 (조준 안내 · 시전 불가 이유 · 인벤토리 안내). 빈 문자열 = 지움.</summary>
        public event System.Action<string> HintChanged;

        public bool IsAiming => _aimSpellId != null;
        public InventoryMenu Inventory => _inventory;

        /// <summary>안내 문구 (한글 · 주문 · 아이템 표시 이름). GameRoot가 Catalog 이름으로 채운다.</summary>
        public HudText Text { get; set; } = new HudText();

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

        private void Awake() => _inventory.Changed += DrawInventory;

        private void OnDestroy() => _inventory.Changed -= DrawInventory;

        public void Bind(World world, GameConfig config, ITickClock clock, ISightRule sight, SpellBook spells, ITargetingRule targeting,
            ItemBook items, FusionBook fusions, ProjectM.Dungeon.FloorTransition stairs = null)
        {
            Unbind();
            _world = world;
            _config = config;
            _clock = clock;
            _sight = sight;
            _spells = spells;
            _targeting = targeting;
            _items = items ?? ItemBook.Empty;
            _fusions = fusions ?? FusionBook.Empty;
            _stairs = stairs;
            _stairsHintShown = false;
            _comboPending = false;
            _world.Events.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
            _world.Events.Subscribe<QuickSlotsChangedEvent>(OnQuickSlotsChanged);
            _world.Events.Subscribe<ProjectM.Dungeon.FloorChangedEvent>(OnFloorChanged);
            _world.Events.Subscribe<EntityMovedEvent>(OnEntityMoved);
        }

        /// <summary>층이 바뀌면 (3-4a) 이전 층 기준의 클릭 경로 · 조준을 버린다.</summary>
        private void OnFloorChanged(ProjectM.Dungeon.FloorChangedEvent e)
        {
            CancelPath();
            CancelAim();
            RefreshStairsHint();
        }

        private void OnEntityMoved(EntityMovedEvent e)
        {
            if (PlayerActions.TryGetPlayer(_world, out var player) && e.Id == player.Id) RefreshStairsHint();
        }

        // ── 계단 (3-4a, 결정 2026-10-18): 계단 칸으로는 걸어가기만, 그 위에서 한 번 더(Enter · 자기 칸 클릭) = 층 이동, 틱 안 씀 ──

        /// <summary>계단 위면 안내를 띄우고, 계단에서 내려서면 그 안내만 지운다.</summary>
        private void RefreshStairsHint()
        {
            int dir = _stairs != null ? _stairs.StairsDirection() : 0;
            if (dir != 0)
            {
                Hint(HudText.StairsHint(dir > 0));
                _stairsHintShown = true;
            }
            else if (_stairsHintShown)
            {
                Hint(string.Empty);
                _stairsHintShown = false;
            }
        }

        private bool TryUseStairs()
        {
            if (_stairs == null || !_stairs.TryUse()) return false;
            CancelPath();
            _world.Events.Flush(); // 틱 밖 변경 — 바로 화면에
            return true;
        }

        public void Unbind()
        {
            CancelPath();
            CancelAim();
            _inventory.Close();
            if (_world != null)
            {
                _world.Events.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
                _world.Events.Unsubscribe<QuickSlotsChangedEvent>(OnQuickSlotsChanged);
                _world.Events.Unsubscribe<ProjectM.Dungeon.FloorChangedEvent>(OnFloorChanged);
                _world.Events.Unsubscribe<EntityMovedEvent>(OnEntityMoved);
            }
            _world = null;
            _clock = null;
        }

        private void Update()
        {
            if (_world == null) return;
            if (!PlayerActions.TryGetPlayer(_world, out var player))
            {
                CancelPath();
                CancelAim();
                _inventory.Close();
                return;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (_inventory.IsOpen)
            {
                HandleInventory(player, keyboard, mouse);
                return;
            }
            if (keyboard != null && keyboard[Key.I].wasPressedThisFrame)
            {
                OpenInventory();
                return;
            }
            if (IsAiming)
            {
                HandleAim(player, keyboard, mouse);
                return;
            }
            if (keyboard != null)
            {
                int slot = PressedSlotKey(keyboard, player.QuickSlots.Length);
                if (slot >= 0)
                {
                    UseQuickSlot(player, slot);
                    return;
                }
                if (HandleKeyboard(keyboard)) return;
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                var screen = mouse.position.ReadValue();
                int slot = _hud != null ? _hud.QuickSlotAt(screen) : -1;
                if (slot >= 0) UseQuickSlot(player, slot);
                else if (_camera != null && _grid != null) OnClick(player, screen);
                return;
            }

            if (_path != null && Time.time >= _nextPathStepTime)
                StepPath();
        }

        // ── 빠른 칸 (3-2a) ──

        /// <summary>빠른 칸 사용: 주문 = 조준, 아이템 = 사용, 빈 칸 = 인벤토리에서 등록할 항목 고르기.</summary>
        private void UseQuickSlot(Unit player, int slot)
        {
            CancelPath();
            _comboPending = false;
            var entry = player.QuickSlots[slot];
            if (entry.IsEmpty)
            {
                _inventory.OpenForAssign(slot);
                Hint(HudText.AssignHint(slot));
                return;
            }
            if (entry.Kind == InventoryKind.Spell) TryStartAim(player, entry.Id, slot);
            else UseItem(entry);
        }

        /// <summary>
        /// 아이템 사용 — 마법 융합기(3-2b)는 인벤토리를 "주문 두 개 고르기"로 연다. 차례는 쓰지 않는다 (시전할 때 1회).
        /// 마법서(3-5)는 책을 쓰고 주문을 배운다 (빈 빠른 칸에 자동 등록) — 틱 밖 변경이라 이벤트를 바로 비운다.
        /// </summary>
        private void UseItem(InventoryRef entry)
        {
            if (_items.TryGet(entry.Id, out var book) && book.Use == ItemUse.LearnSpell && PlayerActions.TryGetPlayer(_world, out var reader))
            {
                var result = Spellbooks.TryLearn(_world, _spells, _items, reader, System.Array.IndexOf(reader.InventorySlots, entry));
                _world.Events.Flush();
                if (result == LearnResult.AlreadyKnown) Hint(Text.AlreadyKnown(book.SpellId));
                else if (result != LearnResult.Learned) Hint(Text.ItemNotUsable(entry));
                else Hint(string.Empty); // 배운 내용은 로그에 (SpellLearnedEvent)
                return;
            }
            if (_items.TryGet(entry.Id, out var weapon) && weapon.Use == ItemUse.EquipWeapon && PlayerActions.TryGetPlayer(_world, out var holder))
            {
                // 3-7a: 무기 장착 = 차례 1 (결정 2026-10-19). 인벤토리를 닫고 행동으로 넘긴다
                int index = System.Array.IndexOf(holder.InventorySlots, entry);
                CloseInventory();
                _clock.SubmitPlayerCommand(new EquipWeaponCommand(_world, _items, index, entry, _config.DefaultActionCostTicks));
                return;
            }
            if (_items.TryGet(entry.Id, out var consumable)
                && (consumable.Use == ItemUse.StrengthPotion || consumable.Use == ItemUse.UpgradeScroll)
                && PlayerActions.TryGetPlayer(_world, out var user))
            {
                // 3-7b: 힘 물약 · 강화 주문서 = 차례 1 (결정 2026-10-19). 든 무기 없이 주문서는 차례를 쓰지 않고 안내만
                if (Consumables.Check(_items, user, entry) == ConsumeResult.NoWeapon)
                {
                    Hint(HudText.NoWeaponToUpgrade);
                    return;
                }
                int index = System.Array.IndexOf(user.InventorySlots, entry);
                CloseInventory();
                _clock.SubmitPlayerCommand(new UseConsumableCommand(_world, _items, index, entry, _config.DefaultActionCostTicks));
                return;
            }
            if (_items.TryGet(entry.Id, out var item) && item.Use == ItemUse.Fusion)
            {
                CancelAim();
                _inventory.OpenForFusion();
                Hint(HudText.FusionPickHint);
                return;
            }
            Hint(Text.ItemNotUsable(entry));
        }

        // ── 인벤토리 (3-2a) ──

        private void OpenInventory()
        {
            CancelPath();
            CancelAim();
            _comboPending = false;
            _inventory.Open();
            Hint(HudText.InventoryHint);
        }

        private void HandleInventory(Unit player, Keyboard keyboard, Mouse mouse)
        {
            if (keyboard != null)
            {
                if (keyboard[Key.I].wasPressedThisFrame)
                {
                    CloseInventory();
                    return;
                }
                if (keyboard[Key.Escape].wasPressedThisFrame)
                {
                    _inventory.Cancel();
                    Hint(InventoryHint(player));
                    return;
                }
                int slot = PressedSlotKey(keyboard, player.QuickSlots.Length);
                if (slot >= 0)
                {
                    Apply(player, _inventory.PressQuickSlot(_world, player, slot));
                    return;
                }
                foreach (var key in CastKeys)
                {
                    if (!keyboard[key].wasPressedThisFrame) continue;
                    Apply(player, _inventory.Confirm(_world, player));
                    return;
                }
                var dir = ReadHeldDirection(keyboard, out bool pressedNow);
                if (dir != default && (pressedNow || Time.time >= _nextRepeatTime))
                {
                    _inventory.Move(dir.X, dir.Y, player.InventorySlots.Length);
                    _nextRepeatTime = Time.time + (pressedNow ? _keyRepeatDelay : _keyRepeatInterval);
                    return;
                }
            }

            if (mouse == null || _inventoryView == null) return;
            var screen = mouse.position.ReadValue();
            bool moved = (screen - _lastMousePos).sqrMagnitude > 0.01f;
            bool clicked = mouse.leftButton.wasPressedThisFrame;
            if (!moved && !clicked) return;
            _lastMousePos = screen;

            int option = _inventoryView.MenuOptionAt(screen);
            int cell = _inventoryView.CellAt(screen);
            if (option >= 0) _inventory.SelectMenu(option);
            else if (cell >= 0) _inventory.Select(cell, player.InventorySlots.Length);
            if (!clicked) return;

            int quick = _hud != null ? _hud.QuickSlotAt(screen) : -1;
            if (quick >= 0) Apply(player, _inventory.PressQuickSlot(_world, player, quick));
            else if (option >= 0 || cell >= 0) Apply(player, _inventory.Confirm(_world, player));
            else if (!_inventoryView.Contains(screen))
            {
                _inventory.Cancel(); // 창 밖 클릭 = 한 단계 뒤로
                Hint(InventoryHint(player));
            }
        }

        /// <summary>인벤토리 조작 결과 처리 — 틱 밖 변경(빠른 칸 등록)은 바로 HUD에 보이게 이벤트를 비운다.</summary>
        private void Apply(Unit player, InventoryAction action)
        {
            _world.Events.Flush();
            switch (action.Kind)
            {
                case InventoryActionKind.CastSpell:
                    TryStartAim(player, action.Entry.Id, Items.Inventory.QuickSlotOf(player, action.Entry));
                    break;
                case InventoryActionKind.UseItem:
                    UseItem(action.Entry);
                    break;
                case InventoryActionKind.Assigned:
                    Hint(Text.Assigned(action.Entry, action.Slot));
                    break;
                case InventoryActionKind.Unassigned:
                    Hint(Text.Unassigned(action.Entry, action.Slot));
                    break;
                case InventoryActionKind.FuseSpells:
                    TryStartFusionAim(player, action.Entry.Id, action.Second.Id);
                    break;
                default:
                    Hint(InventoryHint(player));
                    break;
            }
            DrawInventory();
        }

        /// <summary>인벤토리 창의 지금 상태에 맞는 안내 한 줄 (닫혀 있으면 빈 문자열).</summary>
        private string InventoryHint(Unit player)
        {
            switch (_inventory.State)
            {
                case InventoryMenuState.Closed:
                    return string.Empty;
                case InventoryMenuState.ChooseSlot:
                    return Text.ChooseSlotHint(_inventory.SelectedEntry(player));
                case InventoryMenuState.FusionPick:
                    return _inventory.FusionFirst >= 0 ? Text.FusionSecondHint(player.InventorySlots[_inventory.FusionFirst]) : HudText.FusionPickHint;
                default:
                    return _inventory.AssignTarget >= 0 ? HudText.AssignHint(_inventory.AssignTarget) : HudText.InventoryHint;
            }
        }

        private void CloseInventory()
        {
            _inventory.Close();
            Hint(string.Empty);
        }

        private void DrawInventory()
        {
            if (_inventoryView == null) return;
            Unit player = null;
            if (_world != null) PlayerActions.TryGetPlayer(_world, out player);
            _inventoryView.Draw(_inventory, player, Text);
            if (_hud != null) _hud.SetHighlightSlot(_inventory.IsOpen ? _inventory.AssignTarget : _aimSlot);
        }

        private void OnInventoryChanged(InventoryChangedEvent e)
        {
            if (_inventory.IsOpen) DrawInventory();
        }

        private void OnQuickSlotsChanged(QuickSlotsChangedEvent e)
        {
            if (_inventory.IsOpen) DrawInventory();
            // 조준 중인 칸에서 주문이 빠지면 조준을 끝낸다
            if (_aimSlot >= 0 && PlayerActions.TryGetPlayer(_world, out var player) && player.QuickSlotSpell(_aimSlot) != _aimSpellId)
                _aimSlot = -1;
        }

        // ── 주문 조준 (2-2) ──

        /// <summary>주문으로 조준 시작. 마나가 모자라면 힌트만. slot = 빠른 칸 번호 (-1 = 인벤토리에서 바로).</summary>
        private void TryStartAim(Unit player, string spellId, int slot)
        {
            if (_spells == null || !_spells.TryGet(spellId, out var spell))
            {
                Hint(HudText.CastBlockReason(CastBlock.UnknownSpell));
                return;
            }
            if (player.Mana < spell.ManaCost)
            {
                Hint(Text.NotEnoughMana(slot, spell.Id, player.Mana, spell.ManaCost));
                return;
            }

            BeginAim(player, spellId, null, slot, spell.Range);
        }

        /// <summary>
        /// 마법 융합 조준 시작 (3-2b): 대상과 무관한 판정(융합기 · 같은 주문 · 가진 주문 · 조합 · 마나)을 먼저 보고, 막히면 힌트만.
        /// 대상 · 사거리 · 시야는 조준 중에 FusionCasting.Check가 매번 본다 (시전과 같은 계산).
        /// </summary>
        private void TryStartFusionAim(Unit player, string firstId, string secondId)
        {
            var block = FusionCasting.Check(_world, _spells, _fusions, _items, _targeting, _sight, player, firstId, secondId, player.Pos, out var fused, out _);
            if (fused == null)
            {
                Hint(Text.FusionBlocked(firstId, secondId, block));
                return;
            }
            if (player.Mana < fused.ManaCost)
            {
                Hint(Text.NotEnoughMana(-1, fused.Id, player.Mana, fused.ManaCost));
                return;
            }
            BeginAim(player, firstId, secondId, -1, fused.Range);
        }

        private void BeginAim(Unit player, string spellId, string fusionSecondId, int slot, int range)
        {
            _aimSpellId = spellId;
            _aimFusionSecond = fusionSecondId;
            _aimSlot = slot;
            _aimCursor = NearestVisibleEnemy(player, range) ?? player.Pos;
            if (Mouse.current != null) _lastMousePos = Mouse.current.position.ReadValue();
            _nextRepeatTime = Time.time + _keyRepeatDelay;
            if (_hud != null) _hud.SetHighlightSlot(_aimSlot);
            RefreshAim(player);
        }

        private void HandleAim(Unit player, Keyboard keyboard, Mouse mouse)
        {
            if (keyboard != null)
            {
                int slot = PressedSlotKey(keyboard, player.QuickSlots.Length);
                if (keyboard[Key.Escape].wasPressedThisFrame || (slot >= 0 && slot == _aimSlot))
                {
                    CancelAim();
                    return;
                }
                if (slot >= 0)
                {
                    // 다른 빠른 칸으로 바꿈
                    CancelAim();
                    UseQuickSlot(player, slot);
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
                if (clicked && _hud != null)
                {
                    int slot = _hud.QuickSlotAt(screen);
                    if (slot >= 0)
                    {
                        CancelAim();
                        if (slot != _aimSlot) UseQuickSlot(player, slot);
                        return;
                    }
                }
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
            ICommand command;
            var block = _aimFusionSecond != null
                ? FusionCasting.TryCreateCast(_world, _config, _spells, _fusions, _items, _targeting, _sight, _aimSpellId, _aimFusionSecond, _aimCursor, out command)
                : SpellCasting.TryCreateCastSpell(_world, _config, _spells, _targeting, _sight, _aimSpellId, _aimCursor, out command);
            if (block != CastBlock.None)
            {
                Hint(AimHint() + "  — " + HudText.CastBlockReason(block));
                return;
            }
            CancelAim();
            _clock.SubmitPlayerCommand(command);
        }

        private void RefreshAim(Unit player)
        {
            SpellPreview preview;
            var block = _aimFusionSecond != null
                ? FusionCasting.Check(_world, _spells, _fusions, _items, _targeting, _sight, player, _aimSpellId, _aimFusionSecond, _aimCursor, out _, out preview)
                : SpellCasting.CheckSpell(_world, _spells, _targeting, _sight, player, _aimSpellId, _aimCursor, out _, out preview);
            if (_aimPreview != null) _aimPreview.Show(_aimCursor, preview, block == CastBlock.None);
            Hint(block == CastBlock.None ? AimHint() : AimHint() + "  — " + HudText.CastBlockReason(block));
        }

        private void CancelAim()
        {
            bool wasAiming = IsAiming;
            _aimSpellId = null;
            _aimFusionSecond = null;
            _aimSlot = -1;
            if (_aimPreview != null) _aimPreview.Hide();
            if (_hud != null && !_inventory.IsOpen) _hud.SetHighlightSlot(-1);
            if (wasAiming) Hint(string.Empty);
        }

        /// <summary>조준 중인 주문 — 융합이면 FusionBook이 만든 융합 주문 (사거리 · 마나 표시용). 없으면 null.</summary>
        private SpellDef AimSpell()
        {
            if (_aimSpellId == null || !_spells.TryGet(_aimSpellId, out var first)) return null;
            if (_aimFusionSecond == null) return first;
            return _spells.TryGet(_aimFusionSecond, out var second) ? _fusions.Build(first, second) : null;
        }

        private string AimHint()
        {
            var spell = AimSpell();
            if (spell == null) return HudText.CastBlockReason(CastBlock.UnknownSpell);
            return _aimFusionSecond != null
                ? Text.FusionAimHint(_aimSpellId, _aimFusionSecond, spell.Id, spell.ManaCost)
                : Text.AimHint(_aimSlot, _aimSpellId, spell.ManaCost);
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
            var spell = AimSpell();
            var list = VisibleEnemies(player, spell != null ? spell.Range : 0);
            if (list.Count == 0) return null;
            int index = list.FindIndex(u => u.Pos == current);
            return list[(index + 1) % list.Count].Pos;
        }

        /// <summary>화면 좌표 → 칸 (G-1 3D): 카메라 광선과 바닥 평면(y = 0)의 교점을 Grid로 바꾼다 (물리 안 씀). 하늘이면 맵 밖 칸.</summary>
        private GridPos ScreenToCell(Vector2 screenPos)
        {
            if (!ProjectM.Presentation.CameraController.ScreenToGround(_camera, screenPos, out var ground)) return new GridPos(-1, -1);
            var cell = _grid.WorldToCell(ground);
            return new GridPos(cell.x, cell.y);
        }

        /// <returns>키 입력을 처리했으면 true.</returns>
        private bool HandleKeyboard(Keyboard keyboard)
        {
            foreach (var key in StairsKeys)
            {
                if (!keyboard[key].wasPressedThisFrame) continue;
                TryUseStairs(); // 계단 위가 아니면 아무 일 없음
                return true;
            }

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
                // 계단 위에서 자기 칸 클릭 = 계단 사용 (틱 안 씀), 아니면 제자리 대기
                if (!TryUseStairs()) _clock.SubmitPlayerCommand(PlayerActions.Wait(_config));
                return;
            }

            // 3-7a: 보이는 적을 클릭했고 지금 자리에서 닿으면(옆 칸 · 창은 2칸) 바로 공격
            if (_sight.CanSee(_world.Map, player.Pos, target, player.SightRange)
                && PlayerActions.TryAttackAt(_world, _config, player, target, out var attack))
            {
                _clock.SubmitPlayerCommand(attack);
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
