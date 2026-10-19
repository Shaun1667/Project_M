using System.Collections.Generic;
using System.Text;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.UI
{
    /// <summary>
    /// 결과 화면 내용 (3-4b, 결정 2026-10-18): 승패 · 도달 층 · 처치 수 · 턴 수 · 시드 · 빠른 칸 등록 요소 · 장착 장비(자리만) · 사망 원인.
    /// 순수 C# — World · 플레이어(죽은 뒤에도 객체는 남음)에서 읽어 만든다. 그리기는 ResultPresenter.
    /// </summary>
    public sealed class ResultModel
    {
        public RunOutcome Outcome { get; }
        public int Floor { get; }
        public int Kills { get; }
        public long Turns { get; }
        public ulong Seed { get; }
        /// <summary>빠른 칸 이름 (빈 칸은 "-").</summary>
        public IReadOnlyList<string> QuickSlots { get; }
        /// <summary>장착 장비 — 든 무기 이름 (3-7a), 맨손이면 "없음".</summary>
        public string Equipment { get; }
        /// <summary>사망 원인 (패배일 때만, 승리면 빈 문자열).</summary>
        public string Cause { get; }

        public ResultModel(RunOutcome outcome, int floor, int kills, long turns, ulong seed, IReadOnlyList<string> quickSlots, string equipment, string cause)
        {
            Outcome = outcome;
            Floor = floor;
            Kills = kills;
            Turns = turns;
            Seed = seed;
            QuickSlots = quickSlots;
            Equipment = equipment;
            Cause = cause;
        }

        public static ResultModel From(World world, ulong seed, Unit player, HudText text)
        {
            var run = world.Run;
            var slots = new List<string>();
            if (player != null)
                foreach (var entry in player.QuickSlots) slots.Add(text.Entry(entry));
            string cause = run.Outcome == RunOutcome.Defeat
                ? text.DeathCause(run.LastPlayerDamage, run.LastPlayerAttacker, run.BossId, player != null ? player.Id : EntityId.None)
                : string.Empty;
            string equipment = player != null && !player.Weapon.IsEmpty ? text.WeaponName(player.Weapon) : HudText.NoEquipment;
            return new ResultModel(run.Outcome, world.DeepestFloor, run.Kills, world.Scheduler.CurrentTick, seed, slots, equipment, cause);
        }

        public string Title => HudText.ResultTitle(Outcome);

        /// <summary>본문 여러 줄.</summary>
        public string Body()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"도달 층: {Floor}층");
            sb.AppendLine($"처치: {Kills}");
            sb.AppendLine($"턴: {Turns}");
            sb.AppendLine($"시드: {(long)Seed}");
            var slots = new StringBuilder();
            for (int i = 0; i < QuickSlots.Count; i++)
            {
                if (i > 0) slots.Append(" · ");
                slots.Append($"[{i + 1}] {QuickSlots[i]}");
            }
            sb.AppendLine($"빠른 칸: {slots}");
            sb.AppendLine($"장비: {Equipment}");
            if (Outcome == RunOutcome.Defeat) sb.AppendLine($"사망 원인: {Cause}");
            sb.AppendLine();
            sb.Append(HudText.ResultRestart);
            return sb.ToString();
        }
    }
}
