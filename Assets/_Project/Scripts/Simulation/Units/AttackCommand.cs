using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 인접한 대상에게 근접 공격 (고정 피해 = 공격자의 AttackDamage). 대각선 공격도 이동과 같은 모서리 규칙을 따른다.
    /// 실행 시점에 대상이 없거나 · 죽었거나 · 닿지 않으면 아무 일 없이 차례만 쓴다.
    /// </summary>
    public sealed class AttackCommand : ICommand
    {
        private readonly World _world;

        public EntityId Target { get; }
        public int CostTicks { get; }

        public AttackCommand(World world, EntityId target, int costTicks)
        {
            if (costTicks < 1) throw new ArgumentOutOfRangeException(nameof(costTicks), costTicks, "행동 비용은 1틱 이상이어야 한다.");
            _world = world ?? throw new ArgumentNullException(nameof(world));
            Target = target;
            CostTicks = costTicks;
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var attacker) || !attacker.IsAlive) return;
            if (!_world.Units.TryGet(Target, out var target) || !target.IsAlive) return;
            if (!GridMovement.CanStep(_world.Map, attacker.Pos, target.Pos)) return;

            Combat.ApplyDamage(_world, attacker.Id, target, attacker.AttackDamage);
        }
    }
}
