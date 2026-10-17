using System;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.UI;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>3-2b 마법 융합: 융합 표 · 융합 주문 만들기 · 막힘 이유 · 시전 결과 · 인벤토리 고르기 (REACTIONS.md 5번 · BALANCE.md 10번).</summary>
    public class FusionTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly ItemBook Items = new ItemBook(new[] { new ItemDef("fusion_device", ItemUse.Fusion) });
        private static readonly InventoryRef Device = InventoryRef.Item("fusion_device");
        private static readonly InventoryRef WaterSpell = InventoryRef.Spell("water_basic");

        // BALANCE.md 10번
        internal static FusionBook BalanceFusions(StatusBook statuses, TerrainBook terrains) => new FusionBook(new[]
        {
            new FusionDef("fusion_fire_water", Element.Fire, Element.Water, TerrainEffectType.FireZone, 6),
            new FusionDef("fusion_water_poison", Element.Water, Element.Poison, TerrainEffectType.ContaminatedWater, 8),
            new FusionDef("fusion_fire_lightning", Element.Fire, Element.Lightning),
        }, statuses, terrains);

        private sealed class Session
        {
            public ReactionTests.Session Inner;
            public FusionBook Fusions;
            public World World => Inner.World;
            public SpellBook Book => Inner.Book;
            public Unit Player => Inner.Player;

            public CastBlock Check(string first, string second, GridPos target, out SpellDef fused) =>
                FusionCasting.Check(World, Book, Fusions, Items, Targeting, Sight, Player, first, second, target, out fused, out _);

            public ICommand Fuse(string first, string second, GridPos target)
            {
                var block = FusionCasting.TryCreateCast(World, Config, Book, Fusions, Items, Targeting, Sight, first, second, target, out var command);
                Assert.That(block, Is.EqualTo(CastBlock.None));
                Inner.Reactions.Clear();
                Inner.Hits.Clear();
                Inner.Clock.SubmitPlayerCommand(command);
                World.Events.Flush();
                return command;
            }
        }

        // 실제 수치(상태 70% · 지형 · 반응 표 전부)로 — 융합이 두 주문의 상태 · 반응을 쓰지 않는지 보려고
        private static Session Start(GridPos player, bool withDevice = true, params GridPos[] enemies)
        {
            var book = SpellTests.BalanceBookFull(ReactionTests.BalanceReactions);
            var inner = ReactionTests.Start(book, player, enemies); // 시작 주문: 불 · 얼음 · 번개 · 독
            Assert.That(Inventory.TryAdd(inner.World, book, Items, inner.Player, WaterSpell), Is.GreaterThanOrEqualTo(0));
            if (withDevice) Assert.That(Inventory.TryAdd(inner.World, book, Items, inner.Player, Device), Is.GreaterThanOrEqualTo(0));
            inner.World.Events.Flush();
            return new Session { Inner = inner, Fusions = BalanceFusions(book.Statuses, book.Terrains) };
        }

        private static bool Has(Unit unit, StatusType type) => unit.Statuses.Exists(x => x.Type == type);

        private static int RemainingAt(World world, GridPos pos, TerrainEffectType type)
        {
            foreach (var t in world.Terrain.At(pos))
                if (t.Type == type) return t.Remaining;
            return 0;
        }

        // ── 융합 표 ──

        [Test]
        public void Book_RejectsSameElement_DuplicateCombo_MissingTerrain()
        {
            var statuses = SpellTests.BalanceStatuses();
            var terrains = SpellTests.BalanceTerrains();
            Assert.Throws<ArgumentException>(() => new FusionDef("x", Element.Fire, Element.Fire));
            Assert.Throws<ArgumentException>(() => new FusionBook(new[]
            {
                new FusionDef("a", Element.Fire, Element.Water),
                new FusionDef("b", Element.Water, Element.Fire), // 순서만 다른 같은 조합
            }, statuses, terrains));
            Assert.Throws<ArgumentException>(() => new FusionBook(new[]
            {
                new FusionDef("a", Element.Fire, Element.Water),
                new FusionDef("a", Element.Ice, Element.Water),
            }, statuses, terrains));
            Assert.Throws<ArgumentException>(() => new FusionBook(new[] { new FusionDef("a", Element.Fire, Element.Water, TerrainEffectType.FireZone) },
                statuses, TerrainBook.Empty));
        }

        // F2 · F3 · 3-2b 결정: 마나 · 피해 = 합, 모양 = 큰 쪽 (사거리 · 반지름 · 속성도 그 주문), 순서 무관, 반응 없음
        [Test]
        public void Build_SumsManaAndDamage_TakesLargerShape()
        {
            var book = SpellTests.BalanceBookFull();
            var fusions = BalanceFusions(book.Statuses, book.Terrains);
            book.TryGet("fire_basic", out var fire);
            book.TryGet("water_basic", out var water);
            book.TryGet("poison_basic", out var poison);
            book.TryGet("lightning_basic", out var lightning);
            book.TryGet("ice_basic", out var ice);

            var lava = fusions.Build(fire, water);
            Assert.That(lava.Id, Is.EqualTo("fusion_fire_water"));
            Assert.That((lava.Shape, lava.Range, lava.Radius, lava.Element), Is.EqualTo((SpellShape.Burst, 5, 1, Element.Water)), "불 Bolt < 물 Burst");
            Assert.That((lava.Damage, lava.ManaCost), Is.EqualTo((5, 5)));
            Assert.That((lava.LeavesTerrain, lava.TerrainDurationTicks), Is.EqualTo((TerrainEffectType.FireZone, 6)));
            Assert.That(lava.AppliesStatus, Is.EqualTo(StatusType.None), "두 주문의 상태(화상 · 젖음)는 안 걸림");
            Assert.That(lava.TriggersReactions, Is.False);

            var reversed = fusions.Build(water, fire);
            Assert.That((reversed.Id, reversed.Shape, reversed.Damage, reversed.ManaCost), Is.EqualTo((lava.Id, lava.Shape, lava.Damage, lava.ManaCost)), "순서 무관");

            var blast = fusions.Build(fire, lightning);
            Assert.That((blast.Shape, blast.Range, blast.Damage, blast.ManaCost), Is.EqualTo((SpellShape.Beam, 5, 8, 7)), "Bolt < Beam, 5 + 3");
            Assert.That(blast.LeavesTerrain, Is.EqualTo(TerrainEffectType.None));

            var sludge = fusions.Build(water, poison);
            Assert.That((sludge.Shape, sludge.Radius, sludge.Damage, sludge.ManaCost), Is.EqualTo((SpellShape.Burst, 1, 2, 5)));
            Assert.That((sludge.LeavesTerrain, sludge.TerrainDurationTicks), Is.EqualTo((TerrainEffectType.ContaminatedWater, 8)));

            Assert.That(fusions.Build(fire, ice), Is.Null, "표에 없는 조합");
        }

        // 판정 순서: 융합기 → 같은 주문 → 가진 주문 → 조합 → 일반 판정(대상 · 사거리 · 마나)
        [Test]
        public void Check_BlockReasons()
        {
            var s = Start(new GridPos(1, 2), true, new GridPos(5, 2));
            var target = s.Inner.Enemies[0].Pos;
            Assert.That(s.Check("fire_basic", "fire_basic", target, out _), Is.EqualTo(CastBlock.SameSpell));
            Assert.That(s.Check("fire_basic", "no_such", target, out _), Is.EqualTo(CastBlock.NotOwned));
            Assert.That(s.Check("fire_basic", "ice_basic", target, out var none), Is.EqualTo(CastBlock.NoFusionRecipe));
            Assert.That(none, Is.Null);
            Assert.That(s.Check("fire_basic", "water_basic", s.Player.Pos, out var fused), Is.EqualTo(CastBlock.TargetIsSelf));
            Assert.That(fused, Is.Not.Null, "대상 판정 전에 융합 주문은 만들어짐 (입력이 조준 시작 전에 씀)");
            Assert.That(s.Check("fire_basic", "water_basic", new GridPos(10, 2), out _), Is.EqualTo(CastBlock.OutOfRange));
            Assert.That(s.Check("fire_basic", "water_basic", target, out _), Is.EqualTo(CastBlock.None));

            Mana.TrySpend(s.World, s.Player, 6); // 10 → 4 < 5
            Assert.That(s.Check("fire_basic", "water_basic", target, out _), Is.EqualTo(CastBlock.NotEnoughMana));

            var noDevice = Start(new GridPos(1, 2), false, new GridPos(5, 2));
            Assert.That(noDevice.Check("fire_basic", "water_basic", target, out _), Is.EqualTo(CastBlock.NoFusionDevice));
        }

        // 불 + 물: 피해 5, 3×3 화염 지대 6틱 (지형 규칙 4틱을 덮어씀), 물 타일 · 젖음 · 화상(주문) 없음, 마나 5, 행동 1회
        [Test]
        public void FireWater_LeavesFireZoneSixTicks_NoSpellStatusOrWater()
        {
            var s = Start(new GridPos(1, 2), true, new GridPos(5, 2));
            var enemy = s.Inner.Enemies[0];
            var command = s.Fuse("fire_basic", "water_basic", enemy.Pos);

            Assert.That(command.CostTicks, Is.EqualTo(1), "행동 1회 (F2)");
            Assert.That(s.Player.Mana, Is.EqualTo(5));
            Assert.That(s.Inner.HitsOn(enemy), Is.EqualTo(new[] { 5 }));
            Assert.That(s.Inner.Reactions, Is.Empty);
            for (int x = 4; x <= 6; x++)
            for (int y = 1; y <= 3; y++)
            {
                var pos = new GridPos(x, y);
                Assert.That(RemainingAt(s.World, pos, TerrainEffectType.FireZone), Is.EqualTo(5), $"({x},{y}) 6틱, 시전 틱의 ③에서 1 줄어듦");
                Assert.That(s.World.Terrain.Has(pos, TerrainEffectType.Water), Is.False, $"({x},{y}) 물 타일 없음");
            }
            Assert.That(Has(enemy, StatusType.Wet), Is.False, "물 주문의 젖음은 안 걸림");
            Assert.That(Has(enemy, StatusType.Burn), Is.True, "화상은 화염 지대(③, 100%)가 건다");
        }

        // 물 + 독: 피해 2, 3×3 오염된 물 8틱, 중독 · 독구름 · 물 타일 없음, 오염 반응 없음
        [Test]
        public void WaterPoison_LeavesContaminatedWaterEightTicks()
        {
            var s = Start(new GridPos(1, 2), true, new GridPos(5, 2));
            var enemy = s.Inner.Enemies[0];
            s.Fuse("water_basic", "poison_basic", enemy.Pos);

            Assert.That(s.Player.Mana, Is.EqualTo(5));
            Assert.That(s.Inner.HitsOn(enemy), Is.EqualTo(new[] { 2 }));
            Assert.That(s.Inner.Reactions, Is.Empty);
            Assert.That(RemainingAt(s.World, enemy.Pos, TerrainEffectType.ContaminatedWater), Is.EqualTo(7));
            Assert.That(s.World.Terrain.Has(enemy.Pos, TerrainEffectType.PoisonCloud), Is.False);
            Assert.That(s.World.Terrain.Has(enemy.Pos, TerrainEffectType.Water), Is.False);
            Assert.That(Has(enemy, StatusType.Poison), Is.False, "독 주문의 중독은 안 걸림");
            Assert.That(Has(enemy, StatusType.Wet), Is.True, "젖음은 오염된 물(③)이 건다");
        }

        // 불 + 번개: 직선 관통 8, 지형 없음. 중독된 적이어도 유독성 폭발이 안 남 (F4 — 반응 엔진을 거치지 않음)
        [Test]
        public void FireLightning_BeamEight_DoesNotReact()
        {
            var s = Start(new GridPos(1, 2), true, new GridPos(3, 2), new GridPos(5, 2));
            s.Inner.Poison(s.Inner.Enemies[0]);
            s.Inner.Place(new GridPos(5, 2), TerrainEffectType.PoisonCloud); // 가스 폭발도 안 남
            s.Fuse("fire_basic", "lightning_basic", s.Inner.Enemies[1].Pos);

            Assert.That(s.Player.Mana, Is.EqualTo(3));
            Assert.That(s.Inner.HitsOn(s.Inner.Enemies[0]), Is.EqualTo(new[] { 8 }));
            Assert.That(s.Inner.HitsOn(s.Inner.Enemies[1]), Is.EqualTo(new[] { 8 }), "관통");
            Assert.That(s.Inner.Reactions, Is.Empty);
            Assert.That(s.World.Terrain.Has(new GridPos(5, 2), TerrainEffectType.PoisonCloud), Is.True, "구름이 소모되지 않음");
            Assert.That(Has(s.Inner.Enemies[1], StatusType.Paralyze), Is.False, "번개의 마비는 안 걸림");
        }

        // 지형 지속 재정의: 0이면 지형 규칙(4틱), 아니면 그 값
        [Test]
        public void TerrainPlace_DurationOverride()
        {
            var s = Start(new GridPos(1, 2), true, new GridPos(9, 3));
            s.Book.Terrains.TryGet(TerrainEffectType.FireZone, out var fireZone);
            TerrainEffects.Place(s.World, new GridPos(3, 2), fireZone);
            TerrainEffects.Place(s.World, new GridPos(4, 2), fireZone, 6);
            Assert.That(RemainingAt(s.World, new GridPos(3, 2), TerrainEffectType.FireZone), Is.EqualTo(4));
            Assert.That(RemainingAt(s.World, new GridPos(4, 2), TerrainEffectType.FireZone), Is.EqualTo(6));
        }

        // ── 인벤토리 창: 융합기 사용 → 주문 두 개 고르기 (F1) ──

        [Test]
        public void Menu_FusionPick_SpellsOnly_SameCellCancels_SecondFuses()
        {
            var s = Start(new GridPos(1, 2));
            // 인벤토리: 0 불 · 1 얼음 · 2 번개 · 3 독 · 4 물 · 5 융합기
            var menu = new InventoryMenu();
            menu.OpenForFusion();
            Assert.That(menu.State, Is.EqualTo(InventoryMenuState.FusionPick));

            menu.Select(5, s.Player.InventorySlots.Length);
            Assert.That(menu.Confirm(s.World, s.Player).Kind, Is.EqualTo(InventoryActionKind.None), "융합기(아이템)는 못 고름");
            Assert.That(menu.FusionFirst, Is.EqualTo(-1));
            menu.Select(7, s.Player.InventorySlots.Length);
            menu.Confirm(s.World, s.Player);
            Assert.That(menu.FusionFirst, Is.EqualTo(-1), "빈 칸도 못 고름");

            menu.Select(0, s.Player.InventorySlots.Length);
            menu.Confirm(s.World, s.Player);
            Assert.That(menu.FusionFirst, Is.EqualTo(0));
            menu.Confirm(s.World, s.Player);
            Assert.That(menu.FusionFirst, Is.EqualTo(-1), "같은 칸 다시 = 취소");

            menu.Confirm(s.World, s.Player); // 다시 불을 첫째로
            menu.Select(4, s.Player.InventorySlots.Length);
            var action = menu.Confirm(s.World, s.Player);
            Assert.That(action.Kind, Is.EqualTo(InventoryActionKind.FuseSpells));
            Assert.That(action.Entry, Is.EqualTo(InventoryRef.Spell("fire_basic")));
            Assert.That(action.Second, Is.EqualTo(WaterSpell));
            Assert.That(menu.IsOpen, Is.False);
        }

        [Test]
        public void Menu_FusionPick_EscStepsBack()
        {
            var s = Start(new GridPos(1, 2));
            var menu = new InventoryMenu();
            menu.OpenForFusion();
            menu.Confirm(s.World, s.Player); // 칸 0 불
            Assert.That(menu.FusionFirst, Is.EqualTo(0));
            menu.Cancel();
            Assert.That(menu.IsOpen, Is.True, "첫 주문만 취소");
            Assert.That(menu.FusionFirst, Is.EqualTo(-1));
            menu.Cancel();
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(menu.PressQuickSlot(s.World, s.Player, 1).Kind, Is.EqualTo(InventoryActionKind.None), "닫힌 뒤 등록 안 됨");
        }

        // 융합 주문 이름은 FusionCatalog의 DisplayName (로그 "용암 분출 시전")
        [Test]
        public void HudText_FusionNames()
        {
            var text = new HudText(new System.Collections.Generic.Dictionary<string, string> { { "fire_basic", "불꽃 화살" } }, null, null,
                new System.Collections.Generic.Dictionary<string, string> { { "fusion_fire_water", "용암 분출" } });
            Assert.That(text.Spell("fusion_fire_water"), Is.EqualTo("용암 분출"));
            Assert.That(text.Spell("fire_basic"), Is.EqualTo("불꽃 화살"));
            Assert.That(text.Spell("fusion_unknown"), Is.EqualTo("fusion_unknown"));
            Assert.That(HudText.CastBlockReason(CastBlock.NoFusionRecipe), Is.EqualTo("융합할 수 없는 조합"));
        }

        // 검산: FusionCatalog 에셋이 BALANCE.md 10번과 같은지
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var asset = AssetDatabase.LoadAssetAtPath<FusionCatalogAsset>("Assets/_Project/Data/Fusions/FusionCatalog.asset");
            Assert.That(asset, Is.Not.Null, "FusionCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var statuses = SpellTests.BalanceStatuses();
            var terrains = SpellTests.BalanceTerrains();
            var book = asset.ToBook(statuses, terrains);
            var expected = BalanceFusions(statuses, terrains);
            Assert.That(book.Count, Is.EqualTo(expected.Count));
            foreach (var e in expected.All)
            {
                var f = book.Find(e.ElementA, e.ElementB);
                Assert.That(f, Is.Not.Null, e.Id);
                Assert.That((f.Id, f.LeavesTerrain, f.TerrainDurationTicks, f.AppliesStatus),
                    Is.EqualTo((e.Id, e.LeavesTerrain, e.TerrainDurationTicks, e.AppliesStatus)), e.Id);
            }
            var names = asset.DisplayNames();
            Assert.That(names["fusion_fire_water"], Is.EqualTo("용암 분출"));
            Assert.That(names["fusion_water_poison"], Is.EqualTo("오염 물결"));
            Assert.That(names["fusion_fire_lightning"], Is.EqualTo("폭발 마법"));
        }
    }
}
