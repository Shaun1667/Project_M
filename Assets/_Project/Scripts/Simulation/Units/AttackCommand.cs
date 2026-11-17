using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 인접한 대상에게 근접 공격 (고정 피해 = 공격자의 AttackDamage). 대각선 공격도 이동과 같은 모서리 규칙을 따른다.
    /// 3-7: 먼저 명중 판정(Combat.RollHit) — 빗나가면 피해 없이 AttackMissedEvent.
    /// 실행 시점에 대상이 없거나 · 죽었거나 · 닿지 않으면 아무 일 없이 차례만 쓴다.
    /// </summary>
    public sealed class AttackCommand : ICommand
    {
        private readonly World _world;

        public EntityId Target { get; }
        public int CostTicks { get; }
        /// <summary>이 공격에서 치는 횟수 (3-39 공격 지연 — 사이 2 · 시미터 1~2). 타격 수(너클 2)는 한 번 칠 때마다.</summary>
        public int Swings { get; }
        private readonly int _creditAfter; // −1 = 지연 몫을 건드리지 않음

        public AttackCommand(World world, EntityId target, int costTicks)
        {
            if (costTicks < 1) throw new ArgumentOutOfRangeException(nameof(costTicks), costTicks, "행동 비용은 1틱 이상이어야 한다.");
            _world = world ?? throw new ArgumentNullException(nameof(world));
            Target = target;
            CostTicks = costTicks;
            Swings = 1;
            _creditAfter = -1;
        }

        /// <summary>무기 공격 계획대로 (3-39, MeleeWeapon.PlanAttack — 플레이어).</summary>
        public AttackCommand(World world, EntityId target, MeleeWeapon.AttackPlan plan) : this(world, target, plan.Ticks)
        {
            if (plan.Swings < 1) throw new ArgumentOutOfRangeException(nameof(plan), plan.Swings, "치는 횟수는 1 이상.");
            Swings = plan.Swings;
            _creditAfter = plan.CreditAfter;
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var attacker) || !attacker.IsAlive) return;
            if (_creditAfter >= 0) attacker.AttackCredit = _creditAfter; // 3-39: 시간은 이미 썼다 (대상이 사라져도)
            if (!_world.Units.TryGet(Target, out var target) || !target.IsAlive) return;
            if (!MeleeWeapon.CanReach(_world, attacker, target.Pos)) return; // 3-7a: 창은 2칸

            bool surprise = MeleeWeapon.IsSurprise(attacker, target); // 3-7c: 못 본 적 = 항상 명중 (첫 타격 전에 정함 — 타격마다 같음)
            int hits = attacker.Weapon.HitsPerAttack * Swings; // 3-22a 너클: 한 번 공격에 2타 (명중 · 피해 · 방어 따로) · 3-39 지연 무기: 친 횟수만큼
            bool landed = false;
            for (int h = 0; h < hits && target.IsAlive && attacker.IsAlive; h++) landed |= Strike(attacker, target, surprise);
            // 3-22c 빠른 손: 맞혔으면 % 확률로 한 번 더 (추가 타격은 다시 굴리지 않음). 특성이 없으면 난수를 쓰지 않는다
            int extra = Talents.MeleeExtraHitPercent(_world, attacker);
            if (landed && extra > 0 && target.IsAlive && attacker.IsAlive && _world.Rng.Get(RngStream.Combat).Range(0, 100) < extra)
                Strike(attacker, target, surprise);
        }

        /// <summary>타격 하나: 명중 판정 → 피해 굴림(+ 형태 · 특성 근접 피해) → 방어 → 피해 · 명중 시 효과 · 맞힘 마나. 맞았으면 true.</summary>
        private bool Strike(Unit attacker, Unit target, bool surprise)
        {
            if (surprise) _world.Events.Publish(new SurpriseAttackEvent(attacker.Id, target.Id));
            else if (!Combat.RollHit(_world, attacker, target)) // 3-7 명중 · 회피 (SPD)
            {
                _world.Events.Publish(new AttackMissedEvent(attacker.Id, target.Id));
                return false;
            }
            int damage = MeleeWeapon.RollDamage(_world, attacker, surprise) + Talents.MeleeDamageBonus(_world, attacker); // 3-7a 굴림 · 3-22b 연타 자세
            damage = MeleeWeapon.ApplyDefense(_world, target, damage); // 3-7c: 방어 굴림 (육척봉)
            Combat.ApplyDamage(_world, attacker.Id, target, damage, DamageCause.Attack);
            if (damage > 0) _world.EnemyRules?.OnMeleeHit(_world, attacker, target); // 3-17b: 전갈 중독
            int manaPercent = Talents.MeleeHitManaPercent(_world, attacker); // 3-21c 창의 기세: 맞으면 % 확률로 마나 +1
            if (manaPercent > 0 && attacker.IsAlive && _world.Rng.Get(RngStream.Combat).Range(0, 100) < manaPercent)
                ProjectM.Magic.Mana.Restore(_world, attacker, 1);
            return true;
        }
    }
}
