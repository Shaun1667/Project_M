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

        /// <summary>
        /// 근접 공격 명중 판정 (3-7, SPD Char.hit): 명중 굴림(0~공격자 명중) ≥ 회피 굴림(0~대상 회피)이면 맞음.
        /// 대상이 행동 불가(마비 · 빙결 — StatusDef.PreventsAction)이거나 회피 0이면 굴리지 않고 맞음. 난수는 RngStream.Combat.
        /// 주문은 판정하지 않는다 — 항상 명중 (결정 2026-10-19).
        /// </summary>
        public static bool RollHit(World world, Unit attacker, Unit target)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (target == null) throw new ArgumentNullException(nameof(target));

            int evasion = world.Scheduler.CanAct(target.Id) ? target.Evasion : 0;
            if (evasion <= 0) return true;
            var rng = world.Rng.Get(RngStream.Combat);
            float accuracyRoll = rng.NextFloat() * attacker.Accuracy;
            float evasionRoll = rng.NextFloat() * evasion;
            return accuracyRoll >= evasionRoll;
        }
    }

    /// <summary>근접 공격이 빗나갔다 (3-7). 피해 · 상태 변화 없음.</summary>
    public sealed class AttackMissedEvent : ISimEvent
    {
        public EntityId Attacker { get; }
        public EntityId Target { get; }

        public AttackMissedEvent(EntityId attacker, EntityId target)
        {
            Attacker = attacker;
            Target = target;
        }
    }
}
