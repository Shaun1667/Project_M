using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>유닛 생성: ID 발급 → World.Units 추가 → 스케줄러 등록 → EntitySpawnedEvent.</summary>
    public static class UnitSpawner
    {
        /// <summary>플레이어 생성. 주문 장착 칸 spellSlotCount개를 시작 주문으로 앞쪽부터 채운다 (2-1).</summary>
        public static Unit SpawnPlayer(World world, UnitDef def, GridPos pos, int spellSlotCount = 0)
        {
            var unit = Create(world, def, Faction.Player, pos, spellSlotCount);
            world.Scheduler.RegisterPlayer(unit.Id, world.PlayerInput);
            Announce(world, unit);
            return unit;
        }

        public static Unit SpawnEnemy(World world, UnitDef def, GridPos pos, ICommandSource ai)
        {
            if (ai == null) throw new ArgumentNullException(nameof(ai));
            var unit = Create(world, def, Faction.Enemy, pos, 0);
            world.Scheduler.RegisterActor(unit.Id, ai);
            Announce(world, unit);
            return unit;
        }

        private static Unit Create(World world, UnitDef def, Faction faction, GridPos pos, int spellSlotCount)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (!world.Map.IsWalkable(pos)) throw new ArgumentException($"{pos}는 걸을 수 없는 칸이다.", nameof(pos));

            var unit = Unit.FromDef(world.Ids.Allocate(), faction, def, pos, spellSlotCount);
            world.Units.Add(unit);
            return unit;
        }

        private static void Announce(World world, Unit unit)
        {
            world.Events.Publish(new EntitySpawnedEvent(unit.Id, unit.Pos, unit.VisualId));
        }
    }
}
