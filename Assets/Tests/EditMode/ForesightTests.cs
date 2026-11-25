using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>
    /// 3-60 예지 (결정 2026-11-25 — 예지 상태 + 고른 행동의 미래): 스냅샷 복사본에서 행동을 해 보고 플레이어 차례가 다시 올 때까지 돌린 결과.
    /// 같은 상태 · 같은 난수 = 실제로 같은 행동을 했을 때와 같은 자리 (규칙 6 · 7). 원래 World는 바뀌지 않는다. 예지 주문서 = 예지 상태 20틱 (BALANCE.md 6번 · 14번).
    /// </summary>
    public class ForesightTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);

        private static SpellBook Spells(StatusBook statuses) => new SpellBook(new[]
        {
            new SpellDef("blast", Element.Fire, SpellShape.Bolt, 8, 99, 1),
        }, statuses, SpellTests.BalanceTerrains());

        private sealed class Run
        {
            public World World;
            public Unit Player;
            public Unit Enemy;
            public StatusBook Statuses;
            public SpellBook Spells;
        }

        private static World Connect(World world, StatusBook statuses, SpellBook spells)
        {
            world.InstallCoreSystems(Sight, Magic, statuses, SpellTests.BalanceTerrains());
            return world;
        }

        private static Run Start()
        {
            var world = new World(GridMovementTests.Parse("##########", "#........#", "#........#", "#........#", "##########"), 11UL);
            var statuses = SpellTests.BalanceStatuses();
            var spells = Spells(statuses);
            Connect(world, statuses, spells);
            var ai = new MeleeChaserAi(world, Config, Sight, statuses);
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 30, 2, 8, 10, new[] { "blast" }), new GridPos(1, 2));
            var enemy = UnitSpawner.SpawnEnemy(world, new UnitDef("enemy", 20, 2, 8), new GridPos(6, 2), ai);
            world.Events.Flush();
            return new Run { World = world, Player = player, Enemy = enemy, Statuses = statuses, Spells = spells };
        }

        private static World Clone(Run r)
        {
            MeleeChaserAi ai = null;
            var clone = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(r.World.Capture())),
                (w, id) => ai ?? (ai = new MeleeChaserAi(w, Config, Sight, r.Statuses)));
            return Connect(clone, r.Statuses, r.Spells);
        }

        private static ICommand Build(Run r, World clone, ForesightIntent intent) =>
            Foresight.BuildCommand(clone, Config, r.Spells, null, null, r.Statuses, new BresenhamTargetingRule(), Sight, intent);

        // 쉬기 미리보기 = 실제로 쉬었을 때 적이 간 칸. 미리 보는 동안 원래 World는 그대로
        [Test]
        public void Wait_PredictsRealOutcome_AndLeavesWorldUntouched()
        {
            var r = Start();
            var clone = Clone(r);
            var marks = Foresight.Run(clone, Build(r, clone, ForesightIntent.Wait), new[] { r.Enemy.Id });
            Assert.That(marks.Count, Is.EqualTo(1));
            Assert.That(marks[0].From, Is.EqualTo(new GridPos(6, 2)));
            Assert.That(r.Enemy.Pos, Is.EqualTo(new GridPos(6, 2)), "원래 World는 그대로");
            Assert.That(r.World.Scheduler.CurrentTick, Is.LessThan(clone.Scheduler.CurrentTick), "복사본만 진행");

            new TurnClock(r.World.Scheduler, r.World.PlayerInput).SubmitPlayerCommand(PlayerActions.Wait(Config));
            Assert.That(r.Enemy.Pos, Is.EqualTo(marks[0].To), "미리 본 칸 = 실제 칸");
            Assert.That(marks[0].To, Is.Not.EqualTo(marks[0].From), "적이 다가옴");
        }

        // 한 걸음 미리보기: 옆 칸만, 멀리 있는 칸은 커맨드 없음
        [Test]
        public void Step_OnlyAdjacent()
        {
            var r = Start();
            var clone = Clone(r);
            Assert.That(Build(r, clone, new ForesightIntent(ForesightKind.Step, new GridPos(2, 2))), Is.Not.Null);
            Assert.That(Build(r, clone, new ForesightIntent(ForesightKind.Step, new GridPos(4, 2))), Is.Null);
            Assert.That(Build(r, clone, new ForesightIntent(ForesightKind.Cast, new GridPos(6, 2), "nope")), Is.Null, "모르는 주문");
        }

        // 주문 미리보기: 적이 그 사이에 죽으면 Dies (자리는 지금 자리)
        [Test]
        public void Cast_MarksDeath()
        {
            var r = Start();
            var clone = Clone(r);
            var command = Build(r, clone, new ForesightIntent(ForesightKind.Cast, r.Enemy.Pos, "blast"));
            Assert.That(command, Is.Not.Null);
            var marks = Foresight.Run(clone, command, new[] { r.Enemy.Id });
            Assert.That(marks[0].Dies, Is.True);
            Assert.That(marks[0].To, Is.EqualTo(marks[0].From));
            Assert.That(r.Enemy.IsAlive, Is.True, "원래 적은 살아 있음");
        }

        // 예지 주문서 = 예지 상태 → Foresight.IsActive
        [Test]
        public void Scroll_GivesForesightStatus()
        {
            var r = Start();
            var items = new ItemBook(new[] { new ItemDef("scroll_foresight", ItemUse.Scroll, effect: ConsumableEffect.Foresight, weight: 1) }, new WeaponRules(3, 2));
            var rules = new ConsumableRules(items, ConsumableConfig.Default, r.Statuses, SpellTests.BalanceTerrains(), null, Sight, new AllyAi(r.World, Config, Sight));
            Assert.That(Foresight.IsActive(r.Statuses, r.Player), Is.False);
            Inventory.TryAdd(r.World, null, items, r.Player, InventoryRef.Item("scroll_foresight"));
            int index = System.Array.IndexOf(r.Player.InventorySlots, InventoryRef.Item("scroll_foresight"));
            Assert.That(Consumables.Use(r.World, rules, r.Player, index), Is.EqualTo(ConsumeResult.Used));
            Assert.That(Foresight.IsActive(r.Statuses, r.Player), Is.True);
        }

        // 검산: 예지 상태 20틱 · 예지 주문서 효과 · 무게 1 · 값 30 (BALANCE.md 6번 · 14번)
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            Assert.That(statuses.TryGet(StatusType.Foresight, out var def) && def.DurationTicks == 20 && def.Foresees, Is.True);
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            Assert.That(items.TryGet("scroll_foresight", out var scroll), Is.True);
            Assert.That((scroll.Use, scroll.Effect, scroll.Weight, scroll.Value), Is.EqualTo((ItemUse.Scroll, ConsumableEffect.Foresight, 1, 30)));
        }
    }
}
