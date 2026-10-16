using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>피해 적용 지점 하나. 사망 처리는 여기서 하지 않고 ⑥ 사망 판정 단계(DeathCheckSystem)가 한다.</summary>
    public static class Combat
    {
        public static void ApplyDamage(World world, EntityId attacker, Unit target, int amount)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "피해는 0 이상이어야 한다.");
            if (!target.IsAlive) return;

            target.Hp = Math.Max(0, target.Hp - amount);
            world.Events.Publish(new UnitDamagedEvent(target.Id, attacker, amount, target.Hp, target.MaxHp));
        }
    }
}
