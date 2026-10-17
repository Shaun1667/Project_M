using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Units;

namespace ProjectM.Status
{
    /// <summary>
    /// 반응 규칙 엔진 (3-1, REACTIONS.md 2번). 주문 시전 중 "원래 피해 → 반응 → 새 상태 → 지형" 순서의 반응 단계에서 한 번 부른다.
    /// 1) 후보 모으기: 맞은 유닛의 상태 × 주문 속성, 주문이 닿은 칸의 지형 × 주문 속성.
    /// 2) 우선순위(큰 것 먼저) → 상태 반응 먼저 → 모은 순서로 정렬.
    /// 3) 같은 그룹은 대상(유닛, 유닛이 없으면 칸)당 1회, 주문당 최대 N회. 그룹 중복으로 건너뛴 줄도 트리거 소모 · 지형 변환은 한다 (3-2).
    /// 4) 발동: 추가 피해(상한) · 트리거 소모 / 지형 변환 · 폭발 예약. 폭발은 마지막에 대상당 가장 큰 것 1회만 (피해 → 상태 확률).
    /// 반응의 피해는 다시 반응을 일으키지 않는다 (연쇄 금지). 상태를 갖지 않는다.
    /// </summary>
    public static class ReactionEngine
    {
        private sealed class Candidate
        {
            public ReactionDef Reaction;
            public Unit Unit;          // 상태 반응: 대상, 지형 반응: 그 칸 유닛 (없으면 null)
            public GridPos Pos;        // 상태 반응: 대상 칸, 지형 반응: 그 칸
            public int Remaining;      // 트리거의 남은 틱
            public int Order;          // 모은 순서
        }

        private sealed class ExplosionHit
        {
            public int Damage;
            public StatusType Status;
            public int ChancePercent;
        }

        /// <returns>발동한 반응 수.</returns>
        public static int Resolve(World world, ReactionBook book, StatusBook statuses, Unit caster, SpellDef spell,
            IReadOnlyList<Unit> victims, IReadOnlyList<GridPos> cells)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (book == null || book.Count == 0 || spell == null) return 0;

            // 1) 후보
            var candidates = new List<Candidate>();
            foreach (var victim in victims)
            {
                foreach (var s in victim.Statuses)
                {
                    var r = book.FindStatus(s.Type, spell.Element);
                    if (r != null)
                        candidates.Add(new Candidate { Reaction = r, Unit = victim, Pos = victim.Pos, Remaining = s.Remaining, Order = candidates.Count });
                }
            }
            foreach (var cell in cells)
            {
                foreach (var t in world.Terrain.At(cell))
                {
                    var r = book.FindTerrain(t.Type, spell.Element);
                    if (r == null) continue;
                    world.Units.TryGetAt(cell, out var unitOnCell);
                    candidates.Add(new Candidate { Reaction = r, Unit = unitOnCell, Pos = cell, Remaining = t.Remaining, Order = candidates.Count });
                }
            }
            if (candidates.Count == 0) return 0;

            // 2) 순서
            candidates.Sort((a, b) =>
            {
                if (a.Reaction.Priority != b.Reaction.Priority) return b.Reaction.Priority.CompareTo(a.Reaction.Priority);
                if (a.Reaction.Trigger != b.Reaction.Trigger) return a.Reaction.Trigger.CompareTo(b.Reaction.Trigger); // 상태 먼저
                return a.Order.CompareTo(b.Order);
            });

            // 3) · 4) 발동
            var config = book.Config;
            var done = new HashSet<string>();
            var explosions = new SortedDictionary<int, ExplosionHit>(); // 유닛 ID → 가장 큰 폭발
            var cellOnlyLogged = new HashSet<string>();
            int fired = 0;
            foreach (var c in candidates)
            {
                var r = c.Reaction;
                // 지형만 바꾸는 반응(폭발 없음, 빈 칸이거나 추가 피해 없음 — 오염 · 빈 칸의 증발 · 소화)은
                // 4회 상한에 세지 않고 로그는 반응당 한 줄 (3-2 ✎ — 물 3×3에 독을 쏘면 9칸이 다 오염)
                if (r.Trigger == ReactionTrigger.Terrain && r.ExplosionRadius == 0 && (c.Unit == null || r.ExtraDamage == 0))
                {
                    if (cellOnlyLogged.Add(r.Id))
                        world.Events.Publish(new ReactionTriggeredEvent(r.Id, caster.Id, c.Pos, EntityId.None));
                    ApplyTriggerChange(world, r, c);
                    continue;
                }
                if (fired >= config.MaxReactionsPerCast) continue;
                string key = r.Group + "|" + (c.Unit != null ? "u" + c.Unit.Id.Value : "c" + c.Pos.X + "," + c.Pos.Y);
                if (!done.Add(key))
                {
                    // 같은 그룹이 이 대상에서 이미 발동 — 피해 · 폭발 · 로그는 한 번만, 트리거 소모 · 지형 변환은 그대로 (3-2: 젖은 유닛이 선 물 타일도 증발)
                    ApplyTriggerChange(world, r, c);
                    continue;
                }
                fired++;

                world.Events.Publish(new ReactionTriggeredEvent(r.Id, caster.Id, c.Pos, c.Unit != null ? c.Unit.Id : EntityId.None));

                if (r.ExtraDamage > 0 && c.Unit != null && c.Unit.IsAlive)
                    Combat.ApplyDamage(world, caster.Id, c.Unit, Math.Min(r.ExtraDamage, config.ExtraDamageCap(spell.Damage)));

                if (r.ExplosionRadius > 0)
                {
                    int damage = Math.Min(r.ExplosionDamageFromRemaining ? c.Remaining : r.ExplosionDamage, config.ExplosionDamageCap(spell.Damage));
                    foreach (var unit in world.Units.ToSortedList())
                    {
                        if (!unit.IsAlive || GridMovement.Chebyshev(unit.Pos, c.Pos) > r.ExplosionRadius) continue;
                        if (!explosions.TryGetValue(unit.Id.Value, out var hit) || damage > hit.Damage)
                            explosions[unit.Id.Value] = new ExplosionHit { Damage = damage, Status = r.ExplosionStatus, ChancePercent = r.ExplosionStatusChancePercent };
                    }
                }

                ApplyTriggerChange(world, r, c);
            }

            // 폭발: 대상당 1회 (EntityId 순)
            foreach (var pair in explosions)
            {
                if (!world.Units.TryGet(new EntityId(pair.Key), out var unit) || !unit.IsAlive) continue;
                Combat.ApplyDamage(world, caster.Id, unit, pair.Value.Damage);
                if (pair.Value.Status != StatusType.None && unit.IsAlive && statuses.TryGet(pair.Value.Status, out var def))
                    StatusEffects.TryApply(world, unit, def, pair.Value.ChancePercent);
            }
            return fired;
        }

        /// <summary>트리거 소모 · 지형 변환 (3-2).</summary>
        private static void ApplyTriggerChange(World world, ReactionDef r, Candidate c)
        {
            if (r.ConsumeTrigger)
            {
                if (r.Trigger == ReactionTrigger.Status && c.Unit != null) StatusEffects.Remove(world, c.Unit, r.Status);
                else if (r.Trigger == ReactionTrigger.Terrain) TerrainEffects.Remove(world, c.Pos, r.Terrain);
            }
            else if (r.TransformTerrainTo != TerrainEffectType.None)
                TerrainEffects.Transform(world, c.Pos, r.Terrain, r.TransformTerrainTo);
        }
    }

    /// <summary>반응이 일어났다 (로그 · 연출용). Target은 대상 유닛, 없으면 None (빈 칸의 지형 반응).</summary>
    public sealed class ReactionTriggeredEvent : ISimEvent
    {
        public string ReactionId { get; }
        public EntityId Caster { get; }
        public GridPos Pos { get; }
        public EntityId Target { get; }

        public ReactionTriggeredEvent(string reactionId, EntityId caster, GridPos pos, EntityId target)
        {
            ReactionId = reactionId;
            Caster = caster;
            Pos = pos;
            Target = target;
        }
    }
}
