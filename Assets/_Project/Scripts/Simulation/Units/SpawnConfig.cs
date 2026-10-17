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
        /// <summary>적 재생성 주기 (틱, 3-4a). (틱 + 1)이 이 값의 배수인 틱에 1마리. 0 = 재생성 없음.</summary>
        public int RespawnIntervalTicks { get; }
        /// <summary>재생성 상한 — 지금 층에 살아 있는 적이 이 수 이상이면 만들지 않는다 (3-4a).</summary>
        public int RespawnMaxEnemies { get; }

        public SpawnConfig(int enemyCount, int enemyMinDistanceFromStart, int respawnIntervalTicks = 0, int respawnMaxEnemies = 0)
        {
            if (enemyCount < 0) throw new ArgumentOutOfRangeException(nameof(enemyCount), enemyCount, "적 수는 0 이상이어야 한다.");
            if (enemyMinDistanceFromStart < 1)
                throw new ArgumentOutOfRangeException(nameof(enemyMinDistanceFromStart), enemyMinDistanceFromStart, "최소 거리는 1 이상이어야 한다.");
            if (respawnIntervalTicks < 0) throw new ArgumentOutOfRangeException(nameof(respawnIntervalTicks), respawnIntervalTicks, "재생성 주기는 0 이상.");
            if (respawnMaxEnemies < 0) throw new ArgumentOutOfRangeException(nameof(respawnMaxEnemies), respawnMaxEnemies, "재생성 상한은 0 이상.");

            EnemyCount = enemyCount;
            EnemyMinDistanceFromStart = enemyMinDistanceFromStart;
            RespawnIntervalTicks = respawnIntervalTicks;
            RespawnMaxEnemies = respawnMaxEnemies;
        }
    }
}
