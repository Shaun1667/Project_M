using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>
    /// 3-51 약한 바닥 · 구덩이 (SPD Weak Floor — 결정 2026-11-18): 발판 한 줄 · 구덩이 · 돌기둥에 보물 방 목록 1개 · 방 밖 부양 물약,
    /// 부양이면 떠서 건넘 · 떨어지면 아래층 아무 빈 맨땅 + 최대 체력 12~25% 피해(죽지는 않음) · 다음 층이 보스면 방이 안 생김 (BALANCE.md 16번).
    /// </summary>
    public class ChasmTests
    {
        private static readonly ChasmConfig Cfg = new ChasmConfig(12, 25);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 8, 2, 6);

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        // 구덩이 칸: 걸을 수 없음(적 · 길 찾기) · 시야 · 주문 경로는 안 막음
        [Test]
        public void ChasmTile_Rules()
        {
            var map = GridMovementTests.Parse("#####", "#...#", "#####");
            map.Set(new GridPos(2, 1), TileType.Chasm);
            Assert.That(map.IsWalkable(new GridPos(2, 1)), Is.False);
            Assert.That(map.BlocksSight(new GridPos(2, 1)), Is.False);
            Assert.That(map.BlocksProjectile(new GridPos(2, 1)), Is.False);
            Assert.That(GridPathfinder.ChaseStep(map, new GridPos(1, 1), new GridPos(3, 1)), Is.Null, "적은 건너지 못함");
        }

        private static (World World, Unit Player, StatusBook Statuses) Small()
        {
            var world = new World(GridMovementTests.Parse("#####", "#...#", "#####"), 3UL);
            world.SetTile(new GridPos(2, 1), TileType.Chasm);
            var statuses = SpellTests.BalanceStatuses();
            world.InstallCoreSystems(Sight, Magic, statuses, SpellTests.BalanceTerrains());
            world.SetChasmRules(new ChasmRules(Cfg, f => true));
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 40, 3, 8), new GridPos(1, 1));
            world.Events.Flush();
            return (world, player, statuses);
        }

        // 부양 중이면 구덩이 칸으로 떠서 들어가고 떨어지지 않는다. 부양이 아니면 떠 가지 못한다
        [Test]
        public void Hover_OnlyWhileLevitating()
        {
            var (world, player, statuses) = Small();
            var hover = new HoverCommand(world, statuses, new GridPos(2, 1), 1);
            hover.Execute(new TickContext(0, player.Id));
            Assert.That(player.Pos, Is.EqualTo(new GridPos(1, 1)), "부양 아님");

            statuses.TryGet(StatusType.Levitating, out var lev);
            StatusEffects.Apply(world, player, lev);
            hover.Execute(new TickContext(0, player.Id));
            Assert.That(player.Pos, Is.EqualTo(new GridPos(2, 1)));
            Assert.That(Chasms.ShouldFall(world, statuses), Is.False, "떠 있음");

            StatusEffects.Remove(world, player, StatusType.Levitating);
            Assert.That(Chasms.ShouldFall(world, statuses), Is.True, "부양이 끝나면 떨어져야 함");
        }

        // 추락 피해 = 최대 체력 12 ~ 25%, 체력 1은 남긴다
        [Test]
        public void FallDamage_RangeAndNonLethal()
        {
            var (world, player, _) = Small();
            for (int i = 0; i < 30; i++)
            {
                int d = Chasms.FallDamage(world, player);
                Assert.That(d, Is.InRange(40 * 12 / 100, 40 * 25 / 100));
            }
            Combat.ApplyDamage(world, player.Id, player, 38, DamageCause.Attack); // 체력 2
            Assert.That(Chasms.FallDamage(world, player), Is.LessThanOrEqualTo(1));
        }

        // 떨어지면: 한 층 아래 · 계단이 아닌 아무 빈 맨땅 · 피해 · 이벤트. 다음 층이 마지막을 넘으면 못 떨어짐
        [Test]
        public void TryFall_LandsOnNextFloorWithDamage()
        {
            var generator = new DungeonGenerator(new DungeonGenConfig(50, 50, 10, 20, 7, 20, 10, 3, 5, 1000, 10, 3));
            DungeonFloor floor = null;
            var world = World.Create(11UL, rng => (floor = generator.Generate(rng, stairsUp: false, stairsDown: true)).Map);
            var vision = world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var idle = new IdleAi();
            var spawn = new SpawnConfig(0, 5);
            var stairs = new FloorTransition(world, generator, 3, EnemyDef, spawn, (w, id) => idle, vision);
            world.SetChasmRules(new ChasmRules(Cfg, stairs.CanFallFrom));
            var player = FloorPopulator.Populate(world, floor.Start, new UnitDef("player", 40, 3, 8), EnemyDef, spawn, idle, Magic.QuickSlotCount, Magic.InventorySlotCount);
            world.Events.Flush();
            var fell = new List<FellEvent>();
            world.Events.Subscribe<FellEvent>(fell.Add);

            Assert.That(stairs.TryFall(), Is.True);
            world.Events.Flush();
            Assert.That(world.Floor, Is.EqualTo(2));
            Assert.That(GridMap.IsGroundType(world.Map.Get(player.Pos)), Is.True, "계단이 아닌 맨땅");
            Assert.That(fell.Count, Is.EqualTo(1));
            Assert.That(player.Hp, Is.EqualTo(40 - fell[0].Damage));
            Assert.That(fell[0].Damage, Is.InRange(4, 10));

            Assert.That(stairs.TryFall(), Is.True, "2층 → 3층 (마지막 층)");
            Assert.That(stairs.CanFallFrom(3), Is.False, "마지막 층에서는 못 떨어짐");
            Assert.That(stairs.TryFall(), Is.False);
        }

        // 약한 바닥: 입구 = 문, 첫 줄(발판) = 바닥, 나머지 = 구덩이(맨 안쪽 줄 가운데 돌기둥 빼고), 돌기둥에 보물 방 목록 1개, 방 밖에 부양 물약
        [Test]
        public void Room_LedgeChasmAndPillar()
        {
            int carved = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var (world, _, room) = SpecialRoomTests.CarveFor(seed, SpecialRoomType.WeakFloor, w => w.SetChasmRules(new ChasmRules(Cfg, f => true)));
                if (room == null) continue;
                carved++;
                int size = (int)System.Math.Round(System.Math.Sqrt(room.Cells.Count));
                var pillar = room.Cells[(size - 1) * size + size / 2];
                Assert.That(world.Map.Get(room.Entrance), Is.EqualTo(TileType.Door), $"시드 {seed}");
                for (int i = 0; i < room.Cells.Count; i++)
                {
                    var c = room.Cells[i];
                    var expected = i < size || c == pillar ? TileType.Floor : TileType.Chasm;
                    Assert.That(world.Map.Get(c), Is.EqualTo(expected), $"시드 {seed} {c}");
                }
                Assert.That(world.FloorItems.ItemsAt(pillar).Count, Is.EqualTo(1), $"시드 {seed}");
                var roomSet = new HashSet<GridPos>(room.Cells);
                bool outside = false;
                for (int y = 0; y < world.Map.Height; y++)
                    for (int x = 0; x < world.Map.Width; x++)
                    {
                        var p = new GridPos(x, y);
                        if (roomSet.Contains(p)) continue;
                        foreach (var item in world.FloorItems.ItemsAt(p)) if (item.ItemId == "potion_levitation") outside = true;
                    }
                Assert.That(outside, Is.True, $"시드 {seed}: 방 밖 부양 물약");
            }
            Assert.That(carved, Is.GreaterThan(0));
        }

        // 떨어질 수 없는 층(규칙 없음 · 다음 층이 보스)에는 약한 바닥이 생기지 않는다
        [Test]
        public void Room_NotOnFloorsWithoutFall()
        {
            for (ulong seed = 1; seed <= 10; seed++)
            {
                var (world, _, room) = SpecialRoomTests.CarveFor(seed, SpecialRoomType.WeakFloor, w => w.SetChasmRules(new ChasmRules(Cfg, f => false)));
                if (room == null) continue;
                Assert.That(room.Type, Is.Not.EqualTo(SpecialRoomType.WeakFloor), $"시드 {seed}");
                foreach (var c in room.Cells) Assert.That(world.Map.Get(c), Is.Not.EqualTo(TileType.Chasm));
            }
        }

        // 검산: FloorDecorConfig 약한 바닥 · 추락 값 = BALANCE.md 16번
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset");
            var c = asset.ToChasm();
            Assert.That((c.FallDamageMinPercent, c.FallDamageMaxPercent), Is.EqualTo((12, 25)));
            Assert.That(asset.ToConfig().SpecialRooms.WeakFloorItem, Is.EqualTo("potion_levitation"));
        }
    }
}
