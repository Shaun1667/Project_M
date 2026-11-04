using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.UI;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-12a 함정 7종 (SPD — 결정 2026-11-04): 밟으면 발동(적도) · 숨김 · 배치 · 저장 (BALANCE.md 17번).</summary>
    public class TrapTests
    {
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);

        /// <summary>SpellCatalog의 함정 효과 주문 수 (SpellTests 검산이 쓴다).</summary>
        internal const int TrapSpellCount = 5;

        /// <summary>BALANCE.md 17번 함정 효과 주문 — 3×3, 마나 0.</summary>
        internal static SpellDef[] BalanceTrapSpells() => new[]
        {
            new SpellDef("trap_fire", Element.Fire, SpellShape.Burst, 1, 2, 0, 1, StatusType.Burn, 100, TerrainEffectType.FireZone),
            new SpellDef("trap_toxic", Element.Poison, SpellShape.Burst, 1, 0, 0, 1, StatusType.Poison, 100, TerrainEffectType.PoisonCloud),
            new SpellDef("trap_paralytic", Element.Wind, SpellShape.Burst, 1, 0, 0, 1, StatusType.None, 0, TerrainEffectType.ParalyticGas),
            new SpellDef("trap_flood", Element.Water, SpellShape.Burst, 1, 0, 0, 1, StatusType.Wet, 100, TerrainEffectType.Water),
            new SpellDef("trap_shock", Element.Lightning, SpellShape.Burst, 1, 3, 0, 1, StatusType.Paralyze, 30),
        };

        /// <summary>BALANCE.md 17번 함정 표 · 배치 수치.</summary>
        internal static TrapBook BalanceTraps(SpellBook spells) => new TrapBook(new[]
        {
            new TrapDef(TrapType.Fire, "trap_fire", TrapSpecial.None, 2),
            new TrapDef(TrapType.ToxicGas, "trap_toxic", TrapSpecial.None, 2),
            new TrapDef(TrapType.ParalyticGas, "trap_paralytic", TrapSpecial.None, 1),
            new TrapDef(TrapType.Flood, "trap_flood", TrapSpecial.None, 2),
            new TrapDef(TrapType.Shock, "trap_shock", TrapSpecial.None, 2),
            new TrapDef(TrapType.Teleport, null, TrapSpecial.Teleport, 1),
            new TrapDef(TrapType.Alarm, null, TrapSpecial.Alarm, 1),
        }, spells, trapsPerFloor: 4, hiddenPercent: 85, minDistanceFromStart: 3);

        private sealed class Run
        {
            public ReactionTests.Session S;
            public TrapBook Traps;
            public readonly List<TrapTriggeredEvent> Triggered = new List<TrapTriggeredEvent>();
            public readonly List<TrapChangedEvent> Changed = new List<TrapChangedEvent>();

            public void Step(int dx, int dy)
            {
                Assert.That(PlayerActions.TryStep(S.World, StepConfig, new GridPos(dx, dy), out var step), Is.True);
                S.Clock.SubmitPlayerCommand(step);
                S.World.Events.Flush();
            }

            public void Wait()
            {
                S.Clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
                S.World.Events.Flush();
            }
        }

        private static Run Start(GridPos player, params GridPos[] enemies)
        {
            var book = ReactionTests.Book(BalanceTrapSpells());
            var s = ReactionTests.Start(book, player, enemies);
            var run = new Run { S = s, Traps = BalanceTraps(book) };
            s.World.Scheduler.AddSystem(TickPhase.ManaRegen, new TrapSystem(s.World, run.Traps, book));
            s.World.Events.Subscribe<TrapTriggeredEvent>(run.Triggered.Add);
            s.World.Events.Subscribe<TrapChangedEvent>(run.Changed.Add);
            return run;
        }

        // 숨겨진 화염 함정을 밟으면: 함정 사라짐 · 3×3 불 2 + 화상 + 화염 지대
        [Test]
        public void HiddenFireTrap_TriggersOnStep_AndIsConsumed()
        {
            var r = Start(new GridPos(3, 2));
            Traps.Place(r.S.World, new GridPos(4, 2), TrapType.Fire, hidden: true);
            r.S.World.Events.Flush();
            Assert.That(r.Changed, Is.Empty, "숨겨진 함정은 화면에 알리지 않는다");

            r.Step(1, 0);
            Assert.That(r.Triggered.Count, Is.EqualTo(1));
            Assert.That(r.Triggered[0].Unit, Is.EqualTo(r.S.Player.Id));
            Assert.That(r.S.World.Traps.Has(new GridPos(4, 2)), Is.False, "한 번 발동하면 사라진다");
            Assert.That(r.S.Hits.Exists(h => h.Target == r.S.Player.Id && h.Amount == 2), Is.True, "불 2 (시전자 없음)");
            Assert.That(r.S.Player.HasStatus(StatusType.Burn), Is.True);
            Assert.That(r.S.World.Terrain.Has(new GridPos(5, 3), TerrainEffectType.FireZone), Is.True, "3×3 화염 지대");

            r.Wait();
            Assert.That(r.Triggered.Count, Is.EqualTo(1), "다시 발동하지 않는다");
        }

        // 적도 밟으면 발동 (결정 2026-11-04)
        [Test]
        public void EnemyTriggersTrap()
        {
            var r = Start(new GridPos(1, 2), new GridPos(7, 2));
            Traps.Place(r.S.World, new GridPos(7, 2), TrapType.ToxicGas, hidden: true);
            r.Wait();
            Assert.That(r.Triggered.Count, Is.EqualTo(1));
            Assert.That(r.Triggered[0].Unit, Is.EqualTo(r.S.Enemies[0].Id));
            Assert.That(r.S.Enemies[0].HasStatus(StatusType.Poison), Is.True);
            Assert.That(r.S.Player.HasStatus(StatusType.Poison), Is.False, "3×3 밖");
        }

        // 함정도 반응을 일으킨다: 젖은 유닛이 번개 함정을 밟으면 감전
        [Test]
        public void ShockTrap_OnWetTargets_Shocks()
        {
            var r = Start(new GridPos(3, 2), new GridPos(5, 2));
            r.S.Book.Statuses.TryGet(StatusType.Wet, out var wet);
            StatusEffects.Apply(r.S.World, r.S.Player, wet);
            StatusEffects.Apply(r.S.World, r.S.Enemies[0], wet);
            Traps.Place(r.S.World, new GridPos(4, 2), TrapType.Shock, hidden: false);
            r.S.Reactions.Clear();
            r.Step(1, 0);
            Assert.That(r.S.Reactions, Does.Contain("shock"));
        }

        [Test]
        public void TeleportTrap_MovesUnit()
        {
            var r = Start(new GridPos(3, 2));
            Traps.Place(r.S.World, new GridPos(4, 2), TrapType.Teleport, hidden: true);
            r.Step(1, 0);
            Assert.That(r.S.Player.Pos, Is.Not.EqualTo(new GridPos(4, 2)));
            Assert.That(r.S.World.Map.IsWalkable(r.S.Player.Pos), Is.True);
        }

        [Test]
        public void AlarmTrap_AlertsEnemiesToTrapCell()
        {
            var r = Start(new GridPos(3, 2), new GridPos(10, 3));
            Traps.Place(r.S.World, new GridPos(4, 2), TrapType.Alarm, hidden: true);
            r.Step(1, 0);
            Assert.That(r.S.Enemies[0].LastSeenTargetPos, Is.EqualTo(new GridPos(4, 2)));
        }

        // 드러내기: 숨겨진 함정만, 한 번
        [Test]
        public void Reveal_PublishesOnce()
        {
            var r = Start(new GridPos(1, 2));
            Traps.Place(r.S.World, new GridPos(6, 2), TrapType.Flood, hidden: true);
            Assert.That(Traps.Reveal(r.S.World, new GridPos(6, 2)), Is.True);
            Assert.That(Traps.Reveal(r.S.World, new GridPos(6, 2)), Is.False, "이미 드러남");
            Assert.That(Traps.Reveal(r.S.World, new GridPos(7, 2)), Is.False, "함정 없음");
            r.S.World.Events.Flush();
            Assert.That(r.Changed.Count, Is.EqualTo(1));
            Assert.That(r.Changed[0].Visible, Is.True);
        }

        // 층 배치: 같은 시드 같은 결과, 개수 · 시작 칸 거리 · 지형 없는 맨땅
        [Test]
        public void PlaceForFloor_Deterministic_AndRespectsRules()
        {
            var book = ReactionTests.Book(BalanceTrapSpells());
            var traps = BalanceTraps(book);
            List<TrapCellState> Place(ulong seed)
            {
                var world = new World(GridMovementTests.Parse(
                    "############", "#..........#", "#..........#", "#..........#", "############"), seed);
                Traps.PlaceForFloor(world, new GridPos(1, 1), traps, world.Rng.Get(RngStream.Decor));
                var list = new List<TrapCellState>();
                foreach (var p in world.Traps.ToSortedList())
                {
                    var pos = world.Traps.PosOf(p.Key);
                    Assert.That(GridMovement.Chebyshev(pos, new GridPos(1, 1)), Is.GreaterThanOrEqualTo(3));
                    Assert.That(GridMap.IsGroundType(world.Map.Get(pos)), Is.True);
                    list.Add(new TrapCellState(p.Key, p.Value.Type, p.Value.Hidden));
                }
                return list;
            }
            var a = Place(9UL);
            Assert.That(a.Count, Is.EqualTo(4));
            Assert.That(Place(9UL), Is.EqualTo(a));
        }

        // 적 배치는 함정 칸을 피한다
        [Test]
        public void EnemyCells_ExcludeTraps()
        {
            var map = GridMovementTests.Parse("#######", "#.....#", "#######");
            var cells = FloorPopulator.PickEnemyCells(map, new GridPos(1, 1), 10, 0, new RngSet(3).Get(RngStream.MapGen), p => p == new GridPos(3, 1));
            Assert.That(cells, Has.No.Member(new GridPos(3, 1)));
            Assert.That(cells.Count, Is.EqualTo(4));
        }

        // 스냅샷 v18: 지금 층 · 떠난 층의 함정이 숨김 여부까지 왕복
        [Test]
        public void Snapshot_RoundTripsTraps()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(18), "3-12a 함정");
            var r = Start(new GridPos(1, 2));
            Traps.Place(r.S.World, new GridPos(6, 2), TrapType.Fire, hidden: true);
            Traps.Place(r.S.World, new GridPos(8, 3), TrapType.Alarm, hidden: false);
            r.S.World.Events.Flush();
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(r.S.World.Capture())), (w, id) => null);
            Assert.That(restored.Traps.TryGet(new GridPos(6, 2), out var a), Is.True);
            Assert.That((a.Type, a.Hidden), Is.EqualTo((TrapType.Fire, true)));
            Assert.That(restored.Traps.TryGet(new GridPos(8, 3), out var b), Is.True);
            Assert.That((b.Type, b.Hidden), Is.EqualTo((TrapType.Alarm, false)));

            r.S.World.StoreCurrentFloor(r.S.Player.Id);
            var stored = WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(r.S.World.Capture())).StoredFloors[0];
            Assert.That(stored.Traps.Length, Is.EqualTo(2), "떠난 층에 함께 저장");
        }

        [Test]
        public void HudText_TrapLines()
        {
            Assert.That(HudText.TrapTriggered("나", TrapType.Fire), Is.EqualTo("나: 화염 함정 발동!"));
            Assert.That(HudText.TrapFound(TrapType.Alarm), Is.EqualTo("함정 발견: 경보 함정"));
        }

        // 검산: TrapCatalog · SpellCatalog의 함정 줄 = BALANCE.md 17번
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            foreach (var e in BalanceTrapSpells())
            {
                Assert.That(spells.TryGet(e.Id, out var a), Is.True, e.Id);
                Assert.That((a.Element, a.Shape, a.Range, a.Damage, a.ManaCost, a.Radius, a.AppliesStatus, a.LeavesTerrain),
                    Is.EqualTo((e.Element, e.Shape, e.Range, e.Damage, e.ManaCost, e.Radius, e.AppliesStatus, e.LeavesTerrain)), e.Id);
                if (e.AppliesStatus != StatusType.None) Assert.That(a.StatusChancePercent, Is.EqualTo(e.StatusChancePercent), e.Id);
            }

            var asset = AssetDatabase.LoadAssetAtPath<TrapCatalogAsset>("Assets/_Project/Data/Dungeon/TrapCatalog.asset");
            Assert.That(asset, Is.Not.Null, "TrapCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var book = asset.ToBook(spells);
            var expected = BalanceTraps(spells);
            Assert.That((book.TrapsPerFloor, book.HiddenPercent, book.MinDistanceFromStart),
                Is.EqualTo((expected.TrapsPerFloor, expected.HiddenPercent, expected.MinDistanceFromStart)));
            Assert.That(book.All.Count, Is.EqualTo(expected.All.Count));
            foreach (var e in expected.All)
            {
                Assert.That(book.TryGet(e.Type, out var a), Is.True, e.Type.ToString());
                Assert.That((a.SpellId, a.Special, a.Weight), Is.EqualTo((e.SpellId, e.Special, e.Weight)), e.Type.ToString());
            }
        }
    }
}
