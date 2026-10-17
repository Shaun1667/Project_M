using ProjectM.Units;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 층의 적 배치 수치 에셋 (Assets/_Project/Data/Units/SpawnConfig.asset). 기준은 BALANCE.md 3번.
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Spawn Config", fileName = "SpawnConfig")]
    public sealed class SpawnConfigAsset : ScriptableObject
    {
        [Tooltip("층마다 놓는 적 수")]
        [Min(0)] [SerializeField] private int _enemyCount = 5;
        [Tooltip("시작 칸에서 적까지 최소 거리 (칸, 대각선 포함)")]
        [Min(1)] [SerializeField] private int _enemyMinDistanceFromStart = 8;
        [Tooltip("적 재생성 주기 (틱, 3-4a). 0 = 재생성 없음. 플레이어 시야 밖 바닥 칸 중 무작위")]
        [Min(0)] [SerializeField] private int _respawnIntervalTicks = 30;
        [Tooltip("살아 있는 적이 이 수 이상이면 재생성하지 않는다")]
        [Min(0)] [SerializeField] private int _respawnMaxEnemies = 5;

        public SpawnConfig ToConfig() => new SpawnConfig(_enemyCount, _enemyMinDistanceFromStart, _respawnIntervalTicks, _respawnMaxEnemies);
    }
}
