using System;

namespace ProjectM.Core
{
    /// <summary>
    /// World 전체의 저장 가능한 상태 (CLAUDE.md 핵심 설계 규칙 6). 참조 없는 값과 배열만 담는다.
    /// 시간 마법(되감기 · 기록) · 세이브 · 회귀 테스트가 같은 스냅샷을 쓴다.
    /// 필드를 추가하면 FormatVersion을 올리고 WorldSnapshotCodec과 World.Capture/Restore를 함께 고친다.
    /// </summary>
    [Serializable]
    public sealed class WorldSnapshot
    {
        public const int CurrentFormatVersion = 1;

        public int FormatVersion = CurrentFormatVersion;

        public long CurrentTick;

        public int MapWidth;
        public int MapHeight;
        /// <summary>인덱스 = y * MapWidth + x.</summary>
        public TileType[] Cells = Array.Empty<TileType>();

        public int NextEntityId;

        public ulong RngSeed;
        /// <summary>지금까지 쓴 난수 수열만, RngStream 오름차순.</summary>
        public RngStreamState[] RngStreams = Array.Empty<RngStreamState>();

        /// <summary>EntityId 오름차순.</summary>
        public ActorState[] Actors = Array.Empty<ActorState>();
    }

    /// <summary>용도별 난수 수열 하나의 상태.</summary>
    [Serializable]
    public readonly struct RngStreamState
    {
        public readonly RngStream Stream;
        public readonly Pcg32State State;

        public RngStreamState(RngStream stream, Pcg32State state)
        {
            Stream = stream;
            State = state;
        }
    }
}
