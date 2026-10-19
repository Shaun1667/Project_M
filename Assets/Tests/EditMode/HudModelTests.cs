using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.UI;
using ProjectM.Units;

namespace ProjectM.Tests
{
    /// <summary>2-5 HUD 모델: 표시 값 · 한글 로그 · 지속 갱신 숨김 · 줄 수 제한.</summary>
    public class HudModelTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10,
            new[] { "fire_basic", "ice_basic", "lightning_basic", "poison_basic" });
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 8, 2, 6);
        private static readonly HudText Text = new HudText(new Dictionary<string, string> { { "fire_basic", "불꽃 화살" } });

        private sealed class Session
        {
            public World World;
            public SpellBook Book;
            public TurnClock Clock;
            public Unit Player;
            public Unit Enemy;
            public HudModel Hud;

            public void Wait(int turns)
            {
                for (int i = 0; i < turns; i++) Clock.SubmitPlayerCommand(PlayerActions.Wait(Config));
                World.Events.Flush();
            }
        }

        private static Session Start(int logLines = 8)
        {
            var map = GridMovementTests.Parse(
                "############",
                "#..........#",
                "#..........#",
                "#..........#",
                "############");
            var world = new World(map, 7);
            var s = new Session { World = world, Book = SpellTests.BalanceBookFull(), Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            world.InstallCoreSystems(new ShadowcastSightRule(), Magic, s.Book.Statuses, s.Book.Terrains);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 2), Magic.QuickSlotCount);
            s.Enemy = UnitSpawner.SpawnEnemy(world, EnemyDef, new GridPos(9, 2), new IdleAi());
            s.Hud = new HudModel(logLines, Text);
            s.Hud.Bind(world.Events, s.Player, s.Book);
            world.Events.Flush();
            return s;
        }

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        [Test]
        public void Bind_ShowsPlayerValuesAndSlotCosts()
        {
            var s = Start();
            Assert.That(s.Hud.Hp, Is.EqualTo(20));
            Assert.That(s.Hud.MaxHp, Is.EqualTo(20));
            Assert.That(s.Hud.Mana, Is.EqualTo(10));
            Assert.That(s.Hud.MaxMana, Is.EqualTo(10));
            Assert.That(s.Hud.QuickSlots, Is.EqualTo(new[] { InventoryRef.Spell("fire_basic"), InventoryRef.Spell("ice_basic"), InventoryRef.Spell("lightning_basic"), InventoryRef.Spell("poison_basic") }));
            Assert.That(s.Hud.SlotCosts, Is.EqualTo(new[] { 3, 2, 4, 3 }), "BALANCE 5번 마나 비용");
            Assert.That(s.Hud.Log, Is.Empty);
        }

        // 결정 2026-10-16: 같은 상태의 지속 갱신은 로그에 남기지 않는다 — 새로 걸림 · 풀림만
        [Test]
        public void StandingInFireZone_LogsBurnOnceAndWearOffOnce()
        {
            var s = Start();
            s.Book.Terrains.TryGet(TerrainEffectType.FireZone, out var fireZone);
            TerrainEffects.Place(s.World, s.Player.Pos, fireZone);

            s.Wait(4); // 지대 4틱 동안 매 틱 화상 갱신
            Assert.That(s.Hud.Log.Count(l => l.StartsWith("나: 화상") && l.EndsWith("턴")), Is.EqualTo(1), string.Join(" / ", s.Hud.Log));
            Assert.That(s.Hud.PlayerStatuses.ContainsKey(StatusType.Burn), Is.True);

            s.Wait(3);
            Assert.That(s.Hud.Log.Count(l => l == "나: 화상 풀림"), Is.EqualTo(1), string.Join(" / ", s.Hud.Log));
            Assert.That(s.Hud.PlayerStatuses, Is.Empty);
            Assert.That(s.Hud.Hp, Is.EqualTo(s.Player.Hp));
        }

        [Test]
        public void StatusAppliedAgainAfterWearingOff_IsLoggedAgain()
        {
            var s = Start();
            var bus = s.World.Events;
            var id = s.Enemy.Id;
            bus.Publish(new StatusChangedEvent(id, StatusType.Poison, 5, true));
            bus.Publish(new StatusChangedEvent(id, StatusType.Poison, 5, true)); // 갱신 — 숨김
            bus.Publish(new StatusChangedEvent(id, StatusType.Poison, 4, false)); // 1 줄어듦 — 숨김
            bus.Publish(new StatusChangedEvent(id, StatusType.Poison, 0, false));
            bus.Publish(new StatusChangedEvent(id, StatusType.Poison, 5, true));
            bus.Flush();
            string enemy = $"적 {id}";
            Assert.That(s.Hud.Log, Is.EqualTo(new[] { $"{enemy}: 중독 5턴", $"{enemy}: 중독 풀림", $"{enemy}: 중독 5턴" }));
        }

        [Test]
        public void CombatMessages_AreKoreanWithSpellDisplayName()
        {
            var s = Start();
            var bus = s.World.Events;
            var enemyId = s.Enemy.Id;
            var playerId = s.Player.Id;
            bus.Publish(new SpellCastEvent(playerId, "fire_basic", Element.Fire, s.Enemy.Pos, null));
            bus.Publish(new SpellCastEvent(playerId, "ice_basic", Element.Ice, s.Enemy.Pos, null)); // 이름 없으면 ID
            bus.Publish(new UnitDamagedEvent(enemyId, playerId, 5, 3, 8));
            bus.Publish(new UnitDamagedEvent(enemyId, EntityId.None, 1, 2, 8));
            bus.Publish(new UnitDamagedEvent(playerId, enemyId, 2, 18, 20));
            bus.Publish(new UnitDamagedEvent(playerId, EntityId.None, 1, 17, 20));
            bus.Publish(new UnitDiedEvent(enemyId, Faction.Enemy));
            bus.Flush();

            string enemy = $"적 {enemyId}";
            Assert.That(s.Hud.Log, Is.EqualTo(new[]
            {
                "불꽃 화살 시전",
                "ice_basic 시전",
                $"{enemy}에게 피해 5 (3/8)",
                $"{enemy}: 지속 피해 1 (2/8)",
                $"{enemy}의 공격: 피해 2 (HP 18/20)",
                "나: 지속 피해 1 (HP 17/20)",
                $"{enemy} 쓰러짐",
            }));
            Assert.That(s.Hud.Hp, Is.EqualTo(17));
        }

        [Test]
        public void Log_KeepsOnlyNewestLines()
        {
            var s = Start(logLines: 8);
            for (int i = 1; i <= 10; i++)
                s.World.Events.Publish(new UnitDamagedEvent(s.Enemy.Id, s.Player.Id, i, 8, 8));
            s.World.Events.Flush();
            Assert.That(s.Hud.Log.Count, Is.EqualTo(8));
            Assert.That(s.Hud.Log[0], Does.Contain("피해 3 "), "가장 오래된 두 줄은 밀려남");
            Assert.That(s.Hud.Log[7], Does.Contain("피해 10 "), "새 줄이 맨 뒤");
        }

        [Test]
        public void PlayerDeath_SetsDeadAndClearsStatuses()
        {
            var s = Start();
            var bus = s.World.Events;
            bus.Publish(new StatusChangedEvent(s.Player.Id, StatusType.Burn, 3, true));
            bus.Publish(new UnitDiedEvent(s.Player.Id, Faction.Player));
            bus.Flush();
            Assert.That(s.Hud.Dead, Is.True);
            Assert.That(s.Hud.Hp, Is.EqualTo(0));
            Assert.That(s.Hud.PlayerStatuses, Is.Empty);
            Assert.That(s.Hud.Log.Last(), Is.EqualTo("쓰러졌다."));
        }

        [Test]
        public void ManaChange_OnlyForPlayer()
        {
            var s = Start();
            s.World.Events.Publish(new ManaChangedEvent(s.Enemy.Id, 0, 0));
            s.World.Events.Publish(new ManaChangedEvent(s.Player.Id, 7, 10));
            s.World.Events.Flush();
            Assert.That(s.Hud.Mana, Is.EqualTo(7));
            Assert.That(s.Hud.MaxMana, Is.EqualTo(10));
        }

        // 2-6: 자동 회복은 HP 막대만 갱신, 로그 없음
        [Test]
        public void Heal_UpdatesHpWithoutLog()
        {
            var s = Start();
            s.World.Events.Publish(new UnitDamagedEvent(s.Player.Id, EntityId.None, 3, 17, 20));
            s.World.Events.Publish(new UnitHealedEvent(s.Player.Id, 1, 18, 20));
            s.World.Events.Publish(new UnitHealedEvent(s.Enemy.Id, 1, 8, 8));
            s.World.Events.Flush();
            Assert.That(s.Hud.Hp, Is.EqualTo(18));
            Assert.That(s.Hud.Log.Count, Is.EqualTo(1), "피해 한 줄만");
        }

        // 3-1: 반응 로그 = 데이터 이름 + "!" (+ 대상). 이름이 없으면 ID
        [Test]
        public void Reaction_LogsDisplayName()
        {
            var s = Start();
            var hud = new HudModel(8, new HudText(null, new Dictionary<string, string> { { "toxic_ignite", "독화" } }));
            hud.Bind(s.World.Events, s.Player, s.Book);
            s.World.Events.Publish(new ReactionTriggeredEvent("toxic_ignite", s.Player.Id, s.Enemy.Pos, s.Enemy.Id));
            s.World.Events.Publish(new ReactionTriggeredEvent("gas_explosion", s.Player.Id, new GridPos(3, 2), EntityId.None));
            s.World.Events.Flush();
            Assert.That(hud.Log, Is.EqualTo(new[] { $"독화! 적 {s.Enemy.Id}", "gas_explosion!" }));
        }

        // 3-2a: 빠른 칸을 바꾸면 HUD 칸 · 비용이 따라 바뀐다 (3-1 장착 칸 테스트를 옮김)
        [Test]
        public void QuickSlotsChange_UpdatesSlotsAndCosts()
        {
            var s = Start();
            Inventory.AssignQuickSlot(s.World, s.Player, 0, InventoryRef.Empty);
            Inventory.AssignQuickSlot(s.World, s.Player, 3, InventoryRef.Spell("fire_basic"));
            s.World.Events.Flush();
            Assert.That(s.Hud.QuickSlots, Is.EqualTo(new[] { InventoryRef.Empty, InventoryRef.Spell("ice_basic"), InventoryRef.Spell("lightning_basic"), InventoryRef.Spell("fire_basic") }));
            Assert.That(s.Hud.SlotCosts, Is.EqualTo(new[] { -1, 2, 4, 3 }));
            Assert.Throws<System.ArgumentException>(() => Inventory.AssignQuickSlot(s.World, s.Player, 0, InventoryRef.Spell("no_such_spell")), "인벤토리에 없는 주문");
        }

        // 3-6 · 3-7: 경험치 막대 · 레벨업 · 빗나감 로그
        [Test]
        public void ExpAndLevelUp_UpdateBarAndLog()
        {
            var s = Start();
            var progress = new ProgressionConfig(30, 5, 5, 5, 1, 1, 1);
            s.Hud.Bind(s.World.Events, s.Player, s.Book, progress);
            Assert.That((s.Hud.Level, s.Hud.Exp, s.Hud.ExpToNext), Is.EqualTo((1, 0, 10)));

            Progression.GainExp(s.World, s.Player, 12, progress);
            s.World.Events.Flush();
            Assert.That((s.Hud.Level, s.Hud.Exp, s.Hud.ExpToNext), Is.EqualTo((2, 2, 15)));
            Assert.That((s.Hud.MaxHp, s.Hud.MaxMana), Is.EqualTo((25, 11)));
            Assert.That(s.Hud.Log, Does.Contain("레벨 2! 최대 HP 25 · MP 11"));
            Assert.That(s.Hud.Log.Last(), Is.EqualTo("경험치 +12"));
            Assert.That(HudText.ExpLabel(2, 2, 15), Is.EqualTo("Lv 2  EXP 2/15"));
            Assert.That(HudText.ExpLabel(30, 0, 0), Is.EqualTo("Lv 30 (최대)"));
        }

        [Test]
        public void AttackMissed_LogsOnlyWhenPlayerInvolved()
        {
            var s = Start();
            s.World.Events.Publish(new AttackMissedEvent(s.Player.Id, s.Enemy.Id));
            s.World.Events.Publish(new AttackMissedEvent(s.Enemy.Id, s.Player.Id));
            s.World.Events.Publish(new AttackMissedEvent(s.Enemy.Id, s.Enemy.Id));
            s.World.Events.Flush();
            Assert.That(s.Hud.Log, Is.EqualTo(new[] { $"적 {s.Enemy.Id}에게 빗나감", $"적 {s.Enemy.Id}의 공격: 빗나감" }));
        }
    }
}
