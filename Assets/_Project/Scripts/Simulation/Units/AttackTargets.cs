using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 공격 대상 표시 (3-25, SPD — 결정 2026-11-05): 지금 자리에서 근접으로 칠 수 있는 보이는 적 고르기.
    /// 순서 = 마지막에 공격한 적(닿으면) → 가까운 순 → ID 순. 입력 · HUD가 같은 계산을 쓴다. 상태가 없다.
    /// </summary>
    public static class AttackTargets
    {
        /// <summary>근접으로 닿는 보이는 적 (가까운 순, 같으면 ID 순).</summary>
        public static List<Unit> InReach(World world, ISightRule sight, Unit player)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var list = new List<Unit>();
            if (player == null || !player.IsAlive) return list;
            foreach (var unit in world.Units.ToSortedList())
            {
                if (!unit.IsAlive || !Factions.AreHostile(unit.Faction, player.Faction)) continue;
                if (!MeleeWeapon.CanReach(world, player, unit.Pos)) continue;
                if (sight != null && !sight.CanSee(world.Map, player.Pos, unit.Pos, player.SightRange)) continue;
                list.Add(unit);
            }
            list.Sort((a, b) =>
            {
                int da = GridMovement.Chebyshev(player.Pos, a.Pos), db = GridMovement.Chebyshev(player.Pos, b.Pos);
                return da != db ? da.CompareTo(db) : a.Id.Value.CompareTo(b.Id.Value);
            });
            return list;
        }

        /// <summary>표시할 대상: lastAttacked가 닿으면 그 적, 아니면 가장 가까운 적. 없으면 null.</summary>
        public static Unit Pick(World world, ISightRule sight, Unit player, EntityId lastAttacked)
        {
            var list = InReach(world, sight, player);
            if (list.Count == 0) return null;
            if (lastAttacked.IsValid)
                foreach (var unit in list)
                    if (unit.Id == lastAttacked) return unit;
            return list[0];
        }

        /// <summary>Tab: current 다음 대상 (끝이면 처음). current가 목록에 없으면 첫 대상. 없으면 null.</summary>
        public static Unit Next(World world, ISightRule sight, Unit player, EntityId current)
        {
            var list = InReach(world, sight, player);
            if (list.Count == 0) return null;
            int index = list.FindIndex(u => u.Id == current);
            return list[(index + 1) % list.Count];
        }
    }
}
