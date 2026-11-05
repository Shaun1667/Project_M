using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.UI;
using UnityEditor;

namespace ProjectM.Tests.EditMode
{
    /// <summary>3-33 명예의 전당: 점수 · 순위 · 파일 · 타이틀 화면.</summary>
    public class HallOfFameTests
    {
        private static readonly HallOfFameConfig Config = new HallOfFameConfig(10000, 20, 300, 20000, 1, 3);

        private static HallOfFameRecord Rec(int score, long turns, string who = "루멘") =>
            new HallOfFameRecord("2026-11-05 18:00", who, "근접", RunMode.TurnBased, turns, 10, 100, 123UL, new[] { "회중시계" }, score);

        [Test]
        public void Score_IsClearBonusPlusKillsLevelsAndSavedTurns()
        {
            // 10000 + 200 × 20 + 15 × 300 + (20000 − 12000) × 1 = 10000 + 4000 + 4500 + 8000
            Assert.That(HallOfFame.Score(Config, 12000, 15, 200), Is.EqualTo(26500));
            Assert.That(HallOfFame.Score(Config, 30000, 15, 200), Is.EqualTo(18500), "기준 턴보다 늦으면 빠르기 점수 0 (음수 아님)");
        }

        [Test]
        public void Insert_HigherScoreFirst_TieFewerTurns_CutsToMax()
        {
            var list = new List<HallOfFameRecord>();
            Assert.That(HallOfFame.Insert(list, Rec(100, 500, "가"), Config), Is.EqualTo(1));
            Assert.That(HallOfFame.Insert(list, Rec(300, 900, "나"), Config), Is.EqualTo(1));
            Assert.That(HallOfFame.Insert(list, Rec(100, 400, "다"), Config), Is.EqualTo(2), "같은 점수면 적은 턴이 위");
            Assert.That(HallOfFame.Insert(list, Rec(100, 400, "라"), Config), Is.EqualTo(3), "같은 점수 · 같은 턴이면 먼저 남긴 기록(다) 아래");
            Assert.That(list.ConvertAll(r => r.Character), Is.EqualTo(new[] { "나", "다", "라" }), "최대 3개 — 밀려난 가는 잘림");
            Assert.That(HallOfFame.Insert(list, Rec(50, 100, "마"), Config), Is.EqualTo(0), "꼴찌 밖이면 0");
        }

        [Test]
        public void Store_RoundTrip_KeepsAllFields()
        {
            string path = Path.Combine(Path.GetTempPath(), "hof_test.json");
            try
            {
                var big = new HallOfFameRecord("2026-11-05 18:00", "시아", "연타", RunMode.RealTime, 9876, 18, 321, 18446744073709551615UL, new[] { "회중시계", "가속기" }, 31234);
                HallOfFameStore.Save(new[] { big }, path);
                var back = HallOfFameStore.Load(path);
                Assert.That(back.Count, Is.EqualTo(1));
                var r = back[0];
                Assert.That((r.Date, r.Character, r.Form, r.Mode, r.Turns, r.Level, r.Kills, r.Seed, r.Score),
                    Is.EqualTo(("2026-11-05 18:00", "시아", "연타", RunMode.RealTime, 9876L, 18, 321, 18446744073709551615UL, 31234)));
                Assert.That(r.Relics, Is.EqualTo(new[] { "회중시계", "가속기" }));
                File.WriteAllText(path, "{ 깨진 파일");
                Assert.That(HallOfFameStore.Load(path), Is.Empty, "읽지 못하면 기록 없음");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        [Test]
        public void Title_HallOfFameScreen_OpensAndGoesBack()
        {
            var m = new TitleMenuModel();
            m.Open(new[] { new SlotInfo(false, null, null) });
            m.SetHallOfFame(new[] { HudText.HallOfFameLine(1, Rec(26500, 12000)) });
            m.Select(1);
            m.Confirm();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.HallOfFame));
            Assert.That(m.HallOfFame.Count, Is.EqualTo(1));
            StringAssert.Contains("1위", m.HallOfFame[0]);
            StringAssert.Contains("26,500점", m.HallOfFame[0]);
            StringAssert.Contains("루멘 (근접)", m.HallOfFame[0]);
            StringAssert.Contains("회중시계", m.HallOfFame[0]);
            m.Back();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.Main));
            m.Confirm();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.HallOfFame), "돌아와도 고른 칸 유지");
            m.Confirm();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.Main), "명예의 전당에서 확정 = 뒤로");
            m.Select(0);
            m.Confirm();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.Slots), "0번 = 게임 시작은 그대로");
        }

        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            var c = catalog.ToHallOfFame();
            Assert.That((c.ClearBonus, c.PerKill, c.PerLevel, c.TurnBudget, c.PerTurnSaved, c.MaxEntries), Is.EqualTo((10000, 20, 300, 20000, 1, 20)));
        }
    }
}
