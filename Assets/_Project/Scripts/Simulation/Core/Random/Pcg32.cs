using System;

namespace ProjectM.Core
{
    /// <summary>
    /// PCG32 (pcg-random.org, pcg32_random_r / pcg32_boundedrand_r). 플랫폼 · .NET 버전과 무관하게 같은 순서를 낸다.
    /// 상태는 State로 꺼내고 되돌릴 수 있다 (스냅샷 · 되감기용).
    /// </summary>
    public sealed class Pcg32 : IRng
    {
        private const ulong Multiplier = 6364136223846793005UL;

        private ulong _state;
        private ulong _increment;

        public Pcg32(ulong initState, ulong initSequence)
        {
            _state = 0UL;
            _increment = (initSequence << 1) | 1UL;
            NextUInt();
            _state += initState;
            NextUInt();
        }

        public Pcg32(Pcg32State state)
        {
            State = state;
        }

        public Pcg32State State
        {
            get => new Pcg32State(_state, _increment);
            set
            {
                if ((value.Increment & 1UL) == 0UL)
                    throw new ArgumentException("PCG32 증분은 홀수여야 한다.", nameof(value));
                _state = value.State;
                _increment = value.Increment;
            }
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + _increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentException($"빈 범위 [{minInclusive}, {maxExclusive}).");

            uint bound = (uint)((long)maxExclusive - minInclusive);
            uint threshold = unchecked((uint)-bound) % bound;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold)
                    return (int)(minInclusive + (long)(r % bound));
            }
        }

        public float NextFloat()
        {
            // 상위 24비트 → float 가수 정밀도 그대로, 1.0은 나오지 않는다.
            return (NextUInt() >> 8) * (1f / 16777216f);
        }
    }

    /// <summary>Pcg32의 저장 가능한 상태.</summary>
    [Serializable]
    public readonly struct Pcg32State : IEquatable<Pcg32State>
    {
        public readonly ulong State;
        public readonly ulong Increment;

        public Pcg32State(ulong state, ulong increment)
        {
            State = state;
            Increment = increment;
        }

        public bool Equals(Pcg32State other) => State == other.State && Increment == other.Increment;
        public override bool Equals(object obj) => obj is Pcg32State other && Equals(other);
        public override int GetHashCode() => State.GetHashCode() ^ (Increment.GetHashCode() * 397);
    }
}
