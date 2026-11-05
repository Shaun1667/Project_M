using System;
using System.IO;
using ProjectM.Core;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 진행 중인 판 자동 저장 (3-27, 결정 2026-11-05 — SPD처럼 파일 하나, 매 행동 · 매 틱 저장, 패배 · 승리하면 삭제):
    /// `Application.persistentDataPath/run.sav` = World 스냅샷(WorldSnapshotCodec) 그대로. 시드 · 모드 · 직업은 스냅샷 안(RunState)에 있다.
    /// 쓰기는 임시 파일 뒤 바꾸기(쓰다 꺼져도 앞 저장이 남음). 버전이 다르거나 깨졌으면 없는 것으로 본다.
    /// </summary>
    public static class RunSaveStore
    {
        public const string FileName = "run.sav";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists(string path = null) => File.Exists(path ?? DefaultPath);

        public static void Save(World world, string path = null)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            Write(WorldSnapshotCodec.Encode(world.Capture()), path);
        }

        public static void Write(byte[] bytes, string path = null)
        {
            path = path ?? DefaultPath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var tmp = path + ".tmp";
            File.WriteAllBytes(tmp, bytes);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        /// <summary>읽는다. 없거나 깨졌거나 버전이 다르면 null (경고만).</summary>
        public static WorldSnapshot Load(string path = null)
        {
            path = path ?? DefaultPath;
            if (!File.Exists(path)) return null;
            try
            {
                var snapshot = WorldSnapshotCodec.Decode(File.ReadAllBytes(path));
                if (snapshot.FormatVersion != WorldSnapshot.CurrentFormatVersion)
                {
                    Debug.LogWarning($"[RunSaveStore] 저장 버전 {snapshot.FormatVersion} ≠ {WorldSnapshot.CurrentFormatVersion} — 이어할 수 없음");
                    return null;
                }
                return snapshot;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RunSaveStore] 저장을 읽지 못했다 — 없는 것으로 봄: {e.Message}");
                return null;
            }
        }

        public static void Delete(string path = null)
        {
            path = path ?? DefaultPath;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
