using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>석상 방 수치 (3-48, SPD Statue). 값은 FloorDecorConfigAsset에서 — 기준은 BALANCE.md 16번.</summary>
    public sealed class StatueConfig
    {
        /// <summary>석상 (적 표 ID = 모습 ID).</summary>
        public string StatueId { get; }
        /// <summary>석상 세기 % = 100 + 층마다 × (층 − 1) — UnitDef.Scaled.</summary>
        public int PercentPerFloor { get; }
        /// <summary>떨어뜨리는 무기 티어 — FloorsPerTier 층마다 다음 칸 (마지막 칸은 그 뒤 모두).</summary>
        public IReadOnlyList<int> WeaponTiers { get; }
        public int FloorsPerTier { get; }
        /// <summary>떨어뜨리는 무기 강화 최소 ~ 최대.</summary>
        public int UpgradeMin { get; }
        public int UpgradeMax { get; }

        public StatueConfig(string statueId, int percentPerFloor, IReadOnlyList<int> weaponTiers, int floorsPerTier, int upgradeMin, int upgradeMax)
        {
            if (string.IsNullOrEmpty(statueId)) throw new ArgumentException("석상 ID가 비어 있다", nameof(statueId));
            if (percentPerFloor < 0) throw new ArgumentOutOfRangeException(nameof(percentPerFloor));
            if (weaponTiers == null || weaponTiers.Count == 0) throw new ArgumentException("무기 티어가 없다", nameof(weaponTiers));
            foreach (var t in weaponTiers) if (t < 1) throw new ArgumentOutOfRangeException(nameof(weaponTiers), "티어는 1 이상");
            if (floorsPerTier < 1) throw new ArgumentOutOfRangeException(nameof(floorsPerTier));
            if (upgradeMin < 0 || upgradeMax < upgradeMin) throw new ArgumentOutOfRangeException(nameof(upgradeMax), "0 ≤ 최소 ≤ 최대");
            StatueId = statueId;
            PercentPerFloor = percentPerFloor;
            WeaponTiers = weaponTiers;
            FloorsPerTier = floorsPerTier;
            UpgradeMin = upgradeMin;
            UpgradeMax = upgradeMax;
        }

        public int TierFor(int floor) => WeaponTiers[Math.Min(WeaponTiers.Count - 1, Math.Max(0, (floor - 1) / FloorsPerTier))];

        public int PercentFor(int floor) => 100 + PercentPerFloor * Math.Max(0, floor - 1);
    }

    /// <summary>석상 방 규칙 연결 (3-48): 수치 · 석상 기본 수치 · 적 AI · 아이템 표(무기 티어). World.SetStatueRules — 규칙 연결이라 저장하지 않는다.</summary>
    public sealed class StatueRules
    {
        public StatueConfig Config { get; }
        public UnitDef Statue { get; }
        public ICommandSource Ai { get; }
        public ItemBook Items { get; }

        public StatueRules(StatueConfig config, UnitDef statue, ICommandSource ai, ItemBook items)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Statue = statue ?? throw new ArgumentNullException(nameof(statue));
            Ai = ai ?? throw new ArgumentNullException(nameof(ai));
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }
    }

    /// <summary>
    /// 석상 (3-48, 결정 2026-11-18 — SPD Statue): 석상 방 가운데에 잠든(Unit.Dormant) 적으로 선다. 잠든 동안 차례를 쉬고,
    /// 피해를 입거나(Combat.ApplyDamage) 플레이어가 바로 옆(8방향)에 오면(MeleeChaserAi) 깨어난다. 쓰러지면 그 층 티어 무기 하나(강화 UpgradeMin ~ Max)를
    /// 그 칸에 떨어뜨린다 — 무기 · 강화는 쓰러질 때 정한다(RngStream.Loot, 무기 = ItemBook 순서에서 그 티어 EquipWeapon 중 하나).
    /// </summary>
    public static class Statues
    {
        /// <summary>석상 방 가운데에 잠든 석상을 세운다. 규칙이 없거나 칸이 막혔으면 null.</summary>
        public static Unit Place(World world, GridPos pos)
        {
            var rules = world.StatueRules;
            if (rules == null || !world.Map.IsWalkable(pos) || world.Units.TryGetAt(pos, out _)) return null;
            var statue = UnitSpawner.SpawnEnemy(world, rules.Statue.Scaled(rules.Config.PercentFor(world.Floor)), pos, rules.Ai);
            statue.Dormant = true;
            return statue;
        }

        /// <summary>잠든 유닛을 깨운다. 깨웠으면 true.</summary>
        public static bool Wake(World world, Unit unit)
        {
            if (unit == null || !unit.Dormant) return false;
            unit.Dormant = false;
            world.Events.Publish(new StatueAwokenEvent(unit.Id));
            return true;
        }

        /// <summary>쓰러진 석상이면 무기를 떨어뜨린다 (DeathCheckSystem). 떨어뜨렸으면 그 아이템 ID, 아니면 null.</summary>
        public static string OnDied(World world, Unit unit)
        {
            var rules = world.StatueRules;
            if (rules == null || unit == null || unit.VisualId != rules.Config.StatueId) return null;
            int tier = rules.Config.TierFor(world.Floor);
            var choices = new List<string>();
            foreach (var id in rules.Items.Ids)
                if (rules.Items.TryGet(id, out var def) && def.Use == ItemUse.EquipWeapon && def.Weapon != null && def.Weapon.Tier == tier) choices.Add(id);
            if (choices.Count == 0) return null;
            var rng = world.Rng.Get(RngStream.Loot);
            var weapon = choices[rng.Range(0, choices.Count)];
            int upgrade = rules.Config.UpgradeMin + rng.Range(0, rules.Config.UpgradeMax - rules.Config.UpgradeMin + 1);
            world.FloorItems.Add(unit.Pos, weapon, upgrade: upgrade);
            world.Events.Publish(new FloorItemPlacedEvent(unit.Pos, weapon, Identification.VisualOf(world, weapon)));
            return weapon;
        }
    }

    /// <summary>석상이 깨어났다 (3-48 — 로그).</summary>
    public sealed class StatueAwokenEvent : ISimEvent
    {
        public EntityId Unit { get; }

        public StatueAwokenEvent(EntityId unit)
        {
            Unit = unit;
        }
    }
}
