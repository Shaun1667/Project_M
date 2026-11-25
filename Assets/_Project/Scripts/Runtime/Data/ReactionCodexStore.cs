using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 반응 도감 파일 (3-59, 결정 2026-11-25 — 영구 도감). 판 밖에 남는 데이터라 World 스냅샷과 따로, Application.persistentDataPath/codex.json에
    /// JsonUtility로 쓴다 (HallOfFameStore와 같은 방식 — 임시 파일 뒤 바꾸기). 값 = 한 번이라도 일으킨 반응 그룹 ID (처음 발견 순서).
    /// 읽다 실패하면 빈 도감으로 본다.
    /// </summary>
    public static class ReactionCodexStore
    {
        public const string FileName = "codex.json";
        public const int FormatVersion = 1;

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        [Serializable]
        public sealed class Dto
        {
            public int Version = FormatVersion;
            public List<string> Reactions = new List<string>();
        }

        public static List<string> Load() => Load(DefaultPath);

        public static List<string> Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return new List<string>();
                var dto = JsonUtility.FromJson<Dto>(File.ReadAllText(path));
                if (dto == null || dto.Version != FormatVersion || dto.Reactions == null) return new List<string>();
                return dto.Reactions;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ReactionCodexStore] 읽지 못함 — 빈 도감으로 봄: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>group을 더한다 (이미 있으면 아무것도 안 함). 처음이면 true.</summary>
        public static bool Add(string group) => Add(group, DefaultPath);

        public static bool Add(string group, string path)
        {
            if (string.IsNullOrEmpty(group)) return false;
            var list = Load(path);
            if (list.Contains(group)) return false;
            list.Add(group);
            Save(list, path);
            return true;
        }

        public static void Save(IReadOnlyList<string> groups, string path)
        {
            var dto = new Dto();
            dto.Reactions.AddRange(groups);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(dto, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
    }
}
