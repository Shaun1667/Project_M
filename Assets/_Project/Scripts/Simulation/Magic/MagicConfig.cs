using System;

namespace ProjectM.Magic
{
    /// <summary>
    /// 마나 회복 · 주문 장착 칸 수치 (2-1, Q1 · Q2 임시값). 값은 MagicConfigAsset에서 온다 — 기준은 BALANCE.md 4번.
    /// </summary>
    public sealed class MagicConfig
    {
        /// <summary>마나 회복 주기 (틱). 이 틱 수마다 한 번 회복한다.</summary>
        public int ManaRegenIntervalTicks { get; }
        /// <summary>한 번에 회복하는 마나.</summary>
        public int ManaRegenAmount { get; }
        /// <summary>주문 장착 칸 수 (키 1 ~ N).</summary>
        public int SpellSlotCount { get; }

        public MagicConfig(int manaRegenIntervalTicks, int manaRegenAmount, int spellSlotCount)
        {
            if (manaRegenIntervalTicks < 1)
                throw new ArgumentOutOfRangeException(nameof(manaRegenIntervalTicks), manaRegenIntervalTicks, "회복 주기는 1틱 이상이어야 한다.");
            if (manaRegenAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(manaRegenAmount), manaRegenAmount, "회복량은 0 이상이어야 한다.");
            if (spellSlotCount < 1 || spellSlotCount > 9)
                throw new ArgumentOutOfRangeException(nameof(spellSlotCount), spellSlotCount, "장착 칸은 1 ~ 9개 (숫자 키).");

            ManaRegenIntervalTicks = manaRegenIntervalTicks;
            ManaRegenAmount = manaRegenAmount;
            SpellSlotCount = spellSlotCount;
        }
    }
}
