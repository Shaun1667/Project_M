using System;
using System.Collections.Generic;
using ProjectM.Status;

namespace ProjectM.Magic
{
    /// <summary>기본 속성 7개 (CLAUDE.md 규칙 11). 끝에만 추가한다 (에셋이 정수로 저장).</summary>
    public enum Element
    {
        Fire = 0,
        Ice = 1,
        Lightning = 2,
        Poison = 3,
        Earth = 4,
        Wind = 5,
        Water = 6,
        /// <summary>
        /// 무속성 (3-21a, 결정 2026-11-05 — 루멘의 빛의 창). 속성이 아니다: 규칙 11의 7속성은 그대로이고,
        /// 반응 표에 줄이 없어 반응을 일으키지 않으며 적의 약점 · 저항(EnemyTraits)도 받지 않는다. 빛은 이름 · 연출뿐.
        /// </summary>
        Neutral = 7,
    }

    /// <summary>주문이 맞히는 모양 (2-2). 끝에만 추가한다.</summary>
    public enum SpellShape
    {
        /// <summary>단일 투사체: 조준선을 따라 날아가 처음 만난 유닛 하나.</summary>
        Bolt = 0,
        /// <summary>직선 관통: 조준 방향으로 사거리 끝(또는 벽)까지, 선 위의 모든 유닛.</summary>
        Beam = 1,
        /// <summary>범위 폭발: 투사체가 멈춘 칸 중심, 반지름(체비셰프) 안의 모든 유닛 — 시전자 포함.</summary>
        Burst = 2,
    }

    /// <summary>
    /// 주문 하나의 데이터 (2-2, Q4 임시값). 값은 SpellCatalogAsset에서 온다 — 기준은 BALANCE.md 5번.
    /// 상태 이상 · 지형 태그(2-3 · 2-4)는 이 데이터에 필드로 추가한다 — 마법끼리의 조합은 하드코딩하지 않는다 (규칙 9).
    /// </summary>
    public sealed class SpellDef
    {
        public string Id { get; }
        public Element Element { get; }
        public SpellShape Shape { get; }
        /// <summary>사거리 (체비셰프, ITargetingRule.InRange).</summary>
        public int Range { get; }
        /// <summary>맞은 유닛 하나당 고정 피해 (항상 명중 — 결정 2026-10-16).</summary>
        public int Damage { get; }
        public int ManaCost { get; }
        /// <summary>Burst 반지름. 다른 모양은 0.</summary>
        public int Radius { get; }
        /// <summary>맞은 유닛에 거는 상태 이상 (2-3). None이면 없음. 규칙은 StatusBook.</summary>
        public StatusType AppliesStatus { get; }
        /// <summary>상태가 걸릴 확률 (0~100, 맞은 유닛마다 따로). 100이면 난수를 굴리지 않는다.</summary>
        public int StatusChancePercent { get; }
        /// <summary>남기는 지형 효과 (2-4). Bolt = 멈춘 칸, Beam = 지나간 칸, Burst = 폭발 범위. None이면 없음. 규칙은 TerrainBook.</summary>
        public TerrainEffectType LeavesTerrain { get; }
        /// <summary>남기는 지형의 지속 (틱). 0이면 지형 규칙(TerrainDef)의 지속 (3-2b 융합: 불 + 물 화염 지대 6틱).</summary>
        public int TerrainDurationTicks { get; }
        /// <summary>맞으면 반응을 일으키는지. 융합 시전은 false (3-2b F4).</summary>
        public bool TriggersReactions { get; }
        /// <summary>
        /// 무기 강화 (3-21a, 결정 2026-11-05 — 빛의 창): 시전자가 이 무기(아이템 ID)를 들고 있으면 피해 × BoostDamagePercent% (내림) · 사거리 + BoostRange.
        /// null = 강화 없음. 적용은 SpellModifiers.For 한 곳 — 미리보기 · 시전이 같은 값을 쓴다.
        /// </summary>
        public string BoostWeaponId { get; }
        public int BoostDamagePercent { get; }
        public int BoostRange { get; }

        public SpellDef(string id, Element element, SpellShape shape, int range, int damage, int manaCost, int radius = 0,
            StatusType appliesStatus = StatusType.None, int statusChancePercent = 100, TerrainEffectType leavesTerrain = TerrainEffectType.None,
            int terrainDurationTicks = 0, bool triggersReactions = true, string boostWeaponId = null, int boostDamagePercent = 100, int boostRange = 0)
        {
            BoostWeaponId = string.IsNullOrEmpty(boostWeaponId) ? null : boostWeaponId;
            if (boostDamagePercent < 0) throw new ArgumentOutOfRangeException(nameof(boostDamagePercent), boostDamagePercent, "강화 피해 %는 0 이상.");
            if (boostRange < 0) throw new ArgumentOutOfRangeException(nameof(boostRange), boostRange, "강화 사거리는 0 이상.");
            BoostDamagePercent = boostDamagePercent;
            BoostRange = boostRange;
            if (statusChancePercent < 0 || statusChancePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(statusChancePercent), statusChancePercent, "상태 확률은 0 ~ 100.");
            AppliesStatus = appliesStatus;
            StatusChancePercent = statusChancePercent;
            LeavesTerrain = leavesTerrain;
            if (terrainDurationTicks < 0) throw new ArgumentOutOfRangeException(nameof(terrainDurationTicks), terrainDurationTicks, "지형 지속은 0(기본) 이상.");
            TerrainDurationTicks = terrainDurationTicks;
            TriggersReactions = triggersReactions;
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("주문 ID가 비어 있다.", nameof(id));
            if (range < 1) throw new ArgumentOutOfRangeException(nameof(range), range, "사거리는 1 이상이어야 한다.");
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage), damage, "피해는 0 이상이어야 한다.");
            if (manaCost < 0) throw new ArgumentOutOfRangeException(nameof(manaCost), manaCost, "마나 소모는 0 이상이어야 한다.");
            if (shape == SpellShape.Burst ? radius < 1 : radius != 0)
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "범위 폭발만 반지름 1 이상, 나머지는 0.");

            Id = id;
            Element = element;
            Shape = shape;
            Range = range;
            Damage = damage;
            ManaCost = manaCost;
            Radius = radius;
        }

        /// <summary>무기 강화를 적용한 사본 (SpellModifiers 전용) — 같은 ID, 강화 필드는 지워 두 번 적용되지 않게.</summary>
        internal SpellDef Boosted() => new SpellDef(Id, Element, Shape, Range + BoostRange, (int)((long)Damage * BoostDamagePercent / 100), ManaCost, Radius,
            AppliesStatus, StatusChancePercent, LeavesTerrain, TerrainDurationTicks, TriggersReactions);
    }

    /// <summary>
    /// 시전자에 따라 바뀌는 주문 값의 한 조회 지점 (3-21a — CLAUDE.md 코딩 규칙 "마법서 · 아이템 효과는 한 조회 지점"):
    /// 입력 미리보기 · 사거리 표시 · 실제 시전이 모두 이것을 거친다. 지금은 무기 강화(빛의 창 + 창)뿐 — 3-21c 특성도 여기에 더한다.
    /// </summary>
    public static class SpellModifiers
    {
        public static SpellDef For(ProjectM.Units.Unit caster, SpellDef spell)
        {
            if (spell == null || caster == null || spell.BoostWeaponId == null) return spell;
            return caster.Weapon.ItemId == spell.BoostWeaponId ? spell.Boosted() : spell; // 맨손 = Empty (ItemId null)
        }
    }

    /// <summary>
    /// 주문 ID → 데이터 + 주문이 거는 상태 규칙(StatusBook). 같은 ID가 두 번 나오거나, 거는 상태가 StatusBook에 없으면 예외.
    /// </summary>
    public sealed class SpellBook
    {
        private readonly Dictionary<string, SpellDef> _spells = new Dictionary<string, SpellDef>();

        /// <summary>주문이 거는 상태 이상의 규칙. 상태를 거는 주문이 없으면 비어 있다.</summary>
        public StatusBook Statuses { get; }

        /// <summary>주문이 남기는 지형 효과의 규칙. 없으면 비어 있다.</summary>
        public TerrainBook Terrains { get; }

        /// <summary>반응 규칙 표 (3-1). 없으면 비어 있다 — 반응 없음.</summary>
        public ReactionBook Reactions { get; }

        public SpellBook(IEnumerable<SpellDef> spells, StatusBook statuses = null, TerrainBook terrains = null, ReactionBook reactions = null)
        {
            if (spells == null) throw new ArgumentNullException(nameof(spells));
            Statuses = statuses ?? new StatusBook(Array.Empty<StatusDef>());
            Terrains = terrains ?? TerrainBook.Empty;
            Reactions = reactions ?? ReactionBook.Empty;
            foreach (var spell in spells)
            {
                if (spell == null) throw new ArgumentException("null 주문", nameof(spells));
                if (_spells.ContainsKey(spell.Id)) throw new ArgumentException("같은 주문 ID가 두 번: " + spell.Id, nameof(spells));
                if (spell.AppliesStatus != StatusType.None && !Statuses.TryGet(spell.AppliesStatus, out _))
                    throw new ArgumentException($"주문 {spell.Id}의 상태 {spell.AppliesStatus}가 StatusBook에 없다.", nameof(spells));
                if (spell.LeavesTerrain != TerrainEffectType.None && !Terrains.TryGet(spell.LeavesTerrain, out _))
                    throw new ArgumentException($"주문 {spell.Id}의 지형 {spell.LeavesTerrain}가 TerrainBook에 없다.", nameof(spells));
                _spells.Add(spell.Id, spell);
                _ids.Add(spell.Id);
            }
        }

        private readonly List<string> _ids = new List<string>();

        public int Count => _spells.Count;

        /// <summary>주문 ID, 데이터에 적힌 순서.</summary>
        public IReadOnlyList<string> Ids => _ids;

        public bool TryGet(string id, out SpellDef spell)
        {
            spell = null;
            return id != null && _spells.TryGetValue(id, out spell);
        }
    }
}
