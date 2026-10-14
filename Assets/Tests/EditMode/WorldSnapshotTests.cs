using System;
using System.IO;
using NUnit.Framework;
using ProjectM.Core;

namespace ProjectM.Tests
{
    public class WorldSnapshotTests
    {
        /// <summary>
        /// 시험용 AI: Ai 난수로 행동 비용(1~3)을 정하고, Combat 난수로 고른 안쪽 칸의 바닥/벽을 뒤집는다.
        /// 상태는 World에만 남긴다 (AI 자체는 상태 없음).
        /// </summary>
        private sealed class TileFlipAi : ICommandSource
        {
            private readonly World _world;

            public TileFlipAi(World world) => _world = world;

            public ICommand NextCommand(in TickContext context)
            {
                int cost = _world.Rng.Get(RngStream.Ai).Range(1, 4);
                return new FlipCommand(_world, cost);
            }
        }

        private sealed class FlipCommand : ICommand
        {
            private readonly World _world;
            public int CostTicks { get; }

            public FlipCommand(World world, int cost)
            {
                _world = world;
                CostTicks = cost;
            }

            public void Execute(in TickContext context)
            {
                var rng = _world.Rng.Get(RngStream.Combat);
                var pos = new GridPos(rng.Range(1, _world.Map.Width - 1), rng.Range(1, _world.Map.Height - 1));
                _world.Map.Set(pos, _world.Map.Get(pos) == TileType.Floor ? TileType.Wall : TileType.Floor);
            }
        }

        private static readonly int[] PlayerCosts = { 1, 2, 1, 3, 1, 1, 2, 1, 1, 3, 2, 1, 1, 1, 2 };
        private const int SplitAt = 6;
        private const ulong Seed = 777UL;

        private static World NewWorld()
        {
            var map = new GridMap(10, 8);
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    map.Set(new GridPos(x, y), x == 0 || y == 0 || x == map.Width - 1 || y == map.Height - 1 ? TileType.Wall : TileType.Floor);

            var world = new World(map, Seed);
            world.Scheduler.RegisterPlayer(world.Ids.Allocate(), world.PlayerInput);
            for (int i = 0; i < 3; i++)
                world.Scheduler.RegisterActor(world.Ids.Allocate(), new TileFlipAi(world));
            return world;
        }

        private static ICommandSource AiFactory(World world, EntityId id) => new TileFlipAi(world);

        private static void Play(World world, int from, int to)
        {
            var clock = new TurnClock(world.Scheduler, world.PlayerInput);
            for (int i = from; i < to; i++)
                clock.SubmitPlayerCommand(new WaitCommand(PlayerCosts[i]));
        }

        private static byte[] Bytes(World world) => WorldSnapshotCodec.Encode(world.Capture());

        // 가이드 6장: "저장 → 불러와 진행 = 저장 없이 진행"
        [Test]
        public void SaveLoadThenPlay_EqualsPlayWithoutSave()
        {
            var original = NewWorld();
            Play(original, 0, SplitAt);
            byte[] saved = Bytes(original);

            Play(original, SplitAt, PlayerCosts.Length);
            byte[] withoutSave = Bytes(original);

            var restored = World.Restore(WorldSnapshotCodec.Decode(saved), AiFactory);
            Play(restored, SplitAt, PlayerCosts.Length);
            byte[] afterLoad = Bytes(restored);

            Assert.That(withoutSave, Is.Not.EqualTo(saved), "시험 진행 중 상태가 실제로 바뀌어야 의미가 있다");
            Assert.That(afterLoad, Is.EqualTo(withoutSave));
        }

        [Test]
        public void SameSeedSameInputs_SameBytes()
        {
            var a = NewWorld();
            var b = NewWorld();
            Play(a, 0, PlayerCosts.Length);
            Play(b, 0, PlayerCosts.Length);
            Assert.That(Bytes(a), Is.EqualTo(Bytes(b)));
        }

        [Test]
        public void Restore_KeepsSchedulerState()
        {
            var original = NewWorld();
            Play(original, 0, SplitAt);
            var restored = World.Restore(original.Capture(), AiFactory);

            Assert.That(restored.Scheduler.CurrentTick, Is.EqualTo(original.Scheduler.CurrentTick));
            Assert.That(restored.Scheduler.PlayerId, Is.EqualTo(original.Scheduler.PlayerId));
            Assert.That(restored.Scheduler.IsWaitingForPlayer, Is.True);
            Assert.That(restored.Scheduler.CaptureActors(), Is.EqualTo(original.Scheduler.CaptureActors()));
            Assert.That(restored.Ids.NextValue, Is.EqualTo(original.Ids.NextValue));
        }

        [Test]
        public void Capture_IsIndependentCopy()
        {
            var world = NewWorld();
            var snapshot = world.Capture();
            world.Map.Set(new GridPos(1, 1), TileType.Wall);

            Assert.That(snapshot.Cells[1 * snapshot.MapWidth + 1], Is.EqualTo(TileType.Floor));
        }

        [Test]
        public void Capture_WithPendingPlayerInput_Throws()
        {
            var world = NewWorld();
            world.PlayerInput.Enqueue(new WaitCommand(1));
            Assert.Throws<InvalidOperationException>(() => world.Capture());
        }

        [Test]
        public void Codec_RoundTrip_SameBytes()
        {
            var world = NewWorld();
            Play(world, 0, SplitAt);
            byte[] first = Bytes(world);
            byte[] second = WorldSnapshotCodec.Encode(WorldSnapshotCodec.Decode(first));
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Codec_RejectsForeignOrOtherVersionData()
        {
            Assert.Throws<InvalidDataException>(() => WorldSnapshotCodec.Decode(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));

            var future = NewWorld().Capture();
            future.FormatVersion = WorldSnapshot.CurrentFormatVersion + 1;
            Assert.Throws<InvalidDataException>(() => WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(future)));

            var bytes = Bytes(NewWorld());
            Array.Resize(ref bytes, bytes.Length + 1);
            Assert.Throws<InvalidDataException>(() => WorldSnapshotCodec.Decode(bytes), "끝에 남는 데이터");
        }
    }
}
