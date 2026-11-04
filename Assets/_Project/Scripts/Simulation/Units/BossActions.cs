using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Status;

namespace ProjectM.Units
{
    /// <summary>
    /// 패턴 보스 행동 (3-17d, 결정 2026-11-04 — 5층 슬라임 소녀 · 10층 파라오 술사). 모두 EnemyTraits 데이터로 켠다:
    /// 힘 모으기(예고 칸 표시 → 다음 차례에 그 칸에 주문) · 순간이동 · 체력이 줄면 소환. 예고 칸은 Unit.ChargeTarget(스냅샷 v23)에 둔다.
    /// </summary>
    public static class BossActions
    {
        /// <summary>예고 칸: 주문이 center에서 터질 때 맞는 칸 (시전자 칸은 뺀다 — 자기 주문에 맞지 않음).</summary>
        public static SpellPreview ChargeArea(World world, SpellDef spell, GridPos center, GridPos self)
        {
            var preview = SpellCasting.PreviewAt(world, spell, center);
            var area = new List<GridPos>();
            foreach (var c in preview.AffectedCells) if (c != self) area.Add(c);
            return new SpellPreview(new List<GridPos> { center }, area);
        }
    }

    /// <summary>힘 모으기: 예고 칸을 정하고 바닥에 경고 지형을 깐다 (다음 차례에 ReleaseChargeCommand).</summary>
    public sealed class ChargeCommand : ICommand
    {
        private readonly World _world;
        private readonly SpellBook _spells;
        private readonly string _spellId;
        private readonly GridPos _center;
        public int CostTicks { get; }

        public ChargeCommand(World world, SpellBook spells, string spellId, GridPos center, int costTicks)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _spells = spells ?? throw new ArgumentNullException(nameof(spells));
            _spellId = spellId;
            _center = center;
            CostTicks = Math.Max(1, costTicks);
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var self) || !self.IsAlive || !_spells.TryGet(_spellId, out var spell)) return;
            self.ChargeTarget = _center;
            var area = BossActions.ChargeArea(_world, spell, _center, self.Pos);
            if (_spells.Terrains != null && _spells.Terrains.TryGet(TerrainEffectType.Warning, out var warning))
                foreach (var c in area.AffectedCells) TerrainEffects.Place(_world, c, warning);
            _world.Events.Publish(new BossChargingEvent(self.Id, _center));
        }
    }

    /// <summary>모은 힘을 푼다: 예고 칸에 주문 효과 (시전자 칸 제외) · 경고 지형을 지운다.</summary>
    public sealed class ReleaseChargeCommand : ICommand
    {
        private readonly World _world;
        private readonly SpellBook _spells;
        private readonly string _spellId;
        public int CostTicks { get; }

        public ReleaseChargeCommand(World world, SpellBook spells, string spellId, int costTicks)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _spells = spells ?? throw new ArgumentNullException(nameof(spells));
            _spellId = spellId;
            CostTicks = Math.Max(1, costTicks);
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var self) || !self.IsAlive || !self.ChargeTarget.HasValue) return;
            var center = self.ChargeTarget.Value;
            self.ChargeTarget = null;
            if (!_spells.TryGet(_spellId, out var spell)) return;
            var area = BossActions.ChargeArea(_world, spell, center, self.Pos);
            foreach (var c in SpellCasting.PreviewAt(_world, spell, center).AffectedCells)
                TerrainEffects.Remove(_world, c, TerrainEffectType.Warning);
            _world.Events.Publish(new SpellCastEvent(self.Id, spell.Id, spell.Element, center, area));
            SpellCasting.ApplyEffect(_world, _spells, self.Id, spell, area);
        }
    }

    /// <summary>순간이동: 플레이어에게서 떨어진 빈 칸으로 (칸은 RngStream.Ai, 후보 y → x 순).</summary>
    public sealed class TeleportCommand : ICommand
    {
        private readonly World _world;
        private readonly GridPos _away;
        private readonly int _minDistance;
        public int CostTicks { get; }

        public TeleportCommand(World world, GridPos away, int minDistance, int costTicks)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _away = away;
            _minDistance = minDistance;
            CostTicks = Math.Max(1, costTicks);
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var self) || !self.IsAlive) return;
            var map = _world.Map;
            var candidates = new List<GridPos>();
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (GridMap.IsGroundType(map.Get(p)) && !_world.Units.IsOccupied(p) && GridMovement.Chebyshev(p, _away) >= _minDistance
                        && GridMovement.Chebyshev(p, _away) <= _minDistance + 4)
                        candidates.Add(p);
                }
            if (candidates.Count == 0) return;
            var to = candidates[_world.Rng.Get(RngStream.Ai).Range(0, candidates.Count)];
            var from = self.Pos;
            _world.Units.Move(self.Id, to);
            _world.Events.Publish(new EntityMovedEvent(self.Id, from, to));
            _world.Events.Publish(new BossTeleportedEvent(self.Id, from, to));
        }
    }

    /// <summary>소환 (한 판에 한 번 — Unit.Summoned): 둘레 가장 가까운 빈 칸에 count마리.</summary>
    public sealed class SummonCommand : ICommand
    {
        private readonly World _world;
        private readonly UnitDef _def;
        private readonly int _count;
        private readonly ICommandSource _ai;
        public int CostTicks { get; }

        public SummonCommand(World world, UnitDef def, int count, ICommandSource ai, int costTicks)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _def = def ?? throw new ArgumentNullException(nameof(def));
            _count = count;
            _ai = ai ?? throw new ArgumentNullException(nameof(ai));
            CostTicks = Math.Max(1, costTicks);
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var self) || !self.IsAlive || self.Summoned) return;
            self.Summoned = true;
            int made = 0;
            for (int ring = 1; ring <= 3 && made < _count; ring++)
                for (int dy = -ring; dy <= ring && made < _count; dy++)
                    for (int dx = -ring; dx <= ring && made < _count; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring) continue;
                        var p = new GridPos(self.Pos.X + dx, self.Pos.Y + dy);
                        if (!_world.Map.InBounds(p) || !GridMap.IsGroundType(_world.Map.Get(p)) || _world.Units.IsOccupied(p)) continue;
                        UnitSpawner.SpawnEnemy(_world, _def, p, _ai);
                        made++;
                    }
            _world.Events.Publish(new BossSummonedEvent(self.Id, _def.VisualId, made));
        }
    }

    /// <summary>보스가 힘을 모은다 — 예고 칸 (로그 · 화면).</summary>
    public sealed class BossChargingEvent : ISimEvent
    {
        public EntityId Boss { get; }
        public GridPos Center { get; }

        public BossChargingEvent(EntityId boss, GridPos center)
        {
            Boss = boss;
            Center = center;
        }
    }

    public sealed class BossTeleportedEvent : ISimEvent
    {
        public EntityId Boss { get; }
        public GridPos From { get; }
        public GridPos To { get; }

        public BossTeleportedEvent(EntityId boss, GridPos from, GridPos to)
        {
            Boss = boss;
            From = from;
            To = to;
        }
    }

    public sealed class BossSummonedEvent : ISimEvent
    {
        public EntityId Boss { get; }
        public string KindId { get; }
        public int Count { get; }

        public BossSummonedEvent(EntityId boss, string kindId, int count)
        {
            Boss = boss;
            KindId = kindId;
            Count = count;
        }
    }
}
