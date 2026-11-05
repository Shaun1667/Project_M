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
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(27)); // 정확한 값은 RelicTests (28)
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

        // 타이틀 (3-30 슬롯): 게임 시작 → 슬롯 5칸 — 찬 슬롯 = 요약 → 시작 / 삭제(한 번 더 묻기), 빈 슬롯 = 모드 · 시드 → 시작
        [Test]
        public void TitleMenu_Flow()
        {
            var slots = new[]
            {
                new SlotInfo(true, "루멘 · Lv 3 · 2층 · 턴제", "요약"),
                new SlotInfo(false, null, null), new SlotInfo(false, null, null), new SlotInfo(false, null, null), new SlotInfo(false, null, null),
            };
            var m = new TitleMenuModel();
            m.Open(slots);
            Assert.That(m.Items, Is.EqualTo(new[] { TitleItem.NewGame, TitleItem.HallOfFame, TitleItem.Controls, TitleItem.Quit }), "3-30: 이어하기는 슬롯 화면으로 합침 · 3-33 명예의 전당은 게임 시작 아래 · 2026-11-05 조작 안내");
            m.Confirm();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.Slots));

            // 찬 슬롯 1 → 요약 → 시작 = 이어하기(슬롯 1)
            int continued = 0;
            m.ContinueChosen += s => continued = s;
            m.Confirm();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.SlotDetail));
            m.Confirm();
            Assert.That((continued, m.IsOpen), Is.EqualTo((1, false)));

            // 삭제: 요약 → 데이터 삭제 → 확인 화면(처음은 '취소') → 삭제
            m.Open(slots);
            m.Confirm(); m.Confirm();
            m.Move(1);
            m.Confirm();
            Assert.That((m.Screen, m.OptionSelected), Is.EqualTo((TitleScreen.ConfirmDelete, 1)), "안전하게 취소에서 시작");
            m.Confirm();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.SlotDetail), "취소 = 요약으로");
            m.Move(1); m.Confirm();
            int deleted = 0;
            m.DeleteChosen += s => deleted = s;
            m.Move(-1); m.Confirm();
            Assert.That((deleted, m.Screen), Is.EqualTo((1, TitleScreen.Slots)));

            // 빈 슬롯 3 → 새 게임 → 시드 · 모드 → 시작(슬롯 3)
            m.Select(2);
            m.Confirm();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.NewGame));
            m.Type('a'); m.Type('\n'); m.Type('7'); m.Backspace(); m.Type('9');
            Assert.That(m.SeedText, Is.EqualTo("a9"), "제어 문자 무시 · 지우기");
            for (int i = 0; i < 40; i++) m.Type('x');
            Assert.That(m.SeedText.Length, Is.EqualTo(SeedInput.MaxLength));
            m.ToggleMode();
            Assert.That(m.Mode, Is.EqualTo(RunMode.RealTime));
            int startedSlot = 0;
            RunMode chosen = RunMode.TurnBased;
            m.StartChosen += (s, mode, text) => { startedSlot = s; chosen = mode; };
            m.Confirm();
            Assert.That((startedSlot, chosen, m.IsOpen), Is.EqualTo((3, RunMode.RealTime, false)));

            // Esc: 새 게임 → 슬롯 → 첫 화면
            m.Open(slots);
            m.Confirm(); m.Select(1); m.Confirm();
            m.Back();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.Slots));
            m.Back();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.Main));
        }

        // 2026-11-05 지스타 시연: 조작 안내 (타이틀에서 = 첫 화면으로 · 게임 중 = 닫힘) · 실시간 잠금
        [Test]
        public void TitleMenu_ControlsAndRealTimeLock()
        {
            var slots = new[] { new SlotInfo(false, null, null) };
            var m = new TitleMenuModel { RealTimeLocked = true };
            m.Open(slots);
            m.Select(2);
            Assert.That(m.Items[2], Is.EqualTo(TitleItem.Controls));
            m.Confirm();
            Assert.That((m.Screen, m.ControlsInGame), Is.EqualTo((TitleScreen.Controls, false)));
            m.Confirm();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.Main), "타이틀에서 연 안내 = 닫으면 첫 화면");
            m.Select(2); m.Confirm(); m.Back();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.Main), "Esc도 첫 화면");

            m.Close();
            m.OpenControls();
            Assert.That((m.Screen, m.ControlsInGame, m.IsOpen), Is.EqualTo((TitleScreen.Controls, true, true)));
            m.Confirm();
            Assert.That((m.IsOpen, m.ControlsInGame), Is.EqualTo((false, false)), "게임 중 안내 = 닫으면 게임으로");

            // 실시간 잠금: 새 게임 화면에서 골라도 · 바꿔도 턴제 그대로
            m.Open(slots);
            m.Select(0); m.Confirm(); m.Confirm();
            Assert.That(m.Screen, Is.EqualTo(TitleScreen.NewGame));
            m.SetMode(RunMode.RealTime);
            m.ToggleMode();
            Assert.That(m.Mode, Is.EqualTo(RunMode.TurnBased));
            RunMode chosen = RunMode.RealTime;
            m.StartChosen += (s, mode, text) => chosen = mode;
            m.Confirm();
            Assert.That(chosen, Is.EqualTo(RunMode.TurnBased));
            Assert.That(HudText.TitleItemName(TitleItem.Controls), Is.EqualTo("조작 안내"));
            StringAssert.Contains("잠김", HudText.ModeRealLockedName);
            Assert.That(HudText.DemoClearTitle(5), Does.Contain("시연 완료").And.Contain("5층")); // 3-37
            Assert.That((HudText.DemoContinue, HudText.DemoToTitle), Is.EqualTo(("이어서 하기", "타이틀로")));
        }

        // 판 글자: 모드 이름 · 결과 화면 안내
        [Test]
        public void HudText_TitleLines()
        {
            Assert.That(HudText.ModeName(RunMode.RealTime), Is.EqualTo("실시간"));
            Assert.That(HudText.ModeName(RunMode.TurnBased), Is.EqualTo("턴제"));
            // 3-30 슬롯 글
            Assert.That(HudText.SlotLabel(2, false, null), Does.Contain("슬롯 2").And.Contain(HudText.SlotEmpty));
            Assert.That(HudText.SlotLine("루멘", 5, 7, RunMode.RealTime), Is.EqualTo("루멘 · Lv 5 · 7층 · 실시간"));
            var detail = new HudText().SlotDetail("루멘", "근접형", 5, 40, 18, 30, 6, 14, 11, 12, 6, "창+1", null, new[] { "생명석 부적" }, 7, RunMode.TurnBased, -5, 120, 6, 23, 980);
            Assert.That(detail, Does.Contain("HP 18/30").And.Contain("7층").And.Contain("생명석 부적").And.Contain("에테르 6").And.Contain("980턴"));
            Assert.That(HudText.TitleItemName(TitleItem.Continue), Is.EqualTo("이어하기"));
            Assert.That(HudText.ResultRestart, Does.Contain("타이틀"));
        }
    }
}
