using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.UI;
using ProjectM.Units;

namespace ProjectM.Tests
{
    /// <summary>3-27 타이틀 · 모드 · 시드 · 자동 저장 (결정 2026-11-05): 실시간 시계 · 즉시 행동 · 시드 글 · 스냅샷 v27 · 타이틀 상태.</summary>
    public class RunModeTests
    {
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);
        private static readonly UnitDef EnemyDef = new UnitDef("rat", 8, 1, 8);

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        /// <summary>시험용: 대상에게 피해 (명중 굴림 없음).</summary>
        private sealed class HitCommand : ICommand
        {
            private readonly World _world;
            private readonly Unit _target;
            private readonly int _amount;
            public int CostTicks => 1;
            public HitCommand(World world, Unit target, int amount) { _world = world; _target = target; _amount = amount; }
            public void Execute(in TickContext context) => Combat.ApplyDamage(_world, context.Actor, _target, _amount, DamageCause.Attack);
        }

        /// <summary>방 (1..9, 1..3). 플레이어 (2,2), 적 (6,2).</summary>
        private static (World world, Unit player, Unit enemy) Start()
        {
            var world = new World(GridMovementTests.Parse("###########", "#.........#", "#.........#", "#.........#", "###########"), 5UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(2, 2), Magic.QuickSlotCount);
            var enemy = UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(6, 2), new IdleAi());
            world.Events.Flush();
            return (world, player, enemy);
        }

        // 실시간: 플레이어 행동은 틱을 쓰지 않고 바로 — 여러 번 연달아 가능, 세계는 1초마다 1틱
        [Test]
        public void RealTime_PlayerActsImmediately_WorldTicksPerSecond()
        {
            var (world, player, _) = Start();
            var clock = new RealTimeClock(world.Scheduler, StepConfig.RealtimeTickIntervalSeconds);
            long tick = world.Scheduler.CurrentTick;
            Assert.That(PlayerActions.TryStep(world, StepConfig, new GridPos(1, 0), out var step), Is.True);
            clock.SubmitPlayerCommand(step);
            Assert.That(PlayerActions.TryStep(world, StepConfig, new GridPos(1, 0), out var step2), Is.True);
            clock.SubmitPlayerCommand(step2);
            Assert.That(player.Pos, Is.EqualTo(new GridPos(4, 2)), "두 번 바로 이동");
            Assert.That(world.Scheduler.CurrentTick, Is.EqualTo(tick), "틱을 쓰지 않음");
            Assert.That(world.Scheduler.IsWaitingForPlayer, Is.False, "틱은 플레이어를 기다리지 않음");

            clock.Update(0.5f);
            Assert.That(world.Scheduler.CurrentTick, Is.EqualTo(tick), "0.5초 — 아직");
            clock.Update(0.6f);
            Assert.That(world.Scheduler.CurrentTick, Is.EqualTo(tick + 1), "1초 넘음 = 1틱");
            Assert.That(clock.LastTicks, Is.EqualTo(1));
            clock.Update(100f);
            Assert.That(clock.LastTicks, Is.EqualTo(RealTimeClock.MaxTicksPerUpdate), "멈췄다 온 프레임은 몰아서 돌지 않음");
        }

        // 즉시 행동 뒤에도 위치 · 결과 시스템(사망 판정 · 이벤트 발행)은 바로 돈다
        [Test]
        public void RealTime_InstantSystemsRunAfterAction()
        {
            var (world, player, enemy) = Start();
            var clock = new RealTimeClock(world.Scheduler, 1f);
            var died = new List<UnitDiedEvent>();
            world.Events.Subscribe<UnitDiedEvent>(died.Add);
            clock.SubmitPlayerCommand(new HitCommand(world, enemy, 100));
            Assert.That(world.Units.TryGet(enemy.Id, out _), Is.False, "사망 판정이 틱 없이 돌았다");
            Assert.That(died.Count, Is.EqualTo(1), "이벤트도 바로 발행 (Flush 안 불러도)");
        }

        // 행동 불가(마비 등)면 즉시 행동도 안 됨
        [Test]
        public void RealTime_CannotActWhenDisabled()
        {
            var (world, player, enemy) = Start();
            world.Scheduler.SetActionRules(id => id != player.Id, (id, cost) => cost);
            Assert.That(world.Scheduler.ExecutePlayerNow(new HitCommand(world, enemy, 1)), Is.False);
            Assert.That(enemy.Hp, Is.EqualTo(enemy.MaxHp));
        }

        // 스냅샷 v27: 판의 모드 왕복
        [Test]
        public void Snapshot_RoundTripsMode()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(27));
            var (world, _, _) = Start();
            world.Run.Mode = RunMode.RealTime;
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(world.Capture())), (w, id) => new IdleAi());
            Assert.That(restored.Run.Mode, Is.EqualTo(RunMode.RealTime));
        }

        // 시드 글: 비면 무작위, 정수는 그대로(음수 포함 — 결과 화면 값), 글자는 해시 — 같은 글 = 같은 시드
        [Test]
        public void SeedInput_ParsesNumbersAndText()
        {
            Assert.That(SeedInput.TryParse("", out _), Is.False);
            Assert.That(SeedInput.TryParse("   ", out _), Is.False);
            Assert.That(SeedInput.TryParse(" 12345 ", out var n) && n == 12345UL, Is.True);
            Assert.That(SeedInput.TryParse("-5", out var neg) && neg == unchecked((ulong)-5L), Is.True);
            Assert.That(SeedInput.TryParse("불꽃 던전", out var a), Is.True);
            Assert.That(SeedInput.TryParse("불꽃 던전", out var b) && a == b, Is.True, "같은 글 = 같은 시드");
            Assert.That(SeedInput.TryParse("불꽃 던젼", out var c) && c != a, Is.True);
            Assert.That(SeedInput.TryParse("0", out var zero) && zero != 0UL, Is.True, "0은 '시드 없음'과 겹치지 않게");
            Assert.That(SeedInput.Hash("abc"), Is.EqualTo(SeedInput.Hash("abc")));
        }

        // 타이틀: 저장 있으면 이어하기 맨 위, 게임 시작 → 모드 · 시드 → 시작
        [Test]
        public void TitleMenu_Flow()
        {
            var m = new TitleMenuModel();
            m.Open(canContinue: false);
            Assert.That(m.Items, Is.EqualTo(new[] { TitleItem.NewGame, TitleItem.Quit }));
            m.Open(canContinue: true);
            Assert.That(m.Items[0], Is.EqualTo(TitleItem.Continue));
            bool continued = false;
            m.ContinueChosen += () => continued = true;
            m.Confirm();
            Assert.That(continued && !m.IsOpen, Is.True, "이어하기");

            m.Open(canContinue: true);
            m.Move(1);
            m.Confirm();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.NewGame));
            m.Type('a'); m.Type('\n'); m.Type('7'); m.Backspace(); m.Type('9');
            Assert.That(m.SeedText, Is.EqualTo("a9"), "제어 문자 무시 · 지우기");
            for (int i = 0; i < 40; i++) m.Type('x');
            Assert.That(m.SeedText.Length, Is.EqualTo(SeedInput.MaxLength));
            m.ToggleMode();
            Assert.That(m.Mode, Is.EqualTo(RunMode.RealTime));
            m.Back();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.Main));
            m.Move(-1);
            m.Move(1);
            m.Confirm(); // 게임 시작 (칸 1)
            RunMode chosen = RunMode.TurnBased;
            string seed = null;
            m.StartChosen += (mode, text) => { chosen = mode; seed = text; };
            m.Confirm();
            Assert.That(chosen, Is.EqualTo(RunMode.RealTime), "모드가 남아 있다");
            Assert.That(seed, Has.Length.EqualTo(SeedInput.MaxLength), "시드 글이 남아 있다");
            Assert.That(m.IsOpen, Is.False);
        }

        // 판 글자: 모드 이름 · 결과 화면 안내
        [Test]
        public void HudText_TitleLines()
        {
            Assert.That(HudText.ModeName(RunMode.RealTime), Is.EqualTo("실시간"));
            Assert.That(HudText.ModeName(RunMode.TurnBased), Is.EqualTo("턴제"));
            Assert.That(HudText.TitleItemName(TitleItem.Continue), Is.EqualTo("이어하기"));
            Assert.That(HudText.ResultRestart, Does.Contain("타이틀"));
        }
    }
}
