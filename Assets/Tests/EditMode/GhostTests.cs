using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;
using EntityId = ProjectM.Core.EntityId;

namespace ProjectM.Tests
{
    /// <summary>
    /// 3-23 시간의 모래시계 · 지난 회차 플레이어 보스 (결정 2026-11-05 — 사용자): 마지막 보스 층 → 봉인 계단 → 제단 층(큰 방 · 적 없음 · 유물) →
    /// 주우면 승리. 기록(GhostBoss.Capture — 능력치 · 장비 · 빠른 칸 주문, 특성 · 무기 강화 반영) → 다음 판 마지막 보스 = 기록(체력 3배 · 무기 · 갑옷 ·
    /// 주문을 빠른 칸 순서로). 저장 파일(GhostStore JSON) 왕복 · 깨진 파일 = 없음. 에셋 값 (BALANCE.md 20 · 23번).
    /// </summary>
    public class GhostTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("hero_blond", 20, 3, 8, 10, new[] { "light_spear_thrust", "sia_leap", "fire_basic" },
            accuracy: 10, evasion: 5, strength: 11);
        private static readonly UnitDef Rat = new UnitDef("rat", 8, 2, 6);
        private static readonly UnitDef BossDef = new UnitDef("boss", 60, 5, 10);

        private sealed class IdleAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static SpellBook Spells() => new SpellBook(new[]
        {
            new SpellDef("light_spear_thrust", Element.Neutral, SpellShape.Beam, 2, 4, 2, boostWeaponId: "spear", boostDamagePercent: 150, boostRange: 1),
            new SpellDef("sia_leap", Element.Neutral, SpellShape.Leap, 3, 0, 2),
            new SpellDef("fire_basic", Element.Fire, SpellShape.Bolt, 6, 5, 3, 0, StatusType.Burn, 70),
        }, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());

        private static ItemBook Items() => new ItemBook(new[]
        {
            new ItemDef("spear", ItemUse.EquipWeapon, weapon: new WeaponDef(20, 4, 100, 2, 2, strengthRequirement: 12)),
            new ItemDef("time_hourglass", ItemUse.Artifact),
        }, new WeaponRules(3, 2));

        // 1층 · 2층(마지막 보스 층) · 3층(제단)
        private static ZoneBook Zones() => new ZoneBook(new[]
        {
            new ZoneDef("a", 1, 2, new[] { new ZoneEnemy(Rat, 1) }, 100, BossDef),
            new ZoneDef("altar", 3, 3, null, altarItemId: "time_hourglass"),
        });

        private sealed class Run
        {
            public World World;
            public FloorTransition Stairs;
            public Unit Player;
            public ItemBook Items;
            public SpellBook Spells;
            public readonly List<RunEndedEvent> Ends = new List<RunEndedEvent>();
            public readonly List<ArtifactObtainedEvent> Artifacts = new List<ArtifactObtainedEvent>();
        }

        private static Run Start(GhostRecord ghost = null)
        {
            var dungeon = new DungeonGenConfig(50, 50, 10, 20, 7, 20, 10, 3, 5, 1000, 10, floorCount: 3);
            var generator = new DungeonGenerator(dungeon);
            DungeonFloor floor = null;
            var world = World.Create(12UL, rng => (floor = generator.Generate(rng, stairsUp: false, stairsDown: true)).Map);
            var r = new Run { World = world, Items = Items(), Spells = Spells() };
            var vision = world.InstallCoreSystems(Sight, Magic, r.Spells.Statuses, r.Spells.Terrains);
            world.Scheduler.AddSystem(TickPhase.ManaRegen, new ItemPickupSystem(world, r.Items));
            var idle = new IdleAi();
            var zones = Zones();
            r.Stairs = new FloorTransition(world, generator, 3, Rat, new SpawnConfig(0, 8), (w, id) => idle, vision, zones: zones,
                ghost: ghost, ghostDef: ghost != null ? GhostBoss.ToUnitDef(ghost, 300, 50, 30) : null);
            world.Events.Subscribe<RunEndedEvent>(r.Ends.Add);
            world.Events.Subscribe<ArtifactObtainedEvent>(r.Artifacts.Add);
            r.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, floor.Start, Magic.QuickSlotCount, Magic.InventorySlotCount);
            vision.Refresh();
            world.Events.Flush();
            return r;
        }

        private static void Descend(Run r)
        {
            var down = r.World.Map.Find(TileType.StairsDown);
            Assert.That(down.HasValue, Is.True, $"{r.World.Floor}층 내려가는 계단");
            r.World.Units.Teleport(r.Player.Id, down.Value);
            Assert.That(r.Stairs.TryUse(), Is.True);
            r.World.Events.Flush();
        }

        private static void Tick(Run r)
        {
            new TurnClock(r.World.Scheduler, r.World.PlayerInput).SubmitPlayerCommand(PlayerActions.Wait(Config));
            r.World.Events.Flush();
        }

        private static GhostRecord SampleGhost() => new GhostRecord("루멘의 그림자", "hero_blond", 12, 75, 3, 8, 21, 22, 16, 12,
            new EquippedWeapon("spear", 2, 11, 100, 2, 2, 3, 10), EquippedArmor.Empty,
            new[] { new SpellDef("light_spear_thrust", Element.Neutral, SpellShape.Beam, 3, 7, 2), new SpellDef("fire_basic", Element.Fire, SpellShape.Bolt, 6, 5, 3) });

        // ── 26층 제단 · 모래시계 ──

        // 마지막 보스 층(2층) 보스를 쓰러뜨리면 봉인 계단 → 제단 층(3층): 적 없음 · 보스 자리에 유물 → 주우면 승리 · 유물 이벤트
        [Test]
        public void LastBoss_OpensAltar_HourglassIsVictory()
        {
            var r = Start();
            Descend(r);
            Assert.That(r.World.Floor, Is.EqualTo(2));
            Assert.That(r.World.Units.TryGet(r.World.Run.BossId, out var boss) && boss.VisualId == "boss", Is.True, "기록이 없으면 원래 보스");
            Combat.ApplyDamage(r.World, EntityId.None, boss, 9999);
            Tick(r);
            Assert.That(r.Ends, Is.Empty, "보스를 쓰러뜨려도 아직 승리 아님 (제단이 남음)");
            Descend(r);
            Assert.That(r.World.Floor, Is.EqualTo(3));
            int enemies = 0;
            foreach (var u in r.World.Units.ToSortedList()) if (u.Faction == Faction.Enemy) enemies++;
            Assert.That(enemies, Is.EqualTo(0), "제단에는 적 없음");
            Assert.That(r.World.Map.Find(TileType.StairsDown).HasValue, Is.False, "더 내려갈 곳 없음");
            GridPos? altar = null;
            foreach (var item in r.World.FloorItems.All) if (item.ItemId == "time_hourglass") altar = item.Pos;
            Assert.That(altar.HasValue, Is.True, "모래시계");

            r.World.Units.Teleport(r.Player.Id, altar.Value);
            Tick(r);
            Assert.That(r.Artifacts.Count, Is.EqualTo(1));
            Assert.That(r.Ends.Count, Is.EqualTo(1));
            Assert.That(r.Ends[0].Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(r.World.Run.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(r.Player.InventorySlots, Has.No.Member(InventoryRef.Item("time_hourglass")), "배낭에 넣지 않음");
        }

        // ── 기록 ──

        // 기록 = 모습 · 레벨 · 능력치 · 무기 · 갑옷 · 빠른 칸 주문(도약 뺌), 주문 값은 무기 강화가 반영된 채로 굳힘
        [Test]
        public void Capture_BakesStatsGearAndSpells()
        {
            var r = Start();
            Inventory.TryAdd(r.World, r.Spells, r.Items, r.Player, InventoryRef.Item("spear"));
            Weapons.Equip(r.World, r.Items, r.Player, System.Array.IndexOf(r.Player.InventorySlots, InventoryRef.Item("spear")));
            var g = GhostBoss.Capture(r.World, r.Player, r.Spells, "루멘의 그림자");
            Assert.That((g.DisplayName, g.VisualId, g.Level, g.MaxHp, g.MaxMana, g.Accuracy, g.Evasion, g.Strength), Is.EqualTo(("루멘의 그림자", "hero_blond", 1, 20, 10, 10, 5, 11)));
            Assert.That(g.Weapon, Is.EqualTo(r.Player.Weapon));
            Assert.That(new List<SpellDef>(g.Spells).ConvertAll(s => s.Id), Is.EqualTo(new[] { "light_spear_thrust", "fire_basic" }), "도약은 뺌 · 빠른 칸 순서");
            Assert.That((g.Spells[0].Damage, g.Spells[0].Range, g.Spells[0].BoostWeaponId), Is.EqualTo((6, 3, (string)null)), "창 강화가 굳음");
        }

        // 다음 판: 마지막 보스 = 기록 (체력 × 3 · 무기 · 주문 ID ghost_ · 행동은 주문을 빠른 칸 순서로)
        [Test]
        public void NextRun_LastBossIsGhost()
        {
            var ghost = SampleGhost();
            var def = GhostBoss.ToUnitDef(ghost, 300, 50, 30);
            Assert.That((def.VisualId, def.MaxHp, def.AttackDamage, def.Accuracy, def.Evasion, def.MaxMana, def.ExpReward), Is.EqualTo(("hero_blond", 225, 3, 22, 16, 21, 50)));
            var traits = GhostBoss.Traits(ghost);
            Assert.That((traits.Id, traits.DisplayName, traits.SpellId), Is.EqualTo(("hero_blond", "루멘의 그림자", "ghost_light_spear_thrust")));
            Assert.That(traits.ExtraSpellIds, Is.EqualTo(new[] { "ghost_fire_basic" }));
            var spells = GhostBoss.SpellsWithPrefix(ghost);
            Assert.That(spells.ConvertAll(s => (s.Id, s.Damage)), Is.EqualTo(new[] { ("ghost_light_spear_thrust", 7), ("ghost_fire_basic", 5) }));

            var r = Start(ghost);
            Descend(r);
            Assert.That(r.World.Units.TryGet(r.World.Run.BossId, out var boss), Is.True);
            Assert.That((boss.VisualId, boss.MaxHp, boss.Faction), Is.EqualTo(("hero_blond", 225, Faction.Enemy)));
            Assert.That(boss.Weapon, Is.EqualTo(ghost.Weapon), "기록의 무기를 듦");
            Assert.That(r.World.Map.Find(TileType.SealedStairs).HasValue, Is.True, "그림자도 중간 보스 — 쓰러뜨리면 제단으로");
        }

        // ── 저장 파일 ──

        // JSON 왕복 = 같은 기록 · 깨진 파일은 없음(null) · 지우기
        [Test]
        public void Store_RoundTrip_CorruptIsNull_Delete()
        {
            var path = Path.Combine(Path.GetTempPath(), "projectm_ghost_test_" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var ghost = SampleGhost();
                GhostStore.Save(ghost, path);
                var back = GhostStore.Load(path);
                Assert.That(back, Is.Not.Null);
                Assert.That((back.DisplayName, back.VisualId, back.Level, back.MaxHp, back.Accuracy, back.Strength), Is.EqualTo((ghost.DisplayName, ghost.VisualId, 12, 75, 22, 12)));
                Assert.That(back.Weapon, Is.EqualTo(ghost.Weapon));
                Assert.That(back.Armor.IsEmpty, Is.True);
                Assert.That(back.Spells.Count, Is.EqualTo(2));
                Assert.That((back.Spells[0].Id, back.Spells[0].Shape, back.Spells[0].Range, back.Spells[0].Damage), Is.EqualTo(("light_spear_thrust", SpellShape.Beam, 3, 7)));

                File.WriteAllText(path, "{ not json");
                UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("기록을 읽지 못했다"));
                Assert.That(GhostStore.Load(path), Is.Null);
                GhostStore.Delete(path);
                Assert.That(File.Exists(path), Is.False);
                Assert.That(GhostStore.Load(path), Is.Null, "없으면 null");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // ── 검산 ──

        // BALANCE.md 20 · 23번: 26층 제단 · 모래시계 아이템 · 층 수 26 · 그림자 체력 300% · 경험치 50 · 최고 레벨 30
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ZoneCatalogAsset>("Assets/_Project/Data/Units/ZoneCatalog.asset");
            var zones = catalog.ToBook();
            var altar = zones.ZoneOf(26);
            Assert.That((altar.Name, altar.FirstFloor, altar.LastFloor, altar.IsAltar, altar.AltarItemId, altar.Boss), Is.EqualTo(("시간의 제단", 26, 26, true, "time_hourglass", (UnitDef)null)));
            Assert.That(zones.LastBossFloor, Is.EqualTo(25));
            Assert.That((catalog.GhostHpPercent, catalog.GhostExpReward, catalog.GhostExpMaxLevel), Is.EqualTo((300, 50, 30)));
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            Assert.That(items.TryGet("time_hourglass", out var h) && h.Use == ItemUse.Artifact, Is.True);
        }
    }
}
