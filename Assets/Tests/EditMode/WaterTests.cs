using System;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Tests
{
    /// <summary>3-2 물: 물 주문 · 젖음 · 물 타일 · 오염된 물 · 증발 · 오염 (REACTIONS.md 3 · 4번).</summary>
    public class WaterTests
    {
        private const int Fire = 0, Poison = 3, Water = 0; // 물은 칸 1(불 자리)에 장착해서 쓴다
        private static readonly GameConfig WaitConfig = new GameConfig(1, 1f);

        // 물 주문 (원본 6-1): 3×3, 피해 0, 마나 2, 사거리 5, 젖음 100%, 물 타일
        private static readonly SpellDef WaterSpell = new SpellDef("water_basic", Element.Water, SpellShape.Burst, 5, 0, 2, 1,
            StatusType.Wet, 100, TerrainEffectType.Water);

        private static ReactionTests.Session Start(GridPos player, params GridPos[] enemies) =>
            ReactionTests.Start(ReactionTests.Book(WaterSpell), player, enemies);

        private static void EquipWater(ReactionTests.Session s)
        {
            TestInventory.Equip(s.World, s.Book, s.Player, Water, "water_basic");
            s.World.Events.Flush();
        }

        private static void Wet(ReactionTests.Session s, Unit unit)
        {
            s.Book.Statuses.TryGet(StatusType.Wet, out var def);
            StatusEffects.Apply(s.World, unit, def);
        }

        private static bool IsWet(Unit unit) => unit.Statuses.Exists(x => x.Type == StatusType.Wet);

        private static int RemainingAt(World world, GridPos pos, TerrainEffectType type)
        {
            foreach (var t in world.Terrain.At(pos))
                if (t.Type == type) return t.Remaining;
            return 0;
        }

        private static void Wait(ReactionTests.Session s, int turns)
        {
            for (int i = 0; i < turns; i++) s.Clock.SubmitPlayerCommand(PlayerActions.Wait(WaitConfig));
            s.World.Events.Flush();
        }

        [Test]
        public void WaterSpell_WetsTargets_LeavesPool_NoDamage()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            EquipWater(s);
            var enemy = s.Enemies[0];
            s.Cast(Water, enemy.Pos);
            s.World.Events.Flush();

            Assert.That(IsWet(enemy), Is.True, "젖음 100%");
            Assert.That(s.Hits, Is.Empty, "피해 0 주문은 피해 이벤트를 내지 않는다");
            Assert.That(s.Player.Mana, Is.EqualTo(8));
            for (int x = 4; x <= 6; x++)
            for (int y = 1; y <= 3; y++)
                Assert.That(s.World.Terrain.Has(new GridPos(x, y), TerrainEffectType.Water), Is.True, $"({x},{y})");
            Assert.That(RemainingAt(s.World, new GridPos(5, 2), TerrainEffectType.Water), Is.EqualTo(11), "12틱, 시전 틱의 ③에서 1 줄어듦");
        }

        // 물 위에 있는 동안 젖음 유지, 물이 사라지면 젖음 3틱(남은 ④ 처리)이 지나면 풀림
        [Test]
        public void StandingOnWater_StaysWet_ThenWearsOff()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            var enemy = s.Enemies[0];
            s.Place(enemy.Pos, TerrainEffectType.Water);
            Wait(s, 10);
            Assert.That(IsWet(enemy), Is.True);

            TerrainEffects.Remove(s.World, enemy.Pos, TerrainEffectType.Water);
            Wait(s, 1);
            Assert.That(IsWet(enemy), Is.True, "떠난 뒤에도 잠시 젖어 있음");
            Wait(s, 2);
            Assert.That(IsWet(enemy), Is.False);
        }

        // 오염된 물 (D7): 위 유닛 매 틱 젖음 + 피해 1
        [Test]
        public void ContaminatedWater_WetsAndDamagesEveryTick()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            var enemy = s.Enemies[0];
            int hp = enemy.Hp;
            s.Place(enemy.Pos, TerrainEffectType.ContaminatedWater);
            Wait(s, 3);
            Assert.That(enemy.Hp, Is.EqualTo(hp - 3));
            Assert.That(IsWet(enemy), Is.True);
        }

        // #3 증발: 젖은 대상에 불 → 젖음 해제 + 2
        [Test]
        public void Evaporate_FireOnWetTarget()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            var enemy = s.Enemies[0];
            Wet(s, enemy);
            s.Cast(Fire, enemy.Pos);
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "evaporate" }));
            Assert.That(s.HitsOn(enemy), Is.EqualTo(new[] { 5, 2 }));
            Assert.That(IsWet(enemy), Is.False);
        }

        // 젖은 대상이 물 위: 증발은 한 그룹 → 추가 피해 · 로그 한 번, 젖음과 물 타일은 둘 다 사라짐
        [Test]
        public void Evaporate_WetTargetOnWater_OnceButBothRemoved()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            var enemy = s.Enemies[0];
            s.Place(enemy.Pos, TerrainEffectType.Water);
            Wet(s, enemy);
            s.Cast(Fire, enemy.Pos);
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "evaporate" }));
            Assert.That(s.HitsOn(enemy), Is.EqualTo(new[] { 5, 2 }));
            Assert.That(IsWet(enemy), Is.False);
            Assert.That(s.World.Terrain.Has(enemy.Pos, TerrainEffectType.Water), Is.False);
        }

        // #12 증발: 빈 물 칸에 불 → 물 제거
        [Test]
        public void Evaporate_FireOnEmptyWaterCell_RemovesWater()
        {
            var s = Start(new GridPos(1, 2), new GridPos(9, 3));
            var cell = new GridPos(4, 2);
            s.Place(cell, TerrainEffectType.Water);
            s.Place(new GridPos(5, 2), TerrainEffectType.ContaminatedWater);
            s.Cast(Fire, cell);
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "evaporate_water" }));
            Assert.That(s.World.Terrain.Has(cell, TerrainEffectType.Water), Is.False);
            Assert.That(s.World.Terrain.Has(new GridPos(5, 2), TerrainEffectType.ContaminatedWater), Is.True, "불은 멈춘 칸 하나만");
        }

        // #15 오염: 물 웅덩이(9칸)에 독 폭발 → 9칸 모두 오염된 물 (남은 틱 유지). 빈 칸 지형 변환은 4회 상한에 안 셈, 로그 한 줄
        [Test]
        public void Contaminate_PoisonOnPool_AllCellsKeepRemaining()
        {
            var s = Start(new GridPos(1, 2), new GridPos(10, 3));
            for (int x = 4; x <= 6; x++)
            for (int y = 1; y <= 3; y++)
                s.Place(new GridPos(x, y), TerrainEffectType.Water);
            Wait(s, 2); // 남은 틱 12 → 10

            s.Cast(Poison, new GridPos(5, 2));
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "contaminate" }));
            for (int x = 4; x <= 6; x++)
            for (int y = 1; y <= 3; y++)
            {
                var pos = new GridPos(x, y);
                Assert.That(s.World.Terrain.Has(pos, TerrainEffectType.Water), Is.False, $"({x},{y})");
                Assert.That(RemainingAt(s.World, pos, TerrainEffectType.ContaminatedWater), Is.EqualTo(9), $"({x},{y}) 10 유지 → ③에서 9");
            }
        }

        // #9 증발: 화염 지대 위 대상에 물 → +2, 지대 유지. 화염 지대 칸에는 물 타일이 깔리지 않음
        [Test]
        public void WaterOnFireZone_EvaporatesAndLeavesNoWaterThere()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            EquipWater(s);
            var enemy = s.Enemies[0];
            s.Place(enemy.Pos, TerrainEffectType.FireZone);
            s.Cast(Water, enemy.Pos);
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "evaporate_fire_zone" }));
            Assert.That(s.HitsOn(enemy), Is.EqualTo(new[] { 2 }), "물 피해 0 + 증발 2 (상한 max(0, 3))");
            Assert.That(s.World.Terrain.Has(enemy.Pos, TerrainEffectType.FireZone), Is.True, "지대 유지");
            Assert.That(s.World.Terrain.Has(enemy.Pos, TerrainEffectType.Water), Is.False, "화염 지대 위에는 물이 안 깔림");
            Assert.That(s.World.Terrain.Has(new GridPos(4, 2), TerrainEffectType.Water), Is.True, "옆 칸은 깔림");
        }

        [Test]
        public void TerrainTransform_OnlyOnTerrainReactions()
        {
            Assert.Throws<ArgumentException>(() => new ReactionDef("x", ReactionTrigger.Status, StatusType.Wet, TerrainEffectType.None,
                Element.Poison, 0, transformTerrainTo: TerrainEffectType.ContaminatedWater));
            Assert.Throws<ArgumentException>(() => new ReactionDef("x", ReactionTrigger.Terrain, StatusType.None, TerrainEffectType.Water,
                Element.Poison, 0, consumeTrigger: true, transformTerrainTo: TerrainEffectType.ContaminatedWater));
            Assert.Throws<ArgumentException>(() => new TerrainDef(TerrainEffectType.Water, 12, blockedBy: TerrainEffectType.Water));
        }
    }
}
