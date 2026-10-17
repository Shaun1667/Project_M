using System;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Tests
{
    /// <summary>
    /// 3-3b 번개 계열: 감전(젖음 × 번개 — 사슬, 인원 제한 없음) · 수면 감전(물 칸 × 번개 — 8방향 8칸 · 4명).
    /// 전이 피해 = 첫 대상이 받은 피해 × 50% 내림, 젖은 플레이어도 맞는다 (결정 2026-10-18).
    /// </summary>
    public class ShockTests
    {
        private const int Lightning = 2;

        // 방: x 1~10, y 1~3. 번개는 Beam 사거리 5 — (1,2)에서 오른쪽으로 쏘면 (2,2) ~ (6,2)
        private static ReactionTests.Session Start(GridPos player, params GridPos[] enemies) =>
            ReactionTests.Start(ReactionTests.Book(), player, enemies);

        private static void Wet(ReactionTests.Session s, Unit unit)
        {
            s.Book.Statuses.TryGet(StatusType.Wet, out var def);
            StatusEffects.Apply(s.World, unit, def);
        }

        // #5 감전: 맞은 적 +2, 거기서 3칸 안 젖은 유닛 → 다시 3칸 안 … (5 × 50% = 2). 마른 유닛 · 끊긴 유닛은 안 맞음
        [Test]
        public void Shock_ChainsThroughWetUnits_NoLimit()
        {
            var s = Start(new GridPos(1, 2),
                new GridPos(4, 2),   // 0 맞은 적
                new GridPos(7, 3),   // 1 0에서 3칸
                new GridPos(10, 3),  // 2 1에서 3칸 (0에서 6칸)
                new GridPos(10, 1),  // 3 2에서 2칸
                new GridPos(5, 1));  // 4 마름
            for (int i = 0; i < 4; i++) Wet(s, s.Enemies[i]);
            s.Cast(Lightning, s.Enemies[0].Pos);
            s.World.Events.Flush();

            Assert.That(s.Reactions, Is.EqualTo(new[] { "shock" }));
            Assert.That(s.HitsOn(s.Enemies[0]), Is.EqualTo(new[] { 3, 2 }));
            Assert.That(s.HitsOn(s.Enemies[1]), Is.EqualTo(new[] { 2 }));
            Assert.That(s.HitsOn(s.Enemies[2]), Is.EqualTo(new[] { 2 }), "사슬로 멀리까지");
            Assert.That(s.HitsOn(s.Enemies[3]), Is.EqualTo(new[] { 2 }));
            Assert.That(s.HitsOn(s.Enemies[4]), Is.Empty, "젖지 않음");
        }

        [Test]
        public void Shock_GapOverThreeCells_Stops()
        {
            var s = Start(new GridPos(1, 2), new GridPos(4, 2), new GridPos(8, 3));
            Wet(s, s.Enemies[0]);
            Wet(s, s.Enemies[1]);
            s.Cast(Lightning, s.Enemies[0].Pos);
            s.World.Events.Flush();
            Assert.That(s.HitsOn(s.Enemies[1]), Is.Empty, "4칸 떨어짐");
        }

        // Q7: 젖은 시전자도 사슬에 맞는다
        [Test]
        public void Shock_ReachesWetCaster()
        {
            var s = Start(new GridPos(1, 2), new GridPos(3, 2));
            Wet(s, s.Player);
            Wet(s, s.Enemies[0]);
            s.Cast(Lightning, s.Enemies[0].Pos);
            s.World.Events.Flush();
            Assert.That(s.HitsOn(s.Player), Is.EqualTo(new[] { 2 }));
        }

        // 번개가 직접 맞힌 젖은 적은 각자 감전(+2)하고, 서로의 전이 피해는 받지 않는다
        [Test]
        public void Shock_DirectVictims_NoChainDamageOnEachOther()
        {
            var s = Start(new GridPos(1, 2), new GridPos(3, 2), new GridPos(5, 2));
            Wet(s, s.Enemies[0]);
            Wet(s, s.Enemies[1]);
            s.Cast(Lightning, s.Enemies[0].Pos);
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "shock", "shock" }));
            Assert.That(s.HitsOn(s.Enemies[0]), Is.EqualTo(new[] { 3, 2 }));
            Assert.That(s.HitsOn(s.Enemies[1]), Is.EqualTo(new[] { 3, 2 }));
        }

        // 물 칸 (3,2)와 아래 줄 (3,3) ~ (10,3). 놓은 직후라 유닛은 아직 젖지 않음 → 수면 감전만
        private static void Pool(ReactionTests.Session s, TerrainEffectType type)
        {
            s.Place(new GridPos(3, 2), type);
            for (int x = 3; x <= 10; x++) s.Place(new GridPos(x, 3), type);
        }

        // #14 수면 감전: 8방향으로 연결된 물 칸 8칸까지, 그 위 유닛 4명까지 3 × 50% = 1
        [Test]
        public void ShockWater_FloodsConnectedWater_FourUnits()
        {
            var s = Start(new GridPos(1, 2), new GridPos(4, 3), new GridPos(5, 3), new GridPos(6, 3), new GridPos(7, 3), new GridPos(8, 3));
            Pool(s, TerrainEffectType.Water);
            s.Cast(Lightning, new GridPos(3, 2));
            s.World.Events.Flush();

            Assert.That(s.Reactions, Is.EqualTo(new[] { "shock_water" }));
            for (int i = 0; i < 4; i++) Assert.That(s.HitsOn(s.Enemies[i]), Is.EqualTo(new[] { 1 }), "적 " + i);
            Assert.That(s.HitsOn(s.Enemies[4]), Is.Empty, "4명까지");
        }

        [Test]
        public void ShockWater_StopsAfterEightCells()
        {
            // 칸 순서: (3,2) (3,3) (4,3) … (9,3) = 8칸 → (10,3)은 못 감
            var s = Start(new GridPos(1, 2), new GridPos(9, 3), new GridPos(10, 3));
            Pool(s, TerrainEffectType.Water);
            s.Cast(Lightning, new GridPos(3, 2));
            s.World.Events.Flush();
            Assert.That(s.HitsOn(s.Enemies[0]), Is.EqualTo(new[] { 1 }));
            Assert.That(s.HitsOn(s.Enemies[1]), Is.Empty);
        }

        [Test]
        public void ShockContaminated_SameAsWater()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 3));
            Pool(s, TerrainEffectType.ContaminatedWater);
            s.Cast(Lightning, new GridPos(3, 2));
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "shock_contaminated" }));
            Assert.That(s.HitsOn(s.Enemies[0]), Is.EqualTo(new[] { 1 }));
        }

        // 보충: 젖은 유닛이 물 위 → 감전 · 수면 감전 중 감전 하나 (한 그룹)
        [Test]
        public void WetUnitOnWater_ShockOnly()
        {
            var s = Start(new GridPos(1, 2), new GridPos(3, 2));
            s.Place(new GridPos(3, 2), TerrainEffectType.Water);
            Wet(s, s.Enemies[0]);
            s.Cast(Lightning, s.Enemies[0].Pos);
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "shock" }));
            Assert.That(s.HitsOn(s.Enemies[0]), Is.EqualTo(new[] { 3, 2 }));
        }

        [Test]
        public void Spread_RejectsBadDefinitions()
        {
            Assert.Throws<ArgumentException>(() => new ReactionDef("x", ReactionTrigger.Terrain, StatusType.None, TerrainEffectType.Water,
                Element.Lightning, 0, spread: ReactionSpread.StatusChain, spreadRange: 3), "사슬은 상태 반응만");
            Assert.Throws<ArgumentException>(() => new ReactionDef("x", ReactionTrigger.Status, StatusType.Wet, TerrainEffectType.None,
                Element.Lightning, 0, spread: ReactionSpread.TerrainFlood, spreadRange: 8), "지형 전이는 지형 반응만");
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReactionDef("x", ReactionTrigger.Status, StatusType.Wet, TerrainEffectType.None,
                Element.Lightning, 0, spread: ReactionSpread.StatusChain, spreadRange: 3, spreadDamagePercent: 100), "전이 피해 < 첫 대상 피해");
            Assert.Throws<ArgumentException>(() => new ReactionDef("x", ReactionTrigger.Status, StatusType.Wet, TerrainEffectType.None,
                Element.Lightning, 0, consumeTrigger: true, spread: ReactionSpread.StatusChain, spreadRange: 3), "소모와 함께 못 씀");
        }
    }
}
