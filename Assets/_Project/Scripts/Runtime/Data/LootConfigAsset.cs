using ProjectM.Items;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 바닥 아이템(마법서) 배치 수치 (Assets/_Project/Data/Items/LootConfig.asset, 3-5). 기준은 BALANCE.md 11번.
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Loot Config", fileName = "LootConfig")]
    public sealed class LootConfigAsset : ScriptableObject
    {
        [Tooltip("한 층에 놓는 마법서 최대 수 (층마다 1권부터 채우고 남는 책을 무작위 층에 더)")]
        [Min(1)] [SerializeField] private int _maxBooksPerFloor = 2;
        [Tooltip("층 시작 칸(도착 계단)에서 아이템까지 최소 거리 (칸, 대각선 포함)")]
        [Min(0)] [SerializeField] private int _minDistanceFromStart = 3;

        public LootConfig ToConfig() => new LootConfig(_maxBooksPerFloor, _minDistanceFromStart);
    }
}
