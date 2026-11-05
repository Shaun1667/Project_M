using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.UI
{
    /// <summary>타이틀 첫 화면 항목 (3-27). 끝에만 추가한다 — Continue는 3-30부터 슬롯 화면으로 합쳐 쓰지 않음.</summary>
    public enum TitleItem
    {
        Continue = 0,
        NewGame = 1,
        Quit = 2,
        /// <summary>3-33 명예의 전당 (게임 시작 아래).</summary>
        HallOfFame = 3,
    }

    /// <summary>타이틀 화면 단계 (3-27 · 3-30).</summary>
    public enum TitleScreen
    {
        Closed = 0,
        /// <summary>게임 시작 · 종료.</summary>
        Main = 1,
        /// <summary>새 게임: 턴제 / 실시간 + 시드 입력 칸.</summary>
        NewGame = 2,
        /// <summary>3-30 저장 슬롯 5칸.</summary>
        Slots = 3,
        /// <summary>3-30 찬 슬롯: 요약 + 시작 · 데이터 삭제 · 뒤로.</summary>
        SlotDetail = 4,
        /// <summary>3-30 삭제 한 번 더 묻기: 삭제 · 취소.</summary>
        ConfirmDelete = 5,
        /// <summary>3-33 명예의 전당: 클리어 기록 (높은 점수 순).</summary>
        HallOfFame = 6,
    }

    /// <summary>찬 슬롯 화면의 단추 (3-30).</summary>
    public enum SlotAction
    {
        Start = 0,
        Delete = 1,
        Back = 2,
    }

    /// <summary>저장 슬롯 한 칸의 화면 정보 (3-30) — 비었으면 Filled = false. 글은 HudText가 만든다.</summary>
    public readonly struct SlotInfo
    {
        public readonly bool Filled;
        public readonly string Label;
        public readonly string Detail;

        public SlotInfo(bool filled, string label, string detail)
        {
            Filled = filled;
            Label = label ?? string.Empty;
            Detail = detail ?? string.Empty;
        }
    }

    /// <summary>
    /// 타이틀 화면 상태 (3-27 · 3-30, 결정 2026-11-05): 첫 화면(게임 시작 · 종료) → 저장 슬롯 5칸 → 빈 슬롯 = 새 게임(모드 · 시드) / 찬 슬롯 = 요약 + 시작 · 삭제(한 번 더 묻기) · 뒤로.
    /// 순수 C# — 그리기는 TitlePresenter. 확정하면 ContinueChosen(슬롯) · StartChosen(슬롯, 모드, 시드 글) · DeleteChosen(슬롯) · QuitChosen 중 하나.
    /// 슬롯 번호는 1 ~ 슬롯 수. 삭제 뒤 슬롯 정보는 부른 쪽이 SetSlots로 다시 넣는다.
    /// </summary>
    public sealed class TitleMenuModel
    {
        private readonly List<TitleItem> _items = new List<TitleItem>();
        private readonly List<SlotInfo> _slots = new List<SlotInfo>();
        private readonly List<string> _hallOfFame = new List<string>();

        public TitleScreen Screen { get; private set; }
        public IReadOnlyList<TitleItem> Items => _items;
        public IReadOnlyList<SlotInfo> Slots => _slots;
        /// <summary>3-33 명예의 전당 줄 (순위 순, 글은 HudText가 만듦). 비면 기록 없음.</summary>
        public IReadOnlyList<string> HallOfFame => _hallOfFame;
        /// <summary>첫 화면에서 고른 항목.</summary>
        public int Selected { get; private set; }
        /// <summary>슬롯 화면에서 고른 칸 (0부터). 슬롯 번호 = SlotSelected + 1.</summary>
        public int SlotSelected { get; private set; }
        /// <summary>요약 · 삭제 확인 화면에서 고른 단추 (요약: SlotAction 순, 확인: 0 = 삭제 · 1 = 취소).</summary>
        public int OptionSelected { get; private set; }
        public int CurrentSlot => SlotSelected + 1;
        public RunMode Mode { get; private set; }
        public string SeedText { get; private set; } = string.Empty;
        public bool IsOpen => Screen != TitleScreen.Closed;

        public event Action Changed;
        public event Action<int> ContinueChosen;
        public event Action<int, RunMode, string> StartChosen;
        public event Action<int> DeleteChosen;
        public event Action QuitChosen;

        /// <summary>첫 화면을 연다. slots = 저장 슬롯 정보 (슬롯 1부터 순서대로).</summary>
        public void Open(IReadOnlyList<SlotInfo> slots)
        {
            SetSlots(slots);
            _items.Clear();
            _items.Add(TitleItem.NewGame);
            _items.Add(TitleItem.HallOfFame); // 3-33 게임 시작 아래
            _items.Add(TitleItem.Quit);
            Selected = 0;
            Screen = TitleScreen.Main;
            Changed?.Invoke();
        }

        /// <summary>3-33 명예의 전당 줄을 바꾼다 (순위 순). 화면은 그대로.</summary>
        public void SetHallOfFame(IReadOnlyList<string> lines)
        {
            _hallOfFame.Clear();
            if (lines != null) _hallOfFame.AddRange(lines);
            Changed?.Invoke();
        }

        /// <summary>슬롯 정보를 바꾼다 (삭제 뒤 등). 화면은 그대로.</summary>
        public void SetSlots(IReadOnlyList<SlotInfo> slots)
        {
            _slots.Clear();
            if (slots != null) _slots.AddRange(slots);
            if (SlotSelected >= _slots.Count) SlotSelected = Math.Max(0, _slots.Count - 1);
            Changed?.Invoke();
        }

        public void Close()
        {
            Screen = TitleScreen.Closed;
            Changed?.Invoke();
        }

        /// <summary>위 · 아래 (첫 화면 항목 · 슬롯 · 단추 — 끝에서 반대쪽으로).</summary>
        public void Move(int delta)
        {
            switch (Screen)
            {
                case TitleScreen.Main:
                    if (_items.Count == 0) return;
                    Selected = Wrap(Selected + delta, _items.Count);
                    break;
                case TitleScreen.Slots:
                    if (_slots.Count == 0) return;
                    SlotSelected = Wrap(SlotSelected + delta, _slots.Count);
                    break;
                case TitleScreen.SlotDetail:
                    OptionSelected = Wrap(OptionSelected + delta, 3);
                    break;
                case TitleScreen.ConfirmDelete:
                    OptionSelected = Wrap(OptionSelected + delta, 2);
                    break;
                default:
                    return;
            }
            Changed?.Invoke();
        }

        private static int Wrap(int value, int count) => ((value % count) + count) % count;

        /// <summary>마우스로 고르기 (지금 화면의 칸 번호).</summary>
        public void Select(int index)
        {
            switch (Screen)
            {
                case TitleScreen.Main when index >= 0 && index < _items.Count && index != Selected: Selected = index; break;
                case TitleScreen.Slots when index >= 0 && index < _slots.Count && index != SlotSelected: SlotSelected = index; break;
                case TitleScreen.SlotDetail when index >= 0 && index < 3 && index != OptionSelected: OptionSelected = index; break;
                case TitleScreen.ConfirmDelete when index >= 0 && index < 2 && index != OptionSelected: OptionSelected = index; break;
                default: return;
            }
            Changed?.Invoke();
        }

        /// <summary>새 게임 화면: 모드 고르기.</summary>
        public void SetMode(RunMode mode)
        {
            if (Screen != TitleScreen.NewGame || Mode == mode) return;
            Mode = mode;
            Changed?.Invoke();
        }

        public void ToggleMode() => SetMode(Mode == RunMode.TurnBased ? RunMode.RealTime : RunMode.TurnBased);

        /// <summary>시드 칸에 글자 하나 (제어 문자 · 길이 넘침은 무시).</summary>
        public void Type(char c)
        {
            if (Screen != TitleScreen.NewGame || char.IsControl(c) || SeedText.Length >= SeedInput.MaxLength) return;
            SeedText += c;
            Changed?.Invoke();
        }

        public void Backspace()
        {
            if (Screen != TitleScreen.NewGame || SeedText.Length == 0) return;
            SeedText = SeedText.Substring(0, SeedText.Length - 1);
            Changed?.Invoke();
        }

        /// <summary>Enter · 클릭: 지금 화면에서 고른 것을 확정.</summary>
        public void Confirm()
        {
            switch (Screen)
            {
                case TitleScreen.Main:
                    if (_items.Count == 0) return;
                    if (_items[Selected] == TitleItem.Quit) { QuitChosen?.Invoke(); return; }
                    if (_items[Selected] == TitleItem.HallOfFame) { Go(TitleScreen.HallOfFame); return; }
                    Go(TitleScreen.Slots);
                    return;
                case TitleScreen.Slots:
                    if (_slots.Count == 0) return;
                    if (_slots[SlotSelected].Filled) { OptionSelected = 0; Go(TitleScreen.SlotDetail); }
                    else Go(TitleScreen.NewGame);
                    return;
                case TitleScreen.SlotDetail:
                    switch ((SlotAction)OptionSelected)
                    {
                        case SlotAction.Start:
                            int slot = CurrentSlot;
                            Close();
                            ContinueChosen?.Invoke(slot);
                            return;
                        case SlotAction.Delete:
                            OptionSelected = 1; // 안전하게 '취소'에서 시작
                            Go(TitleScreen.ConfirmDelete);
                            return;
                        default:
                            Go(TitleScreen.Slots);
                            return;
                    }
                case TitleScreen.ConfirmDelete:
                    if (OptionSelected == 0)
                    {
                        Go(TitleScreen.Slots);
                        DeleteChosen?.Invoke(CurrentSlot);
                    }
                    else
                    {
                        OptionSelected = 0;
                        Go(TitleScreen.SlotDetail);
                    }
                    return;
                case TitleScreen.HallOfFame:
                    Go(TitleScreen.Main); // 3-33: 확정 = 뒤로
                    return;
                case TitleScreen.NewGame:
                    int target = CurrentSlot;
                    var mode = Mode;
                    var seed = SeedText;
                    Close();
                    StartChosen?.Invoke(target, mode, seed);
                    return;
            }
        }

        /// <summary>Esc: 한 단계 뒤로 (새 게임 · 요약 → 슬롯, 삭제 확인 → 요약, 슬롯 → 첫 화면). 첫 화면에서는 아무 일 없음.</summary>
        public void Back()
        {
            switch (Screen)
            {
                case TitleScreen.NewGame:
                case TitleScreen.SlotDetail: Go(TitleScreen.Slots); break;
                case TitleScreen.ConfirmDelete: OptionSelected = 0; Go(TitleScreen.SlotDetail); break;
                case TitleScreen.Slots: Go(TitleScreen.Main); break;
                case TitleScreen.HallOfFame: Go(TitleScreen.Main); break;
            }
        }

        private void Go(TitleScreen screen)
        {
            Screen = screen;
            Changed?.Invoke();
        }
    }
}
