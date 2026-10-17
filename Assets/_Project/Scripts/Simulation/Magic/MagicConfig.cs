using System;

namespace ProjectM.Magic
{
    /// <summary>
    /// 마나 회복 · 주문 장착 칸 수치 (2-1, Q1 · Q2 임시값) + 플레이어 체력 자동 회복 (2-6).
    /// 값은 MagicConfigAsset에서 온다 — 기준은 BALANCE.md 4번.
    /// </summary>
    public sealed class MagicConfig
    {
        /// <summary>마나 회복 주기 (틱). 이 틱 수마다 한 번 회복한다.</summary>
        public int ManaRegenIntervalTicks { get; }
        /// <summary>한 번에 회복하는 마나.</summary>
        public int ManaRegenAmount { get; }
        /// <summary>주문 장착 칸 수 (키 1 ~ N).</summary>
        public int SpellSlotCount { get; }
        /// <summary>플레이어 체력 회복 주기 (틱). 0이면 체력 자동 회복 없음.</summary>
        public int HpRegenIntervalTicks { get; }
        /// <summary>한 번에 회복하는 체력 = 최대 체력의 이 퍼센트, 올림 · 최소 1.</summary>
        public int HpRegenPercent { get; }

        public MagicConfig(int manaRegenIntervalTicks, int manaRegenAmount, int spellSlotCount,
            int hpRegenIntervalTicks = 0, int hpRegenPercent = 0)
        {
            if (manaRegenIntervalTicks < 1)
                throw new ArgumentOutOfRangeException(nameof(manaRegenIntervalTicks), manaRegenIntervalTicks, "회복 주기는 1틱 이상이어야 한다.");
            if (manaRegenAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(manaRegenAmount), manaRegenAmount, "회복량은 0 이상이어야 한다.");
            if (spellSlotCount < 1 || spellSlotCount > 9)
                throw new ArgumentOutOfRangeException(nameof(spellSlotCount), spellSlotCount, "장착 칸은 1 ~ 9개 (숫자 키).");
            if (hpRegenIntervalTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(hpRegenIntervalTicks), hpRegenIntervalTicks, "체력 회복 주기는 0(없음) 이상이어야 한다.");
            if (hpRegenPercent < 0 || hpRegenPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(hpRegenPercent), hpRegenPercent, "체력 회복 퍼센트는 0 ~ 100.");

            ManaRegenIntervalTicks = manaRegenIntervalTicks;
            ManaRegenAmount = manaRegenAmount;
            SpellSlotCount = spellSlotCount;
            HpRegenIntervalTicks = hpRegenIntervalTicks;
            HpRegenPercent = hpRegenPercent;
        }

        /// <summary>최대 체력 maxHp인 유닛이 한 번에 회복하는 체력. 회복이 꺼져 있으면 0.</summary>
        public int HpRegenAmount(int maxHp)
        {
            if (HpRegenIntervalTicks == 0 || HpRegenPercent == 0 || maxHp <= 0) return 0;
            return Math.Max(1, (maxHp * HpRegenPercent + 99) / 100);
        }
    }
}
