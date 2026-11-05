using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.UI
{
    /// <summary>타이틀 첫 화면 항목 (3-27).</summary>
    public enum TitleItem
    {
        Continue = 0,
        NewGame = 1,
        Quit = 2,
    }

    /// <summary>타이틀 화면 단계 (3-27).</summary>
    public enum TitleScreen
    {
        Closed = 0,
        /// <summary>이어하기(저장 있을 때) · 게임 시작 · 종료.</summary>
        Main = 1,
        /// <summary>게임 시작 → 턴제 / 실시간 + 시드 입력 칸 (결정 2026-11-05).</summary>
        NewGame = 2,
    }

    /// <summary>
    /// 타이틀 화면 상태 (3-27, 결정 2026-11-05): 첫 화면 고르기 → 모드(턴제 · 실시간) · 시드 글 → 시작. 순수 C# — 그리기는 TitlePresenter.
    /// 확정하면 화면을 닫고 ContinueChosen / StartChosen(모드, 시드 글) / QuitChosen 중 하나를 한 번 낸다.
    /// </summary>
    public sealed class TitleMenuModel
    {
        private readonly List<TitleItem> _items = new List<TitleItem>();

        public TitleScreen Screen { get; private set; }
        public IReadOnlyList<TitleItem> Items => _items;
        public int Selected { get; private set; }
        public RunMode Mode { get; private set; }
        public string SeedText { get; private set; } = string.Empty;
        public bool IsOpen => Screen != TitleScreen.Closed;

        public event Action Changed;
        public event Action ContinueChosen;
        public event Action<RunMode, string> StartChosen;
        public event Action QuitChosen;

        /// <summary>첫 화면을 연다. 저장이 있으면 '이어하기'가 맨 위 · 처음 고른 칸. 모드 · 시드 글은 지난 값 그대로.</summary>
        public void Open(bool canContinue)
        {
            _items.Clear();
            if (canContinue) _items.Add(TitleItem.Continue);
            _items.Add(TitleItem.NewGame);
            _items.Add(TitleItem.Quit);
            Selected = 0;
            Screen = TitleScreen.Main;
            Changed?.Invoke();
        }

        public void Close()
        {
            Screen = TitleScreen.Closed;
            Changed?.Invoke();
        }

        /// <summary>첫 화면: 위 · 아래 (끝에서 반대쪽으로).</summary>
        public void Move(int delta)
        {
            if (Screen != TitleScreen.Main || _items.Count == 0) return;
            Selected = ((Selected + delta) % _items.Count + _items.Count) % _items.Count;
            Changed?.Invoke();
        }

        public void Select(int index)
        {
            if (Screen != TitleScreen.Main || index < 0 || index >= _items.Count || index == Selected) return;
            Selected = index;
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

        /// <summary>Enter · 클릭: 첫 화면이면 그 항목, 새 게임 화면이면 시작.</summary>
        public void Confirm()
        {
            if (Screen == TitleScreen.Main)
            {
                if (_items.Count == 0) return;
                switch (_items[Selected])
                {
                    case TitleItem.Continue:
                        Close();
                        ContinueChosen?.Invoke();
                        break;
                    case TitleItem.NewGame:
                        Screen = TitleScreen.NewGame;
                        Changed?.Invoke();
                        break;
                    case TitleItem.Quit:
                        QuitChosen?.Invoke();
                        break;
                }
            }
            else if (Screen == TitleScreen.NewGame)
            {
                var mode = Mode;
                var seed = SeedText;
                Close();
                StartChosen?.Invoke(mode, seed);
            }
        }

        /// <summary>Esc: 새 게임 화면 → 첫 화면. 첫 화면에서는 아무 일 없음.</summary>
        public void Back()
        {
            if (Screen != TitleScreen.NewGame) return;
            Screen = TitleScreen.Main;
            Changed?.Invoke();
        }
    }
}
