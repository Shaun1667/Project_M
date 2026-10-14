using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;

namespace ProjectM.Tests
{
    public class TickSchedulerTests
    {
        /// <summary>실행되면 "틱:행위자" 를 기록하는 커맨드.</summary>
        private sealed class LogCommand : ICommand
        {
            private readonly List<string> _log;
            private readonly Action<TickContext> _onExecute;
            public int CostTicks { get; }

            public LogCommand(List<string> log, int cost, Action<TickContext> onExecute = null)
            {
                _log = log;
                CostTicks = cost;
                _onExecute = onExecute;
            }

            public void Execute(in TickContext context)
            {
                _log.Add(context.Tick + ":" + context.Actor.Value);
                _onExecute?.Invoke(context);
            }
        }

        /// <summary>항상 같은 비용의 LogCommand를 내는 AI.</summary>
        private sealed class FixedAi : ICommandSource
        {
            private readonly List<string> _log;
            private readonly int _cost;
            public Action<TickContext> OnExecute;

            public FixedAi(List<string> log, int cost)
            {
                _log = log;
                _cost = cost;
            }

            public ICommand NextCommand(in TickContext context) => new LogCommand(_log, _cost, OnExecute);
        }

        private sealed class PhaseRecorder : ITickSystem
        {
            private readonly List<string> _log;
            private readonly string _name;

            public PhaseRecorder(List<string> log, string name)
            {
                _log = log;
                _name = name;
            }

            public void Run(in TickContext context) => _log.Add(context.Tick + ":" + _name);
        }

        private static readonly EntityId Player = new EntityId(1);

        private List<string> _log;
        private TickScheduler _scheduler;
        private PlayerCommandSource _input;
        private TurnClock _clock;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _scheduler = new TickScheduler();
            _input = new PlayerCommandSource();
            _scheduler.RegisterPlayer(Player, _input);
            _clock = new TurnClock(_scheduler, _input);
        }

        [Test]
        public void TurnMode_WaitsForPlayerInput()
        {
            Assert.That(_scheduler.IsWaitingForPlayer, Is.True);
            Assert.Throws<InvalidOperationException>(() => _scheduler.Tick());
            Assert.That(_scheduler.CurrentTick, Is.EqualTo(0));
        }

        [Test]
        public void OneSubmit_CostOne_AdvancesOneTick_PlayerFirstThenEnemiesById()
        {
            _scheduler.RegisterActor(new EntityId(5), new FixedAi(_log, 1));
            _scheduler.RegisterActor(new EntityId(3), new FixedAi(_log, 1));

            _clock.SubmitPlayerCommand(new LogCommand(_log, 1));

            Assert.That(_log, Is.EqualTo(new[] { "0:1", "0:3", "0:5" }));
            Assert.That(_scheduler.CurrentTick, Is.EqualTo(1));
            Assert.That(_scheduler.IsWaitingForPlayer, Is.True);
        }

        [Test]
        public void PlayerCostTwo_EnemiesActTwice()
        {
            _scheduler.RegisterActor(new EntityId(2), new FixedAi(_log, 1));

            _clock.SubmitPlayerCommand(new LogCommand(_log, 2));

            Assert.That(_log, Is.EqualTo(new[] { "0:1", "0:2", "1:2" }));
            Assert.That(_scheduler.CurrentTick, Is.EqualTo(2));
            Assert.That(_scheduler.GetReadyTick(Player), Is.EqualTo(2));
        }

        [Test]
        public void SlowEnemy_ActsEveryCostTicks()
        {
            _scheduler.RegisterActor(new EntityId(2), new FixedAi(_log, 3));

            for (int i = 0; i < 6; i++)
                _clock.SubmitPlayerCommand(new LogCommand(_log, 1));

            Assert.That(_log.FindAll(e => e.EndsWith(":2")), Is.EqualTo(new[] { "0:2", "3:2" }));
        }

        [Test]
        public void PhaseSystems_RunAfterActions_InTickPhaseOrder()
        {
            // 일부러 역순으로 등록한다.
            _scheduler.AddSystem(TickPhase.EventFlush, new PhaseRecorder(_log, "EventFlush"));
            _scheduler.AddSystem(TickPhase.DeathCheck, new PhaseRecorder(_log, "DeathCheck"));
            _scheduler.AddSystem(TickPhase.ManaRegen, new PhaseRecorder(_log, "ManaRegen"));
            _scheduler.AddSystem(TickPhase.Status, new PhaseRecorder(_log, "Status"));
            _scheduler.AddSystem(TickPhase.Terrain, new PhaseRecorder(_log, "Terrain"));
            _scheduler.AddSystem(TickPhase.Reactions, new PhaseRecorder(_log, "Reactions"));

            _clock.SubmitPlayerCommand(new LogCommand(_log, 1));

            Assert.That(_log, Is.EqualTo(new[]
            {
                "0:1", "0:Reactions", "0:Terrain", "0:Status", "0:ManaRegen", "0:DeathCheck", "0:EventFlush",
            }));
        }

        [Test]
        public void SystemsInSamePhase_RunInRegistrationOrder()
        {
            _scheduler.AddSystem(TickPhase.Status, new PhaseRecorder(_log, "A"));
            _scheduler.AddSystem(TickPhase.Status, new PhaseRecorder(_log, "B"));

            _clock.SubmitPlayerCommand(new WaitCommand(1));

            Assert.That(_log, Is.EqualTo(new[] { "0:A", "0:B" }));
        }

        [Test]
        public void ActionsPhase_CannotTakeSystems()
        {
            Assert.Throws<ArgumentException>(() => _scheduler.AddSystem(TickPhase.Actions, new PhaseRecorder(_log, "x")));
        }

        [Test]
        public void ActorRemovedByEarlierAction_DoesNotActThatTick()
        {
            var victim = new EntityId(3);
            _scheduler.RegisterActor(victim, new FixedAi(_log, 1));

            _clock.SubmitPlayerCommand(new LogCommand(_log, 1, _ => _scheduler.Unregister(victim)));

            Assert.That(_log, Is.EqualTo(new[] { "0:1" }));
            Assert.That(_scheduler.IsRegistered(victim), Is.False);
        }

        [Test]
        public void ActorSpawnedDuringTick_ActsFromNextTick()
        {
            var spawned = new EntityId(9);
            _clock.SubmitPlayerCommand(new LogCommand(_log, 1, _ => _scheduler.RegisterActor(spawned, new FixedAi(_log, 1))));
            Assert.That(_log, Is.EqualTo(new[] { "0:1" }));

            _clock.SubmitPlayerCommand(new LogCommand(_log, 1));
            Assert.That(_log, Is.EqualTo(new[] { "0:1", "1:1", "1:9" }));
        }

        [Test]
        public void CommandCostBelowOne_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => _clock.SubmitPlayerCommand(new LogCommand(_log, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WaitCommand(0));
        }

        [Test]
        public void Registration_RejectsDuplicatesAndSecondPlayer()
        {
            Assert.Throws<InvalidOperationException>(() => _scheduler.RegisterActor(Player, new FixedAi(_log, 1)));
            Assert.Throws<InvalidOperationException>(() => _scheduler.RegisterPlayer(new EntityId(2), new PlayerCommandSource()));
            Assert.Throws<ArgumentException>(() => _scheduler.RegisterActor(EntityId.None, new FixedAi(_log, 1)));
        }

        [Test]
        public void TurnClock_StopsWhenPlayerRemoved_AndRejectsInputWithoutPlayer()
        {
            _scheduler.RegisterActor(new EntityId(2), new FixedAi(_log, 1));
            _clock.SubmitPlayerCommand(new LogCommand(_log, 3, _ => _scheduler.Unregister(Player)));

            Assert.That(_scheduler.CurrentTick, Is.EqualTo(1), "플레이어가 빠지면 진행을 멈춘다");
            Assert.Throws<InvalidOperationException>(() => _clock.SubmitPlayerCommand(new WaitCommand(1)));
        }

        [Test]
        public void TurnClock_UpdateDoesNotAdvance()
        {
            _clock.Update(10f);
            Assert.That(_scheduler.CurrentTick, Is.EqualTo(0));
        }

        [Test]
        public void SameInputs_SameLog()
        {
            List<string> Run()
            {
                var log = new List<string>();
                var scheduler = new TickScheduler();
                var input = new PlayerCommandSource();
                scheduler.RegisterPlayer(Player, input);
                scheduler.RegisterActor(new EntityId(4), new FixedAi(log, 2));
                scheduler.RegisterActor(new EntityId(2), new FixedAi(log, 1));
                var clock = new TurnClock(scheduler, input);
                foreach (var cost in new[] { 1, 3, 1, 2 })
                    clock.SubmitPlayerCommand(new LogCommand(log, cost));
                return log;
            }

            Assert.That(Run(), Is.EqualTo(Run()));
        }
    }
}
