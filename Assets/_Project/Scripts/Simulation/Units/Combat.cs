using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>피해 적용 지점 하나. 사망 처리는 여기서 하지 않고 ⑥ 사망 판정 단계(DeathCheckSystem)가 한다.</summary>
    public static class Combat
    {
        /// <param name="cause">피해 출처 (3-4b) — 플레이어가 받으면 World.Run.LastPlayerDamage에 남아 사망 원인이 된다.</param>
        public static void ApplyDamage(World world, EntityId attacker, Unit target, int amount, DamageCause cause = default)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "피해는 0 이상이어야 한다.");
            if (!target.IsAlive) return;

            target.Hp = Math.Max(0, target.Hp - amount);
            if (target.Faction == Faction.Player && amount > 0)
            {
                world.Run.LastPlayerDamage = cause;
                world.Run.LastPlayerAttacker = attacker;
            }
            world.Events.Publish(new UnitDamagedEvent(target.Id, attacker, amount, target.Hp, target.MaxHp, cause));
        }
    }
}
