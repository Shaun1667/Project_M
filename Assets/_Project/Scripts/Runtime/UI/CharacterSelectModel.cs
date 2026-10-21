using System;

namespace ProjectM.UI
{
    /// <summary>
    /// 캐릭터 선택 창의 상태 (결정 2026-10-21 — 판을 시작할 때마다 고름). 엔진 타입을 쓰지 않아 테스트에서 그대로 돈다 — 그리기는 CharacterSelectPresenter.
    /// 왼쪽 · 오른쪽 = 고르기(양 끝에서 멈춤), 확정 = Confirmed 이벤트(고른 칸 번호).
    /// </summary>
    public sealed class CharacterSelectModel
    {
        public int Count { get; private set; }
        public int Selected { get; private set; }
        public bool IsOpen { get; private set; }

        /// <summary>고른 칸이 바뀜 · 열림 · 닫힘 (다시 그리기).</summary>
        public event Action Changed;
        /// <summary>확정 — 고른 칸 번호.</summary>
        public event Action<int> Confirmed;

        /// <summary>count명 중에서 고르기를 연다. initial = 처음 고른 칸 (지난 판에 고른 캐릭터).</summary>
        public void Open(int count, int initial = 0)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), count, "고를 캐릭터가 없다.");
            Count = count;
            Selected = Math.Max(0, Math.Min(count - 1, initial));
            IsOpen = true;
            Changed?.Invoke();
        }

        public void Move(int dx)
        {
            if (!IsOpen || dx == 0) return;
            int next = Math.Max(0, Math.Min(Count - 1, Selected + Math.Sign(dx)));
            if (next == Selected) return;
            Selected = next;
            Changed?.Invoke();
        }

        /// <summary>마우스가 가리킨 칸.</summary>
        public void Select(int index)
        {
            if (!IsOpen || index < 0 || index >= Count || index == Selected) return;
            Selected = index;
            Changed?.Invoke();
        }

        public void Confirm()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Changed?.Invoke();
            Confirmed?.Invoke(Selected);
        }
    }
}
