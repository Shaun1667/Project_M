using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Status;

namespace ProjectM.Units
{
    /// <summary>
    /// 적 종류 하나의 행동 데이터 (3-17b, 결정 2026-11-04 — 구역당 4종 · 행동 다르게). 종류 ID = 유닛 VisualId.
    /// 수치는 ZoneCatalog.asset 적 줄 (BALANCE.md 20번). 행동 종류별 분기 코드를 늘리지 않고 필드 조합으로만 정한다.
    /// </summary>
    public sealed class EnemyTraits
    {
        public string Id { get; }
        public string DisplayName { get; }
        /// <summary>한 번 이동에 가는 칸 (1 · 2 — 2면 토끼 · 자칼처럼 빠름, 신속 물약과 같은 두 칸 이동).</summary>
        public int MoveSteps { get; }
        /// <summary>이동 비용 (틱). 2면 느림 (미라).</summary>
        public int MoveCostTicks { get; }
        /// <summary>근접 공격이 맞으면 거는 상태 · 확률 (전갈 중독).</summary>
        public StatusType HitStatus { get; }
        public int HitStatusPercent { get; }
        /// <summary>피해를 받으면 자기 칸에 까는 지형 (슬라임 물).</summary>
        public TerrainEffectType HurtTerrain { get; }
        /// <summary>원거리 주문 (SpellCatalog). 마나를 쓰고(UnitDef.MaxMana) 보이는 플레이어가 사거리 안이면 쏜다.</summary>
        public string SpellId { get; }
        /// <summary>플레이어와 이 거리보다 가까우면 물러난다 (0 = 안 물러남, 주술사).</summary>
        public int KeepDistance { get; }
        private readonly int[] _damagePercent = new int[Enum.GetValues(typeof(Element)).Length];
        /// <summary>3-17d 패턴 보스: 힘 모으기 주문 · 차례마다 시작 확률 % (예고 칸 → 다음 차례에 터짐).</summary>
        public string ChargeSpellId { get; }
        public int ChargePercent { get; }
        /// <summary>플레이어가 붙어 있을 때 순간이동 확률 %.</summary>
        public int TeleportPercent { get; }
        /// <summary>체력이 SummonAtHpPercent% 이하가 되면 한 번 소환 (종류 · 수).</summary>
        public UnitDef SummonDef { get; }
        public int SummonCount { get; }
        public int SummonAtHpPercent { get; }

        public EnemyTraits(string id, string displayName = null, int moveSteps = 1, int moveCostTicks = 1,
            StatusType hitStatus = StatusType.None, int hitStatusPercent = 0, TerrainEffectType hurtTerrain = TerrainEffectType.None,
            string spellId = null, int keepDistance = 0, IReadOnlyDictionary<Element, int> damagePercent = null,
            string chargeSpellId = null, int chargePercent = 0, int teleportPercent = 0, UnitDef summonDef = null, int summonCount = 0, int summonAtHpPercent = 0)
        {
            if (chargePercent < 0 || chargePercent > 100) throw new ArgumentOutOfRangeException(nameof(chargePercent));
            if (teleportPercent < 0 || teleportPercent > 100) throw new ArgumentOutOfRangeException(nameof(teleportPercent));
            ChargeSpellId = string.IsNullOrEmpty(chargeSpellId) ? null : chargeSpellId;
            ChargePercent = chargePercent;
            TeleportPercent = teleportPercent;
            SummonDef = summonDef;
            SummonCount = Math.Max(0, summonCount);
            SummonAtHpPercent = Math.Max(0, summonAtHpPercent);
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("적 종류 ID가 비어 있다.", nameof(id));
            if (moveSteps < 1 || moveSteps > 2) throw new ArgumentOutOfRangeException(nameof(moveSteps), moveSteps, "이동 칸은 1 · 2");
            if (moveCostTicks < 1) throw new ArgumentOutOfRangeException(nameof(moveCostTicks));
            if (hitStatusPercent < 0 || hitStatusPercent > 100) throw new ArgumentOutOfRangeException(nameof(hitStatusPercent));
            if (keepDistance < 0) throw new ArgumentOutOfRangeException(nameof(keepDistance));
            Id = id;
            DisplayName = string.IsNullOrEmpty(displayName) ? id : displayName;
            MoveSteps = moveSteps;
            MoveCostTicks = moveCostTicks;
            HitStatus = hitStatus;
            HitStatusPercent = hitStatusPercent;
            HurtTerrain = hurtTerrain;
            SpellId = string.IsNullOrEmpty(spellId) ? null : spellId;
            KeepDistance = keepDistance;
            for (int i = 0; i < _damagePercent.Length; i++) _damagePercent[i] = 100;
            if (damagePercent != null)
                foreach (var pair in damagePercent)
                {
                    if (pair.Value < 0) throw new ArgumentOutOfRangeException(nameof(damagePercent));
                    if (pair.Key == Element.Neutral) throw new ArgumentException("무속성은 약점 · 저항을 받지 않는다 (3-21a).", nameof(damagePercent));
                    _damagePercent[(int)pair.Key] = pair.Value;
                }
        }

        /// <summary>그 속성 피해 배수 % (약점 > 100, 저항 &lt; 100). 기본 100.</summary>
        public int DamagePercent(Element element) => _damagePercent[(int)element];
    }

    /// <summary>적 종류 표 (3-17b). 상태가 없다.</summary>
    public sealed class EnemyBook
    {
        private readonly Dictionary<string, EnemyTraits> _traits = new Dictionary<string, EnemyTraits>();

        public EnemyBook(IEnumerable<EnemyTraits> traits)
        {
            if (traits == null) throw new ArgumentNullException(nameof(traits));
            foreach (var t in traits)
            {
                if (_traits.ContainsKey(t.Id)) throw new ArgumentException($"적 종류 '{t.Id}'가 두 번 있다.", nameof(traits));
                _traits.Add(t.Id, t);
            }
        }

        public bool TryGet(string id, out EnemyTraits traits)
        {
            traits = null;
            return id != null && _traits.TryGetValue(id, out traits);
        }
    }

    /// <summary>
    /// 적 규칙 연결 (3-17b): 속성 피해 배수 · 맞으면 지형 · 근접 명중 시 상태. World.SetEnemyRules로 넣는다 — 규칙 연결이라 저장하지 않는다.
    /// 피해 속성: 주문 = 주문 속성, 상태 = 화상 불 · 중독 독, 지형 = 화염 지대 불 · 독구름 독. 반응 · 공격 · 아이템은 배수 없음.
    /// </summary>
    public sealed class EnemyRules
    {
        private readonly EnemyBook _enemies;
        private readonly SpellBook _spells;
        private readonly StatusBook _statuses;
        private readonly TerrainBook _terrains;

        public EnemyBook Enemies => _enemies;

        public EnemyRules(EnemyBook enemies, SpellBook spells, StatusBook statuses, TerrainBook terrains)
        {
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _spells = spells;
            _statuses = statuses;
            _terrains = terrains;
        }

        /// <summary>피해 속성. 없으면 false.</summary>
        public bool TryElementOf(DamageCause cause, out Element element)
        {
            element = Element.Fire;
            switch (cause.Kind)
            {
                case DamageKind.Spell:
                    if (_spells == null || !_spells.TryGet(cause.Id, out var spell)) return false;
                    element = spell.Element;
                    return true;
                case DamageKind.Status:
                    if (cause.Status == StatusType.Burn) { element = Element.Fire; return true; }
                    if (cause.Status == StatusType.Poison) { element = Element.Poison; return true; }
                    return false;
                case DamageKind.Terrain:
                    if (cause.Terrain == TerrainEffectType.FireZone) { element = Element.Fire; return true; }
                    if (cause.Terrain == TerrainEffectType.PoisonCloud) { element = Element.Poison; return true; }
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>속성 배수 적용 (내림). 적 종류가 아니거나 속성이 없으면 그대로.</summary>
        public int ModifyDamage(Unit target, DamageCause cause, int amount)
        {
            if (amount <= 0 || !_enemies.TryGet(target.VisualId, out var traits) || !TryElementOf(cause, out var element)) return amount;
            return (int)((long)amount * traits.DamagePercent(element) / 100);
        }

        /// <summary>피해를 받고 살아 있으면: 자기 칸에 지형 (슬라임 물).</summary>
        public void OnHurt(World world, Unit target)
        {
            if (!_enemies.TryGet(target.VisualId, out var traits) || traits.HurtTerrain == TerrainEffectType.None || _terrains == null) return;
            if (_terrains.TryGet(traits.HurtTerrain, out var def)) TerrainEffects.Place(world, target.Pos, def);
        }

        /// <summary>근접 공격이 맞았다: 공격자 종류의 상태를 확률로 (난수 RngStream.Combat — StatusEffects.TryApply).</summary>
        public void OnMeleeHit(World world, Unit attacker, Unit target)
        {
            if (!target.IsAlive || !_enemies.TryGet(attacker.VisualId, out var traits) || traits.HitStatus == StatusType.None || _statuses == null) return;
            if (_statuses.TryGet(traits.HitStatus, out var def)) StatusEffects.TryApply(world, target, def, traits.HitStatusPercent);
        }
    }
}
