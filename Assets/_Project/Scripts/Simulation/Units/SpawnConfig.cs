using System;

namespace ProjectM.Units
{
    /// <summary>층의 적 배치 수치. 값은 SpawnConfigAsset에서 온다 — 기준은 BALANCE.md 3번.</summary>
    public sealed class SpawnConfig
    {
        /// <summary>층마다 놓는 적 수. 놓을 칸이 모자라면 놓을 수 있는 만큼만.</summary>
        public int EnemyCount { get; }
        /// <summary>시작 칸에서 적까지 최소 체비셰프 거리.</summary>
        public int EnemyMinDistanceFromStart { get; }

        public SpawnConfig(int enemyCount, int enemyMinDistanceFromStart)
        {
            if (enemyCount < 0) throw new ArgumentOutOfRangeException(nameof(enemyCount), enemyCount, "적 수는 0 이상이어야 한다.");
            if (enemyMinDistanceFromStart < 1)
                throw new ArgumentOutOfRangeException(nameof(enemyMinDistanceFromStart), enemyMinDistanceFromStart, "최소 거리는 1 이상이어야 한다.");

            EnemyCount = enemyCount;
            EnemyMinDistanceFromStart = enemyMinDistanceFromStart;
        }
    }
}
