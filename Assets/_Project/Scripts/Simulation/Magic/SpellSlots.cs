using System;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Magic
{
    /// <summary>
    /// 주문 장착 칸 변경 지점 하나 (3-1, REACTIONS.md D8). 지금은 디버그 창이 쓰고, 게임 중 주문 획득(Phase 6)도 여기를 쓴다.
    /// </summary>
    public static class SpellSlots
    {
        /// <summary>칸 slot에 주문을 넣는다. spellId가 null이면 빈 칸. 주문이 SpellBook에 없으면 예외.</summary>
        public static void Assign(World world, SpellBook book, Unit unit, int slot, string spellId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (book == null) throw new ArgumentNullException(nameof(book));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (slot < 0 || slot >= unit.SpellSlots.Length) throw new ArgumentOutOfRangeException(nameof(slot), slot, "없는 장착 칸");
            if (spellId != null && !book.TryGet(spellId, out _)) throw new ArgumentException("없는 주문: " + spellId, nameof(spellId));

            unit.SpellSlots[slot] = spellId;
            world.Events.Publish(new SpellSlotsChangedEvent(unit.Id, (string[])unit.SpellSlots.Clone()));
        }
    }

    /// <summary>유닛의 주문 장착 칸이 바뀌었다. 배열은 복사본 (빈 칸 = null).</summary>
    public sealed class SpellSlotsChangedEvent : ISimEvent
    {
        public EntityId Id { get; }
        public string[] Slots { get; }

        public SpellSlotsChangedEvent(EntityId id, string[] slots)
        {
            Id = id;
            Slots = slots ?? throw new ArgumentNullException(nameof(slots));
        }
    }
}
