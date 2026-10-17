using System;

namespace ProjectM.Dungeon
{
    /// <summary>
    /// 층 생성 수치. 값은 데이터 에셋(DungeonGenConfigAsset)에서 넘겨받는다 — 기준은 BALANCE.md 2번.
    /// 크기 뽑기 규칙 (이전 MapGenerator 그대로): v = [0, SizeRoll) 에서 뽑고, MinSize 미만이면 MinSize를 더한다.
    /// → 결과 범위 [MinSize, SizeRoll), 그중 [MinSize, 2·MinSize)가 두 배 자주 나온다 (의도된 분포).
    /// </summary>
    public sealed class DungeonGenConfig
    {
        public int Width { get; }
        public int Height { get; }

        public int RoomCount { get; }
        public int RoomSizeRoll { get; }
        public int RoomMinSize { get; }

        public int WallBlockCount { get; }
        public int WallBlockSizeRoll { get; }
        public int WallBlockMinSize { get; }

        /// <summary>복도가 벽을 뚫고 지날 때의 칸 비용 배수 (바닥 · 빈칸 = 1). 클수록 벽을 돌아간다.</summary>
        public int WallTunnelCostMultiplier { get; }

        /// <summary>방 · 벽 덩어리 하나의 시작점을 다시 뽑는 최대 횟수. 넘으면 그 하나는 건너뛴다.</summary>
        public int PlacementAttemptLimit { get; }

        /// <summary>
        /// 막다른 길 잇기: 막다른 칸에서 벽을 뚫어 이을 상대 칸은 걸어서 이 거리 이상 떨어져 있어야 한다 (8방향 걸음 수).
        /// 바로 옆 자기 길에 붙이지 않고 돌아가는 길(순환로)을 만들기 위한 값.
        /// </summary>
        public int DeadEndLoopMinWalkDistance { get; }

        /// <summary>한 판의 층 수 (3-4a). 마지막 층 = 보스 층 — 내려가는 계단 없음 · 적 재생성 없음 (보스는 3-4b).</summary>
        public int FloorCount { get; }

        /// <summary>보스 층(마지막 층)의 방 크기 — 벽 안쪽 바닥 칸 수 (3-4b). 맵 가운데에 놓인다.</summary>
        public int BossRoomWidth { get; }
        public int BossRoomHeight { get; }

        public DungeonGenConfig(
            int width, int height,
            int roomCount, int roomSizeRoll, int roomMinSize,
            int wallBlockCount, int wallBlockSizeRoll, int wallBlockMinSize,
            int wallTunnelCostMultiplier, int placementAttemptLimit,
            int deadEndLoopMinWalkDistance, int floorCount = 5, int bossRoomWidth = 21, int bossRoomHeight = 15)
        {
            Require(floorCount >= 1, nameof(floorCount), "층 수는 1 이상");
            Require(bossRoomWidth >= 7 && bossRoomWidth <= width - 2, nameof(bossRoomWidth), "보스 방 너비는 7 ~ 맵 너비 − 2");
            Require(bossRoomHeight >= 5 && bossRoomHeight <= height - 2, nameof(bossRoomHeight), "보스 방 높이는 5 ~ 맵 높이 − 2");
            FloorCount = floorCount;
            BossRoomWidth = bossRoomWidth;
            BossRoomHeight = bossRoomHeight;
            Require(deadEndLoopMinWalkDistance >= 2, nameof(deadEndLoopMinWalkDistance), "막다른 길 잇기 최소 걸음 수는 2 이상");
            Require(roomMinSize >= 1, nameof(roomMinSize), "방 최소 크기는 1 이상");
            Require(roomSizeRoll > roomMinSize, nameof(roomSizeRoll), "방 크기 뽑기 범위는 최소 크기보다 커야 한다");
            Require(wallBlockMinSize >= 1, nameof(wallBlockMinSize), "벽 덩어리 최소 크기는 1 이상");
            Require(wallBlockSizeRoll > wallBlockMinSize, nameof(wallBlockSizeRoll), "벽 덩어리 크기 뽑기 범위는 최소 크기보다 커야 한다");
            Require(width > roomMinSize + 2 && height > roomMinSize + 2, nameof(width), "맵은 방 최소 크기 + 테두리보다 커야 한다");
            Require(roomCount >= 2, nameof(roomCount), "방은 2개 이상 (시작 · 도착)");
            Require(wallBlockCount >= 0, nameof(wallBlockCount), "벽 덩어리 수는 0 이상");
            Require(wallTunnelCostMultiplier >= 1, nameof(wallTunnelCostMultiplier), "벽 뚫기 비용 배수는 1 이상");
            Require(placementAttemptLimit >= 1, nameof(placementAttemptLimit), "시도 횟수는 1 이상");

            Width = width;
            Height = height;
            RoomCount = roomCount;
            RoomSizeRoll = roomSizeRoll;
            RoomMinSize = roomMinSize;
            WallBlockCount = wallBlockCount;
            WallBlockSizeRoll = wallBlockSizeRoll;
            WallBlockMinSize = wallBlockMinSize;
            WallTunnelCostMultiplier = wallTunnelCostMultiplier;
            PlacementAttemptLimit = placementAttemptLimit;
            DeadEndLoopMinWalkDistance = deadEndLoopMinWalkDistance;
        }

        private static void Require(bool condition, string name, string message)
        {
            if (!condition) throw new ArgumentOutOfRangeException(name, message);
        }
    }
}
