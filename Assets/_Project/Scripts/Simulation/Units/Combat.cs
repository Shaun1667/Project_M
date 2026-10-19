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
            float accuracyRoll = rng.NextFloat() * attacker.Accuracy * AccuracyPercent(attacker) / 100f * MeleeWeapon.StrengthAccuracyFactor(attacker);
            float evasionRoll = rng.NextFloat() * evasion;
            return accuracyRoll >= evasionRoll;
        }
    }

    /// <summary>무기 공격 규칙 (3-7a): 명중 배수 · 피해 굴림 · 공격 거리 · 공격 비용. 맨손이면 지금까지와 같다.</summary>
    public static class MeleeWeapon
    {
        /// <summary>명중 배수 (%), 맨손 100.</summary>
        public static int AccuracyPercentOf(Unit attacker) => attacker.Weapon.IsEmpty ? 100 : attacker.Weapon.AccuracyPercent;

        /// <summary>
        /// 근접 피해: 무기면 최소~최대 굴림(RngStream.Combat) + 남는 힘 1당 0~1 보너스 굴림(3-7b, SPD), 맨손이면 AttackDamage 고정 (난수 안 씀).
        /// </summary>
        public static int RollDamage(World world, Unit attacker, bool surprise = false)
        {
            var w = attacker.Weapon;
            if (w.IsEmpty) return attacker.AttackDamage;
            var rng = world.Rng.Get(RngStream.Combat);
            int min = surprise ? SurpriseMin(w) : w.MinDamage; // 3-7c 더크
            int damage = min == w.MaxDamage ? w.MaxDamage : rng.Range(min, w.MaxDamage + 1);
            int excess = ExcessStrength(attacker);
            if (excess > 0) damage += rng.Range(0, excess + 1);
            return damage;
        }

        /// <summary>기습 때 최소 피해 (3-7c, SPD 더크): 최소 + (최대 − 최소) × SurpriseMinPercent%, 0.5 올림. 더크 +0 = 1 + 3 × 0.67 → 3.</summary>
        public static int SurpriseMin(EquippedWeapon w) =>
            w.SurpriseMinPercent == 0 ? w.MinDamage
                : System.Math.Min(w.MaxDamage, w.MinDamage + (int)System.Math.Round((w.MaxDamage - w.MinDamage) * w.SurpriseMinPercent / 100.0, System.MidpointRounding.AwayFromZero));

        /// <summary>
        /// 기습인지 (3-7c, 결정 2026-10-19 — SPD): 플레이어가 적을 칠 때 그 적이 플레이어를 못 본 상태 = 기억(LastSeenTargetPos)이 없음
        /// (배회 중 · 아직 못 알아챔 · 쫓다 놓침). 기습은 항상 명중(맨손 포함), 더크는 최소 피해도 오른다.
        /// </summary>
        public static bool IsSurprise(Unit attacker, Unit target) =>
            attacker.Faction == Faction.Player && target.Faction == Faction.Enemy && !target.LastSeenTargetPos.HasValue;

        /// <summary>방어 (3-7c): 든 무기의 방어 (육척봉 2). 갑옷이 생기면 여기에 더한다.</summary>
        public static int DefenseOf(Unit target) => target.Weapon.IsEmpty ? 0 : target.Weapon.Defense;

        /// <summary>
        /// 방어 굴림 (3-7c, SPD): 근접 피해 − (0~방어) 굴림, 0 아래로는 안 감. 방어 0이면 난수를 쓰지 않는다. 막은 양이 있으면 DamageBlockedEvent.
        /// 주문 피해에는 쓰지 않는다 (결정 2026-10-19).
        /// </summary>
        public static int ApplyDefense(World world, Unit target, int damage)
        {
            int defense = DefenseOf(target);
            if (defense <= 0 || damage <= 0) return damage;
            int blocked = System.Math.Min(damage, world.Rng.Get(RngStream.Combat).Range(0, defense + 1));
            if (blocked > 0) world.Events.Publish(new DamageBlockedEvent(target.Id, blocked));
            return damage - blocked;
        }

        /// <summary>남는 힘 (힘 − 요구 힘, 0 이상). 맨손 0.</summary>
        public static int ExcessStrength(Unit attacker) =>
            attacker.Weapon.IsEmpty ? 0 : System.Math.Max(0, attacker.Strength - attacker.Weapon.StrengthRequirement);

        /// <summary>모자란 힘 (요구 힘 − 힘, 0 이상). 맨손 0.</summary>
        public static int StrengthDeficit(Unit attacker) =>
            attacker.Weapon.IsEmpty ? 0 : System.Math.Max(0, attacker.Weapon.StrengthRequirement - attacker.Strength);

        /// <summary>힘 모자람에 따른 명중 배수 (3-7b, SPD): 모자란 1당 ÷ 1.5. 모자라지 않으면 1.</summary>
        public static float StrengthAccuracyFactor(Unit attacker)
        {
            int deficit = StrengthDeficit(attacker);
            return deficit == 0 ? 1f : (float)System.Math.Pow(1.5, -deficit);
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

    /// <summary>기습 공격 (3-7c) — 피해보다 먼저 발행 (로그 "기습!").</summary>
    public sealed class SurpriseAttackEvent : ISimEvent
    {
        public EntityId Attacker { get; }
        public EntityId Target { get; }

        public SurpriseAttackEvent(EntityId attacker, EntityId target)
        {
            Attacker = attacker;
            Target = target;
        }
    }

    /// <summary>방어로 피해를 막았다 (3-7c). Blocked = 줄인 양 (1 이상).</summary>
    public sealed class DamageBlockedEvent : ISimEvent
    {
        public EntityId Target { get; }
        public int Blocked { get; }

        public DamageBlockedEvent(EntityId target, int blocked)
        {
            Target = target;
            Blocked = blocked;
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
