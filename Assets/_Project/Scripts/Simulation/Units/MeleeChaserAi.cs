using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 적 1종 AI. 우선순위:
    /// ① 플레이어가 보임 — 닿으면 근접 공격. 붙어 있다가 플레이어가 물러나면 플레이어가 떠난 칸(발자국)을 그대로 밟아 따라감 (2026-10-16).
    ///    그 밖에는 GridPathfinder.ChaseStep으로 다가감.
    /// ② 놓침 — 마지막으로 본 칸까지 가 본 뒤 그 턴은 대기하고 잊음 (1-7).
    /// ③ 못 봤고 기억도 없음 — 맵을 배회: 아무 바닥 칸을 목적지로 골라(RngStream.Ai) 걸어가고, 도착하거나 막히면 새로 고름 (2026-10-16).
    /// 기억 · 배회 목적지는 Unit(World 상태)에 두고, AI 자체는 상태가 없다 (규칙 6). 하나를 모든 적이 같이 써도 된다.
    /// </summary>
    public sealed class MeleeChaserAi : ICommandSource
    {
        private readonly World _world;
        private readonly GameConfig _config;
        private readonly ISightRule _sight;
        private readonly ProjectM.Status.StatusBook _statuses;
        private readonly EnemyBook _enemies;
        private readonly ProjectM.Magic.SpellBook _spells;
        private readonly ITargetingRule _targeting;

        /// <param name="statuses">투명(3-13a) 판정용. null이면 늘 보인다.</param>
        /// <param name="enemies">3-17b 적 종류 행동 (VisualId로 찾음). null · 없는 종류면 예전처럼 근접 1칸.</param>
        /// <param name="spells">원거리 주문 (EnemyTraits.SpellId) 데이터 · 조준선. null이면 주문 안 씀.</param>
        public MeleeChaserAi(World world, GameConfig config, ISightRule sight, ProjectM.Status.StatusBook statuses = null,
            EnemyBook enemies = null, ProjectM.Magic.SpellBook spells = null, ITargetingRule targeting = null)
        {
            _enemies = enemies;
            _spells = spells;
            _targeting = targeting;
            _statuses = statuses;
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sight = sight ?? throw new ArgumentNullException(nameof(sight));
        }

        public ICommand NextCommand(in TickContext context)
        {
            int cost = _config.DefaultActionCostTicks;
            if (!_world.Units.TryGet(context.Actor, out var self) || !self.IsAlive) return new WaitCommand(cost);

            // 3-13b 공포: 플레이어에게서 멀어진다 (공격하지 않음)
            if (ProjectM.Status.StatusEffects.Any(_statuses, self, d => d.Flees))
                return PlayerActions.TryGetPlayer(_world, out var feared) ? StepAway(self, feared.Pos, cost) : new WaitCommand(cost);

            // 3-13b 분신: 보이는 아군이 플레이어보다 가까우면 그쪽을 친다 (분신이 주의를 끔)
            var ally = NearestVisibleAlly(self);
            bool playerSeen = PlayerActions.TryGetPlayer(_world, out var seenPlayer) && seenPlayer.IsAlive
                && _sight.CanSee(_world.Map, self.Pos, seenPlayer.Pos, self.SightRange)
                && !ProjectM.Status.StatusEffects.Any(_statuses, seenPlayer, d => d.HiddenFromEnemies);
            if (ally != null && (!playerSeen || GridMovement.Chebyshev(self.Pos, ally.Pos) < GridMovement.Chebyshev(self.Pos, seenPlayer.Pos)))
            {
                if (GridMovement.CanStep(_world.Map, self.Pos, ally.Pos)) return new AttackCommand(_world, ally.Id, cost);
                return StepToward(self, ally.Pos, cost) ?? new WaitCommand(cost);
            }

            // 3-17d 패턴 보스: 모은 힘은 보이든 말든 다음 차례에 푼다 · 체력이 줄면 한 번 소환
            var boss = TraitsOf(self);
            if (self.ChargeTarget.HasValue && boss?.ChargeSpellId != null && _spells != null)
                return new ReleaseChargeCommand(_world, _spells, boss.ChargeSpellId, cost);
            if (boss?.SummonDef != null && !self.Summoned && boss.SummonCount > 0 && (long)self.Hp * 100 <= (long)self.MaxHp * boss.SummonAtHpPercent)
                return new SummonCommand(_world, boss.SummonDef, boss.SummonCount, this, cost);

            // ① 보임
            if (PlayerActions.TryGetPlayer(_world, out var player) && player.IsAlive
                && _sight.CanSee(_world.Map, self.Pos, player.Pos, self.SightRange)
                && !ProjectM.Status.StatusEffects.Any(_statuses, player, d => d.HiddenFromEnemies)) // 3-13a 투명
            {
                self.LastSeenTargetPos = player.Pos;
                self.WanderTarget = null;
                var traits = TraitsOf(self);
                if (traits != null && traits.TeleportPercent > 0 && GridMovement.Chebyshev(self.Pos, player.Pos) <= 1
                    && _world.Rng.Get(RngStream.Ai).Range(0, 100) < traits.TeleportPercent)
                    return new TeleportCommand(_world, player.Pos, 4, cost); // 3-17d 파라오: 붙으면 순간이동
                if (traits != null && traits.ChargeSpellId != null && _spells != null && _spells.TryGet(traits.ChargeSpellId, out var charge)
                    && GridMovement.Chebyshev(self.Pos, player.Pos) <= Math.Max(1, charge.Range)
                    && _world.Rng.Get(RngStream.Ai).Range(0, 100) < traits.ChargePercent)
                    return new ChargeCommand(_world, _spells, charge.Id, player.Pos, cost); // 3-17d 예고 강타
                if (traits != null && (traits.SpellId != null || traits.ExtraSpellIds.Count > 0) && TryCast(self, traits, player.Pos, cost, out var cast)) return cast; // 3-17b 주술사 · 3-23b 지난 회차 보스
                if (traits != null && traits.KeepDistance > 0 && GridMovement.Chebyshev(self.Pos, player.Pos) < traits.KeepDistance
                    && self.Mana > 0) // 마나가 있으면 거리를 벌림 (없으면 붙어 싸움)
                {
                    var away = StepAway(self, player.Pos, cost);
                    if (away is MoveCommand) return away;
                }
                if (GridMovement.CanStep(_world.Map, self.Pos, player.Pos))
                    return new AttackCommand(_world, player.Id, cost);
                return FollowTrail(self, player, cost) ?? StepToward(self, player.Pos, cost) ?? new WaitCommand(cost);
            }

            // ② 기억한 칸으로
            if (self.LastSeenTargetPos.HasValue)
            {
                var target = self.LastSeenTargetPos.Value;
                var step = self.Pos == target ? null : StepToward(self, target, cost);
                if (step != null) return step;
                self.LastSeenTargetPos = null; // 도착했거나 갈 수 없음 — 이번 턴은 둘러보고 다음 턴부터 배회
                return new WaitCommand(cost);
            }

            // ③ 배회
            return Wander(self, cost);
        }

        /// <summary>
        /// 발자국 따라가기: 플레이어가 방금 떠난 칸이 플레이어 바로 옆이고, 내가 그 칸으로 한 걸음에 갈 수 있으면(= 붙어 있었음) 그 칸으로.
        /// </summary>
        private ICommand FollowTrail(Unit self, Unit player, int cost)
        {
            if (!player.PreviousPos.HasValue) return null;
            var trail = player.PreviousPos.Value;
            if (trail == self.Pos || GridMovement.Chebyshev(trail, player.Pos) != 1) return null;
            if (!GridMovement.CanStep(_world.Map, self.Pos, trail) || _world.Units.IsOccupied(trail)) return null;
            return new MoveCommand(_world, trail, MoveCost(self, cost));
        }

        private ICommand Wander(Unit self, int cost)
        {
            if (!self.WanderTarget.HasValue || self.WanderTarget.Value == self.Pos)
            {
                self.WanderTarget = PickWanderTarget(self.Pos);
                if (!self.WanderTarget.HasValue) return new WaitCommand(cost);
            }

            var step = StepToward(self, self.WanderTarget.Value, cost);
            if (step != null) return step;
            self.WanderTarget = null; // 막힘 — 다음 턴에 새 목적지
            return new WaitCommand(cost);
        }

        /// <summary>맵의 바닥 · 계단 칸 중 하나 (지금 칸 제외), 후보는 y → x 순, 난수는 RngStream.Ai.</summary>
        private GridPos? PickWanderTarget(GridPos current)
        {
            var map = _world.Map;
            var candidates = new List<GridPos>();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (p != current && map.IsWalkable(p)) candidates.Add(p);
                }
            }
            if (candidates.Count == 0) return null;
            return candidates[_world.Rng.Get(RngStream.Ai).Range(0, candidates.Count)];
        }

        private Unit NearestVisibleAlly(Unit self)
        {
            Unit best = null;
            foreach (var u in _world.Units.ToSortedList())
            {
                if (!u.IsAlive || u.Faction != Faction.Ally || !_sight.CanSee(_world.Map, self.Pos, u.Pos, self.SightRange)) continue;
                if (best == null || GridMovement.Chebyshev(self.Pos, u.Pos) < GridMovement.Chebyshev(self.Pos, best.Pos)) best = u;
            }
            return best;
        }

        /// <summary>from에서 멀어지는 이웃 칸으로 한 걸음 (3-13b 공포 — 거리가 가장 커지는 칸, 방향 순서 고정). 없으면 대기.</summary>
        private ICommand StepAway(Unit self, GridPos from, int cost)
        {
            GridPos? best = null;
            int bestDistance = GridMovement.Chebyshev(self.Pos, from);
            foreach (var dir in GridMovement.Directions)
            {
                var p = self.Pos + dir;
                if (!GridMovement.CanStep(_world.Map, self.Pos, p) || _world.Units.IsOccupied(p)) continue;
                int d = GridMovement.Chebyshev(p, from);
                if (d > bestDistance) { bestDistance = d; best = p; }
            }
            return best.HasValue ? new MoveCommand(_world, best.Value, MoveCost(self, cost)) : (ICommand)new WaitCommand(cost);
        }

        /// <summary>goal 쪽으로 한 걸음 (GridPathfinder.ChaseStep). 길이 없거나 첫 칸이 막혀 있으면 null.</summary>
        private ICommand StepToward(Unit self, GridPos goal, int cost)
        {
            var step = GridPathfinder.ChaseStep(_world.Map, self.Pos, goal, _world.Units.IsOccupied);
            if (!step.HasValue || _world.Units.IsOccupied(step.Value)) return null;
            GridPos? then = null;
            var traits = TraitsOf(self);
            if (traits != null && traits.MoveSteps > 1 && step.Value != goal && GridMovement.Chebyshev(step.Value, goal) > 1) // 3-17b 빠름: 같은 차례에 한 칸 더 (목표 칸 앞에서 멈춤)
            {
                var second = GridPathfinder.ChaseStep(_world.Map, step.Value, goal, p => p != self.Pos && _world.Units.IsOccupied(p));
                if (second.HasValue && second.Value != goal && !_world.Units.IsOccupied(second.Value)) then = second;
            }
            return new MoveCommand(_world, step.Value, MoveCost(self, cost), then);
        }

        private EnemyTraits TraitsOf(Unit self) => _enemies != null && _enemies.TryGet(self.VisualId, out var t) ? t : null;

        /// <summary>이동 비용: 종류의 MoveCostTicks (느림), 없으면 기본.</summary>
        private int MoveCost(Unit self, int cost)
        {
            var traits = TraitsOf(self);
            return traits != null ? Math.Max(cost, traits.MoveCostTicks) : cost;
        }

        /// <summary>3-17b: 주문을 플레이어에게 쓸 수 있으면(사거리 · 시야 · 마나 · 막힘 — 플레이어와 같은 SpellCasting.CheckDef) 시전 커맨드.</summary>
        private bool TryCast(Unit self, EnemyTraits traits, GridPos target, int cost, out ICommand cast)
        {
            cast = null;
            if (_spells == null || _targeting == null) return false;
            // 3-23b: 원거리 주문 → 덧붙은 주문(지난 회차 플레이어 보스 — 빠른 칸 순서) 중 처음으로 쓸 수 있는 것
            if (TryCastOne(self, traits.SpellId, target, cost, out cast)) return true;
            foreach (var id in traits.ExtraSpellIds)
                if (TryCastOne(self, id, target, cost, out cast)) return true;
            return false;
        }

        private bool TryCastOne(Unit self, string spellId, GridPos target, int cost, out ICommand cast)
        {
            cast = null;
            if (spellId == null || !_spells.TryGet(spellId, out var spell)) return false;
            if (ProjectM.Magic.SpellCasting.CheckDef(_world, _targeting, _sight, self, spell, target, out _) != ProjectM.Magic.CastBlock.None) return false;
            cast = new ProjectM.Magic.CastSpellCommand(_world, _spells, _targeting, spell.Id, target, cost);
            return true;
        }
    }
}
