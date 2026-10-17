using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Magic
{
    /// <summary>주문이 실제로 지나는 칸과 맞는 칸. 미리보기(입력 · 화면)와 시전(CastSpellCommand)이 같은 계산을 쓴다.</summary>
    public sealed class SpellPreview
    {
        /// <summary>투사체 · 광선이 지나는 칸 (시전자 칸 제외).</summary>
        public IReadOnlyList<GridPos> Path { get; }
        /// <summary>피해를 받는 칸. 이 칸들에 있는 유닛이 맞는다.</summary>
        public IReadOnlyList<GridPos> AffectedCells { get; }

        public SpellPreview(IReadOnlyList<GridPos> path, IReadOnlyList<GridPos> affectedCells)
        {
            Path = path;
            AffectedCells = affectedCells;
        }
    }

    /// <summary>시전할 수 없는 이유. 끝에만 추가한다.</summary>
    public enum CastBlock
    {
        None = 0,
        EmptySlot,
        UnknownSpell,
        NotEnoughMana,
        OutOfRange,
        NotVisible,
        TargetIsSelf,
        Blocked,
    }

    /// <summary>
    /// 주문 조준 · 시전 규칙 (2-2). 사거리 · 조준선은 ITargetingRule, 대상 칸이 보이는지는 ISightRule.
    /// </summary>
    public static class SpellCasting
    {
        /// <summary>caster에서 target을 조준했을 때의 경로와 맞는 칸.</summary>
        public static SpellPreview Preview(World world, ITargetingRule targeting, GridPos caster, SpellDef spell, GridPos target)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (targeting == null) throw new ArgumentNullException(nameof(targeting));
            if (spell == null) throw new ArgumentNullException(nameof(spell));
            var map = world.Map;
            var empty = new List<GridPos>();
            if (target == caster) return new SpellPreview(empty, empty);

            switch (spell.Shape)
            {
                case SpellShape.Bolt:
                {
                    var path = targeting.Trace(map, caster, target, spell.Range, world.Units.IsOccupied);
                    var hit = new List<GridPos>();
                    if (path.Count > 0 && world.Units.IsOccupied(path[path.Count - 1])) hit.Add(path[path.Count - 1]);
                    return new SpellPreview(path, hit);
                }
                case SpellShape.Beam:
                {
                    // 조준 방향으로 사거리 끝까지 늘린 점을 향해 쏜다 (벽 · 사거리에서 멈춤, 유닛은 관통)
                    int dx = target.X - caster.X, dy = target.Y - caster.Y;
                    int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    int k = (spell.Range + steps - 1) / steps;
                    var end = new GridPos(caster.X + dx * k, caster.Y + dy * k);
                    var path = targeting.Trace(map, caster, end, spell.Range, null);
                    var hit = new List<GridPos>();
                    foreach (var p in path)
                        if (world.Units.IsOccupied(p)) hit.Add(p);
                    return new SpellPreview(path, hit);
                }
                case SpellShape.Burst:
                {
                    var path = targeting.Trace(map, caster, target, spell.Range, world.Units.IsOccupied);
                    if (path.Count == 0) return new SpellPreview(path, empty);
                    var center = path[path.Count - 1];
                    var area = new List<GridPos>();
                    for (int y = center.Y - spell.Radius; y <= center.Y + spell.Radius; y++)
                        for (int x = center.X - spell.Radius; x <= center.X + spell.Radius; x++)
                        {
                            var p = new GridPos(x, y);
                            if (map.IsWalkable(p)) area.Add(p);
                        }
                    return new SpellPreview(path, area);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(spell), spell.Shape, "알 수 없는 주문 모양");
            }
        }

        /// <summary>
        /// 플레이어가 장착 칸 slot의 주문을 target에 쓸 수 있는지. 쓸 수 있으면 CastBlock.None과 주문 · 미리보기.
        /// </summary>
        public static CastBlock Check(World world, SpellBook book, ITargetingRule targeting, ISightRule sight,
            Unit caster, int slot, GridPos target, out SpellDef spell, out SpellPreview preview)
        {
            spell = null;
            preview = null;
            if (caster == null || slot < 0 || slot >= caster.SpellSlots.Length || caster.SpellSlots[slot] == null) return CastBlock.EmptySlot;
            if (!book.TryGet(caster.SpellSlots[slot], out spell)) return CastBlock.UnknownSpell;
            if (target == caster.Pos) return CastBlock.TargetIsSelf;
            if (!targeting.InRange(caster.Pos, target, spell.Range)) return CastBlock.OutOfRange;
            if (!world.Map.InBounds(target) || !sight.CanSee(world.Map, caster.Pos, target, caster.SightRange)) return CastBlock.NotVisible;
            if (caster.Mana < spell.ManaCost) return CastBlock.NotEnoughMana;

            preview = Preview(world, targeting, caster.Pos, spell, target);
            if (preview.Path.Count == 0) return CastBlock.Blocked;
            return CastBlock.None;
        }

        /// <summary>Check를 통과하면 시전 커맨드를 만든다. 행동 비용은 기본 행동 비용.</summary>
        public static CastBlock TryCreateCast(World world, GameConfig config, SpellBook book, ITargetingRule targeting, ISightRule sight,
            int slot, GridPos target, out ICommand command)
        {
            command = null;
            if (!PlayerActions.TryGetPlayer(world, out var player) || !player.IsAlive) return CastBlock.EmptySlot;
            var block = Check(world, book, targeting, sight, player, slot, target, out var spell, out _);
            if (block != CastBlock.None) return block;
            command = new CastSpellCommand(world, book, targeting, spell.Id, target, config.DefaultActionCostTicks);
            return CastBlock.None;
        }
    }

    /// <summary>
    /// 주문 시전 (2-2): 마나를 쓰고, 미리보기와 같은 계산으로 맞는 칸의 유닛에게 고정 피해 (항상 명중).
    /// Burst는 범위 안의 시전자도 맞는다 (규칙 10: 플레이어도 영향을 받는다). 실행 시점에 마나가 모자라면 아무 일 없이 차례만 쓴다.
    /// </summary>
    public sealed class CastSpellCommand : ICommand
    {
        private readonly World _world;
        private readonly SpellBook _book;
        private readonly ITargetingRule _targeting;

        public string SpellId { get; }
        public GridPos Target { get; }
        public int CostTicks { get; }

        public CastSpellCommand(World world, SpellBook book, ITargetingRule targeting, string spellId, GridPos target, int costTicks)
        {
            if (costTicks < 1) throw new ArgumentOutOfRangeException(nameof(costTicks), costTicks, "행동 비용은 1틱 이상이어야 한다.");
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _book = book ?? throw new ArgumentNullException(nameof(book));
            _targeting = targeting ?? throw new ArgumentNullException(nameof(targeting));
            SpellId = spellId;
            Target = target;
            CostTicks = costTicks;
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var caster) || !caster.IsAlive) return;
            if (!_book.TryGet(SpellId, out var spell)) return;
            if (!Mana.TrySpend(_world, caster, spell.ManaCost)) return;

            var preview = SpellCasting.Preview(_world, _targeting, caster.Pos, spell, Target);
            _world.Events.Publish(new SpellCastEvent(caster.Id, spell.Id, spell.Element, Target, preview));

            // 맞는 유닛을 먼저 모은 뒤 피해 (칸 순서 = 결정론적)
            var victims = new List<Unit>();
            foreach (var cell in preview.AffectedCells)
            {
                if (_world.Units.TryGetAt(cell, out var unit) && unit.IsAlive) victims.Add(unit);
            }
            // 순서 (REACTIONS.md 2-1): 원래 피해 → 반응 → 새 상태 → 지형
            foreach (var victim in victims)
                Combat.ApplyDamage(_world, caster.Id, victim, spell.Damage);

            // 반응 (3-1): 맞은 유닛의 기존 상태 · 주문이 닿은 칸의 지형 × 주문 속성
            var cells = TerrainCells(spell, preview);
            ReactionEngine.Resolve(_world, _book.Reactions, _book.Statuses, caster, spell, victims, cells);

            // 새 상태 이상 (살아 있으면). 확률은 맞은 유닛마다 전투 난수로 따로 (같은 시드 = 같은 결과)
            if (_book.Statuses.TryGet(spell.AppliesStatus, out var status))
            {
                foreach (var victim in victims)
                {
                    if (victim.IsAlive) StatusEffects.TryApply(_world, victim, status, spell.StatusChancePercent);
                }
            }

            // 지형 효과 (2-4): 놓인 틱의 ③에서 바로 그 칸 유닛에게 상태를 건다
            if (_book.Terrains.TryGet(spell.LeavesTerrain, out var terrain))
            {
                foreach (var cell in cells)
                    TerrainEffects.Place(_world, cell, terrain);
            }
        }

        /// <summary>지형이 남는 칸: Bolt = 투사체가 멈춘 칸, Beam = 지나간 칸 전부, Burst = 폭발 범위.</summary>
        public static IReadOnlyList<GridPos> TerrainCells(SpellDef spell, SpellPreview preview)
        {
            switch (spell.Shape)
            {
                case SpellShape.Bolt:
                    return preview.Path.Count > 0 ? new[] { preview.Path[preview.Path.Count - 1] } : Array.Empty<GridPos>();
                case SpellShape.Beam:
                    return preview.Path;
                default:
                    return preview.AffectedCells;
            }
        }
    }

    /// <summary>주문이 시전됐다 (표현: 경로 · 범위 연출). 피해는 UnitDamagedEvent로 따로 온다.</summary>
    public sealed class SpellCastEvent : ISimEvent
    {
        public EntityId Caster { get; }
        public string SpellId { get; }
        public Element Element { get; }
        public GridPos Target { get; }
        public SpellPreview Preview { get; }

        public SpellCastEvent(EntityId caster, string spellId, Element element, GridPos target, SpellPreview preview)
        {
            Caster = caster;
            SpellId = spellId;
            Element = element;
            Target = target;
            Preview = preview;
        }
    }
}
