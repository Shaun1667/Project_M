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
    }

    public enum InventoryMenuOption
    {
        Use,
        Assign,
        Unassign,
        Close,
    }

    public enum InventoryActionKind
    {
        None,
        /// <summary>주문 사용 → 입력이 조준 모드로 (창은 닫힘).</summary>
        CastSpell,
        /// <summary>아이템 사용 (창은 닫힘). 마법 융합기는 3-2b.</summary>
        UseItem,
        Assigned,
        Unassigned,
    }

    /// <summary>InventoryMenu 조작의 결과 — 입력 쪽이 조준 시작 · 안내 문구에 쓴다.</summary>
    public readonly struct InventoryAction
    {
        public InventoryActionKind Kind { get; }
        public InventoryRef Entry { get; }
        public int Slot { get; }

        public InventoryAction(InventoryActionKind kind, InventoryRef entry = default, int slot = -1)
        {
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

        public void Close()
        {
            if (State == InventoryMenuState.Closed) return;
            State = InventoryMenuState.Closed;
            AssignTarget = -1;
            _options.Clear();
            Changed?.Invoke();
        }

        /// <summary>Esc: 메뉴 · 칸 번호 기다림 → 칸 고르기, 칸 고르기 → 닫기.</summary>
        public void Cancel()
        {
            if (State == InventoryMenuState.Menu || State == InventoryMenuState.ChooseSlot)
            {
                State = InventoryMenuState.Browse;
                _options.Clear();
                Changed?.Invoke();
            }
            else Close();
        }

        /// <summary>방향 입력: 칸 고르기에서는 격자 이동, 메뉴에서는 위아래.</summary>
        public void Move(int dx, int dy, int slotCount)
        {
            if (State == InventoryMenuState.Browse && slotCount > 0)
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
            if (State != InventoryMenuState.Browse || index < 0 || index >= slotCount || index == Selected) return;
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
