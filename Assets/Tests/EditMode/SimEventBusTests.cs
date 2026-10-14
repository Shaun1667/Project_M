using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;

namespace ProjectM.Tests
{
    public class SimEventBusTests
    {
        private SimEventBus _bus;
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _bus = new SimEventBus();
            _log = new List<string>();
        }

        [Test]
        public void Publish_IsBufferedUntilFlush_ThenDeliveredInOrder()
        {
            _bus.Subscribe<EntityRemovedEvent>(e => _log.Add("removed " + e.Id.Value));
            _bus.Subscribe<TileChangedEvent>(e => _log.Add("tile " + e.Pos));

            _bus.Publish(new EntityRemovedEvent(new EntityId(2)));
            _bus.Publish(new TileChangedEvent(new GridPos(1, 1), TileType.Wall));
            _bus.Publish(new EntityRemovedEvent(new EntityId(3)));
            Assert.That(_log, Is.Empty);
            Assert.That(_bus.PendingCount, Is.EqualTo(3));

            _bus.Flush();
            Assert.That(_log, Is.EqualTo(new[] { "removed 2", "tile (1, 1)", "removed 3" }));
            Assert.That(_bus.PendingCount, Is.EqualTo(0));

            _bus.Flush();
            Assert.That(_log.Count, Is.EqualTo(3), "두 번 보내지 않음");
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            System.Action<EntityRemovedEvent> handler = e => _log.Add("x");
            _bus.Subscribe(handler);
            _bus.Unsubscribe(handler);

            _bus.Publish(new EntityRemovedEvent(new EntityId(1)));
            _bus.Flush();
            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void EventsPublishedDuringFlush_GoToNextFlush()
        {
            _bus.Subscribe<EntityRemovedEvent>(e =>
            {
                _log.Add("removed " + e.Id.Value);
                if (e.Id.Value == 1) _bus.Publish(new EntityRemovedEvent(new EntityId(2)));
            });

            _bus.Publish(new EntityRemovedEvent(new EntityId(1)));
            _bus.Flush();
            Assert.That(_log, Is.EqualTo(new[] { "removed 1" }));

            _bus.Flush();
            Assert.That(_log, Is.EqualTo(new[] { "removed 1", "removed 2" }));
        }

        [Test]
        public void EventFlushSystem_DeliversAtEndOfTick_AfterOtherPhases()
        {
            var scheduler = new TickScheduler();
            var input = new PlayerCommandSource();
            scheduler.RegisterPlayer(new EntityId(1), input);
            scheduler.AddSystem(TickPhase.EventFlush, new EventFlushSystem(_bus));
            scheduler.AddSystem(TickPhase.Status, new PublishingSystem(_bus, _log));
            _bus.Subscribe<TileChangedEvent>(e => _log.Add("delivered"));

            new TurnClock(scheduler, input).SubmitPlayerCommand(new WaitCommand(1));

            Assert.That(_log, Is.EqualTo(new[] { "published", "delivered" }));
        }

        [Test]
        public void MapLoadedEvent_CopiesCells()
        {
            var map = new GridMap(3, 2);
            map.Set(new GridPos(2, 1), TileType.Wall);
            var e = new MapLoadedEvent(map);

            map.Set(new GridPos(2, 1), TileType.Floor);

            Assert.That(e.Width, Is.EqualTo(3));
            Assert.That(e.Cells[1 * 3 + 2], Is.EqualTo(TileType.Wall), "이벤트 이후 맵 변경이 이벤트에 새지 않음");
        }

        private sealed class PublishingSystem : ITickSystem
        {
            private readonly SimEventBus _bus;
            private readonly List<string> _log;

            public PublishingSystem(SimEventBus bus, List<string> log)
            {
                _bus = bus;
                _log = log;
            }

            public void Run(in TickContext context)
            {
                _bus.Publish(new TileChangedEvent(new GridPos(0, 0), TileType.Floor));
                _log.Add("published");
            }
        }
    }
}
