using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Magic;
using ProjectM.UI;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-12b 찾기 · 숨겨진 문 (SPD — 결정 2026-11-04): 찾기 행동 · 저절로 발견 · 지도 주문서 · 숨겨진 문 배치 (BALANCE.md 16 · 17번).</summary>
    public class SearchTests
    {
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);

        /// <summary>BALANCE.md 17번 찾기 값.</summary>
        private static SearchConfig BalanceSearch() => new SearchConfig(radius: 2, costTicks: 2, passivePercent: 10, passiveRadius: 1, quickRadius: 1, quickCostTicks: 1);

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;
            public readonly List<SearchedEvent> Searched = new List<SearchedEvent>();
        }

        /// <summary>방 (1..9, 1..3). 플레이어 (2,2). passive = 저절로 발견 시스템을 넣을지.</summary>
        private static Session Start(SearchConfig passive = null)
        {
            var world = new World(GridMovementTests.Parse("###########", "#.........#", "#.........#", "#.........#", "###########"), 5UL);
            world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            if (passive != null) world.Scheduler.AddSystem(TickPhase.ManaRegen, new PassiveSearchSystem(world, passive));
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(2, 2), Magic.QuickSlotCount);
            world.Events.Subscribe<SearchedEvent>(s.Searched.Add);
            world.Events.Flush();
            return s;
        }

        // 찾기 행동: 반경 2 안은 확실히, 밖은 그대로 · 비용 2틱
        [Test]
        public void SearchAction_RevealsWithinRadius()
        {
            var s = Start();
            Traps.Place(s.World, new GridPos(4, 3), TrapType.Fire, hidden: true);   // 거리 2
            Traps.Place(s.World, new GridPos(5, 2), TrapType.Alarm, hidden: true);  // 거리 3
            long tick = s.World.Scheduler.CurrentTick;
            s.Clock.SubmitPlayerCommand(PlayerActions.Search(s.World, BalanceSearch()));
            s.World.Events.Flush();
            Assert.That(s.World.Traps.TryGet(new GridPos(4, 3), out var near) && !near.Hidden, Is.True, "반경 안");
            Assert.That(s.World.Traps.TryGet(new GridPos(5, 2), out var far) && far.Hidden, Is.True, "반경 밖");
            Assert.That(s.Searched.Count, Is.EqualTo(1));
            Assert.That(s.Searched[0].Found, Is.EqualTo(1));
            Assert.That(s.World.Scheduler.CurrentTick - tick, Is.EqualTo(2), "찾기 = 2틱");
        }

        // 3-24 빠른 찾기 (정보 모드에서 E 한 번 더): 주변 8칸만 · 1틱
        [Test]
        public void QuickSearch_RevealsAdjacentOnly()
        {
            var s = Start();
            Traps.Place(s.World, new GridPos(3, 3), TrapType.Fire, hidden: true);   // 거리 1
            Traps.Place(s.World, new GridPos(4, 2), TrapType.Alarm, hidden: true);  // 거리 2
            long tick = s.World.Scheduler.CurrentTick;
            s.Clock.SubmitPlayerCommand(PlayerActions.QuickSearch(s.World, BalanceSearch()));
            s.World.Events.Flush();
            Assert.That(s.World.Traps.TryGet(new GridPos(3, 3), out var near) && !near.Hidden, Is.True, "옆 칸");
            Assert.That(s.World.Traps.TryGet(new GridPos(4, 2), out var far) && far.Hidden, Is.True, "두 칸 떨어진 곳은 그대로");
            Assert.That(s.Searched.Count, Is.EqualTo(1));
            Assert.That(s.World.Scheduler.CurrentTick - tick, Is.EqualTo(1), "빠른 찾기 = 1틱");
        }

        // 3-24 정보 모드 글: 모르는 칸 · 바닥 · 드러난 함정 / 숨은 함정 · 숨겨진 문 = 벽 · 나
        [Test]
        public void HudText_CellInfo()
        {
            var s = Start();
            var text = new HudText();
            Assert.That(text.CellInfo(s.World, new GridPos(5, 2), explored: false, visible: false), Is.EqualTo(HudText.CellUnknown));
            Assert.That(text.CellInfo(s.World, new GridPos(5, 2), true, true), Does.Contain("바닥"));
            Traps.Place(s.World, new GridPos(4, 2), TrapType.Fire, hidden: true);
            Assert.That(text.CellInfo(s.World, new GridPos(4, 2), true, true), Does.Not.Contain(HudText.Trap(TrapType.Fire)), "숨은 함정은 안 보임");
            Traps.Reveal(s.World, new GridPos(4, 2));
            Assert.That(text.CellInfo(s.World, new GridPos(4, 2), true, true), Does.Contain(HudText.Trap(TrapType.Fire)));
            s.World.Map.Set(new GridPos(3, 3), TileType.HiddenDoor);
            Assert.That(text.CellInfo(s.World, new GridPos(3, 3), true, true), Does.Contain("벽"));
            var self = text.CellInfo(s.World, s.Player.Pos, true, true);
            Assert.That(self, Does.Contain("나").And.Contain($"HP {s.Player.Hp}/{s.Player.MaxHp}"));
            Assert.That(text.CellInfo(s.World, s.Player.Pos, true, visible: false), Does.Not.Contain("HP"), "안 보이는 칸의 유닛은 숨김");
        }

        // 숨겨진 문: 벽처럼 못 지나가고 시야를 막다가, 찾으면 닫힌 문
        [Test]
        public void HiddenDoor_IsWallUntilFound()
        {
            var s = Start();
            var door = new GridPos(3, 3);
            s.World.Map.Set(door, TileType.HiddenDoor);
            Assert.That(s.World.Map.IsWalkable(door), Is.False);
            Assert.That(s.World.Map.BlocksSight(door), Is.True);
            Assert.That(GridMap.IsWallLikeType(TileType.HiddenDoor), Is.True, "벽으로 그림");
            var found = new List<HiddenDoorFoundEvent>();
            s.World.Events.Subscribe<HiddenDoorFoundEvent>(found.Add);
            Searching.Search(s.World, s.Player.Id, s.Player.Pos, 2);
            s.World.Events.Flush();
            Assert.That(s.World.Map.Get(door), Is.EqualTo(TileType.Door));
            Assert.That(found.Count, Is.EqualTo(1));
        }

        // 저절로 발견: 옆 칸만, 확률 100%면 다음 틱에 찾음 / 0%면 못 찾음
        [Test]
        public void PassiveSearch_RevealsAdjacentByChance()
        {
            var sure = Start(new SearchConfig(2, 2, passivePercent: 100, passiveRadius: 1));
            Traps.Place(sure.World, new GridPos(3, 2), TrapType.Flood, hidden: true);
            Traps.Place(sure.World, new GridPos(4, 2), TrapType.Flood, hidden: true);
            sure.Clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
            sure.World.Events.Flush();
            Assert.That(sure.World.Traps.TryGet(new GridPos(3, 2), out var a) && !a.Hidden, Is.True, "옆 칸");
            Assert.That(sure.World.Traps.TryGet(new GridPos(4, 2), out var b) && b.Hidden, Is.True, "두 칸 밖");

            var never = Start(new SearchConfig(2, 2, passivePercent: 0, passiveRadius: 1));
            Traps.Place(never.World, new GridPos(3, 2), TrapType.Flood, hidden: true);
            for (int i = 0; i < 20; i++) never.Clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
            Assert.That(never.World.Traps.TryGet(new GridPos(3, 2), out var c) && c.Hidden, Is.True);
        }

        // 저절로 발견은 Search 난수만 — 같은 시드면 같은 턴에 찾는다
        [Test]
        public void PassiveSearch_Deterministic()
        {
            int FoundTick()
            {
                var s = Start(BalanceSearch());
                Traps.Place(s.World, new GridPos(3, 3), TrapType.Fire, hidden: true);
                for (int i = 1; i <= 200; i++)
                {
                    s.Clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
                    if (s.World.Traps.TryGet(new GridPos(3, 3), out var t) && !t.Hidden) return i;
                }
                return -1;
            }
            int first = FoundTick();
            Assert.That(first, Is.GreaterThan(0), "10%면 200턴 안에는 찾는다");
            Assert.That(FoundTick(), Is.EqualTo(first));
        }

        [Test]
        public void RevealAll_RevealsEverything()
        {
            var s = Start();
            Traps.Place(s.World, new GridPos(8, 1), TrapType.Shock, hidden: true);
            s.World.Map.Set(new GridPos(10, 2), TileType.HiddenDoor);
            Searching.RevealAll(s.World);
            Assert.That(s.World.Traps.TryGet(new GridPos(8, 1), out var t) && !t.Hidden, Is.True);
            Assert.That(s.World.Map.Get(new GridPos(10, 2)), Is.EqualTo(TileType.Door));
        }

        // 생성: 숨겨진 문은 막아도 층이 끊기지 않는 자리에만 (100%로 놓아도 걸을 수 있는 칸은 모두 이어짐)
        [Test]
        public void HiddenDoors_NeverDisconnectFloor()
        {
            var gen = new DungeonGenConfig(width: 50, height: 50, roomCount: 10, roomSizeRoll: 20, roomMinSize: 7,
                wallBlockCount: 20, wallBlockSizeRoll: 10, wallBlockMinSize: 3, wallTunnelCostMultiplier: 5, placementAttemptLimit: 1000,
                deadEndLoopMinWalkDistance: 10);
            int hidden = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                DungeonFloor floor = null;
                var world = World.Create(seed, rng => (floor = new DungeonGenerator(gen).Generate(rng)).Map);
                FloorDecor.Apply(world, floor, new FloorDecorConfig(0, 1, 1, 0, 0, 1, 1, doorPercent: 100, hiddenDoorPercent: 100), null);
                var reached = DungeonGenerator.FloodWalkable(world.Map, floor.Start);
                for (int i = 0; i < reached.Length; i++)
                {
                    var p = new GridPos(i % world.Map.Width, i / world.Map.Width);
                    if (world.Map.IsWalkable(p)) Assert.That(reached[i], Is.True, $"시드 {seed} {p}: 끊긴 칸");
                    if (world.Map.Get(p) == TileType.HiddenDoor) hidden++;
                }
            }
            Assert.That(hidden, Is.GreaterThan(0), "우회로가 있는 자리에는 숨겨진 문이 생긴다");
        }

        [Test]
        public void HudText_SearchLines()
        {
            Assert.That(HudText.SearchFoundNothing, Is.EqualTo("주변을 살폈지만 아무것도 없다"));
            Assert.That(HudText.HiddenDoorFound, Is.EqualTo("숨겨진 문을 찾았다"));
        }

        // 검산: TrapCatalog의 찾기 값 = BALANCE.md 17번, FloorDecorConfig의 숨겨진 문 = 16번
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var traps = AssetDatabase.LoadAssetAtPath<TrapCatalogAsset>("Assets/_Project/Data/Dungeon/TrapCatalog.asset");
            Assert.That(traps, Is.Not.Null);
            var c = traps.ToSearch();
            var e = BalanceSearch();
            Assert.That((c.Radius, c.CostTicks, c.PassivePercent, c.PassiveRadius), Is.EqualTo((e.Radius, e.CostTicks, e.PassivePercent, e.PassiveRadius)));
            Assert.That((c.QuickRadius, c.QuickCostTicks), Is.EqualTo((e.QuickRadius, e.QuickCostTicks)), "3-24 빠른 찾기");
            var decor = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToConfig();
            Assert.That(decor.HiddenDoorPercent, Is.EqualTo(15));
        }
    }
}
