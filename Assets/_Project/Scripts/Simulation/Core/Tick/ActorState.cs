using System;

namespace ProjectM.Core
{
    /// <summary>스케줄러에 등록된 행위자 하나의 저장 가능한 상태.</summary>
    [Serializable]
    public readonly struct ActorState : IEquatable<ActorState>
    {
        public readonly EntityId Id;
        public readonly long ReadyTick;
        public readonly bool IsPlayer;

        public ActorState(EntityId id, long readyTick, bool isPlayer)
        {
            Id = id;
            ReadyTick = readyTick;
            IsPlayer = isPlayer;
        }

        public bool Equals(ActorState other) => Id == other.Id && ReadyTick == other.ReadyTick && IsPlayer == other.IsPlayer;
        public override bool Equals(object obj) => obj is ActorState other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode() ^ ReadyTick.GetHashCode();
    }
}
