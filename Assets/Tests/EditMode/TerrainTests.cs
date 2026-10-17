using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    public class TerrainTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10,
            new[] { "fire_basic", "ice_basic", "lightning_basic", "poison_basic" });
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 8, 2, 6);
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        private const int Fire = 0, Poison = 3;

        private static GridMap Room() => GridMovementTests.Parse(
            "############",
            "#..........#",
            "#..........#",
            "#..........#",
            "############");

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private sealed class Session
        {
            public World World;
            public SpellBook Book;
            public TurnClock Clock;
            public Unit Player;
            public readonly List<Unit> Enemies = new List<Unit>();

            public void Wait(int turns)
            {
                for (int i = 0; i < turns; i++) Clock.SubmitPlayerCommand(PlayerActions.Wait(Config));
            }

            public void Cast(int slot, GridPos target)
            {
                var block = SpellCasting.TryCreateCast(World, Config, Book, Targeting, Sight, slot, target, out var command);
                Assert.That(block, Is.EqualTo(CastBlock.None));
                Clock.SubmitPlayerCommand(command);
            }

            public TerrainDef Def(TerrainEffectType type)
            {
                Book.Terrains.TryGet(type, out var def);
                return def;
            }

            public int Remaining(GridPos pos, TerrainEffectType type)
            {
                foreach (var t in World.Terrain.At(pos))
                    if (t.Type == type) return t.Remaining;
                return 0;
            }
        }

        private static Session Start(GridPos player, params GridPos[] enemies)
        {
            var world = new World(Room(), 3UL);
            var s = new Session { World = world, Book = SpellTests.BalanceBookFull(), Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            world.InstallCoreSystems(Sight, Magic, s.Book.Statuses, s.Book.Terrains);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, player, Magic.QuickSlotCount);
            foreach (var p in enemies) s.Enemies.Add(UnitSpawner.SpawnEnemy(world, EnemyDef, p, new IdleAi()));
            world.Events.Flush();
            return s;
        }

        // 3-1 (REACTIONS.md D2): 불 단독으로는 화염 지대가 생기지 않는다 — 불 + 물 융합으로만
        [Test]
        public void Fire_AloneLeavesNoFireZone()
        {
            var s = Start(new GridPos(1, 2), new GridPos(9, 2), new GridPos(10, 3));
            s.Cast(Fire, new GridPos(5, 2));
            Assert.That(s.World.Terrain.CellCount, Is.EqualTo(0));
        }

        // 화염 지대 위에 서 있으면 매 틱 화상이 갱신되고, 지대가 사라진 뒤에도 화상이 남은 만큼 탄다
        [Test]
        public void StandingInFireZone_KeepsBurning()
        {
            var s = Start(new GridPos(1, 2), new GridPos(9, 2));
            var enemy = s.Enemies[0];
            TerrainEffects.Place(s.World, enemy.Pos, s.Def(TerrainEffectType.FireZone));

            s.Wait(4);   // 지대 4틱: 매 틱 화상 갱신 → 피해 4
            Assert.That(enemy.Hp, Is.EqualTo(4));
            Assert.That(s.World.Terrain.CellCount, Is.EqualTo(0), "지대는 4틱 뒤 사라짐");
            Assert.That(enemy.HasStatus(StatusType.Burn), Is.True, "화상은 아직 남음");
            s.Wait(2);
            Assert.That(enemy.Hp, Is.EqualTo(2));
            Assert.That(enemy.HasStatus(StatusType.Burn), Is.False);
        }

        [Test]
        public void Poison_LeavesCloudOverBlastArea_PlayerAlsoAffected()
        {
            var s = Start(new GridPos(1, 2), new GridPos(6, 2));
            s.Cast(Poison, new GridPos(6, 2));
            int clouds = 0;
            for (int y = 1; y <= 3; y++)
                for (int x = 5; x <= 7; x++)
                    if (s.World.Terrain.Has(new GridPos(x, y), TerrainEffectType.PoisonCloud)) clouds++;
            Assert.That(clouds, Is.EqualTo(9), "3×3 전부");

            // 플레이어도 독구름 위면 중독 (규칙 10)
            TerrainEffects.Place(s.World, s.Player.Pos, s.Def(TerrainEffectType.PoisonCloud));
            s.Wait(1);
            Assert.That(s.Player.HasStatus(StatusType.Poison), Is.True);
        }

        // 결정 2026-10-16: 한 칸에 여러 효과 공존, 같은 종류는 지속 갱신
        [Test]
        public void MultipleEffectsCoexist_SameTypeRefreshes()
        {
            var s = Start(new GridPos(1, 2), new GridPos(9, 2));
            var pos = s.Enemies[0].Pos;
            TerrainEffects.Place(s.World, pos, s.Def(TerrainEffectType.PoisonCloud));
            TerrainEffects.Place(s.World, pos, s.Def(TerrainEffectType.FireZone));
            Assert.That(s.World.Terrain.At(pos).Count, Is.EqualTo(2));
            Assert.That(s.World.Terrain.At(pos)[0].Type, Is.EqualTo(TerrainEffectType.FireZone), "종류 순");

            s.Wait(2);
            Assert.That(s.Enemies[0].HasStatus(StatusType.Burn) && s.Enemies[0].HasStatus(StatusType.Poison), Is.True, "둘 다 적용");

            TerrainEffects.Place(s.World, pos, s.Def(TerrainEffectType.FireZone)); // 2 남음 → 4로
            Assert.That(s.World.Terrain.At(pos).Count, Is.EqualTo(2), "같은 종류는 하나");
            Assert.That(s.Remaining(pos, TerrainEffectType.FireZone), Is.EqualTo(4));
        }

        [Test]
        public void Place_IgnoresWalls()
        {
            var s = Start(new GridPos(1, 2));
            TerrainEffects.Place(s.World, new GridPos(0, 0), s.Def(TerrainEffectType.FireZone));
            Assert.That(s.World.Terrain.CellCount, Is.EqualTo(0));
        }

        [Test]
        public void Snapshot_RoundTripsTerrain()
        {
            var s = Start(new GridPos(1, 2), new GridPos(9, 2));
            TerrainEffects.Place(s.World, new GridPos(4, 2), s.Def(TerrainEffectType.FireZone));
            TerrainEffects.Place(s.World, new GridPos(4, 2), s.Def(TerrainEffectType.PoisonCloud));
            TerrainEffects.Place(s.World, new GridPos(2, 3), s.Def(TerrainEffectType.PoisonCloud));
            s.Wait(1);
            var bytes = WorldSnapshotCodec.Encode(s.World.Capture());
            var restored = World.Restore(WorldSnapshotCodec.Decode(bytes), (w, id) => new IdleAi());

            Assert.That(restored.Terrain.CellCount, Is.EqualTo(2));
            Assert.That(restored.Terrain.At(new GridPos(4, 2)).Count, Is.EqualTo(2));
            Assert.That(WorldSnapshotCodec.Encode(restored.Capture()), Is.EqualTo(bytes));
        }

        [Test]
        public void Defs_RejectInvalidValues()
        {
            var statuses = SpellTests.BalanceStatuses();
            Assert.Throws<ArgumentException>(() => new TerrainDef(TerrainEffectType.None, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TerrainDef(TerrainEffectType.FireZone, 0));
            Assert.Throws<ArgumentException>(() => new TerrainBook(new[]
                { new TerrainDef(TerrainEffectType.FireZone, 1), new TerrainDef(TerrainEffectType.FireZone, 2) }, statuses));
            Assert.Throws<ArgumentException>(() => new TerrainBook(new[]
                { new TerrainDef(TerrainEffectType.FireZone, 1, StatusType.Burn) }, new StatusBook(new StatusDef[0])), "StatusBook에 없는 상태");
            Assert.Throws<ArgumentException>(() => new SpellBook(new[]
                { new SpellDef("a", Element.Fire, SpellShape.Bolt, 1, 1, 1, 0, StatusType.None, 100, TerrainEffectType.FireZone) }),
                "TerrainBook에 없는 지형");
        }

        // 검산: TerrainCatalog 에셋이 BALANCE.md 7번 표와 같은 값인지
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset");
            var statusAsset = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset");
            Assert.That(asset, Is.Not.Null, "TerrainCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var book = asset.ToBook(statusAsset.ToBook());
            var expected = SpellTests.BalanceTerrains();
            Assert.That(book.Count, Is.EqualTo(expected.Count));
            foreach (var type in new[] { TerrainEffectType.FireZone, TerrainEffectType.PoisonCloud, TerrainEffectType.Water, TerrainEffectType.ContaminatedWater })
            {
                Assert.That(book.TryGet(type, out var a), Is.True, type.ToString());
                expected.TryGet(type, out var e);
                Assert.That((a.DurationTicks, a.AppliesStatus, a.DamagePerTick, a.BlockedBy), Is.EqualTo((e.DurationTicks, e.AppliesStatus, e.DamagePerTick, e.BlockedBy)), type.ToString());
            }
        }
    }
}
