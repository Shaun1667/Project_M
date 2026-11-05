using System;
using System.IO;
using ProjectM.Core;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 진행 중인 판 자동 저장 (3-27 → 3-30 슬롯 5개, 결정 2026-11-05 — 사용자): `Application.persistentDataPath/run{1~5}.sav` = World 스냅샷(WorldSnapshotCodec) 그대로.
    /// 시드 · 모드 · 직업은 스냅샷 안(RunState)에 있다. 매 행동 · 매 틱 저장, 패배 · 승리하면 그 슬롯을 비운다.
    /// 쓰기는 임시 파일 뒤 바꾸기(쓰다 꺼져도 앞 저장이 남음). 버전이 다르거나 깨졌으면 없는 것으로 본다. 예전 단일 저장 run.sav는 처음 읽을 때 빈 슬롯 1로 옮긴다.
    /// </summary>
    public static class RunSaveStore
    {
        public const int SlotCount = 5;
        public const string LegacyFileName = "run.sav";

        public static string PathOf(int slot)
        {
            if (slot < 1 || slot > SlotCount) throw new ArgumentOutOfRangeException(nameof(slot), slot, "슬롯은 1 ~ " + SlotCount);
            return Path.Combine(Application.persistentDataPath, $"run{slot}.sav");
        }

        public static bool Exists(int slot) => File.Exists(PathOf(slot));

        public static void Save(World world, int slot)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            Write(WorldSnapshotCodec.Encode(world.Capture()), PathOf(slot));
        }

        public static void Write(byte[] bytes, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var tmp = path + ".tmp";
            File.WriteAllBytes(tmp, bytes);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        /// <summary>읽는다. 없거나 깨졌거나 버전이 다르면 null (경고만).</summary>
        public static WorldSnapshot Load(int slot) => LoadPath(PathOf(slot));

        public static WorldSnapshot LoadPath(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var snapshot = WorldSnapshotCodec.Decode(File.ReadAllBytes(path));
                if (snapshot.FormatVersion != WorldSnapshot.CurrentFormatVersion)
                {
                    Debug.LogWarning($"[RunSaveStore] 저장 버전 {snapshot.FormatVersion} ≠ {WorldSnapshot.CurrentFormatVersion} — 이어할 수 없음 ({path})");
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

        public static void Delete(int slot)
        {
            var path = PathOf(slot);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>3-27 단일 저장(run.sav)이 있으면 빈 슬롯 1로 옮긴다 (슬롯 1이 차 있으면 그대로 둠).</summary>
        public static void MigrateLegacy()
        {
            var legacy = Path.Combine(Application.persistentDataPath, LegacyFileName);
            if (!File.Exists(legacy) || Exists(1)) return;
            try
            {
                File.Move(legacy, PathOf(1));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RunSaveStore] 예전 저장을 슬롯 1로 옮기지 못했다: {e.Message}");
            }
        }
    }
}
