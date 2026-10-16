using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// ⑥ 사망 · 승패 판정 (GDD 3-3). 체력 0인 유닛을 EntityId 순으로 맵 · 스케줄러에서 빼고 UnitDied → EntityRemoved를 발행한다.
    /// 플레이어가 빠지면 스케줄러에 플레이어가 없어지고 TurnClock이 멈춘다 — 패배 표시는 표현 레이어가 UnitDiedEvent로 한다.
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
            foreach (var unit in _world.Units.ToSortedList())
            {
                if (unit.IsAlive) continue;
                _world.Units.Remove(unit.Id);
                _world.Scheduler.Unregister(unit.Id);
                _world.Events.Publish(new UnitDiedEvent(unit.Id, unit.Faction));
                _world.Events.Publish(new EntityRemovedEvent(unit.Id));
            }
        }
    }
}
