using System;
using System.Collections.Generic;

namespace ProjectM.Core
{
    /// <summary>
    /// 런 시드 하나에서 용도별 Pcg32를 만든다. 각 수열은 (시드, 용도)만으로 정해지고 다른 수열과 무관하다.
    /// 그래서 RngStream에 새 값을 추가해도 기존 용도의 결과는 그대로다.
    /// </summary>
    public sealed class RngSet
    {
        private readonly Dictionary<RngStream, Pcg32> _streams = new Dictionary<RngStream, Pcg32>();

        public ulong Seed { get; }

        public RngSet(ulong seed)
        {
            Seed = seed;
        }

        public IRng Get(RngStream stream)
        {
            if (!_streams.TryGetValue(stream, out var rng))
            {
                rng = Create(Seed, stream);
                _streams.Add(stream, rng);
            }
            return rng;
        }

        /// <summary>지금까지 쓴 수열의 상태. 쓰지 않은 수열은 시드로 다시 만들 수 있으므로 포함하지 않는다.</summary>
        public IReadOnlyDictionary<RngStream, Pcg32State> CaptureStates()
        {
            var states = new Dictionary<RngStream, Pcg32State>(_streams.Count);
            foreach (var pair in _streams)
                states.Add(pair.Key, pair.Value.State);
            return states;
        }

        public void RestoreStates(IReadOnlyDictionary<RngStream, Pcg32State> states)
        {
            if (states == null) throw new ArgumentNullException(nameof(states));
            _streams.Clear();
            foreach (var pair in states)
                _streams.Add(pair.Key, new Pcg32(pair.Value));
        }

        private static Pcg32 Create(ulong seed, RngStream stream)
        {
            ulong id = (ulong)(int)stream;
            ulong initState = SplitMix64(seed ^ unchecked(id * 0x9E3779B97F4A7C15UL));
            return new Pcg32(initState, id);
        }

        private static ulong SplitMix64(ulong x)
        {
            unchecked
            {
                x += 0x9E3779B97F4A7C15UL;
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }
    }
}
