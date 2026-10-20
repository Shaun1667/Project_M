using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Units;

namespace ProjectM.UI
{
    public enum InventoryMenuState
    {
        Closed,
        /// <summary>칸을 고르는 중. AssignTarget ≥ 0이면 "빈 빠른 칸에 등록할 항목 고르기".</summary>
        Browse,
        /// <summary>고른 항목의 메뉴 (사용 · 등록하기 · 등록 해제 · 닫기).</summary>
        Menu,
        /// <summary>"등록하기" 뒤 빠른 칸 번호를 기다리는 중.</summary>
        ChooseSlot,
        /// <summary>마법 융합기 사용 (3-2b): 주문 두 개를 고르는 중. FusionFirst = 첫 주문 칸 (-1 = 아직).</summary>
        FusionPick,
    }

    public enum InventoryMenuOption
    {
        Use,
        Assign,
        Unassign,
        Close,
        /// <summary>물약 던지기 (3-9) — 사용 바로 아래.</summary>
        Throw,
    }

    public enum InventoryActionKind
    {
        None,
        /// <summary>주문 사용 → 입력이 조준 모드로 (창은 닫힘).</summary>
        CastSpell,
        /// <summary>아이템 사용 (창은 닫힘). 마법 융합기면 입력 쪽이 OpenForFusion으로 다시 연다 (3-2b).</summary>
        UseItem,
        Assigned,
        Unassigned,
        /// <summary>융합할 주문 두 개를 골랐다 (Entry = 첫째, Second = 둘째, 창은 닫힘) — 3-2b.</summary>
        FuseSpells,
        /// <summary>물약 던지기 (3-9) → 입력이 던지기 조준으로 (창은 닫힘).</summary>
        ThrowItem,
    }

    /// <summary>InventoryMenu 조작의 결과 — 입력 쪽이 조준 시작 · 안내 문구에 쓴다.</summary>
    public readonly struct InventoryAction
    {
        public InventoryActionKind Kind { get; }
        public InventoryRef Entry { get; }
        public int Slot { get; }
        public InventoryRef Second { get; }

        public InventoryAction(InventoryActionKind kind, InventoryRef entry = default, int slot = -1, InventoryRef second = default)
        {
            Second = second;
            Kind = kind;
            Entry = entry;
            Slot = slot;
        }

        public static InventoryAction None => default;
    }

    /// <summary>
    /// 인벤토리 창의 상태 (3-2a, 결정 2026-10-17). 엔진 타입을 쓰지 않아 테스트에서 그대로 돌린다 — 그리기는 InventoryPresenter.
    /// - 열기: I 키 (Open) 또는 빈 빠른 칸을 누름 (OpenForAssign — 고른 항목이 바로 그 칸에 등록되고 닫힘).
    /// - 칸 고르기: 방향키 · 마우스, 한 줄 Columns칸. Enter · 클릭 = 메뉴 (사용 · 등록하기 · 등록 해제 · 닫기). 숫자 키 = 그 빠른 칸에 바로 등록.
    /// - "등록하기" → 숫자 키 · 빠른 칸 클릭을 기다림. Esc = 한 단계 뒤로 (메뉴 → 칸 고르기 → 닫기).
    /// - 마법 융합 (3-2b, OpenForFusion): 주문 칸만 고를 수 있고, 첫째 → 둘째를 고르면 닫히며 FuseSpells. 같은 칸 다시 · Esc = 첫째 취소.
    /// 빠른 칸 등록은 차례를 쓰지 않는다 (틱 밖에서 Items.Inventory로 바꾸고, 부른 쪽이 이벤트를 비운다).
    /// </summary>
    public sealed class InventoryMenu
    {
        public const int Columns = 5;

        private readonly List<InventoryMenuOption> _options = new List<InventoryMenuOption>();

        public InventoryMenuState State { get; private set; } = InventoryMenuState.Closed;
        public bool IsOpen => State != InventoryMenuState.Closed;
        /// <summary>고른 인벤토리 칸.</summary>
        public int Selected { get; private set; }
        /// <summary>빈 빠른 칸을 눌러 열었으면 그 칸 번호, 아니면 -1.</summary>
        public int AssignTarget { get; private set; } = -1;
        public IReadOnlyList<InventoryMenuOption> Options => _options;
        public int MenuIndex { get; private set; }
        /// <summary>융합 고르기에서 첫 주문의 인벤토리 칸, 아직이면 -1 (3-2b).</summary>
        public int FusionFirst { get; private set; } = -1;

        /// <summary>그 항목을 던질 수 있는지 (3-9 — 입력이 Consumables.CanThrow로 채움). null이면 던지기 메뉴 없음.</summary>
        public Func<InventoryRef, bool> CanThrow { get; set; }

        /// <summary>상태가 바뀌면 알린다 (다시 그리기).</summary>
        public event Action Changed;

        public void Open()
        {
            State = InventoryMenuState.Browse;
            AssignTarget = -1;
            Changed?.Invoke();
        }

        public void OpenForAssign(int quickSlot)
        {
            State = InventoryMenuState.Browse;
            AssignTarget = quickSlot;
            Changed?.Invoke();
        }

        /// <summary>마법 융합기 사용 (3-2b): 주문 두 개 고르기로 연다.</summary>
        public void OpenForFusion()
        {
            State = InventoryMenuState.FusionPick;
            AssignTarget = -1;
            FusionFirst = -1;
            Changed?.Invoke();
        }

        public void Close()
        {
            if (State == InventoryMenuState.Closed) return;
            State = InventoryMenuState.Closed;
            AssignTarget = -1;
            FusionFirst = -1;
            _options.Clear();
            Changed?.Invoke();
        }

        /// <summary>Esc: 메뉴 · 칸 번호 기다림 → 칸 고르기, 융합 첫 주문 고름 → 고르기 취소, 그 밖 → 닫기.</summary>
        public void Cancel()
        {
            if (State == InventoryMenuState.Menu || State == InventoryMenuState.ChooseSlot)
            {
                State = InventoryMenuState.Browse;
                _options.Clear();
                Changed?.Invoke();
            }
            else if (State == InventoryMenuState.FusionPick && FusionFirst >= 0)
            {
                FusionFirst = -1;
                Changed?.Invoke();
            }
            else Close();
        }

        /// <summary>방향 입력: 칸 고르기에서는 격자 이동, 메뉴에서는 위아래.</summary>
        public void Move(int dx, int dy, int slotCount)
        {
            if ((State == InventoryMenuState.Browse || State == InventoryMenuState.FusionPick) && slotCount > 0)
            {
                int col = Selected % Columns + dx, row = Selected / Columns - dy; // 위 = 앞줄
                int rows = (slotCount + Columns - 1) / Columns;
                col = Math.Max(0, Math.Min(Columns - 1, col));
                row = Math.Max(0, Math.Min(rows - 1, row));
                Selected = Math.Min(slotCount - 1, row * Columns + col);
                Changed?.Invoke();
            }
            else if (State == InventoryMenuState.Menu && _options.Count > 0)
            {
                MenuIndex = Math.Max(0, Math.Min(_options.Count - 1, MenuIndex - dy));
                Changed?.Invoke();
            }
        }

        /// <summary>마우스가 가리킨 칸 (칸 고르기에서만).</summary>
        public void Select(int index, int slotCount)
        {
            if ((State != InventoryMenuState.Browse && State != InventoryMenuState.FusionPick) || index < 0 || index >= slotCount || index == Selected) return;
            Selected = index;
            Changed?.Invoke();
        }

        public void SelectMenu(int index)
        {
            if (State != InventoryMenuState.Menu || index < 0 || index >= _options.Count || index == MenuIndex) return;
            MenuIndex = index;
            Changed?.Invoke();
        }

        /// <summary>Enter · 클릭.</summary>
        public InventoryAction Confirm(World world, Unit unit)
        {
            if (unit == null) return InventoryAction.None;
            var entry = SelectedEntry(unit);
            switch (State)
            {
                case InventoryMenuState.Browse:
                    if (entry.IsEmpty) return InventoryAction.None;
                    if (AssignTarget >= 0)
                    {
                        int slot = AssignTarget;
                        Inventory.AssignQuickSlot(world, unit, slot, entry);
                        Close();
                        return new InventoryAction(InventoryActionKind.Assigned, entry, slot);
                    }
                    OpenMenu(unit, entry);
                    return InventoryAction.None;

                case InventoryMenuState.Menu:
                    return Choose(world, unit, entry, _options[MenuIndex]);

                case InventoryMenuState.FusionPick:
                    // 주문만 고른다. 같은 칸을 다시 고르면 취소, 둘째를 고르면 닫고 결과를 넘긴다 (조합 판정은 부른 쪽)
                    if (entry.Kind != InventoryKind.Spell) return InventoryAction.None;
                    if (FusionFirst < 0 || FusionFirst == Selected)
                    {
                        FusionFirst = FusionFirst < 0 ? Selected : -1;
                        Changed?.Invoke();
                        return InventoryAction.None;
                    }
                    var first = unit.InventorySlots[FusionFirst];
                    Close();
                    return new InventoryAction(InventoryActionKind.FuseSpells, first, -1, entry);

                default:
                    return InventoryAction.None;
            }
        }

        /// <summary>숫자 키 · 빠른 칸 클릭: 칸 고르기 · 칸 번호 기다림에서 고른 항목을 그 칸에 등록.</summary>
        public InventoryAction PressQuickSlot(World world, Unit unit, int slot)
        {
            if (unit == null || slot < 0 || slot >= unit.QuickSlots.Length) return InventoryAction.None;
            if (State != InventoryMenuState.Browse && State != InventoryMenuState.ChooseSlot) return InventoryAction.None;
            var entry = SelectedEntry(unit);
            if (entry.IsEmpty) return InventoryAction.None;

            Inventory.AssignQuickSlot(world, unit, slot, entry);
            if (State == InventoryMenuState.ChooseSlot) State = InventoryMenuState.Browse;
            if (AssignTarget >= 0) Close();
            else Changed?.Invoke();
            return new InventoryAction(InventoryActionKind.Assigned, entry, slot);
        }

        public InventoryRef SelectedEntry(Unit unit) =>
            unit != null && Selected >= 0 && Selected < unit.InventorySlots.Length ? unit.InventorySlots[Selected] : InventoryRef.Empty;

        private void OpenMenu(Unit unit, InventoryRef entry)
        {
            _options.Clear();
            _options.Add(InventoryMenuOption.Use);
            if (CanThrow != null && CanThrow(entry)) _options.Add(InventoryMenuOption.Throw);
            _options.Add(InventoryMenuOption.Assign);
            if (Inventory.QuickSlotOf(unit, entry) >= 0) _options.Add(InventoryMenuOption.Unassign);
            _options.Add(InventoryMenuOption.Close);
            MenuIndex = 0;
            State = InventoryMenuState.Menu;
            Changed?.Invoke();
        }

        private InventoryAction Choose(World world, Unit unit, InventoryRef entry, InventoryMenuOption option)
        {
            switch (option)
            {
                case InventoryMenuOption.Use:
                    Close();
                    return new InventoryAction(entry.Kind == InventoryKind.Spell ? InventoryActionKind.CastSpell : InventoryActionKind.UseItem, entry);
                case InventoryMenuOption.Throw:
                    Close();
                    return new InventoryAction(InventoryActionKind.ThrowItem, entry);
                case InventoryMenuOption.Assign:
                    State = InventoryMenuState.ChooseSlot;
                    _options.Clear();
                    Changed?.Invoke();
                    return InventoryAction.None;
                case InventoryMenuOption.Unassign:
                    int slot = Inventory.QuickSlotOf(unit, entry);
                    if (slot >= 0) Inventory.AssignQuickSlot(world, unit, slot, InventoryRef.Empty);
                    Cancel();
                    return new InventoryAction(InventoryActionKind.Unassigned, entry, slot);
                default:
                    Cancel();
                    return InventoryAction.None;
            }
        }
    }
}
