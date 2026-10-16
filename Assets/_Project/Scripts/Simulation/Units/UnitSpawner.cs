using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>유닛 생성: ID 발급 → World.Units 추가 → 스케줄러 등록 → EntitySpawnedEvent.</summary>
    public static class UnitSpawner
    {
        public static Unit SpawnPlayer(World world, UnitDef def, GridPos pos)
        {
            var unit = Create(world, def, Faction.Player, pos);
            world.Scheduler.RegisterPlayer(unit.Id, world.PlayerInput);
            Announce(world, unit);
            return unit;
        }

        public static Unit SpawnEnemy(World world, UnitDef def, GridPos pos, ICommandSource ai)
        {
            if (ai == null) throw new ArgumentNullException(nameof(ai));
            var unit = Create(world, def, Faction.Enemy, pos);
            world.Scheduler.RegisterActor(unit.Id, ai);
            Announce(world, unit);
            return unit;
        }

        private static Unit Create(World world, UnitDef def, Faction faction, GridPos pos)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (!world.Map.IsWalkable(pos)) throw new ArgumentException($"{pos}는 걸을 수 없는 칸이다.", nameof(pos));

            var unit = Unit.FromDef(world.Ids.Allocate(), faction, def, pos);
            world.Units.Add(unit);
            return unit;
        }

        private static void Announce(World world, Unit unit)
        {
            world.Events.Publish(new EntitySpawnedEvent(unit.Id, unit.Pos, unit.VisualId));
        }
    }
}
