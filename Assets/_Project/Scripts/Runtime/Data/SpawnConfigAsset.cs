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

        public SpawnConfig ToConfig() => new SpawnConfig(_enemyCount, _enemyMinDistanceFromStart);
    }
}
