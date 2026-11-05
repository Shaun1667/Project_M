using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Status;

namespace ProjectM.Units
{
    /// <summary>
    /// 특성 효과 종류 (3-21c, Docs/직업_루멘.md 4번). 특성은 이 효과 + 점수별 값 데이터로만 정한다 — 특성마다 분기 코드를 늘리지 않는다.
    /// 끝에만 추가한다 (에셋이 정수로 저장).
    /// </summary>
    public enum TalentEffect
    {
        None = 0,
        /// <summary>대상 주문 피해 + 값.</summary>
        SpellDamage = 1,
        /// <summary>대상 주문 사거리 + 값.</summary>
        SpellRange = 2,
        /// <summary>대상 주문 맞히는 횟수 + 값 (연속 찌르기).</summary>
        SpellHitCount = 3,
        /// <summary>대상 주문 최대 대상 수 + 값 (다중 발사).</summary>
        SpellMaxTargets = 4,
        /// <summary>대상 주문 마나 + 값 (음수 = 줄임, 0 아래로는 안 감).</summary>
        SpellManaCost = 5,
        /// <summary>대상 주문이 맞은 유닛에 Status를 값% 확률로 건다 (질주 3점 — 둔화).</summary>
        SpellStatusChance = 6,
        /// <summary>대상 주문으로 적을 쓰러뜨리면 마나 + 값.</summary>
        SpellKillMana = 7,
        /// <summary>근접 공격 · 대상 주문이 맞으면 값% 확률로 마나 +1.</summary>
        HitManaChance = 8,
        /// <summary>WeaponId 무기를 들면 근접 명중 + 값.</summary>
        WeaponAccuracy = 9,
        /// <summary>최대 체력 + 값 (찍을 때 바로 · 현재 체력도 같이).</summary>
        MaxHp = 10,
        /// <summary>자신을 못 본 적(기습과 같은 판정)에게 대상 주문 피해 + 값%.</summary>
        UnseenDamagePercent = 11,
        /// <summary>기본 근접 공격 피해 + 값 (3-22b 시아 연타 자세 — 타격마다, 방어 전).</summary>
        MeleeDamage = 12,
        /// <summary>기본 근접 공격이 맞으면 값% 확률로 한 번 더 친다 (3-22c 시아 빠른 손 — 공격 속도, 추가 타격은 다시 추가 타격을 부르지 않음).</summary>
        MeleeExtraHitChance = 13,
    }

    /// <summary>특성 하나의 효과 한 줄: 종류 · 찍은 점수별 값(Values[점수 − 1], 누적 값) · 대상 주문(비면 효과 종류에 따라 없음) · 무기 · 상태.</summary>
    public sealed class TalentEffectDef
    {
        public TalentEffect Effect { get; }
        public IReadOnlyList<int> Values { get; }
        public IReadOnlyList<string> SpellIds { get; }
        public string WeaponId { get; }
        public StatusType Status { get; }

        public TalentEffectDef(TalentEffect effect, IReadOnlyList<int> values, IReadOnlyList<string> spellIds = null, string weaponId = null,
            StatusType status = StatusType.None)
        {
            if (effect == TalentEffect.None) throw new ArgumentException("효과가 없다.", nameof(effect));
            if (values == null || values.Count == 0) throw new ArgumentException("점수별 값이 없다.", nameof(values));
            if (effect == TalentEffect.SpellStatusChance && status == StatusType.None) throw new ArgumentException("상태 효과에 상태가 없다.", nameof(status));
            Effect = effect;
            Values = values;
            SpellIds = spellIds ?? Array.Empty<string>();
            WeaponId = string.IsNullOrEmpty(weaponId) ? null : weaponId;
            Status = status;
        }

        /// <summary>points점일 때 값 (0점 = 0, 표보다 많으면 마지막 값).</summary>
        public int ValueAt(int points) => points <= 0 ? 0 : Values[Math.Min(points, Values.Count) - 1];

        public bool AppliesTo(string spellId)
        {
            foreach (var id in SpellIds) if (id == spellId) return true;
            return false;
        }
    }

    /// <summary>특성 하나: 단계(1 = 공통 · 2 = 형태) · 형태 ID(2단계만) · 최대 점수 · 효과.</summary>
    public sealed class TalentDef
    {
        public string Id { get; }
        public int Tier { get; }
        public string FormId { get; }
        public int MaxPoints { get; }
        public IReadOnlyList<TalentEffectDef> Effects { get; }

        public TalentDef(string id, int tier, int maxPoints, IReadOnlyList<TalentEffectDef> effects, string formId = null)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("특성 ID가 비어 있다.", nameof(id));
            if (tier < 1) throw new ArgumentOutOfRangeException(nameof(tier));
            if (maxPoints < 1) throw new ArgumentOutOfRangeException(nameof(maxPoints));
            if (effects == null || effects.Count == 0) throw new ArgumentException("효과가 없다.", nameof(effects));
            Id = id;
            Tier = tier;
            MaxPoints = maxPoints;
            Effects = effects;
            FormId = string.IsNullOrEmpty(formId) ? null : formId;
        }
    }

    /// <summary>단계의 점수를 얻는 레벨 범위 (3-21c — SPD: 1단계 레벨 2~6 = 5점, 2단계 7~12 = 6점).</summary>
    public readonly struct TalentTier
    {
        public int FirstLevel { get; }
        public int LastLevel { get; }

        public TalentTier(int firstLevel, int lastLevel)
        {
            if (firstLevel < 2 || lastLevel < firstLevel) throw new ArgumentOutOfRangeException(nameof(lastLevel), "단계 레벨은 2 ≤ 처음 ≤ 끝.");
            FirstLevel = firstLevel;
            LastLevel = lastLevel;
        }

        /// <summary>그 레벨까지 얻은 점수 (레벨업 1번 = 1점).</summary>
        public int PointsAt(int level) => level < FirstLevel ? 0 : Math.Min(level, LastLevel) - FirstLevel + 1;
    }

    /// <summary>특성을 찍은 결과.</summary>
    public enum SpendTalentResult
    {
        Spent = 0,
        NoClass = 1,
        UnknownTalent = 2,
        WrongForm = 3,
        Maxed = 4,
        NoPoints = 5,
    }

    /// <summary>
    /// 특성 (3-21c, 결정 2026-11-05 — SPD식 단계): 레벨업마다 1점, 단계마다 점수 레벨 범위가 있고 그 단계 특성에만 쓴다.
    /// 2단계는 형태를 고른 뒤 그 형태 특성에만. 찍은 점수는 RunState.Talents (스냅샷 v25). 찍기는 틱 밖 변경.
    /// 효과 조회는 여기 한 곳 — 주문 값은 SpellModifiers가, 근접 명중 · 마나는 Combat · AttackCommand가 이 함수들을 부른다.
    /// </summary>
    public static class Talents
    {
        /// <summary>그 단계에서 아직 안 쓴 점수.</summary>
        public static int Unspent(World world, ClassBook classes, Unit player, int tier)
        {
            if (world == null || player == null || classes == null || !classes.TryGet(world.Run.ClassId, out var def)) return 0;
            if (tier < 1 || tier > def.Tiers.Count) return 0;
            int spent = 0;
            foreach (var t in def.Talents)
                if (t.Tier == tier) spent += world.Run.TalentPoints(t.Id);
            return Math.Max(0, def.Tiers[tier - 1].PointsAt(player.Level) - spent);
        }

        /// <summary>찍을 수 있는 특성 (단계 순 · 표 순) — 1단계 공통 + 고른 형태의 2단계.</summary>
        public static List<TalentDef> Available(World world, ClassBook classes)
        {
            var ordered = new List<TalentDef>();
            if (world == null || classes == null || !classes.TryGet(world.Run.ClassId, out var def)) return ordered;
            for (int tier = 1; tier <= def.Tiers.Count; tier++)
                foreach (var t in def.Talents)
                    if (t.Tier == tier && (t.FormId == null || t.FormId == world.Run.FormId)) ordered.Add(t);
            return ordered;
        }

        /// <summary>특성 하나에 1점. 최대 체력 특성이면 바로 오른다. TalentSpentEvent.</summary>
        public static SpendTalentResult Spend(World world, ClassBook classes, Unit player, string talentId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (classes == null || !classes.TryGet(world.Run.ClassId, out var def)) return SpendTalentResult.NoClass;
            var talent = def.TalentOf(talentId);
            if (talent == null) return SpendTalentResult.UnknownTalent;
            if (talent.FormId != null && talent.FormId != world.Run.FormId) return SpendTalentResult.WrongForm;
            int points = world.Run.TalentPoints(talent.Id);
            if (points >= talent.MaxPoints) return SpendTalentResult.Maxed;
            if (Unspent(world, classes, player, talent.Tier) <= 0) return SpendTalentResult.NoPoints;

            world.Run.SetTalentPoints(talent.Id, points + 1);
            foreach (var e in talent.Effects)
            {
                if (e.Effect != TalentEffect.MaxHp) continue;
                int gain = e.ValueAt(points + 1) - e.ValueAt(points);
                player.MaxHp += gain;
                player.Hp = Math.Min(player.MaxHp, player.Hp + gain);
            }
            world.Events.Publish(new TalentSpentEvent(player.Id, talent.Id, points + 1, talent.MaxPoints, player.Hp, player.MaxHp));
            return SpendTalentResult.Spent;
        }

        /// <summary>찍은 특성의 그 효과 값 합 (filter가 true인 효과 줄만). 플레이어(진영)만 특성이 있다.</summary>
        public static int Sum(World world, Unit unit, TalentEffect effect, Func<TalentEffectDef, bool> filter = null)
        {
            if (world == null || unit == null || unit.Faction != Faction.Player || world.Classes == null) return 0;
            if (!world.Classes.TryGet(world.Run.ClassId, out var def)) return 0;
            int total = 0;
            foreach (var t in def.Talents)
            {
                int points = world.Run.TalentPoints(t.Id);
                if (points <= 0) continue;
                foreach (var e in t.Effects)
                    if (e.Effect == effect && (filter == null || filter(e))) total += e.ValueAt(points);
            }
            var form = world.Run.FormId != null ? def.FormOf(world.Run.FormId) : null; // 3-22b: 고른 형태의 늘 켜진 효과 (1점 값)
            if (form != null)
                foreach (var e in form.PassiveEffects)
                    if (e.Effect == effect && (filter == null || filter(e))) total += e.ValueAt(1);
            return total;
        }

        /// <summary>기본 근접 공격 피해 보너스 (3-22b 연타 자세).</summary>
        public static int MeleeDamageBonus(World world, Unit attacker) => Sum(world, attacker, TalentEffect.MeleeDamage);

        /// <summary>기본 근접 공격 추가 타격 확률 % (3-22c 빠른 손).</summary>
        public static int MeleeExtraHitPercent(World world, Unit attacker) => Sum(world, attacker, TalentEffect.MeleeExtraHitChance);

        /// <summary>주문 하나에 걸린 효과 값 합.</summary>
        public static int ForSpell(World world, Unit caster, TalentEffect effect, string spellId) =>
            Sum(world, caster, effect, e => e.AppliesTo(spellId));

        /// <summary>근접 명중 보너스 (든 무기가 WeaponId인 효과만).</summary>
        public static int AccuracyBonus(World world, Unit attacker) =>
            attacker == null || attacker.Weapon.IsEmpty ? 0 : Sum(world, attacker, TalentEffect.WeaponAccuracy, e => e.WeaponId == attacker.Weapon.ItemId);

        /// <summary>근접 공격이 맞았을 때 마나 +1 확률 % (대상 주문 없이 적은 HitManaChance 줄).</summary>
        public static int MeleeHitManaPercent(World world, Unit attacker) =>
            Sum(world, attacker, TalentEffect.HitManaChance);

        /// <summary>주문에 거는 특성 상태 (첫 줄) · 확률. 없으면 None.</summary>
        public static (StatusType Status, int Chance) SpellStatus(World world, Unit caster, string spellId)
        {
            if (world == null || caster == null || caster.Faction != Faction.Player || world.Classes == null || !world.Classes.TryGet(world.Run.ClassId, out var def))
                return (StatusType.None, 0);
            foreach (var t in def.Talents)
            {
                int points = world.Run.TalentPoints(t.Id);
                foreach (var e in t.Effects)
                    if (e.Effect == TalentEffect.SpellStatusChance && e.AppliesTo(spellId) && e.ValueAt(points) > 0) return (e.Status, e.ValueAt(points));
            }
            return (StatusType.None, 0);
        }
    }

    /// <summary>특성에 1점을 찍었다 (3-21c — HUD 로그 · 체력 막대).</summary>
    public sealed class TalentSpentEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public string TalentId { get; }
        public int Points { get; }
        public int MaxPoints { get; }
        public int Hp { get; }
        public int MaxHp { get; }

        public TalentSpentEvent(EntityId unit, string talentId, int points, int maxPoints, int hp, int maxHp)
        {
            Unit = unit;
            TalentId = talentId;
            Points = points;
            MaxPoints = maxPoints;
            Hp = hp;
            MaxHp = maxHp;
        }
    }
}
