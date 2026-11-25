using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Magic
{
    /// <summary>마법 강화 종류 (3-55 — 강화 주문서로 배운 주문을 강화). 끝에만 추가한다 (에셋 · 저장이 정수로).</summary>
    public enum SpellUpgradeKind
    {
        None = 0,
        /// <summary>피해 + 값 (피해가 있는 주문만).</summary>
        Damage = 1,
        /// <summary>범위: 폭발 · 내 둘레 = 반지름 + 값, 화살 = 관통 + 값, 여러 대상 = 대상 수 + 값. 줄기 · 돌진 · 도약은 없음.</summary>
        Area = 2,
        /// <summary>상태 확률 + 값% (상태를 거는 주문, 100% 미만만).</summary>
        StatusChance = 3,
        /// <summary>지속: 거는 상태 + 값 틱 · 남기는 지형 + 값 × TerrainTicksPerLevel 틱.</summary>
        Duration = 4,
        /// <summary>마나 − 값 (1 아래로는 안 감 — 마나가 2 이상인 주문만).</summary>
        ManaCost = 5,
        /// <summary>한 대상 연속 타격 + 값 (피해가 있는 주문만).</summary>
        HitCount = 6,
    }

    /// <summary>강화 종류 하나: 단계별 누적 값 (Values[단계 − 1]). 단계 수 = Values 길이.</summary>
    public sealed class SpellUpgradeDef
    {
        public SpellUpgradeKind Kind { get; }
        public IReadOnlyList<int> Values { get; }
        public int MaxLevel => Values.Count;

        public SpellUpgradeDef(SpellUpgradeKind kind, IReadOnlyList<int> values)
        {
            if (kind == SpellUpgradeKind.None) throw new ArgumentException("None은 강화 종류가 아니다.", nameof(kind));
            if (values == null || values.Count == 0) throw new ArgumentException("단계 값이 하나 이상 있어야 한다.", nameof(values));
            for (int i = 0; i < values.Count; i++)
                if (values[i] <= 0 || (i > 0 && values[i] < values[i - 1]))
                    throw new ArgumentOutOfRangeException(nameof(values), $"{kind}: 단계 값은 1 이상 · 줄지 않음 (마나는 줄이는 양).");
            Kind = kind;
            Values = values;
        }

        /// <summary>단계 level의 누적 값 (0이면 0).</summary>
        public int ValueAt(int level) => level <= 0 ? 0 : Values[Math.Min(level, Values.Count) - 1];
    }

    /// <summary>마법 강화 수치 (3-55 — SpellUpgradeConfig.asset, BALANCE 26번).</summary>
    public sealed class SpellUpgradeConfig
    {
        /// <summary>주문 하나에 강화 주문서를 쓸 수 있는 횟수 (모든 종류의 단계 합).</summary>
        public int MaxPerSpell { get; }
        /// <summary>한 번에 보여 주는 강화 카드 수.</summary>
        public int ChoiceCount { get; }
        /// <summary>지속 강화 한 단계 값당 지형 지속 틱 (상태는 1틱).</summary>
        public int TerrainTicksPerLevel { get; }
        public IReadOnlyList<SpellUpgradeDef> Upgrades { get; }

        public SpellUpgradeConfig(int maxPerSpell, int choiceCount, int terrainTicksPerLevel, IReadOnlyList<SpellUpgradeDef> upgrades)
        {
            if (maxPerSpell < 1) throw new ArgumentOutOfRangeException(nameof(maxPerSpell), maxPerSpell, "주문당 강화 횟수는 1 이상.");
            if (choiceCount < 1) throw new ArgumentOutOfRangeException(nameof(choiceCount), choiceCount, "카드 수는 1 이상.");
            if (terrainTicksPerLevel < 0) throw new ArgumentOutOfRangeException(nameof(terrainTicksPerLevel), terrainTicksPerLevel, "지형 틱은 0 이상.");
            if (upgrades == null) throw new ArgumentNullException(nameof(upgrades));
            var seen = new HashSet<SpellUpgradeKind>();
            foreach (var u in upgrades)
            {
                if (u == null) throw new ArgumentException("null 강화", nameof(upgrades));
                if (!seen.Add(u.Kind)) throw new ArgumentException("같은 강화 종류가 두 번: " + u.Kind, nameof(upgrades));
            }
            MaxPerSpell = maxPerSpell;
            ChoiceCount = choiceCount;
            TerrainTicksPerLevel = terrainTicksPerLevel;
            Upgrades = upgrades;
        }

        public bool TryGet(SpellUpgradeKind kind, out SpellUpgradeDef def)
        {
            foreach (var u in Upgrades)
                if (u.Kind == kind) { def = u; return true; }
            def = null;
            return false;
        }
    }

    /// <summary>강화 주문서로 주문을 강화한 결과 (3-55). 끝에만 추가한다.</summary>
    public enum SpellUpgradeResult
    {
        Done = 0,
        /// <summary>마법 강화 규칙이 없다 (World.SetSpellUpgradeRules 안 함).</summary>
        NoRules = 1,
        /// <summary>배우지 않은 주문 · 표에 없는 주문.</summary>
        NotOwned = 2,
        /// <summary>이 주문은 더 강화할 수 없다 (횟수 다 씀 · 맞는 강화 없음).</summary>
        Maxed = 3,
        /// <summary>지금 카드에 없는 강화.</summary>
        NotOffered = 4,
    }

    /// <summary>
    /// 마법 강화 (3-55, 결정 2026-11-24 — 사용자): 강화 주문서를 배운 주문에 쓰면 강화 카드 ChoiceCount장(기본 3) 중 하나를 고른다.
    /// 종류마다 단계(기본 3) · 주문당 MaxPerSpell번(기본 5). 상태 = RunState.SpellUpgrades (플레이어 것, 스냅샷 v35), 수치 = World.SpellUpgradeRules.
    /// 카드는 판 시드 · 주문 ID · 지금까지 강화한 횟수로만 정한다 (난수 흐름을 쓰지 않음 — 창을 닫았다 열어도 같은 카드).
    /// 효과는 ApplyTo 한 곳 — SpellModifiers.For(미리보기 · 시전)가 부르고, 융합은 ApplyToFusion.
    /// </summary>
    public static class SpellUpgrades
    {
        public static int Level(World world, string spellId, SpellUpgradeKind kind) =>
            world == null || spellId == null ? 0 : world.Run.SpellUpgradeLevel(spellId, kind);

        /// <summary>단계를 바로 정한다 (디버그 · 테스트 — 카드 · 횟수 검사 없음, 이벤트 없음). 0이면 0단계로.</summary>
        public static void SetLevel(World world, string spellId, SpellUpgradeKind kind, int level)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (string.IsNullOrEmpty(spellId) || kind == SpellUpgradeKind.None) throw new ArgumentException("주문 ID · 강화 종류가 비었다.");
            world.Run.SetSpellUpgradeLevel(spellId, kind, Math.Max(0, level));
        }

        /// <summary>이 주문에 쓴 강화 횟수 (모든 종류의 단계 합).</summary>
        public static int Count(World world, string spellId)
        {
            if (world == null || spellId == null) return 0;
            int total = 0;
            foreach (var u in world.Run.SpellUpgrades)
                if (u.SpellId == spellId) total += u.Level;
            return total;
        }

        /// <summary>이 강화가 주문 모양 · 값에 맞는지 (지금 단계 기준 — 마나는 1 아래로 못 내림, 상태 확률은 100%면 끝).</summary>
        public static bool Fits(SpellUpgradeConfig config, SpellDef spell, SpellUpgradeKind kind, int level)
        {
            if (config == null || spell == null || !config.TryGet(kind, out var def) || level >= def.MaxLevel) return false;
            switch (kind)
            {
                case SpellUpgradeKind.Damage:
                case SpellUpgradeKind.HitCount:
                    return spell.Damage > 0;
                case SpellUpgradeKind.Area:
                    return spell.Shape == SpellShape.Burst || spell.Shape == SpellShape.Around || spell.Shape == SpellShape.Bolt || spell.Shape == SpellShape.Multi;
                case SpellUpgradeKind.StatusChance:
                    return spell.AppliesStatus != StatusType.None && spell.StatusChancePercent + def.ValueAt(level) < 100;
                case SpellUpgradeKind.Duration:
                    return spell.AppliesStatus != StatusType.None || spell.LeavesTerrain != TerrainEffectType.None;
                case SpellUpgradeKind.ManaCost:
                    return spell.ManaCost - def.ValueAt(level) > 1;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 지금 고를 수 있는 강화 카드 (최대 ChoiceCount장, 같은 종류 없음). 횟수를 다 썼거나 맞는 강화가 없으면 빈 목록.
        /// 같은 판 · 같은 주문 · 같은 강화 횟수면 늘 같은 카드.
        /// </summary>
        public static IReadOnlyList<SpellUpgradeKind> Offer(World world, SpellBook spells, string spellId)
        {
            var result = new List<SpellUpgradeKind>();
            var config = world?.SpellUpgradeRules;
            if (config == null || spells == null || spellId == null || !spells.TryGet(spellId, out var spell)) return result;
            if (Count(world, spellId) >= config.MaxPerSpell) return result;
            foreach (var u in config.Upgrades)
                if (Fits(config, spell, u.Kind, Level(world, spellId, u.Kind))) result.Add(u.Kind);
            // 결정론적 섞기 (판 시드 · 주문 ID · 모든 주문 강화 횟수) — 난수 흐름은 건드리지 않는다
            ulong total = 0;
            foreach (var u in world.Run.SpellUpgrades) total += (ulong)u.Level;
            var rng = new Pcg32(world.Rng.Seed ^ Hash(spellId), total * 2 + 1);
            for (int i = 0; i < result.Count - 1; i++)
            {
                int j = rng.Range(i, result.Count);
                (result[i], result[j]) = (result[j], result[i]);
            }
            if (result.Count > config.ChoiceCount) result.RemoveRange(config.ChoiceCount, result.Count - config.ChoiceCount);
            return result;
        }

        /// <summary>강화할 수 있는지 미리 본다 (주문서를 쓰기 전).</summary>
        public static SpellUpgradeResult Check(World world, SpellBook spells, Unit unit, string spellId, SpellUpgradeKind kind)
        {
            if (world?.SpellUpgradeRules == null) return SpellUpgradeResult.NoRules;
            if (unit == null || spellId == null || !unit.HasSpell(spellId) || spells == null || !spells.TryGet(spellId, out _)) return SpellUpgradeResult.NotOwned;
            var offer = Offer(world, spells, spellId);
            if (offer.Count == 0) return SpellUpgradeResult.Maxed;
            foreach (var k in offer) if (k == kind) return SpellUpgradeResult.Done;
            return SpellUpgradeResult.NotOffered;
        }

        /// <summary>강화한다 (틱 안 — 강화 주문서 Consumables.Apply가 부름). SpellUpgradedEvent.</summary>
        public static SpellUpgradeResult Upgrade(World world, SpellBook spells, Unit unit, string spellId, SpellUpgradeKind kind)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var check = Check(world, spells, unit, spellId, kind);
            if (check != SpellUpgradeResult.Done) return check;
            int level = Level(world, spellId, kind) + 1;
            world.Run.SetSpellUpgradeLevel(spellId, kind, level);
            spells.TryGet(spellId, out var spell);
            world.Events.Publish(new SpellUpgradedEvent(unit.Id, spellId, kind, level, spell.Shape));
            return SpellUpgradeResult.Done;
        }

        /// <summary>
        /// 강화를 더한 주문 (플레이어가 시전할 때만 — 적 · 함정 · 없는 규칙이면 그대로). 바뀐 것이 없으면 같은 객체.
        /// 사거리는 건드리지 않는다 (플레이어 속성 주문은 이미 무한 — 3-25b).
        /// </summary>
        public static SpellDef ApplyTo(World world, Unit caster, SpellDef spell)
        {
            var config = world?.SpellUpgradeRules;
            if (config == null || caster == null || spell == null || caster.Id != world.Scheduler.PlayerId) return spell;
            int V(SpellUpgradeKind kind) => config.TryGet(kind, out var def) ? def.ValueAt(Level(world, spell.Id, kind)) : 0;
            int damage = V(SpellUpgradeKind.Damage), area = V(SpellUpgradeKind.Area), chance = V(SpellUpgradeKind.StatusChance),
                duration = V(SpellUpgradeKind.Duration), mana = V(SpellUpgradeKind.ManaCost), hits = V(SpellUpgradeKind.HitCount);
            if (damage == 0 && area == 0 && chance == 0 && duration == 0 && mana == 0 && hits == 0) return spell;
            bool radiusShape = spell.Shape == SpellShape.Burst || spell.Shape == SpellShape.Around;
            return spell.Upgraded(
                damage: spell.Damage + damage,
                manaCost: spell.ManaCost <= 1 ? spell.ManaCost : Math.Max(1, spell.ManaCost - mana),
                radius: spell.Radius + (radiusShape ? area : 0),
                hitCount: spell.HitCount + hits,
                maxTargets: spell.MaxTargets + (spell.Shape == SpellShape.Multi ? area : 0),
                statusChance: Math.Min(100, spell.StatusChancePercent + chance),
                pierce: spell.Pierce + (spell.Shape == SpellShape.Bolt ? area : 0),
                statusDurationBonus: spell.StatusDurationBonus + (spell.AppliesStatus != StatusType.None ? duration : 0),
                terrainDurationBonus: spell.TerrainDurationBonus + (spell.LeavesTerrain != TerrainEffectType.None ? duration * config.TerrainTicksPerLevel : 0));
        }

        /// <summary>
        /// 융합 주문에 두 재료의 강화를 더한다 (3-55, 결정 2026-11-25 — 사용자 "1번"). fused는 강화 안 한 재료로 만든 융합 주문.
        /// 피해 · 마나 절약은 두 재료 것을 더함 (융합 피해 · 마나가 원래 두 주문의 합이라서 — 마나는 재료마다 1 아래로 안 내림).
        /// 범위 · 연속 타격 · 상태 확률 · 지속은 두 재료 중 큰 단계를 융합 모양에 맞춰 (폭발 · 둘레 = 반경, 화살 = 관통, 여러 대상 = 대상 수) — 더하지 않는다.
        /// </summary>
        public static SpellDef ApplyToFusion(World world, Unit caster, SpellDef fused, SpellDef first, SpellDef second)
        {
            var config = world?.SpellUpgradeRules;
            if (config == null || caster == null || fused == null || first == null || second == null || caster.Id != world.Scheduler.PlayerId) return fused;
            int V(SpellDef spell, SpellUpgradeKind kind) => config.TryGet(kind, out var def) ? def.ValueAt(Level(world, spell.Id, kind)) : 0;
            int Best(SpellUpgradeKind kind) => Math.Max(V(first, kind), V(second, kind));
            int ManaSaved(SpellDef spell) => spell.ManaCost <= 1 ? 0 : Math.Min(spell.ManaCost - 1, V(spell, SpellUpgradeKind.ManaCost));
            int damage = V(first, SpellUpgradeKind.Damage) + V(second, SpellUpgradeKind.Damage), mana = ManaSaved(first) + ManaSaved(second);
            int area = Best(SpellUpgradeKind.Area), chance = Best(SpellUpgradeKind.StatusChance), duration = Best(SpellUpgradeKind.Duration), hits = Best(SpellUpgradeKind.HitCount);
            if (damage == 0 && area == 0 && chance == 0 && duration == 0 && mana == 0 && hits == 0) return fused;
            bool radiusShape = fused.Shape == SpellShape.Burst || fused.Shape == SpellShape.Around;
            return fused.Upgraded(
                damage: fused.Damage + (fused.Damage > 0 ? damage : 0),
                manaCost: Math.Max(1, fused.ManaCost - mana),
                radius: fused.Radius + (radiusShape ? area : 0),
                hitCount: fused.HitCount + (fused.Damage > 0 ? hits : 0),
                maxTargets: fused.MaxTargets + (fused.Shape == SpellShape.Multi ? area : 0),
                statusChance: fused.AppliesStatus != StatusType.None ? Math.Min(100, fused.StatusChancePercent + chance) : fused.StatusChancePercent,
                pierce: fused.Pierce + (fused.Shape == SpellShape.Bolt ? area : 0),
                statusDurationBonus: fused.StatusDurationBonus + (fused.AppliesStatus != StatusType.None ? duration : 0),
                terrainDurationBonus: fused.TerrainDurationBonus + (fused.LeavesTerrain != TerrainEffectType.None ? duration * config.TerrainTicksPerLevel : 0));
        }

        /// <summary>FNV-1a 64 (string.GetHashCode는 실행마다 달라 쓰지 않는다 — 규칙 7).</summary>
        private static ulong Hash(string text)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in text)
            {
                h ^= c;
                h *= 1099511628211UL;
            }
            return h;
        }
    }

    /// <summary>주문을 강화했다 (3-55). Level = 그 종류의 새 단계 (1부터).</summary>
    public sealed class SpellUpgradedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public string SpellId { get; }
        public SpellUpgradeKind Kind { get; }
        public int Level { get; }
        /// <summary>주문 모양 (글: 범위 강화가 화살이면 "관통", 여러 대상이면 "대상 수").</summary>
        public SpellShape Shape { get; }

        public SpellUpgradedEvent(EntityId unit, string spellId, SpellUpgradeKind kind, int level, SpellShape shape = SpellShape.Bolt)
        {
            Shape = shape;
            Unit = unit;
            SpellId = spellId;
            Kind = kind;
            Level = level;
        }
    }
}
