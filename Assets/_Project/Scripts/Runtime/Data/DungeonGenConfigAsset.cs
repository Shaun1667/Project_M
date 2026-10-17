using ProjectM.Dungeon;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 층 생성 수치 에셋 (Assets/_Project/Data/Dungeon/DungeonGenConfig.asset). 기준은 BALANCE.md 2번.
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Dungeon Gen Config", fileName = "DungeonGenConfig")]
    public sealed class DungeonGenConfigAsset : ScriptableObject
    {
        [Header("맵")]
        [Min(10)] [SerializeField] private int _width = 50;
        [Min(10)] [SerializeField] private int _height = 50;

        [Header("방 — 크기 = [0, 뽑기범위)에서 뽑고 최소 미만이면 최소를 더함")]
        [Min(2)] [SerializeField] private int _roomCount = 10;
        [Min(2)] [SerializeField] private int _roomSizeRoll = 20;
        [Min(1)] [SerializeField] private int _roomMinSize = 7;

        [Header("벽 덩어리 — 방보다 먼저 깔림")]
        [Min(0)] [SerializeField] private int _wallBlockCount = 20;
        [Min(2)] [SerializeField] private int _wallBlockSizeRoll = 10;
        [Min(1)] [SerializeField] private int _wallBlockMinSize = 3;

        [Header("연결")]
        [Tooltip("복도가 벽을 뚫을 때 칸 비용 배수. 클수록 벽을 돌아간다")]
        [Min(1)] [SerializeField] private int _wallTunnelCostMultiplier = 5;
        [Tooltip("방 · 벽 덩어리 시작점을 다시 뽑는 최대 횟수")]
        [Min(1)] [SerializeField] private int _placementAttemptLimit = 1000;
        [Tooltip("막다른 길을 이을 상대 칸까지의 최소 걸음 수. 클수록 크게 돌아가는 순환로가 생긴다")]
        [Min(2)] [SerializeField] private int _deadEndLoopMinWalkDistance = 10;

        [Header("런 (3-4a)")]
        [Tooltip("한 판의 층 수. 마지막 층 = 보스 층 (내려가는 계단 · 적 재생성 없음)")]
        [Min(1)] [SerializeField] private int _floorCount = 5;
        [Tooltip("보스 층 방 크기 (벽 안쪽 바닥 칸, 3-4b). 맵 가운데")]
        [Min(7)] [SerializeField] private int _bossRoomWidth = 21;
        [Min(5)] [SerializeField] private int _bossRoomHeight = 15;

        public DungeonGenConfig ToConfig()
        {
            return new DungeonGenConfig(
                _width, _height,
                _roomCount, _roomSizeRoll, _roomMinSize,
                _wallBlockCount, _wallBlockSizeRoll, _wallBlockMinSize,
                _wallTunnelCostMultiplier, _placementAttemptLimit,
                _deadEndLoopMinWalkDistance, _floorCount, _bossRoomWidth, _bossRoomHeight);
        }
    }
}
