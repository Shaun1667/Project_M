using System;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using UnityEditor;

namespace ProjectM.Tests
{
    public class GameConfigTests
    {
        private const string AssetPath = "Assets/_Project/Data/Config/GameConfig.asset";

        [Test]
        public void Constructor_RejectsActionCostBelowOneTick()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameConfig(0, 1f));
        }

        [Test]
        public void Constructor_RejectsNonPositiveTickInterval()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameConfig(1, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameConfig(1, float.NaN));
        }

        // 검산: 프로젝트의 GameConfig 에셋이 BALANCE.md 1번 표와 같은 값인지.
        // BALANCE.md를 고치면 이 테스트의 기대값도 같이 고친다.
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(AssetPath);
            Assert.That(asset, Is.Not.Null, AssetPath + " 없음");

            var config = asset.ToConfig();
            Assert.That(config.DefaultActionCostTicks, Is.EqualTo(1));
            Assert.That(config.RealtimeTickIntervalSeconds, Is.EqualTo(1f));
        }
    }
}
