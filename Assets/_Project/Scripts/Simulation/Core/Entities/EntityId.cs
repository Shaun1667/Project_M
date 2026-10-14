using System;

namespace ProjectM.Core
{
    /// <summary>
    /// 엔티티 고유 ID (CLAUDE.md 핵심 설계 규칙 8). 0은 "없음". 한 런 안에서 재사용하지 않는다.
    /// </summary>
    [Serializable]
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public static readonly EntityId None = default;

        public readonly int Value;

        public EntityId(int value)
        {
            Value = value;
        }

        public bool IsValid => Value > 0;

        public static bool operator ==(EntityId a, EntityId b) => a.Value == b.Value;
        public static bool operator !=(EntityId a, EntityId b) => a.Value != b.Value;

        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? $"#{Value}" : "#none";
    }
}
