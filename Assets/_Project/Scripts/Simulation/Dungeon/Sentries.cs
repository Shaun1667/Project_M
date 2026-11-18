using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>감시탑 수치 (3-49, SPD Sentry). 값은 FloorDecorConfigAsset에서 — 기준은 BALANCE.md 16번.</summary>
    public sealed class SentryConfig
    {
        /// <summary>감시자 (적 표 ID = 모습 ID).</summary>
        public string SentryId { get; }
        /// <summary>감시자 번개 주문 (SpellCatalog) — FloorsPerTier 층마다 다음 칸 (마지막 칸은 그 뒤 모두).</summary>
        public IReadOnlyList<string> SpellIds { get; }
        public int FloorsPerTier { get; }
        /// <summary>이 거리(체비셰프) 안에서 보이는 플레이어에게만 힘을 모은다.</summary>
        public int Range { get; }

        public SentryConfig(string sentryId, IReadOnlyList<string> spellIds, int floorsPerTier, int range)
        {
            if (string.IsNullOrEmpty(sentryId)) throw new ArgumentException("감시자 ID가 비어 있다", nameof(sentryId));
            if (spellIds == null || spellIds.Count == 0) throw new ArgumentException("감시자 주문이 없다", nameof(spellIds));
            foreach (var s in spellIds) if (string.IsNullOrEmpty(s)) throw new ArgumentException("빈 주문 ID", nameof(spellIds));
            if (floorsPerTier < 1) throw new ArgumentOutOfRangeException(nameof(floorsPerTier));
            if (range < 1) throw new ArgumentOutOfRangeException(nameof(range));
            SentryId = sentryId;
            SpellIds = spellIds;
            FloorsPerTier = floorsPerTier;
            Range = range;
        }

        public string SpellFor(int floor) => SpellIds[Math.Min(SpellIds.Count - 1, Math.Max(0, (floor - 1) / FloorsPerTier))];
    }

    /// <summary>감시탑 규칙 연결 (3-49): 수치 · 감시자 기본 수치 · 적 AI. World.SetSentryRules — 규칙 연결이라 저장하지 않는다.</summary>
    public sealed class SentryRules
    {
        public SentryConfig Config { get; }
        public UnitDef Sentry { get; }
        public ICommandSource Ai { get; }

        public SentryRules(SentryConfig config, UnitDef sentry, ICommandSource ai)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Sentry = sentry ?? throw new ArgumentNullException(nameof(sentry));
            Ai = ai ?? throw new ArgumentNullException(nameof(ai));
        }
    }

    /// <summary>
    /// 감시자 (3-49, 결정 2026-11-18 — SPD Sentry): 감시탑 맨 안쪽 줄 가운데에 서는 무적 장치(적 진영 유닛). 움직이지 않고 근접 공격도 하지 않는다.
    /// 거리 Range 안에서 보이는 플레이어(투명 제외)의 칸에 힘을 모으고(예고 칸 — 패턴 보스와 같은 ChargeCommand · Unit.ChargeTarget) 다음 차례에 번개를 푼다.
    /// 번개 세기는 층 티어 주문(SpellFor). 피해를 받지 않는다(Combat.ApplyDamage). 행동은 적 AI(MeleeChaserAi)가 이 한 곳으로 넘긴다.
    /// </summary>
    public static class Sentries
    {
        /// <summary>감시자를 세운다. 규칙이 없거나 칸이 막혔으면 null.</summary>
        public static Unit Place(World world, GridPos pos)
        {
            var rules = world.SentryRules;
            if (rules == null || !world.Map.IsWalkable(pos) || world.Units.TryGetAt(pos, out _)) return null;
            return UnitSpawner.SpawnEnemy(world, rules.Sentry, pos, rules.Ai);
        }

        /// <summary>감시자인지 (규칙이 연결된 판에서 모습 ID로).</summary>
        public static bool IsSentry(World world, Unit unit) =>
            unit != null && world?.SentryRules != null && unit.VisualId == world.SentryRules.Config.SentryId;

        /// <summary>감시자 한 차례: 모은 힘이 있으면 풀고, 아니면 보이는 플레이어 칸에 힘을 모으고, 그 밖에는 기다린다.</summary>
        public static ICommand NextCommand(World world, SpellBook spells, ISightRule sight, ProjectM.Status.StatusBook statuses, Unit self, int cost)
        {
            var rules = world.SentryRules;
            if (rules == null || spells == null || sight == null) return new WaitCommand(cost);
            string spellId = rules.Config.SpellFor(world.Floor);
            if (!spells.TryGet(spellId, out _)) return new WaitCommand(cost);
            if (self.ChargeTarget.HasValue) return new ReleaseChargeCommand(world, spells, spellId, cost);
            if (PlayerActions.TryGetPlayer(world, out var player) && player.IsAlive
                && GridMovement.Chebyshev(self.Pos, player.Pos) <= rules.Config.Range
                && sight.CanSee(world.Map, self.Pos, player.Pos, rules.Config.Range)
                && !ProjectM.Status.StatusEffects.Any(statuses, player, d => d.HiddenFromEnemies)) // 투명이면 못 봄
                return new ChargeCommand(world, spells, spellId, player.Pos, cost);
            return new WaitCommand(cost);
        }
    }
}
