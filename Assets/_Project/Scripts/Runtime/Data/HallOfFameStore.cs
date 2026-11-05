using System;
using System.Collections.Generic;
using System.IO;
using ProjectM.Core;
using ProjectM.Dungeon;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 명예의 전당 파일 (3-33). 판 밖에 남는 데이터라 World 스냅샷과 따로, Application.persistentDataPath/halloffame.json에 JsonUtility로 쓴다
    /// (GhostStore와 같은 방식 — 임시 파일 뒤 바꾸기). 읽다 실패하면 기록 없음으로 본다. 순위 규칙은 HallOfFame.
    /// </summary>
    public static class HallOfFameStore
    {
        public const string FileName = "halloffame.json";
        public const int FormatVersion = 1;

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        [Serializable]
        public sealed class EntryDto
        {
            public string Date, Character, Form;
            public RunMode Mode;
            public long Turns;
            public int Level, Kills, Score;
            public string Seed; // ulong은 JsonUtility가 못 씀 → 글자로
            public List<string> Relics = new List<string>();
        }

        [Serializable]
        public sealed class Dto
        {
            public int Version = FormatVersion;
            public List<EntryDto> Entries = new List<EntryDto>();
        }

        public static List<HallOfFameRecord> Load() => Load(DefaultPath);

        public static List<HallOfFameRecord> Load(string path)
        {
            var list = new List<HallOfFameRecord>();
            try
            {
                if (!File.Exists(path)) return list;
                var dto = JsonUtility.FromJson<Dto>(File.ReadAllText(path));
                if (dto == null || dto.Version != FormatVersion || dto.Entries == null) return list;
                foreach (var e in dto.Entries)
                {
                    ulong.TryParse(e.Seed, out var seed);
                    list.Add(new HallOfFameRecord(e.Date, e.Character, e.Form, e.Mode, e.Turns, e.Level, e.Kills, seed, e.Relics, e.Score));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HallOfFameStore] 읽지 못함 — 기록 없음으로 봄: {ex.Message}");
                list.Clear();
            }
            return list;
        }

        public static void Save(IReadOnlyList<HallOfFameRecord> records) => Save(records, DefaultPath);

        public static void Save(IReadOnlyList<HallOfFameRecord> records, string path)
        {
            var dto = new Dto();
            foreach (var r in records)
                dto.Entries.Add(new EntryDto
                {
                    Date = r.Date, Character = r.Character, Form = r.Form, Mode = r.Mode, Turns = r.Turns,
                    Level = r.Level, Kills = r.Kills, Score = r.Score, Seed = r.Seed.ToString(), Relics = new List<string>(r.Relics),
                });
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(dto, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
    }
}
