using System;
using ProjectM.Core;
using ProjectM.Magic;

namespace ProjectM.Units
{
    /// <summary>유닛 생성: ID 발급 → World.Units 추가 → 스케줄러 등록 → EntitySpawnedEvent.</summary>
    public static class UnitSpawner
    {
        /// <summary>플레이어 생성. 빠른 칸 quickSlotCount개 · 인벤토리 inventorySize칸, 시작 주문은 둘 다에 앞쪽부터 (3-2a).</summary>
        public static Unit SpawnPlayer(World world, UnitDef def, GridPos pos, int quickSlotCount = 0, int inventorySize = MagicConfig.DefaultInventorySlotCount)
        {
            var unit = Create(world, def, Faction.Player, pos, quickSlotCount, inventorySize);
            world.Scheduler.RegisterPlayer(unit.Id, world.PlayerInput);
            Announce(world, unit);
            return unit;
        }

        public static Unit SpawnEnemy(World world, UnitDef def, GridPos pos, ICommandSource ai)
        {
            if (ai == null) throw new ArgumentNullException(nameof(ai));
            var unit = Create(world, def, Faction.Enemy, pos, 0, 0);
            world.Scheduler.RegisterActor(unit.Id, ai);
            Announce(world, unit);
            return unit;
        }

        private static Unit Create(World world, UnitDef def, Faction faction, GridPos pos, int quickSlotCount, int inventorySize)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (!world.Map.IsWalkable(pos)) throw new ArgumentException($"{pos}는 걸을 수 없는 칸이다.", nameof(pos));

            var unit = Unit.FromDef(world.Ids.Allocate(), faction, def, pos, quickSlotCount, inventorySize);
            world.Units.Add(unit);
            return unit;
        }

        private static void Announce(World world, Unit unit)
        {
            world.Events.Publish(new EntitySpawnedEvent(unit.Id, unit.Pos, unit.VisualId));
        }
    }
}
