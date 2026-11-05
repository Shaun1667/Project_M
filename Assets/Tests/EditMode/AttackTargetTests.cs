using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Units;

namespace ProjectM.Tests
{
    /// <summary>3-25 공격 대상 표시 (SPD — 결정 2026-11-05): 근접으로 닿는 보이는 적 · 마지막에 공격한 적 우선 · Tab 다음.</summary>
    public class AttackTargetTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);
        private static readonly UnitDef EnemyDef = new UnitDef("rat", 8, 1, 8);

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        /// <summary>방 (1..9, 1..3). 플레이어 (2,2).</summary>
        private static (World world, Unit player) Start()
        {
            var world = new World(GridMovementTests.Parse("###########", "#.........#", "#.........#", "#.........#", "###########"), 5UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(2, 2), Magic.QuickSlotCount);
            world.Events.Flush();
            return (world, player);
        }

        // 닿는 적이 없으면 없음, 옆 칸 적만 · 두 칸 떨어진 적은 (창이 아니면) 안 닿음
        [Test]
        public void InReach_OnlyAdjacentWithoutSpear()
        {
            var (world, player) = Start();
            Assert.That(AttackTargets.Pick(world, Sight, player, default), Is.Null);
            UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(4, 2), new IdleAi()); // 거리 2
            Assert.That(AttackTargets.Pick(world, Sight, player, default), Is.Null, "두 칸은 안 닿음");
            var near = UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(3, 3), new IdleAi());
            Assert.That(AttackTargets.Pick(world, Sight, player, default), Is.SameAs(near));
            Assert.That(AttackTargets.InReach(world, Sight, player).Count, Is.EqualTo(1));
        }

        // 여럿이면 ID 순 첫 적, 마지막에 공격한 적이 닿으면 그 적, Tab = 다음 (끝이면 처음)
        [Test]
        public void Pick_PrefersLastAttacked_NextCycles()
        {
            var (world, player) = Start();
            var a = UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(3, 2), new IdleAi());
            var b = UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(1, 2), new IdleAi());
            Assert.That(AttackTargets.Pick(world, Sight, player, default), Is.SameAs(a), "같은 거리면 ID 순");
            Assert.That(AttackTargets.Pick(world, Sight, player, b.Id), Is.SameAs(b), "마지막에 공격한 적 우선");
            Assert.That(AttackTargets.Next(world, Sight, player, a.Id), Is.SameAs(b));
            Assert.That(AttackTargets.Next(world, Sight, player, b.Id), Is.SameAs(a));
            var far = UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(6, 2), new IdleAi());
            Assert.That(AttackTargets.Pick(world, Sight, player, far.Id), Is.SameAs(a), "마지막에 공격한 적이 안 닿으면 가까운 적");
        }

        // 3-26 빠른 칸 조준 안내: 같은 번호를 다시 누르면 시전 (인벤토리에서 바로 쓸 때는 번호 없음)
        [Test]
        public void AimHint_MentionsSameSlotAgain()
        {
            var text = new ProjectM.UI.HudText();
            Assert.That(text.AimHint(2, "fire_basic", 3), Does.StartWith("[3] ").And.Contain("3 다시 · Space/클릭 시전"));
            Assert.That(text.AimHint(-1, "fire_basic", 3), Does.Not.Contain("다시"));
        }

        // 아군 · 중립은 대상이 아님
        [Test]
        public void Pick_IgnoresAlliesAndNeutrals()
        {
            var (world, player) = Start();
            UnitSpawner.SpawnAlly(world, EnemyDef, new GridPos(3, 2), new IdleAi());
            UnitSpawner.SpawnNeutral(world, EnemyDef, new GridPos(1, 2), new IdleAi());
            Assert.That(AttackTargets.Pick(world, Sight, player, default), Is.Null);
        }
    }
}
