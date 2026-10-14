using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProjectM.Core;

namespace ProjectM.Tests
{
    public class RngTests
    {
        // pcg-c 데모(pcg32-demo)의 기준 출력: initstate 42, initseq 54.
        [Test]
        public void Pcg32_MatchesReferenceOutput()
        {
            var rng = new Pcg32(42UL, 54UL);
            uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
            foreach (var value in expected)
                Assert.That(rng.NextUInt(), Is.EqualTo(value));
        }

        [Test]
        public void Pcg32_SameSeed_SameSequence()
        {
            var a = new Pcg32(7UL, 3UL);
            var b = new Pcg32(7UL, 3UL);
            for (int i = 0; i < 100; i++)
                Assert.That(a.Range(-50, 50), Is.EqualTo(b.Range(-50, 50)));
        }

        [Test]
        public void Pcg32_StateRestore_ReproducesRestOfSequence()
        {
            var rng = new Pcg32(123UL, 9UL);
            for (int i = 0; i < 17; i++) rng.NextUInt();

            var saved = rng.State;
            var first = Enumerable.Range(0, 20).Select(_ => rng.NextUInt()).ToArray();

            var restored = new Pcg32(saved);
            var second = Enumerable.Range(0, 20).Select(_ => restored.NextUInt()).ToArray();

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Pcg32_RangeAndFloat_StayInBounds()
        {
            var rng = new Pcg32(1UL, 1UL);
            var hit = new HashSet<int>();
            for (int i = 0; i < 5000; i++)
            {
                int r = rng.Range(-2, 3);
                Assert.That(r, Is.InRange(-2, 2));
                hit.Add(r);

                float f = rng.NextFloat();
                Assert.That(f, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
            Assert.That(hit.Count, Is.EqualTo(5), "범위의 모든 값이 나와야 함");
            Assert.That(rng.Range(int.MinValue, int.MaxValue), Is.InRange(int.MinValue, int.MaxValue - 1));
            Assert.Throws<ArgumentException>(() => rng.Range(3, 3));
        }

        [Test]
        public void RngSet_SameSeed_SameStreams()
        {
            var a = new RngSet(2026UL);
            var b = new RngSet(2026UL);
            foreach (RngStream stream in Enum.GetValues(typeof(RngStream)))
                Assert.That(a.Get(stream).NextUInt(), Is.EqualTo(b.Get(stream).NextUInt()), stream.ToString());
        }

        [Test]
        public void RngSet_StreamsAreIndependent()
        {
            // 맵 생성 난수를 얼마나 쓰든 전투 난수 수열은 같아야 한다.
            var quiet = new RngSet(99UL);
            var busy = new RngSet(99UL);
            for (int i = 0; i < 500; i++) busy.Get(RngStream.MapGen).NextUInt();

            for (int i = 0; i < 50; i++)
                Assert.That(busy.Get(RngStream.Combat).NextUInt(), Is.EqualTo(quiet.Get(RngStream.Combat).NextUInt()));
        }

        [Test]
        public void RngSet_DifferentStreams_DifferentSequences()
        {
            var set = new RngSet(5UL);
            var map = Enumerable.Range(0, 8).Select(_ => set.Get(RngStream.MapGen).NextUInt()).ToArray();
            var combat = Enumerable.Range(0, 8).Select(_ => set.Get(RngStream.Combat).NextUInt()).ToArray();
            Assert.That(combat, Is.Not.EqualTo(map));
        }

        [Test]
        public void RngSet_CaptureRestore_ContinuesSameSequence()
        {
            var set = new RngSet(31UL);
            set.Get(RngStream.Combat).NextUInt();
            set.Get(RngStream.Ai).NextUInt();
            var states = set.CaptureStates();

            var expected = set.Get(RngStream.Combat).NextUInt();

            var restored = new RngSet(31UL);
            restored.RestoreStates(states);
            Assert.That(restored.Get(RngStream.Combat).NextUInt(), Is.EqualTo(expected));
        }

        // CLAUDE.md 핵심 설계 규칙 7: Simulation 소스에서 System.Random · UnityEngine.Random을 쓰지 않는다.
        [Test]
        public void Simulation_DoesNotUseDirectRandom()
        {
            var root = Path.Combine("Assets", "_Project", "Scripts", "Simulation");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f =>
                {
                    var text = File.ReadAllText(f);
                    return text.Contains("System.Random") || text.Contains("new Random(") || text.Contains("UnityEngine.Random");
                })
                .ToArray();
            Assert.That(offenders, Is.Empty);
        }
    }
}
