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
        /// <summary>빠른 칸에 주문이 아니라 아이템이 있다 (3-2a).</summary>
        NotASpell,
        /// <summary>인벤토리에 없는 주문 (3-2a).</summary>
        NotOwned,
        /// <summary>마법 융합기가 인벤토리에 없다 (3-2b).</summary>
        NoFusionDevice,
        /// <summary>두 주문의 속성 조합에 융합이 없다 (3-2b).</summary>
        NoFusionRecipe,
        /// <summary>같은 주문 두 개로는 융합하지 않는다 (3-2b).</summary>
        SameSpell,
    }

    /// <summary>
    /// 주문 조준 · 시전 규칙 (2-2). 사거리 · 조준선은 ITargetingRule, 대상 칸이 보이는지는 ISightRule.
    /// </summary>
    public static class SpellCasting
    {
        /// <summary>
        /// 시전자 없이 center에서 터지는 효과의 칸 (3-12a 함정): Burst = center 둘레 반지름 칸(걸을 수 있는 칸), 그 밖의 모양 = center 한 칸.
        /// 경로는 [center] — 지형 칸 규칙(TerrainCells)이 Bolt · Beam에서도 center만 고른다.
        /// </summary>
        public static SpellPreview PreviewAt(World world, SpellDef spell, GridPos center)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (spell == null) throw new ArgumentNullException(nameof(spell));
            var path = new List<GridPos> { center };
            var area = new List<GridPos>();
            int radius = spell.Shape == SpellShape.Burst ? spell.Radius : 0;
            for (int y = center.Y - radius; y <= center.Y + radius; y++)
                for (int x = center.X - radius; x <= center.X + radius; x++)
                {
                    var p = new GridPos(x, y);
                    if (world.Map.IsWalkable(p)) area.Add(p);
                }
            return new SpellPreview(path, area);
        }

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
                case SpellShape.Dash:
                {
                    // 3-21b 돌진: 조준선(첫 유닛에서 멈춤)을 달려간다 — 끝 칸이 유닛이면 그 유닛을 맞힘(멈추는 칸은 바로 앞), 아니면 이동만
                    var path = targeting.Trace(map, caster, target, spell.Range, world.Units.IsOccupied);
                    var hit = new List<GridPos>();
                    if (path.Count > 0 && world.Units.IsOccupied(path[path.Count - 1])) hit.Add(path[path.Count - 1]);
                    return new SpellPreview(path, hit);
                }
                case SpellShape.Multi:
                    return PreviewMulti(world, targeting, caster, spell, target);
                default:
                    throw new ArgumentOutOfRangeException(nameof(spell), spell.Shape, "알 수 없는 주문 모양");
            }
        }

        /// <summary>
        /// 3-21b 다중 발사: 시전자와 적대하는 살아 있는 유닛 중 사거리 안 · 조준선이 그 유닛에서 끝나는(앞이 막히지 않은) 유닛을
        /// 조준 칸의 유닛 먼저, 그다음 가까운 순(체비셰프) · EntityId 순으로 MaxTargets명. 경로 = 첫 대상까지 (없으면 빈 경로 = 막힘).
        /// </summary>
        private static SpellPreview PreviewMulti(World world, ITargetingRule targeting, GridPos caster, SpellDef spell, GridPos target)
        {
            var empty = new List<GridPos>();
            if (!world.Units.TryGetAt(caster, out var self)) return new SpellPreview(empty, empty);
            var candidates = new List<ProjectM.Units.Unit>();
            foreach (var unit in world.Units.ToSortedList())
            {
                if (!unit.IsAlive || !ProjectM.Units.Factions.AreHostile(self.Faction, unit.Faction)) continue;
                if (!targeting.InRange(caster, unit.Pos, spell.Range)) continue;
                var line = targeting.Trace(world.Map, caster, unit.Pos, spell.Range, world.Units.IsOccupied);
                if (line.Count > 0 && line[line.Count - 1] == unit.Pos) candidates.Add(unit);
            }
            candidates.Sort((a, b) =>
            {
                bool at = a.Pos == target, bt = b.Pos == target;
                if (at != bt) return at ? -1 : 1;
                int da = GridMovement.Chebyshev(caster, a.Pos), db = GridMovement.Chebyshev(caster, b.Pos);
                return da != db ? da.CompareTo(db) : a.Id.Value.CompareTo(b.Id.Value);
            });
            var hit = new List<GridPos>();
            for (int i = 0; i < candidates.Count && hit.Count < spell.MaxTargets; i++) hit.Add(candidates[i].Pos);
            var path = hit.Count > 0 ? targeting.Trace(world.Map, caster, hit[0], spell.Range, world.Units.IsOccupied) : empty;
            return new SpellPreview(path, hit);
        }

        /// <summary>돌진(3-21b)이 멈추는 칸: 맞힌 유닛이 있으면 그 바로 앞 칸(바로 옆이면 제자리), 없으면 경로 끝.</summary>
        public static GridPos DashLanding(GridPos caster, SpellPreview preview)
        {
            var path = preview.Path;
            if (path.Count == 0) return caster;
            if (preview.AffectedCells.Count == 0) return path[path.Count - 1];
            return path.Count >= 2 ? path[path.Count - 2] : caster;
        }

        /// <summary>
        /// 플레이어가 빠른 칸 slot의 주문을 target에 쓸 수 있는지. 쓸 수 있으면 CastBlock.None과 주문 · 미리보기.
        /// </summary>
        public static CastBlock Check(World world, SpellBook book, ITargetingRule targeting, ISightRule sight,
            Unit caster, int slot, GridPos target, out SpellDef spell, out SpellPreview preview)
        {
            spell = null;
            preview = null;
            if (caster == null || slot < 0 || slot >= caster.QuickSlots.Length || caster.QuickSlots[slot].IsEmpty) return CastBlock.EmptySlot;
            var spellId = caster.QuickSlotSpell(slot);
            if (spellId == null) return CastBlock.NotASpell;
            return CheckSpell(world, book, targeting, sight, caster, spellId, target, out spell, out preview);
        }

        /// <summary>인벤토리에 있는 주문 spellId를 target에 쓸 수 있는지 (3-2a: 빠른 칸에 없어도 인벤토리에서 바로 쓴다).</summary>
        public static CastBlock CheckSpell(World world, SpellBook book, ITargetingRule targeting, ISightRule sight,
            Unit caster, string spellId, GridPos target, out SpellDef spell, out SpellPreview preview)
        {
            spell = null;
            preview = null;
            if (caster == null || spellId == null) return CastBlock.EmptySlot;
            if (!caster.HasSpell(spellId)) return CastBlock.NotOwned;
            if (!book.TryGet(spellId, out spell)) return CastBlock.UnknownSpell;
            spell = SpellModifiers.For(world, caster, spell); // 3-21a 무기 강화 · 3-21c 특성 — 시전과 같은 값
            return CheckDef(world, targeting, sight, caster, spell, target, out preview);
        }

        /// <summary>주문 데이터 하나를 target에 쓸 수 있는지 — 대상 · 사거리 · 시야 · 마나 · 막힘 (데이터에 없는 융합 주문도 같은 판정, 3-2b).</summary>
        public static CastBlock CheckDef(World world, ITargetingRule targeting, ISightRule sight, Unit caster, SpellDef spell, GridPos target,
            out SpellPreview preview)
        {
            preview = null;
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

        /// <summary>
        /// 주문 효과를 미리보기 칸에 적용한다 — 시전(CastSpellCommand)과 함정(3-12a, 시전자 없음 = EntityId.None)이 같은 순서를 쓴다.
        /// 순서 (REACTIONS.md 2-1): 원래 피해 → 반응 → 새 상태 → 지형. 마나 · 시전 이벤트는 부른 쪽 몫.
        /// </summary>
        public static void ApplyEffect(World world, SpellBook book, EntityId casterId, SpellDef spell, SpellPreview preview)
        {
            // 맞는 유닛을 먼저 모은 뒤 피해 (칸 순서 = 결정론적)
            var victims = new List<Unit>();
            foreach (var cell in preview.AffectedCells)
            {
                if (world.Units.TryGetAt(cell, out var unit) && unit.IsAlive) victims.Add(unit);
            }
            world.Units.TryGet(casterId, out var caster); // 함정(시전자 없음)이면 null
            if (spell.Damage > 0) // 피해 0 주문(3-2 물)은 "피해 0" 이벤트를 내지 않는다
                foreach (var victim in victims)
                {
                    // 3-21c 그림자 사냥: 시전자를 못 본 적에게 피해 + % (근접 기습과 같은 판정, 맞히기 전에 본다)
                    int damage = spell.Damage;
                    if (spell.UnseenDamagePercent > 0 && caster != null && MeleeWeapon.IsSurprise(caster, victim))
                        damage = (int)((long)damage * (100 + spell.UnseenDamagePercent) / 100);
                    for (int h = 0; h < spell.HitCount && victim.IsAlive; h++) // 3-21b 연속 찌르기: 한 대상 여러 번
                        Combat.ApplyDamage(world, casterId, victim, damage, DamageCause.Spell(spell.Id));
                    if (caster == null || !caster.IsAlive) continue;
                    // 3-21c 마나 순환 · 창의 기세 (특성 — SpellModifiers가 채운 값, 데이터 주문은 0)
                    if (spell.ManaOnKill > 0 && !victim.IsAlive && Factions.AreHostile(caster.Faction, victim.Faction))
                        Mana.Restore(world, caster, spell.ManaOnKill);
                    if (spell.ManaOnHitPercent > 0 && world.Rng.Get(RngStream.Combat).Range(0, 100) < spell.ManaOnHitPercent)
                        Mana.Restore(world, caster, 1);
                }

            // 반응 (3-1): 맞은 유닛의 기존 상태 · 주문이 닿은 칸의 지형 × 주문 속성
            var cells = CastSpellCommand.TerrainCells(spell, preview);
            if (spell.TriggersReactions)
                ReactionEngine.Resolve(world, book.Reactions, book.Statuses, casterId, spell, victims, cells);

            // 새 상태 이상 (살아 있으면). 확률은 맞은 유닛마다 전투 난수로 따로 (같은 시드 = 같은 결과)
            if (book.Statuses.TryGet(spell.AppliesStatus, out var status))
            {
                foreach (var victim in victims)
                {
                    if (victim.IsAlive) StatusEffects.TryApply(world, victim, status, spell.StatusChancePercent);
                }
            }

            world.OnCellsHit?.Invoke(spell.Element, preview.AffectedCells); // 3-14: 바닥 고기 요리 (불 · 얼음)

            // 지형 효과 (2-4): 놓인 틱의 ③에서 바로 그 칸 유닛에게 상태를 건다
            if (book.Terrains.TryGet(spell.LeavesTerrain, out var terrain))
            {
                foreach (var cell in cells)
                    TerrainEffects.Place(world, cell, terrain, spell.TerrainDurationTicks);
            }
        }

        /// <summary>CheckSpell을 통과하면 시전 커맨드를 만든다 (인벤토리에서 바로 쓰기).</summary>
        public static CastBlock TryCreateCastSpell(World world, GameConfig config, SpellBook book, ITargetingRule targeting, ISightRule sight,
            string spellId, GridPos target, out ICommand command)
        {
            command = null;
            if (!PlayerActions.TryGetPlayer(world, out var player) || !player.IsAlive) return CastBlock.EmptySlot;
            var block = CheckSpell(world, book, targeting, sight, player, spellId, target, out var spell, out _);
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
        private readonly SpellDef _spell; // null이면 SpellId로 SpellBook에서 찾는다

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

        /// <summary>데이터에 없는 주문(3-2b 융합 — FusionBook이 만든 SpellDef)을 그대로 시전한다.</summary>
        public CastSpellCommand(World world, SpellBook book, ITargetingRule targeting, SpellDef spell, GridPos target, int costTicks)
            : this(world, book, targeting, spell?.Id, target, costTicks)
        {
            _spell = spell ?? throw new ArgumentNullException(nameof(spell));
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var caster) || !caster.IsAlive) return;
            var spell = _spell;
            if (spell == null && !_book.TryGet(SpellId, out spell)) return;
            spell = SpellModifiers.For(_world, caster, spell); // 3-21a 무기 강화 · 3-21c 특성 (미리보기와 같은 값)
            if (!Mana.TrySpend(_world, caster, spell.ManaCost)) return;

            var preview = SpellCasting.Preview(_world, _targeting, caster.Pos, spell, Target);
            _world.Events.Publish(new SpellCastEvent(caster.Id, spell.Id, spell.Element, Target, preview));
            if (spell.Shape == SpellShape.Dash) // 3-21b 돌진: 맞히기 전에 멈추는 칸으로 (틱 하나 안 — 지나간 칸은 밟지 않음)
            {
                var from = caster.Pos;
                var landing = SpellCasting.DashLanding(from, preview);
                if (landing != from && !_world.Units.IsOccupied(landing))
                {
                    _world.Units.Move(caster.Id, landing);
                    _world.Events.Publish(new EntityMovedEvent(caster.Id, from, landing));
                }
            }
            SpellCasting.ApplyEffect(_world, _book, caster.Id, spell, preview);
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
