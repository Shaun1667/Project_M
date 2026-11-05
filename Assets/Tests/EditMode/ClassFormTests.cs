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
    /// 3-21b 루멘 형태 (Docs/직업_루멘.md 3번, 결정 2026-11-05): 선택 층 보스 뒤 한 번 고르기 · 배우는 주문 · 바뀌는 주문(던지기 → 강화) ·
    /// 스냅샷 v24 · 새 주문 모양(연속 = HitCount · 돌진 = Dash · 다중 = Multi) · 에셋 값 (BALANCE.md 21번).
    /// </summary>
    public class ClassFormTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("hero_blond", 20, 3, 8, 30, new[] { "light_spear_thrust", "light_spear_throw" });
        private static readonly UnitDef DummyDef = new UnitDef("enemy_melee", 50, 2, 6);

        private sealed class Idle : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        // BALANCE.md 21번 — 공통 2 + 형태 6
        private static SpellBook Book() => new SpellBook(new[]
        {
            new SpellDef("light_spear_thrust", Element.Neutral, SpellShape.Beam, 2, 4, 2),
            new SpellDef("light_spear_throw", Element.Neutral, SpellShape.Bolt, 5, 4, 3),
            new SpellDef("light_spear_flurry", Element.Neutral, SpellShape.Bolt, 1, 2, 3, hitCount: 3),
            new SpellDef("light_spear_pierce", Element.Neutral, SpellShape.Beam, 3, 5, 3),
            new SpellDef("light_spear_charge", Element.Neutral, SpellShape.Dash, 4, 6, 4),
            new SpellDef("light_spear_lance", Element.Neutral, SpellShape.Beam, 7, 5, 4),
            new SpellDef("light_spear_volley", Element.Neutral, SpellShape.Multi, 6, 4, 5, maxTargets: 3),
            new SpellDef("light_spear_throw_plus", Element.Neutral, SpellShape.Bolt, 7, 4, 2),
        }, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());

        // 선택 층 = 1 (테스트 세계는 1층) — 실제 데이터는 5층
        private static ClassBook Classes(int chooseAt = 1) => new ClassBook(new[]
        {
            new ClassDef("lumen", chooseAt, new[]
            {
                new ClassFormDef("melee", new[] { "light_spear_flurry", "light_spear_pierce", "light_spear_charge" }),
                new ClassFormDef("ranged", new[] { "light_spear_lance", "light_spear_volley" }, new[] { ("light_spear_throw", "light_spear_throw_plus") }),
            }),
        });

        private sealed class Session
        {
            public World World;
            public SpellBook Book;
            public TurnClock Clock;
            public Unit Player;
            public readonly List<ClassFormChosenEvent> Chosen = new List<ClassFormChosenEvent>();
        }

        private static Session Start(string classId = "lumen")
        {
            var grid = GridMovementTests.Parse(
                "############",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "############");
            var world = new World(grid, 3UL);
            var s = new Session { World = world, Book = Book(), Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            var vision = world.InstallCoreSystems(Sight, Magic, s.Book.Statuses, s.Book.Terrains);
            world.Events.Subscribe<ClassFormChosenEvent>(s.Chosen.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 2), Magic.QuickSlotCount, Magic.InventorySlotCount);
            ClassForms.SetClass(world, classId);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static Unit Dummy(Session s, GridPos pos)
        {
            var u = UnitSpawner.SpawnEnemy(s.World, DummyDef, pos, new Idle());
            s.World.Events.Flush();
            return u;
        }

        private static CastBlock Cast(Session s, string spellId, GridPos target)
        {
            var block = SpellCasting.TryCreateCastSpell(s.World, Config, s.Book, Targeting, Sight, spellId, target, out var command);
            if (block == CastBlock.None) s.Clock.SubmitPlayerCommand(command);
            s.World.Events.Flush();
            return block;
        }

        // ── 고르기 ──

        // 직업 없음 · 선택 층 전 · 선택 층인데 보스 아직 = 못 고름
        [Test]
        public void CanChoose_NeedsClass_FloorAndBoss()
        {
            Assert.That(ClassForms.CanChoose(Start(null).World, Classes(), true), Is.EqualTo(ChooseFormResult.NoClass));
            Assert.That(ClassForms.CanChoose(Start("sia").World, Classes(), true), Is.EqualTo(ChooseFormResult.NoClass), "표에 없는 직업");
            var s = Start();
            Assert.That(ClassForms.CanChoose(s.World, Classes(chooseAt: 5), true), Is.EqualTo(ChooseFormResult.NotYet), "1층 < 5층");
            Assert.That(ClassForms.CanChoose(s.World, Classes(), bossDefeated: false), Is.EqualTo(ChooseFormResult.NotYet));
            Assert.That(ClassForms.CanChoose(s.World, Classes(), true), Is.EqualTo(ChooseFormResult.Chosen));
            Assert.That(ClassForms.Choose(s.World, Classes(), s.Book, s.Player, "wizard"), Is.EqualTo(ChooseFormResult.UnknownForm));
            Assert.That(s.World.Run.FormId, Is.Null);
        }

        // 근접형: 형태 기술 3개를 배워 빈 빠른 칸 3 · 4 · 5에, 던지기는 그대로, 다시 고르기는 안 됨
        [Test]
        public void ChooseMelee_LearnsThreeSkills_Once()
        {
            var s = Start();
            Assert.That(ClassForms.Choose(s.World, Classes(), s.Book, s.Player, "melee"), Is.EqualTo(ChooseFormResult.Chosen));
            s.World.Events.Flush();
            Assert.That(s.World.Run.FormId, Is.EqualTo("melee"));
            Assert.That(new[] { s.Player.QuickSlotSpell(0), s.Player.QuickSlotSpell(1), s.Player.QuickSlotSpell(2), s.Player.QuickSlotSpell(3), s.Player.QuickSlotSpell(4) },
                Is.EqualTo(new[] { "light_spear_thrust", "light_spear_throw", "light_spear_flurry", "light_spear_pierce", "light_spear_charge" }));
            Assert.That(s.Chosen.Count, Is.EqualTo(1));
            Assert.That(s.Chosen[0].Learned, Is.EqualTo(new[] { "light_spear_flurry", "light_spear_pierce", "light_spear_charge" }));
            Assert.That(ClassForms.Choose(s.World, Classes(), s.Book, s.Player, "ranged"), Is.EqualTo(ChooseFormResult.AlreadyChosen));
            Assert.That(s.Player.HasSpell("light_spear_lance"), Is.False);
        }

        // 원거리형: 던지기가 같은 칸(인벤토리 · 빠른 칸 2)에서 강화 던지기로 바뀌고, 관통 · 다중 발사를 배움
        [Test]
        public void ChooseRanged_ReplacesThrowInPlace_LearnsTwo()
        {
            var s = Start();
            int invIndex = System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Spell("light_spear_throw"));
            Assert.That(ClassForms.Choose(s.World, Classes(), s.Book, s.Player, "ranged"), Is.EqualTo(ChooseFormResult.Chosen));
            s.World.Events.Flush();
            Assert.That(s.Player.InventorySlots[invIndex], Is.EqualTo(InventoryRef.Spell("light_spear_throw_plus")));
            Assert.That(s.Player.QuickSlotSpell(1), Is.EqualTo("light_spear_throw_plus"));
            Assert.That(s.Player.HasSpell("light_spear_throw"), Is.False);
            Assert.That(s.Player.QuickSlotSpell(2), Is.EqualTo("light_spear_lance"));
            Assert.That(s.Player.QuickSlotSpell(3), Is.EqualTo("light_spear_volley"));
        }

        // 스냅샷 v24: 직업 · 형태 왕복
        [Test]
        public void Snapshot_RoundTripsClassAndForm()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(24));
            var s = Start();
            ClassForms.Choose(s.World, Classes(), s.Book, s.Player, "melee");
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(s.World.Capture())), (w, id) => null);
            Assert.That((restored.Run.ClassId, restored.Run.FormId), Is.EqualTo(("lumen", "melee")));
            var none = Start(null);
            var restoredNone = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(none.World.Capture())), (w, id) => null);
            Assert.That((restoredNone.Run.ClassId, restoredNone.Run.FormId), Is.EqualTo(((string)null, (string)null)));
        }

        // ── 새 주문 모양 ──

        // 연속 찌르기: 옆 칸 한 대상을 3번 (피해 이벤트 3개, 2 × 3 = 6)
        [Test]
        public void Flurry_HitsSameTargetThreeTimes()
        {
            var s = Start();
            var enemy = Dummy(s, new GridPos(2, 2));
            var hits = new List<UnitDamagedEvent>();
            s.World.Events.Subscribe<UnitDamagedEvent>(hits.Add);
            ClassForms.Choose(s.World, Classes(), s.Book, s.Player, "melee");
            Assert.That(Cast(s, "light_spear_flurry", enemy.Pos), Is.EqualTo(CastBlock.None));
            Assert.That(enemy.Hp, Is.EqualTo(50 - 6));
            Assert.That(hits.FindAll(h => h.Target == enemy.Id).Count, Is.EqualTo(3));
            Assert.That(Cast(s, "light_spear_flurry", new GridPos(3, 2)), Is.EqualTo(CastBlock.OutOfRange), "사거리 1");
        }

        // 돌진: 직선 4칸 안의 첫 적 바로 앞까지 달려가 맞힘 · 적이 없으면 끝 칸까지 이동만
        [Test]
        public void Charge_MovesNextToFirstEnemy_AndHits()
        {
            var s = Start();
            ClassForms.Choose(s.World, Classes(), s.Book, s.Player, "melee");
            var near = Dummy(s, new GridPos(4, 2));
            var far = Dummy(s, new GridPos(5, 2));
            Assert.That(Cast(s, "light_spear_charge", far.Pos), Is.EqualTo(CastBlock.None));
            Assert.That(s.Player.Pos, Is.EqualTo(new GridPos(3, 2)), "첫 적(4,2) 바로 앞");
            Assert.That((near.Hp, far.Hp), Is.EqualTo((44, 50)), "첫 적만, 뒤 적은 안 맞음");

            var empty = Start();
            ClassForms.Choose(empty.World, Classes(), empty.Book, empty.Player, "melee");
            Assert.That(Cast(empty, "light_spear_charge", new GridPos(5, 2)), Is.EqualTo(CastBlock.None));
            Assert.That(empty.Player.Pos, Is.EqualTo(new GridPos(5, 2)), "적이 없으면 이동만");
        }

        // 다중 발사: 조준한 적 먼저 → 가까운 순 3명, 다른 적 뒤에 가려진 적 · 사거리 밖은 빠짐, 대상마다 4
        [Test]
        public void Volley_PicksTargetedThenNearest_Max3_SkipsBlocked()
        {
            var s = Start();
            ClassForms.Choose(s.World, Classes(), s.Book, s.Player, "ranged");
            // 시전자 (1,2). 서로의 조준선을 가리지 않는 자리 (같은 줄 · 같은 열 · 대각선 위주)
            var a = Dummy(s, new GridPos(3, 2)); // 거리 2, 오른쪽 줄
            var hidden = Dummy(s, new GridPos(5, 2)); // a 뒤 같은 줄 — 조준선이 a에서 멈춤
            var b = Dummy(s, new GridPos(1, 4)); // 거리 2, 아래 열
            var c = Dummy(s, new GridPos(4, 4)); // 거리 3 — 조준
            var d = Dummy(s, new GridPos(6, 4)); // 거리 5
            var outOfRange = Dummy(s, new GridPos(10, 2)); // 거리 9 (같은 줄이지만 a · hidden 뒤이기도)
            Assert.That(SpellCasting.CheckSpell(s.World, s.Book, Targeting, Sight, s.Player, "light_spear_volley", c.Pos, out _, out var preview), Is.EqualTo(CastBlock.None));
            Assert.That(preview.AffectedCells, Is.EqualTo(new[] { c.Pos, a.Pos, b.Pos }), "조준한 c → 가까운 a · b (같으면 ID 순), d는 4번째라 빠짐");
            Assert.That(Cast(s, "light_spear_volley", c.Pos), Is.EqualTo(CastBlock.None));
            Assert.That((a.Hp, b.Hp, c.Hp, d.Hp, hidden.Hp, outOfRange.Hp), Is.EqualTo((46, 46, 46, 50, 50, 50)));
        }

        // ── 검산 ──

        // BALANCE.md 21번 = ClassCatalog 루멘 줄 + SpellCatalog 형태 기술 6줄
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ClassCatalogAsset>("Assets/_Project/Data/Units/ClassCatalog.asset");
            Assert.That(catalog, Is.Not.Null, "ClassCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var classes = catalog.ToBook();
            Assert.That(classes.TryGet("lumen", out var lumen), Is.True);
            Assert.That(lumen.ChooseAtFloor, Is.EqualTo(5));
            Assert.That(lumen.Forms.Count, Is.EqualTo(2));
            Assert.That(lumen.FormOf("melee").LearnSpells, Is.EqualTo(new[] { "light_spear_flurry", "light_spear_pierce", "light_spear_charge" }));
            Assert.That(lumen.FormOf("melee").ReplaceSpells, Is.Empty);
            Assert.That(lumen.FormOf("ranged").LearnSpells, Is.EqualTo(new[] { "light_spear_lance", "light_spear_volley" }));
            Assert.That(lumen.FormOf("ranged").ReplaceSpells, Is.EqualTo(new[] { ("light_spear_throw", "light_spear_throw_plus") }));

            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            foreach (var (id, shape, range, damage, mana, hits, targets) in new[]
            {
                ("light_spear_flurry", SpellShape.Bolt, 1, 2, 3, 3, 0),
                ("light_spear_pierce", SpellShape.Beam, 3, 5, 3, 1, 0),
                ("light_spear_charge", SpellShape.Dash, 4, 6, 4, 1, 0),
                ("light_spear_lance", SpellShape.Beam, 7, 5, 4, 1, 0),
                ("light_spear_volley", SpellShape.Multi, 6, 4, 5, 1, 3),
                ("light_spear_throw_plus", SpellShape.Bolt, 7, 4, 2, 1, 0),
            })
            {
                Assert.That(spells.TryGet(id, out var d), Is.True, id);
                Assert.That((d.Element, d.Shape, d.Range, d.Damage, d.ManaCost, d.HitCount, d.MaxTargets), Is.EqualTo((Element.Neutral, shape, range, damage, mana, hits, targets)), id);
                Assert.That((d.BoostWeaponId, d.BoostDamagePercent, d.BoostRange), Is.EqualTo(("spear", 150, 1)), id + " 창 강화");
            }
        }
    }
}
