using System;

namespace ProjectM.Magic
{
    /// <summary>
    /// 마나 회복 · 빠른 칸 · 인벤토리 칸 수치 (2-1 Q1 · Q2 임시값, 3-2a) + 플레이어 체력 자동 회복 (2-6).
    /// 값은 MagicConfigAsset에서 온다 — 기준은 BALANCE.md 4번.
    /// </summary>
    public sealed class MagicConfig
    {
        public const int DefaultInventorySlotCount = 20;

        /// <summary>마나 회복 주기 (틱). 이 틱 수마다 한 번 회복한다.</summary>
        public int ManaRegenIntervalTicks { get; }
        /// <summary>한 번에 회복하는 마나.</summary>
        public int ManaRegenAmount { get; }
        /// <summary>빠른 칸 수 (키 1 ~ N, 3-2a — 2-1 주문 장착 칸을 대신함).</summary>
        public int QuickSlotCount { get; }
        /// <summary>인벤토리 칸 수 (3-2a, 고정 칸).</summary>
        public int InventorySlotCount { get; }
        /// <summary>플레이어 체력 회복 주기 (틱). 0이면 체력 자동 회복 없음.</summary>
        public int HpRegenIntervalTicks { get; }
        /// <summary>주기마다 회복하는 체력 = 최대 체력의 이 퍼센트 (소수점은 다음 주기로 이어짐 — 2026-10-20부터 올림 · 최소 1 없음).</summary>
        public int HpRegenPercent { get; }

        public MagicConfig(int manaRegenIntervalTicks, int manaRegenAmount, int quickSlotCount,
            int hpRegenIntervalTicks = 0, int hpRegenPercent = 0, int inventorySlotCount = DefaultInventorySlotCount)
        {
            if (manaRegenIntervalTicks < 1)
                throw new ArgumentOutOfRangeException(nameof(manaRegenIntervalTicks), manaRegenIntervalTicks, "회복 주기는 1틱 이상이어야 한다.");
            if (manaRegenAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(manaRegenAmount), manaRegenAmount, "회복량은 0 이상이어야 한다.");
            if (quickSlotCount < 1 || quickSlotCount > 9)
                throw new ArgumentOutOfRangeException(nameof(quickSlotCount), quickSlotCount, "빠른 칸은 1 ~ 9개 (숫자 키).");
            if (inventorySlotCount < 1)
                throw new ArgumentOutOfRangeException(nameof(inventorySlotCount), inventorySlotCount, "인벤토리는 1칸 이상.");
            if (hpRegenIntervalTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(hpRegenIntervalTicks), hpRegenIntervalTicks, "체력 회복 주기는 0(없음) 이상이어야 한다.");
            if (hpRegenPercent < 0 || hpRegenPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(hpRegenPercent), hpRegenPercent, "체력 회복 퍼센트는 0 ~ 100.");

            ManaRegenIntervalTicks = manaRegenIntervalTicks;
            ManaRegenAmount = manaRegenAmount;
            QuickSlotCount = quickSlotCount;
            InventorySlotCount = inventorySlotCount;
            HpRegenIntervalTicks = hpRegenIntervalTicks;
            HpRegenPercent = hpRegenPercent;
        }

        /// <summary>
        /// 최대 체력 maxHp인 유닛이 count번째 회복 주기(1부터)에 회복하는 체력. 회복이 꺼져 있으면 0.
        /// 정확히 주기마다 maxHp × 퍼센트 / 100 — 소수점은 누적해 정수가 될 때 준다 (count번째까지 합 = ⌊maxHp × 퍼센트 × count / 100⌋).
        /// count는 틱 번호에서 계산하므로 저장할 상태가 없다 (규칙 6). 최대 체력 20 · 1% → 5번째 주기(50틱)마다 1.
        /// </summary>
        public int HpRegenAmount(int maxHp, long count)
        {
            if (HpRegenIntervalTicks == 0 || HpRegenPercent == 0 || maxHp <= 0 || count < 1) return 0;
            long perCycle = (long)maxHp * HpRegenPercent;
            return (int)(perCycle * count / 100 - perCycle * (count - 1) / 100);
        }
    }
}
