using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Items
{
    /// <summary>
    /// 시작 배낭 (2026-11-05 지스타 시연, 사용자 — 마법 융합기 · 물 마법서 · 치유 물약 + 무작위 마법서 한 권).
    /// 고정 아이템(UnitDef.StartingItems) + 무작위 후보(StartingRandomItems)에서 하나(RngStream.StartKit — 맵 · 적 · 아이템 배치 난수와 따로).
    /// </summary>
    public static class StartingKit
    {
        /// <summary>이번 판에 줄 아이템 ID 목록 (고정 → 무작위 하나 순, 같은 ID는 한 번).</summary>
        public static List<string> Roll(World world, UnitDef def)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (def == null) throw new ArgumentNullException(nameof(def));
            var kit = new List<string>(def.StartingItems);
            var pool = new List<string>();
            foreach (var id in def.StartingRandomItems) if (!kit.Contains(id)) pool.Add(id);
            if (pool.Count > 0) kit.Add(pool[world.Rng.Get(RngStream.StartKit).Range(0, pool.Count)]);
            return kit;
        }
    }
}
