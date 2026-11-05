using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Dungeon
{
    /// <summary>명예의 전당 점수 수치 (3-33) — ZoneCatalog.asset 값 (BALANCE 25번). 생성자에서 범위 검사.</summary>
    public sealed class HallOfFameConfig
    {
        /// <summary>클리어 기본 점수.</summary>
        public int ClearBonus { get; }
        public int PerKill { get; }
        public int PerLevel { get; }
        /// <summary>빠르기 점수: (기준 턴 − 걸린 턴) × PerTurnSaved, 0 아래로는 안 감.</summary>
        public int TurnBudget { get; }
        public int PerTurnSaved { get; }
        /// <summary>남기는 기록 수 (점수 높은 순으로 자름).</summary>
        public int MaxEntries { get; }

        public HallOfFameConfig(int clearBonus, int perKill, int perLevel, int turnBudget, int perTurnSaved, int maxEntries)
        {
            if (clearBonus < 0 || perKill < 0 || perLevel < 0 || turnBudget < 0 || perTurnSaved < 0) throw new ArgumentOutOfRangeException(nameof(clearBonus), "명예의 전당 점수 값은 0 이상이어야 한다.");
            if (maxEntries < 1) throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "기록 수는 1 이상이어야 한다.");
            ClearBonus = clearBonus;
            PerKill = perKill;
            PerLevel = perLevel;
            TurnBudget = turnBudget;
            PerTurnSaved = perTurnSaved;
            MaxEntries = maxEntries;
        }
    }

    /// <summary>
    /// 명예의 전당 기록 하나 (3-33, 결정 2026-11-05 사용자 — 클리어만 · 높은 점수 순 · 기본 묶음 + 시드 + 직업 형태 · 유물).
    /// 판 밖에 남는 데이터라 World 스냅샷에 넣지 않는다 — 저장은 Runtime HallOfFameStore. 표시 글(이름 · 날짜)은 부른 쪽이 넣는다.
    /// </summary>
    public sealed class HallOfFameRecord
    {
        public string Date { get; }
        public string Character { get; }
        /// <summary>고른 형태 이름 (없으면 빈 문자열).</summary>
        public string Form { get; }
        public RunMode Mode { get; }
        public long Turns { get; }
        public int Level { get; }
        public int Kills { get; }
        public ulong Seed { get; }
        /// <summary>낀 유물 이름 (빈 칸 제외).</summary>
        public IReadOnlyList<string> Relics { get; }
        public int Score { get; }

        public HallOfFameRecord(string date, string character, string form, RunMode mode, long turns, int level, int kills, ulong seed, IReadOnlyList<string> relics, int score)
        {
            Date = date ?? string.Empty;
            Character = character ?? string.Empty;
            Form = form ?? string.Empty;
            Mode = mode;
            Turns = Math.Max(0, turns);
            Level = level;
            Kills = kills;
            Seed = seed;
            Relics = relics ?? Array.Empty<string>();
            Score = score;
        }
    }

    /// <summary>명예의 전당 규칙 한 곳 (3-33): 점수 · 순위 넣기.</summary>
    public static class HallOfFame
    {
        /// <summary>점수 = 클리어 기본 + 처치 × PerKill + 레벨 × PerLevel + max(0, 기준 턴 − 걸린 턴) × PerTurnSaved.</summary>
        public static int Score(HallOfFameConfig config, long turns, int level, int kills)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            long saved = Math.Max(0L, config.TurnBudget - Math.Max(0L, turns));
            long score = config.ClearBonus + (long)Math.Max(0, kills) * config.PerKill + (long)Math.Max(0, level) * config.PerLevel + saved * config.PerTurnSaved;
            return (int)Math.Min(int.MaxValue, score);
        }

        /// <summary>
        /// 기록을 순위에 넣는다 — 높은 점수 순, 같으면 적은 턴, 그것도 같으면 먼저 남긴 기록이 위. MaxEntries를 넘으면 아래를 자른다.
        /// 넣은 기록의 순위(1부터), 잘려서 못 들어가면 0.
        /// </summary>
        public static int Insert(List<HallOfFameRecord> records, HallOfFameRecord record, HallOfFameConfig config)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (config == null) throw new ArgumentNullException(nameof(config));
            int index = records.Count;
            for (int i = 0; i < records.Count; i++)
            {
                var other = records[i];
                if (record.Score > other.Score || (record.Score == other.Score && record.Turns < other.Turns))
                {
                    index = i;
                    break;
                }
            }
            records.Insert(index, record);
            if (records.Count > config.MaxEntries) records.RemoveRange(config.MaxEntries, records.Count - config.MaxEntries);
            return index < config.MaxEntries ? index + 1 : 0;
        }
    }
}
