using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Magic;
using ProjectM.Units;
using UnityEditor;
using EntityId = ProjectM.Core.EntityId;

namespace ProjectM.Tests
{
    /// <summary>3-17a 구역 (결정 2026-11-04 — 5구역 · 25층, SPD): 구역 표 · 세기 · 적 종류 뽑기 · 중간 보스 층(봉인된 계단) · 마지막 보스 = 승리 (BALANCE.md 20번).</summary>
    public class ZoneTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8);
        private static readonly UnitDef Rat = new UnitDef("rat", 8, 2, 6, accuracy: 8, evasion: 2);
        private static readonly UnitDef Bunny = new UnitDef("bunny", 6, 1, 6);
        private static readonly UnitDef BossDef = new UnitDef("boss", 60, 5, 10);
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        // 2구역 4층: 1~2층(보스 2층) · 3~4층(세기 200%, 보스 4층)
        private static ZoneBook Book() => new ZoneBook(new[]
        {
            new ZoneDef("a", 1, 2, new[] { new ZoneEnemy(Rat, 3), new ZoneEnemy(Bunny, 1) }, 100, BossDef),
            new ZoneDef("b", 3, 4, new[] { new ZoneEnemy(Rat, 1) }, 200, BossDef),
        });

        [Test]
        public void Book_ZonesBossFloorsAndScaling()
        {
            var book = Book();
            Assert.That(book.FloorCount, Is.EqualTo(4));
            Assert.That(book.ZoneOf(1).Name, Is.EqualTo("a"));
            Assert.That(book.ZoneOf(3).Name, Is.EqualTo("b"));
            Assert.That(book.IsBossFloor(2) && book.IsBossFloor(4) && !book.IsBossFloor(1) && !book.IsBossFloor(3), Is.True);
            var strong = book.ZoneOf(3).Enemies[0].Def;
            Assert.That((strong.MaxHp, strong.AttackDamage, strong.Accuracy, strong.Evasion), Is.EqualTo((16, 4, 16, 4)), "200%");
            Assert.That(book.BossOf(4).MaxHp, Is.EqualTo(120));
            Assert.Throws<System.ArgumentException>(() => new ZoneBook(new[] { new ZoneDef("x", 2, 3, new[] { new ZoneEnemy(Rat, 1) }) }), "1층부터");
        }

        // 무게대로 뽑힌다 (쥐 3 : 토끼 1) — 같은 시드 = 같은 결과
        [Test]
        public void PickEnemy_FollowsWeights_Deterministic()
        {
            var book = Book();
            var first = new List<string>();
            int rats = 0;
            var rng = new RngSet(7UL).Get(RngStream.EnemyKind);
            for (int i = 0; i < 400; i++)
            {
                var d = book.PickEnemy(1, rng);
                if (d.VisualId == "rat") rats++;
                if (i < 10) first.Add(d.VisualId);
            }
            Assert.That(rats, Is.InRange(260, 340));
            var again = new RngSet(7UL).Get(RngStream.EnemyKind);
            for (int i = 0; i < 10; i++) Assert.That(book.PickEnemy(1, again).VisualId, Is.EqualTo(first[i]));
        }

        private static (World World, FloorTransition Stairs, Unit Player, List<RunEndedEvent> Ends) Start()
        {
            var dungeon = new DungeonGenConfig(50, 50, 10, 20, 7, 20, 10, 3, 5, 1000, 10, floorCount: 4);
            var generator = new DungeonGenerator(dungeon);
            DungeonFloor floor = null;
            var world = World.Create(11UL, rng => (floor = generator.Generate(rng, stairsUp: false, stairsDown: true)).Map);
            var vision = world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());
            var spawn = new SpawnConfig(5, 8);
            var idle = new IdleAi();
            var stairs = new FloorTransition(world, generator, 4, Rat, spawn, (w, id) => idle, vision, zones: Book());
            var ends = new List<RunEndedEvent>();
            world.Events.Subscribe<RunEndedEvent>(ends.Add);
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, floor.Start, Magic.QuickSlotCount);
            vision.Refresh();
            world.Events.Flush();
            world.Units.Teleport(player.Id, floor.Exit);
            Assert.That(stairs.TryUse(), Is.True, "1 → 2층");
            world.Events.Flush();
            return (world, stairs, player, ends);
        }

        private static void KillBoss(World world)
        {
            Assert.That(world.Units.TryGet(world.Run.BossId, out var boss), Is.True);
            Combat.ApplyDamage(world, EntityId.None, boss, 9999);
            new TurnClock(world.Scheduler, world.PlayerInput).SubmitPlayerCommand(PlayerActions.Wait(Config));
            world.Events.Flush();
        }

        // 중간 보스 층: 봉인된 계단(벽) → 보스가 죽으면 내려가는 계단, 승리 아님
        [Test]
        public void MidBoss_UnsealsStairs_NotVictory()
        {
            var (world, _, _, ends) = Start();
            Assert.That(world.Floor, Is.EqualTo(2));
            var sealedPos = world.Map.Find(TileType.SealedStairs);
            Assert.That(sealedPos.HasValue, Is.True);
            Assert.That(world.Map.IsWalkable(sealedPos.Value), Is.False);
            Assert.That(world.Map.Find(TileType.StairsDown).HasValue, Is.False);
            var unsealed = new List<StairsUnsealedEvent>();
            world.Events.Subscribe<StairsUnsealedEvent>(unsealed.Add);
            KillBoss(world);
            Assert.That(world.Map.Get(sealedPos.Value), Is.EqualTo(TileType.StairsDown));
            Assert.That(unsealed.Count, Is.EqualTo(1));
            Assert.That(ends, Is.Empty, "승리 아님");
            Assert.That(world.Run.Outcome, Is.EqualTo(RunOutcome.None));
        }

        // 열린 계단으로 3층(새 구역, 200% 적) → 4층 마지막 보스 = 승리
        [Test]
        public void LastBoss_IsVictory_AndZoneEnemiesScaled()
        {
            var (world, stairs, player, ends) = Start();
            KillBoss(world);
            world.Units.Teleport(player.Id, world.Map.Find(TileType.StairsDown).Value);
            Assert.That(stairs.TryUse(), Is.True, "2 → 3층");
            world.Events.Flush();
            int enemies = 0;
            foreach (var u in world.Units.ToSortedList())
                if (u.Faction == Faction.Enemy) { enemies++; Assert.That(u.MaxHp, Is.EqualTo(16), "3층 = 구역 b · 200%"); }
            Assert.That(enemies, Is.GreaterThan(0));
            world.Units.Teleport(player.Id, world.Map.Find(TileType.StairsDown).Value);
            Assert.That(stairs.TryUse(), Is.True, "3 → 4층");
            world.Events.Flush();
            Assert.That(world.Map.Find(TileType.SealedStairs).HasValue, Is.False, "마지막 보스 방엔 봉인 계단 없음");
            KillBoss(world);
            Assert.That(ends.Count, Is.EqualTo(1));
            Assert.That(ends[0].Outcome, Is.EqualTo(RunOutcome.Victory));
        }

        // 검산: ZoneCatalog = BALANCE.md 20번 — 5구역 · 25층, DungeonGenConfig 층 수와 같음, 보스 층 5 · 10 · 25
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            var book = catalog.ToBook();
            var dungeon = AssetDatabase.LoadAssetAtPath<DungeonGenConfigAsset>("Assets/_Project/Data/Dungeon/DungeonGenConfig.asset").ToConfig();
            Assert.That(book.Zones.Count, Is.EqualTo(5));
            Assert.That(book.FloorCount, Is.EqualTo(25));
            Assert.That(dungeon.FloorCount, Is.EqualTo(book.FloorCount));
            var bossFloors = new List<int>();
            for (int f = 1; f <= 25; f++) if (book.IsBossFloor(f)) bossFloors.Add(f);
            Assert.That(bossFloors, Is.EqualTo(new[] { 5, 10, 15, 20, 25 }), "3-18: 15층 얼음 마녀 · 3-19: 20층 용암 군주");
            var percents = new List<int>();
            foreach (var z in book.Zones) percents.Add(z.StatPercent);
            Assert.That(percents, Is.EqualTo(new[] { 100, 100, 100, 100, 200 }), "3-17c · 3-18 · 3-19: 1~4구역은 적 줄이 세기를 가짐, 5구역(임시)은 사막 적 × %");
            // 3-17c: 2 · 5구역 = 사막 4종 (전갈 3 · 자칼 2 · 미라 2 · 사막 주술사 1)
            foreach (int floor in new[] { 6, 21 })
            {
                var ids = new List<string>();
                foreach (var e in book.ZoneOf(floor).Enemies) ids.Add(e.Def.VisualId + ":" + e.Weight);
                Assert.That(ids, Is.EqualTo(new[] { "enemy_scorpion:3", "enemy_jackal:2", "enemy_mummy:2", "enemy_desert_shaman:1" }), $"{floor}층");
            }
            var traits = catalog.ToEnemyBook();
            Assert.That(traits.TryGet("enemy_scorpion", out var scorpion) && scorpion.HitStatus == ProjectM.Status.StatusType.Poison && scorpion.HitStatusPercent == 40, Is.True);
            Assert.That(traits.TryGet("enemy_jackal", out var jackal) && jackal.MoveSteps == 2, Is.True);
            Assert.That(traits.TryGet("enemy_mummy", out var mummy) && mummy.MoveCostTicks == 2
                && mummy.DamagePercent(ProjectM.Magic.Element.Fire) == 150 && mummy.DamagePercent(ProjectM.Magic.Element.Poison) == 50, Is.True);
            Assert.That(traits.TryGet("enemy_desert_shaman", out var shaman) && shaman.SpellId == "enemy_sand_bolt" && shaman.KeepDistance == 3, Is.True);
            Assert.That(catalog.TryGetEnemy("enemy_mummy", out var m) && (m.MaxHp, m.AttackDamage, m.Accuracy, m.Evasion) == (22, 4, 10, 1), Is.True);
            Assert.That(catalog.TryGetLightColor(7, out var sand) && sand.b < sand.r, Is.True, "사막 = 모래빛 해");
            Assert.That(catalog.LootPlanFloors, Is.EqualTo(10));
            foreach (var z in new[] { 1, 6, 21 }) Assert.That(book.ZoneOf(z).PuddleTerrain, Is.EqualTo(ProjectM.Status.TerrainEffectType.Water), $"{z}층 웅덩이 = 물");
            foreach (var z in new[] { 1, 6, 11, 21 }) Assert.That(book.ZoneOf(z).Grass, Is.True, $"{z}층 풀 있음");
        }

        // 검산 (3-19): 4구역 용암 동굴 = BALANCE 20-1 · 20-2 — 적 4종 · 행동 · 용암 군주 · 용암 웅덩이 · 풀 없음 · 붉은 해
        [Test]
        public void ProjectAsset_LavaCave_MatchesBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            var book = catalog.ToBook();
            var zone = book.ZoneOf(16);
            Assert.That((zone.Name, zone.FirstFloor, zone.LastFloor, zone.StatPercent), Is.EqualTo(("용암 동굴", 16, 20, 100)));
            var ids = new List<string>();
            foreach (var e in zone.Enemies) ids.Add(e.Def.VisualId + ":" + e.Weight);
            Assert.That(ids, Is.EqualTo(new[] { "enemy_fire_salamander:3", "enemy_flame_imp:2", "enemy_magma_golem:2", "enemy_fire_shaman:1" }));
            Assert.That(zone.PuddleTerrain, Is.EqualTo(ProjectM.Status.TerrainEffectType.FireZone));
            Assert.That(zone.Grass, Is.False);
            Assert.That(book.BossOf(20)?.VisualId, Is.EqualTo("boss_lava_lord"));
            Assert.That(catalog.TryGetLightColor(18, out var lava) && lava.r > lava.b, Is.True, "용암 동굴 = 붉은 해");

            (int, int, int, int, int, int, int, int) Stats(string id)
            {
                Assert.That(catalog.TryGetEnemy(id, out var e), Is.True, id);
                return (e.MaxHp, e.AttackDamage, e.SightRange, e.Accuracy, e.Evasion, e.ExpReward, e.ExpMaxLevel, e.MaxMana);
            }
            Assert.That(Stats("enemy_fire_salamander"), Is.EqualTo((18, 5, 7, 18, 7, 7, 20, 0)));
            Assert.That(Stats("enemy_flame_imp"), Is.EqualTo((12, 4, 8, 18, 11, 7, 20, 0)));
            Assert.That(Stats("enemy_magma_golem"), Is.EqualTo((40, 6, 5, 14, 2, 9, 21, 0)));
            Assert.That(Stats("enemy_fire_shaman"), Is.EqualTo((18, 4, 8, 16, 8, 9, 21, 9)));
            Assert.That(Stats("boss_lava_lord"), Is.EqualTo((140, 6, 10, 18, 12, 40, 30, 0)));

            var traits = catalog.ToEnemyBook();
            Assert.That(traits.TryGet("enemy_fire_salamander", out var sal) && sal.HitStatus == ProjectM.Status.StatusType.Burn && sal.HitStatusPercent == 40, Is.True);
            Assert.That(traits.TryGet("enemy_flame_imp", out var imp) && imp.MoveSteps == 2, Is.True);
            Assert.That(traits.TryGet("enemy_magma_golem", out var golem) && golem.MoveCostTicks == 2 && golem.HurtTerrain == ProjectM.Status.TerrainEffectType.FireZone
                && golem.DamagePercent(Element.Fire) == 0 && golem.DamagePercent(Element.Water) == 150 && golem.DamagePercent(Element.Ice) == 150, Is.True);
            Assert.That(traits.TryGet("enemy_fire_shaman", out var shaman) && shaman.SpellId == "enemy_fireball" && shaman.KeepDistance == 3, Is.True);
            Assert.That(traits.TryGet("boss_lava_lord", out var lord) && string.IsNullOrEmpty(lord.SpellId) && lord.HurtTerrain == ProjectM.Status.TerrainEffectType.FireZone
                && lord.ChargeSpellId == "boss_lava_eruption" && lord.ChargePercent == 35 && lord.TeleportPercent == 0
                && lord.SummonDef?.VisualId == "enemy_magma_golem" && lord.SummonCount == 2 && lord.SummonAtHpPercent == 50
                && lord.DamagePercent(Element.Fire) == 0 && lord.DamagePercent(Element.Ice) == 150, Is.True);

            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            Assert.That(spells.TryGet("enemy_fireball", out var ball), Is.True);
            Assert.That((ball.Element, ball.Shape, ball.Range, ball.Damage, ball.ManaCost, ball.AppliesStatus, ball.StatusChancePercent),
                Is.EqualTo((Element.Fire, SpellShape.Bolt, 6, 5, 3, ProjectM.Status.StatusType.Burn, 40)));
            Assert.That(spells.TryGet("boss_lava_eruption", out var erupt), Is.True);
            Assert.That((erupt.Element, erupt.Shape, erupt.Range, erupt.Damage, erupt.ManaCost, erupt.Radius, erupt.AppliesStatus, erupt.StatusChancePercent, erupt.LeavesTerrain),
                Is.EqualTo((Element.Fire, SpellShape.Burst, 6, 9, 0, 1, ProjectM.Status.StatusType.Burn, 70, ProjectM.Status.TerrainEffectType.FireZone)));
        }

        // 3-19: 용암 구역 새 층 = 영구 화염 지대 웅덩이, 풀 칸 없음 (풀 덩어리 6개를 깔도록 한 꾸미기여도)
        [Test]
        public void LavaCaveZone_LavaPuddles_NoGrass()
        {
            var dungeon = new DungeonGenConfig(50, 50, 10, 20, 7, 20, 10, 3, 5, 1000, 10, floorCount: 3);
            var generator = new DungeonGenerator(dungeon);
            DungeonFloor floor = null;
            var world = World.Create(21UL, rng => (floor = generator.Generate(rng, stairsUp: false, stairsDown: true)).Map);
            var terrains = SpellTests.BalanceTerrains();
            var vision = world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), terrains);
            var zones = new ZoneBook(new[]
            {
                new ZoneDef("숲", 1, 1, new[] { new ZoneEnemy(Rat, 1) }),
                new ZoneDef("용암", 2, 3, new[] { new ZoneEnemy(Rat, 1) }, puddleTerrain: ProjectM.Status.TerrainEffectType.FireZone, grass: false),
            });
            terrains.TryGet(ProjectM.Status.TerrainEffectType.Water, out var water);
            var decor = new FloorDecorConfig(6, 4, 14, 50, 6, 3, 9);
            var idle = new IdleAi();
            var stairs = new FloorTransition(world, generator, 3, Rat, new SpawnConfig(0, 8), (w, id) => idle, vision,
                decor: decor, water: water, zones: zones, terrains: terrains);
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, floor.Start, Magic.QuickSlotCount);
            vision.Refresh();
            world.Events.Flush();
            world.Units.Teleport(player.Id, floor.Exit);
            Assert.That(stairs.TryUse(), Is.True, "1 → 2층 (용암 구역)");
            world.Events.Flush();

            int lava = 0;
            foreach (var pair in world.Terrain.ToSortedList())
                foreach (var t in pair.Value)
                    if (t.Type == ProjectM.Status.TerrainEffectType.FireZone && t.Remaining == ProjectM.Status.TerrainLayer.Permanent) lava++;
            Assert.That(lava, Is.GreaterThan(0), "영구 용암 웅덩이");
            for (int y = 0; y < world.Map.Height; y++)
                for (int x = 0; x < world.Map.Width; x++)
                {
                    var tile = world.Map.Get(new GridPos(x, y));
                    Assert.That(tile == TileType.Grass || tile == TileType.TallGrass, Is.False, $"({x}, {y}) 풀 없음");
                }
        }

        // 검산 (3-18): 3구역 얼음 동굴 = BALANCE 20-1 · 20-2 — 적 4종 · 행동 · 얼음 마녀 · 빙판 웅덩이 · 하늘색 해
        [Test]
        public void ProjectAsset_IceCave_MatchesBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            var book = catalog.ToBook();
            var zone = book.ZoneOf(11);
            Assert.That((zone.Name, zone.FirstFloor, zone.LastFloor, zone.StatPercent), Is.EqualTo(("얼음 동굴", 11, 15, 100)));
            var ids = new List<string>();
            foreach (var e in zone.Enemies) ids.Add(e.Def.VisualId + ":" + e.Weight);
            Assert.That(ids, Is.EqualTo(new[] { "enemy_frost_bat:3", "enemy_snow_wolf:2", "enemy_ice_golem:2", "enemy_frost_shaman:1" }));
            Assert.That(zone.PuddleTerrain, Is.EqualTo(ProjectM.Status.TerrainEffectType.IcePatch));
            Assert.That(book.BossOf(15)?.VisualId, Is.EqualTo("boss_ice_witch"));
            Assert.That(catalog.TryGetLightColor(13, out var ice) && ice.b > ice.r, Is.True, "얼음 동굴 = 하늘색 해");

            (int, int, int, int, int, int, int, int) Stats(string id)
            {
                Assert.That(catalog.TryGetEnemy(id, out var e), Is.True, id);
                return (e.MaxHp, e.AttackDamage, e.SightRange, e.Accuracy, e.Evasion, e.ExpReward, e.ExpMaxLevel, e.MaxMana);
            }
            Assert.That(Stats("enemy_frost_bat"), Is.EqualTo((10, 3, 8, 15, 9, 5, 15, 0)));
            Assert.That(Stats("enemy_snow_wolf"), Is.EqualTo((16, 4, 7, 15, 6, 6, 15, 0)));
            Assert.That(Stats("enemy_ice_golem"), Is.EqualTo((32, 5, 5, 12, 2, 7, 16, 0)));
            Assert.That(Stats("enemy_frost_shaman"), Is.EqualTo((14, 3, 8, 13, 7, 7, 16, 8)));
            Assert.That(Stats("boss_ice_witch"), Is.EqualTo((110, 5, 10, 16, 12, 30, 30, 9)));

            var traits = catalog.ToEnemyBook();
            Assert.That(traits.TryGet("enemy_frost_bat", out var bat) && bat.MoveSteps == 2, Is.True);
            Assert.That(traits.TryGet("enemy_snow_wolf", out var wolf) && wolf.HitStatus == ProjectM.Status.StatusType.Slow && wolf.HitStatusPercent == 40, Is.True);
            Assert.That(traits.TryGet("enemy_ice_golem", out var golem) && golem.MoveCostTicks == 2 && golem.HurtTerrain == ProjectM.Status.TerrainEffectType.IcePatch
                && golem.DamagePercent(Element.Fire) == 150 && golem.DamagePercent(Element.Ice) == 50, Is.True);
            Assert.That(traits.TryGet("enemy_frost_shaman", out var shaman) && shaman.SpellId == "enemy_frost_bolt" && shaman.KeepDistance == 3, Is.True);
            Assert.That(traits.TryGet("boss_ice_witch", out var witch) && witch.SpellId == "enemy_frost_bolt" && witch.KeepDistance == 2
                && witch.ChargeSpellId == "boss_blizzard" && witch.ChargePercent == 30 && witch.TeleportPercent == 0
                && witch.SummonDef?.VisualId == "enemy_frost_bat" && witch.SummonCount == 3 && witch.SummonAtHpPercent == 50
                && witch.DamagePercent(Element.Ice) == 50, Is.True);

            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            Assert.That(spells.TryGet("enemy_frost_bolt", out var bolt), Is.True);
            Assert.That((bolt.Element, bolt.Shape, bolt.Range, bolt.Damage, bolt.ManaCost, bolt.AppliesStatus, bolt.StatusChancePercent),
                Is.EqualTo((Element.Ice, SpellShape.Bolt, 6, 4, 3, ProjectM.Status.StatusType.Slow, 40)));
            Assert.That(spells.TryGet("boss_blizzard", out var storm), Is.True);
            Assert.That((storm.Element, storm.Shape, storm.Range, storm.Damage, storm.ManaCost, storm.Radius, storm.AppliesStatus, storm.StatusChancePercent),
                Is.EqualTo((Element.Ice, SpellShape.Burst, 6, 8, 0, 1, ProjectM.Status.StatusType.Slow, 70)));
        }

        // 3-18: 구역 웅덩이 지형 — 얼음 동굴 구역의 새 층은 웅덩이가 빙판(영구), 다른 구역은 물
        [Test]
        public void IceCaveZone_PuddlesAreIcePatches()
        {
            var dungeon = new DungeonGenConfig(50, 50, 10, 20, 7, 20, 10, 3, 5, 1000, 10, floorCount: 3);
            var generator = new DungeonGenerator(dungeon);
            DungeonFloor floor = null;
            var world = World.Create(21UL, rng => (floor = generator.Generate(rng, stairsUp: false, stairsDown: true)).Map);
            var terrains = SpellTests.BalanceTerrains();
            var vision = world.InstallCoreSystems(Sight, Magic, SpellTests.BalanceStatuses(), terrains);
            var zones = new ZoneBook(new[]
            {
                new ZoneDef("숲", 1, 1, new[] { new ZoneEnemy(Rat, 1) }),
                new ZoneDef("얼음", 2, 3, new[] { new ZoneEnemy(Rat, 1) }, puddleTerrain: ProjectM.Status.TerrainEffectType.IcePatch),
            });
            terrains.TryGet(ProjectM.Status.TerrainEffectType.Water, out var water);
            var decor = new FloorDecorConfig(0, 1, 1, 0, 6, 3, 9);
            var idle = new IdleAi();
            var stairs = new FloorTransition(world, generator, 3, Rat, new SpawnConfig(0, 8), (w, id) => idle, vision,
                decor: decor, water: water, zones: zones, terrains: terrains);
            var player = UnitSpawner.SpawnPlayer(world, PlayerDef, floor.Start, Magic.QuickSlotCount);
            vision.Refresh();
            world.Events.Flush();
            world.Units.Teleport(player.Id, floor.Exit);
            Assert.That(stairs.TryUse(), Is.True, "1 → 2층 (얼음 구역)");
            world.Events.Flush();

            int ice = 0;
            foreach (var pair in world.Terrain.ToSortedList())
                foreach (var t in pair.Value)
                {
                    Assert.That(t.Type, Is.Not.EqualTo(ProjectM.Status.TerrainEffectType.Water), "얼음 구역엔 물 웅덩이 없음");
                    if (t.Type == ProjectM.Status.TerrainEffectType.IcePatch && t.Remaining == ProjectM.Status.TerrainLayer.Permanent) ice++;
                }
            Assert.That(ice, Is.GreaterThan(0), "영구 빙판 웅덩이");
        }
    }
}
