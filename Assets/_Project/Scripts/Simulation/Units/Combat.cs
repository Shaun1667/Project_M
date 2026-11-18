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
            if (target.TimeFrozenTicks > 0) return; // 3-28c 회중시계: 멈춘 동안 무적
            ProjectM.Dungeon.Statues.Wake(world, target); // 3-48: 잠든 석상은 맞으면 깨어남
            ProjectM.Dungeon.AngryShopkeeper.OnDamaged(world, attacker, target, cause); // 3-29: 상인을 주문 · 던지기로 치면 화냄 (체력이 바뀐 뒤 이 피해가 들어감)
            if (world.EnemyRules != null) amount = world.EnemyRules.ModifyDamage(target, cause, amount); // 3-17b 약점 · 저항
            if (world.DebugInvulnerable && target.Faction == Faction.Player) amount = 0; // 디버그 무적
            if (ProjectM.Dungeon.Sentries.IsSentry(world, target)) amount = 0; // 3-49 감시자는 무적

            target.Hp = Math.Max(0, target.Hp - amount);
            if (target.Faction == Faction.Player && amount > 0)
            {
                world.Run.LastPlayerDamage = cause;
                world.Run.LastPlayerAttacker = attacker;
            }
            world.Events.Publish(new UnitDamagedEvent(target.Id, attacker, amount, target.Hp, target.MaxHp, cause));
            if (amount > 0 && target.IsAlive) world.OnUnitDamaged?.Invoke(target); // 3-13b: 잠이 깸 (StatusDef.WakesOnDamage)
            if (amount > 0 && target.IsAlive) world.EnemyRules?.OnHurt(world, target); // 3-17b: 슬라임 물
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
            int accuracy = attacker.Accuracy + Talents.AccuracyBonus(world, attacker); // 3-21c 창 다루기
            float accuracyRoll = rng.NextFloat() * accuracy * AccuracyPercent(attacker) / 100f * MeleeWeapon.StrengthAccuracyFactor(attacker);
            float evasionRoll = rng.NextFloat() * evasion * ProjectM.Items.Armors.EvasionFactor(target); // 3-10a: 갑옷 힘 모자람 1당 ÷ 1.5
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
            if (damage <= 0 || (defense <= 0 && target.Armor.IsEmpty)) return damage;
            int rolled = defense > 0 ? world.Rng.Get(RngStream.Combat).Range(0, defense + 1) : 0;
            rolled += ProjectM.Items.Armors.RollDefense(world, target); // 3-10a 갑옷 (무기 방어 다음에 굴림)
            int blocked = System.Math.Min(damage, rolled);
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

        /// <summary>공격 한 번의 비용 (틱): 무기 값, 맨손이면 defaultCost. 지연 무기(3-39)는 PlanAttack을 쓴다.</summary>
        public static int AttackCost(Unit attacker, int defaultCost) => PlanAttack(attacker, defaultCost).Ticks;

        /// <summary>공격 한 번의 계획 (3-39): 쓰는 틱 · 치는 횟수 · 끝난 뒤 남는 지연 몫(%).</summary>
        public readonly struct AttackPlan
        {
            public readonly int Ticks, Swings, CreditAfter;

            public AttackPlan(int ticks, int swings, int creditAfter)
            {
                Ticks = ticks;
                Swings = swings;
                CreditAfter = creditAfter;
            }
        }

        /// <summary>
        /// 공격 한 번에 쓰는 시간 (3-39, SPD 공격 지연 — 결정 2026-11-17 "새 규칙 추가"). 지연이 없는 무기 · 맨손은 (비용 틱, 1번, 몫 그대로).
        /// 지연 d%: 한 칸 = 100%. 쓸 틱 = 최소 1, 남은 몫으로 한 번도 못 치면 모자란 만큼 더 → 그 시간(몫 + 100 × 틱) 안에서 d마다 한 번씩 치고 남는 몫을 넘긴다.
        /// 사이 50 = 1틱에 2번, 시미터 80 = 1틱에 1 · 1 · 1 · 2번(평균 1.25), 글레이브 150 = 2틱 · 1틱 번갈아(평균 1.5). 난수를 쓰지 않는다.
        /// </summary>
        public static AttackPlan PlanAttack(Unit attacker, int defaultCost)
        {
            var w = attacker.Weapon;
            if (w.IsEmpty) return new AttackPlan(defaultCost, 1, attacker.AttackCredit);
            if (w.AttackDelayPercent <= 0) return new AttackPlan(w.AttackCostTicks, 1, attacker.AttackCredit);
            int delay = w.AttackDelayPercent;
            int credit = System.Math.Max(0, System.Math.Min(attacker.AttackCredit, delay - 1)); // 다른 무기에서 남은 몫이 커도 한 번 몫 안으로
            int ticks = System.Math.Max(1, (delay - credit + 99) / 100);
            int budget = credit + 100 * ticks;
            int swings = budget / delay;
            return new AttackPlan(ticks, swings, budget - swings * delay);
        }

        /// <summary>
        /// target 칸을 칠 수 있는지: 1칸 = 이동과 같은 모서리 규칙. 2칸 이상(창 2 · 채찍 3, 3-39) = 직선 · 대각선만, 사이 칸이 모두 비어 있고(벽 · 유닛 없음)
        /// 모든 걸음이 모서리 규칙을 통과할 때 (결정 2026-10-19, SPD).
        /// </summary>
        public static bool CanReach(World world, Unit attacker, GridPos target)
        {
            int distance = GridMovement.Chebyshev(attacker.Pos, target);
            if (distance == 1) return GridMovement.CanStep(world.Map, attacker.Pos, target);
            int reach = attacker.Weapon.IsEmpty ? 1 : attacker.Weapon.Reach;
            if (distance < 2 || distance > reach) return false;
            int dx = target.X - attacker.Pos.X, dy = target.Y - attacker.Pos.Y;
            if (dx != 0 && dy != 0 && System.Math.Abs(dx) != System.Math.Abs(dy)) return false; // (2, 1) 같은 칸은 사이 칸이 정해지지 않음
            int sx = System.Math.Sign(dx), sy = System.Math.Sign(dy);
            var from = attacker.Pos;
            for (int step = 1; step <= distance; step++)
            {
                var next = new GridPos(attacker.Pos.X + sx * step, attacker.Pos.Y + sy * step);
                if (!GridMovement.CanStep(world.Map, from, next)) return false;
                if (step < distance && world.Units.IsOccupied(next)) return false;
                from = next;
            }
            return true;
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
