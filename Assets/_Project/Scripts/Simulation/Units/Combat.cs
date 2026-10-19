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

        private static int AccuracyPercent(Unit attacker) => MeleeWeapon.AccuracyPercentOf(attacker);

        /// <summary>
        /// 근접 공격 명중 판정 (3-7, SPD Char.hit): 명중 굴림(0~공격자 명중 × 무기 명중 배수) ≥ 회피 굴림(0~대상 회피)이면 맞음.
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
            float accuracyRoll = rng.NextFloat() * attacker.Accuracy * AccuracyPercent(attacker) / 100f;
            float evasionRoll = rng.NextFloat() * evasion;
            return accuracyRoll >= evasionRoll;
        }
    }

    /// <summary>무기 공격 규칙 (3-7a): 명중 배수 · 피해 굴림 · 공격 거리 · 공격 비용. 맨손이면 지금까지와 같다.</summary>
    public static class MeleeWeapon
    {
        /// <summary>명중 배수 (%), 맨손 100.</summary>
        public static int AccuracyPercentOf(Unit attacker) => attacker.Weapon.IsEmpty ? 100 : attacker.Weapon.AccuracyPercent;

        /// <summary>근접 피해: 무기면 최소~최대 굴림(RngStream.Combat), 맨손이면 AttackDamage 고정 (난수 안 씀).</summary>
        public static int RollDamage(World world, Unit attacker)
        {
            var w = attacker.Weapon;
            if (w.IsEmpty) return attacker.AttackDamage;
            return w.MinDamage == w.MaxDamage ? w.MaxDamage : world.Rng.Get(RngStream.Combat).Range(w.MinDamage, w.MaxDamage + 1);
        }

        /// <summary>공격 한 번의 비용 (틱): 무기 값, 맨손이면 defaultCost.</summary>
        public static int AttackCost(Unit attacker, int defaultCost) => attacker.Weapon.IsEmpty ? defaultCost : attacker.Weapon.AttackCostTicks;

        /// <summary>
        /// target 칸을 칠 수 있는지: 1칸 = 이동과 같은 모서리 규칙. 2칸(공격 거리 2 무기) = 직선 · 대각선만, 사이 칸이 비어 있고(벽 · 유닛 없음)
        /// 두 걸음 모두 모서리 규칙을 통과할 때 (결정 2026-10-19, SPD).
        /// </summary>
        public static bool CanReach(World world, Unit attacker, GridPos target)
        {
            int distance = GridMovement.Chebyshev(attacker.Pos, target);
            if (distance == 1) return GridMovement.CanStep(world.Map, attacker.Pos, target);
            int reach = attacker.Weapon.IsEmpty ? 1 : attacker.Weapon.Reach;
            if (distance != 2 || reach < 2) return false;
            int dx = target.X - attacker.Pos.X, dy = target.Y - attacker.Pos.Y;
            if (dx % 2 != 0 || dy % 2 != 0) return false; // (2, 1) 같은 칸은 사이 칸이 정해지지 않음
            var middle = new GridPos(attacker.Pos.X + dx / 2, attacker.Pos.Y + dy / 2);
            return GridMovement.CanStep(world.Map, attacker.Pos, middle) && GridMovement.CanStep(world.Map, middle, target)
                && !world.Units.IsOccupied(middle);
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
