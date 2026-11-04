using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Items
{
    /// <summary>
    /// 연금술 (3-16c, 결정 2026-11-04 — SPD 연금 솥): 씨앗 3개를 넣으면 가장 많이 넣은 씨앗의 ItemDef.BrewsInto 물약 하나 (같으면 먼저 고른 것).
    /// 만든 물약은 식별된다. 틱 밖 동작 — 입력이 부르고 이벤트를 비운다.
    /// </summary>
    public static class Alchemy
    {
        public const int SeedsPerBrew = 3;

        /// <summary>넣을 씨앗(배낭 항목, 같은 항목 여러 번 = 그만큼). 만든 물약 ID, 못 만들면 null.</summary>
        public static string Brew(World world, ItemBook items, Unit unit, IReadOnlyList<InventoryRef> seeds)
        {
            if (seeds == null || seeds.Count != SeedsPerBrew) return null;
            var counts = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (var s in seeds)
            {
                if (s.IsEmpty || !items.TryGet(s.Id, out var def) || def.Use != ItemUse.Seed || def.BrewsInto == null) return null;
                if (!counts.ContainsKey(s.Id)) { counts[s.Id] = 0; order.Add(s.Id); }
                counts[s.Id]++;
            }
            foreach (var pair in counts) // 배낭에 그만큼 있는지
            {
                int have = 0;
                foreach (var e in unit.InventorySlots) if (!e.IsEmpty && e.Id == pair.Key) have += e.Count;
                if (have < pair.Value) return null;
            }
            string best = order[0];
            foreach (var id in order) if (counts[id] > counts[best]) best = id;
            items.TryGet(best, out var bestDef);
            if (!items.TryGet(bestDef.BrewsInto, out _)) return null;

            foreach (var s in seeds) Inventory.RemoveAt(world, unit, Array.IndexOf(unit.InventorySlots, s));
            if (Inventory.TryAdd(world, null, items, unit, InventoryRef.Item(bestDef.BrewsInto)) < 0)
            {
                world.FloorItems.Add(unit.Pos, bestDef.BrewsInto);
                world.Events.Publish(new FloorItemPlacedEvent(unit.Pos, bestDef.BrewsInto, Identification.VisualOf(world, bestDef.BrewsInto)));
            }
            var look = world.Run.LookOf(bestDef.BrewsInto);
            if (Identification.Identify(world, bestDef.BrewsInto)) world.Events.Publish(new ItemIdentifiedEvent(bestDef.BrewsInto, look));
            world.Events.Publish(new PotionBrewedEvent(bestDef.BrewsInto));
            return bestDef.BrewsInto;
        }
    }

    /// <summary>물약을 만들었다 (3-16c, 로그).</summary>
    public sealed class PotionBrewedEvent : ISimEvent
    {
        public string ItemId { get; }

        public PotionBrewedEvent(string itemId)
        {
            ItemId = itemId;
        }
    }
}
