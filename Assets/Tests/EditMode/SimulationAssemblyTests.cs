using System.Linq;
using NUnit.Framework;
using ProjectM.Core;

namespace ProjectM.Tests
{
    public class SimulationAssemblyTests
    {
        // CLAUDE.md 핵심 설계 규칙 1: 시뮬레이션은 Unity 엔진을 모른다.
        // asmdef의 noEngineReferences 설정이 풀리거나 엔진 어셈블리가 섞이면 여기서 잡는다.
        [Test]
        public void Simulation_HasNoUnityEngineReferences()
        {
            var engineRefs = typeof(SimulationAssembly).Assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("UnityEngine") || n.StartsWith("UnityEditor") || n.StartsWith("Unity."))
                .ToArray();

            Assert.That(engineRefs, Is.Empty, "Simulation references: " + string.Join(", ", engineRefs));
        }
    }
}
