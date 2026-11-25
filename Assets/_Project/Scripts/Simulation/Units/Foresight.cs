using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;

namespace ProjectM.Units
{
    /// <summary>예지로 미리 볼 플레이어 행동 종류 (3-60). 끝에만 추가한다.</summary>
    public enum ForesightKind
    {
        /// <summary>한 턴 쉬기.</summary>
        Wait = 0,
        /// <summary>옆 칸으로 한 걸음 (적이 있으면 근접 공격) — Target = 그 칸.</summary>
        Step = 1,
        /// <summary>주문 시전 — SpellId · Target.</summary>
        Cast = 2,
        /// <summary>융합 시전 — SpellId · SecondSpellId · Target.</summary>
        Fusion = 3,
    }

    /// <summary>예지로 미리 볼 행동 하나 (3-60). 입력이 만들고, 복사한 World 위에서 커맨드로 바꾼다 (커맨드는 World를 잡으므로 입력이 직접 만들지 않음).</summary>
    public readonly struct ForesightIntent : IEquatable<ForesightIntent>
    {
        public readonly ForesightKind Kind;
        public readonly GridPos Target;
        public readonly string SpellId;
        public readonly string SecondSpellId;

        public ForesightIntent(ForesightKind kind, GridPos target = default, string spellId = null, string secondSpellId = null)
        {
            Kind = kind;
            Target = target;
            SpellId = spellId;
            SecondSpellId = secondSpellId;
        }

        public static ForesightIntent Wait => new ForesightIntent(ForesightKind.Wait);

        public bool Equals(ForesightIntent other) =>
            Kind == other.Kind && Target == other.Target && SpellId == other.SpellId && SecondSpellId == other.SecondSpellId;

        public override bool Equals(object obj) => obj is ForesightIntent other && Equals(other);

        public override int GetHashCode() => ((int)Kind * 397) ^ Target.GetHashCode();
    }

    /// <summary>미리 본 유닛 하나 (3-60): 지금 자리 → 행동 뒤 자리. Dies = 그 사이에 죽음. ChargeTarget = 그때 모으는 힘의 중심.</summary>
    public readonly struct ForesightMark
    {
        public readonly EntityId Unit;
        public readonly GridPos From;
        public readonly GridPos To;
        public readonly bool Dies;
        public readonly GridPos? ChargeTarget;

        public ForesightMark(EntityId unit, GridPos from, GridPos to, bool dies, GridPos? chargeTarget)
        {
            Unit = unit;
            From = from;
            To = to;
            Dies = dies;
            ChargeTarget = chargeTarget;
        }
    }

    /// <summary>
    /// 예지 (3-60, 결정 2026-11-25 — 예지 상태 + 고른 행동의 미래): 예지 상태(StatusDef.Foresees)인 동안, 지금 World를 스냅샷으로 복사한 판 위에서
    /// 고른 행동을 실제로 하고 플레이어 차례가 다시 올 때까지 돌려 본 결과 (규칙 6 · 7 — 같은 상태 · 같은 난수 = 같은 미래).
    /// 화면에는 위치 · 죽음 · 예고만 보인다 (명중 · 피해는 보이지 않음). 복사본 만들기(규칙 연결)는 부르는 쪽(GameRoot)이 한다. 상태를 갖지 않는다.
    /// </summary>
    public static class Foresight
    {
        public static bool IsActive(StatusBook statuses, Unit player) => player != null && player.IsAlive && StatusEffects.Any(statuses, player, d => d.Foresees);

        /// <summary>복사본 clone 위의 플레이어 커맨드. 할 수 없는 행동이면 null (미리 보지 않음).</summary>
        public static ICommand BuildCommand(World clone, GameConfig config, SpellBook spells, FusionBook fusions, ItemBook items, StatusBook statuses,
            ITargetingRule targeting, ISightRule sight, ForesightIntent intent)
        {
            if (clone == null) throw new ArgumentNullException(nameof(clone));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (!PlayerActions.TryGetPlayer(clone, out var player) || !player.IsAlive) return null;
            ICommand command;
            switch (intent.Kind)
            {
                case ForesightKind.Wait:
                    return PlayerActions.Wait(config);
                case ForesightKind.Step:
                    return GridMovement.Chebyshev(player.Pos, intent.Target) == 1
                        && PlayerActions.TryStepTo(clone, config, player, intent.Target, out command, null, statuses) ? command : null;
                case ForesightKind.Cast:
                    return spells != null && SpellCasting.TryCreateCastSpell(clone, config, spells, targeting, sight, intent.SpellId, intent.Target, out command) == CastBlock.None
                        ? command : null;
                case ForesightKind.Fusion:
                    return spells != null && fusions != null && FusionCasting.TryCreateCast(clone, config, spells, fusions, items, targeting, sight,
                        intent.SpellId, intent.SecondSpellId, intent.Target, out command) == CastBlock.None ? command : null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 복사본에서 command를 하고 플레이어 차례가 다시 올 때까지 (턴제 시계와 같은 진행). watch 유닛마다 결과 (EntityId 순).
        /// 플레이어가 죽어도 거기서 멈추고 결과를 돌려준다.
        /// </summary>
        public static List<ForesightMark> Run(World clone, ICommand command, IEnumerable<EntityId> watch)
        {
            if (clone == null) throw new ArgumentNullException(nameof(clone));
            var marks = new List<ForesightMark>();
            if (command == null || watch == null) return marks;
            var from = new SortedDictionary<int, GridPos>();
            foreach (var id in watch)
                if (clone.Units.TryGet(id, out var u) && u.IsAlive) from[id.Value] = u.Pos;
            new TurnClock(clone.Scheduler, clone.PlayerInput).SubmitPlayerCommand(command);
            foreach (var pair in from)
            {
                var id = new EntityId(pair.Key);
                bool alive = clone.Units.TryGet(id, out var u) && u.IsAlive;
                marks.Add(new ForesightMark(id, pair.Value, alive ? u.Pos : pair.Value, !alive, alive ? u.ChargeTarget : null));
            }
            return marks;
        }
    }
}
