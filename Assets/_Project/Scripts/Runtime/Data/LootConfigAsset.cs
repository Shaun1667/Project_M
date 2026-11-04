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
        [Tooltip("한 판에 놓는 무기 수 (3-7a) — 무기 종류 중 시드로 고름, 1~마지막 전 층")]
        [Min(0)] [SerializeField] private int _weaponsPerRun = 2;
        [Tooltip("한 판에 놓는 강화 주문서 수 (3-7b)")] [Min(0)] [SerializeField] private int _upgradeScrollsPerRun = 3;
        [Tooltip("한 판에 놓는 힘 물약 수 (3-7b)")] [Min(0)] [SerializeField] private int _strengthPotionsPerRun = 2;
        [Tooltip("한 판에 놓는 갑옷 수 (3-10a) — 2티어 이상 중 시드로")] [Min(0)] [SerializeField] private int _armorsPerRun = 2;
        [Header("금화 (3-15a, SPD — BALANCE 11번)")]
        [Tooltip("층마다 금화 무더기 수")] [Min(0)] [SerializeField] private int _goldPilesPerFloor = 3;
        [Tooltip("무더기 금액 최소 = 기본 + 층 × 증가")] [Min(0)] [SerializeField] private int _goldMinBase = 30;
        [Min(0)] [SerializeField] private int _goldMinPerFloor = 10;
        [Tooltip("무더기 금액 최대 = 기본 + 층 × 증가")] [Min(0)] [SerializeField] private int _goldMaxBase = 60;
        [Min(0)] [SerializeField] private int _goldMaxPerFloor = 20;
        [Tooltip("적이 금화를 떨어뜨릴 확률 %")] [Range(0, 100)] [SerializeField] private int _enemyGoldPercent = 20;
        [Min(0)] [SerializeField] private int _enemyGoldMin = 5;
        [Min(0)] [SerializeField] private int _enemyGoldMax = 15;

        public LootConfig ToConfig() => new LootConfig(_maxBooksPerFloor, _minDistanceFromStart, _weaponsPerRun, _upgradeScrollsPerRun, _strengthPotionsPerRun, _armorsPerRun,
            _goldPilesPerFloor, _goldMinBase, _goldMinPerFloor, _goldMaxBase, _goldMaxPerFloor, _enemyGoldPercent, _enemyGoldMin, _enemyGoldMax);
    }
}
