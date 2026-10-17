using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// ⑥ 사망 · 승패 판정 (GDD 3-3). 체력 0인 유닛을 EntityId 순으로 맵 · 스케줄러에서 빼고 UnitDied → EntityRemoved를 발행한다.
    /// 플레이어가 빠지면 스케줄러에 플레이어가 없어지고 TurnClock이 멈춘다.
    /// 3-4b: 죽은 적은 처치 수에 더하고, 보스가 죽으면 승리 · 플레이어가 죽으면 패배 → World.Run.Outcome + RunEndedEvent (한 번만).
    /// 같은 틱에 둘 다면 패배가 먼저다 (EntityId 순 — 플레이어가 먼저 생성됨).
    /// </summary>
    public sealed class DeathCheckSystem : ITickSystem
    {
        private readonly World _world;

        public DeathCheckSystem(World world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public void Run(in TickContext context)
        {
            var run = _world.Run;
            foreach (var unit in _world.Units.ToSortedList())
            {
                if (unit.IsAlive) continue;
                _world.Units.Remove(unit.Id);
                _world.Scheduler.Unregister(unit.Id);
                _world.Events.Publish(new UnitDiedEvent(unit.Id, unit.Faction));
                _world.Events.Publish(new EntityRemovedEvent(unit.Id));

                if (unit.Faction == Faction.Enemy) run.Kills++;
                if (run.Outcome != RunOutcome.None) continue;
                if (unit.Faction == Faction.Player) End(RunOutcome.Defeat);
                else if (run.BossId.IsValid && unit.Id == run.BossId) End(RunOutcome.Victory);
            }
        }

        private void End(RunOutcome outcome)
        {
            _world.Run.Outcome = outcome;
            _world.Events.Publish(new RunEndedEvent(outcome));
        }
    }
}
