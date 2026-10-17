using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Tests
{
    /// <summary>3-3a 얼음 계열: 빙결 · 결빙 · 빙판 · 해동 · 파쇄 (REACTIONS.md 3번 · BALANCE.md 8번). 빙결 면역 없음 (결정 2026-10-18).</summary>
    public class IceTests
    {
        private const int Fire = 0, Ice = 1, Lightning = 2;
        private static readonly GameConfig WaitConfig = new GameConfig(1, 1f);

        private static ReactionTests.Session Start(GridPos player, params GridPos[] enemies) =>
            ReactionTests.Start(ReactionTests.Book(), player, enemies);

        private static void Apply(ReactionTests.Session s, Unit unit, StatusType type)
        {
            s.Book.Statuses.TryGet(type, out var def);
            StatusEffects.Apply(s.World, unit, def);
        }

        private static bool Has(Unit unit, StatusType type) => unit.Statuses.Exists(x => x.Type == type);

        private static int RemainingAt(World world, GridPos pos, TerrainEffectType type)
        {
            foreach (var t in world.Terrain.At(pos))
                if (t.Type == type) return t.Remaining;
            return 0;
        }

        private static void Wait(ReactionTests.Session s, int turns)
        {
            for (int i = 0; i < turns; i++) s.Clock.SubmitPlayerCommand(ProjectM.Units.PlayerActions.Wait(WaitConfig));
            s.World.Events.Flush();
        }

        // #4 빙결: 젖은 대상 × 얼음 → 젖음 해제 + 빙결 확정
        [Test]
        public void Freeze_WetTargetHitByIce()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            var enemy = s.Enemies[0];
            Apply(s, enemy, StatusType.Wet);
            s.Cast(Ice, enemy.Pos);
            s.World.Events.Flush();

            Assert.That(s.Reactions, Is.EqualTo(new[] { "freeze" }));
            Assert.That(s.HitsOn(enemy), Is.EqualTo(new[] { 3 }), "추가 피해 없음");
            Assert.That(Has(enemy, StatusType.Frozen), Is.True);
            Assert.That(Has(enemy, StatusType.Wet), Is.False);
        }

        // 빙결 2틱 = 행동 불가 (마비와 같은 데이터 규칙), 풀린 뒤 다시 걸 수 있다 (면역 없음)
        [Test]
        public void Frozen_WearsOffAfterTwoTicks_NoImmunity()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            var enemy = s.Enemies[0];
            Apply(s, enemy, StatusType.Frozen);
            Wait(s, 2);
            Assert.That(Has(enemy, StatusType.Frozen), Is.False);

            Apply(s, enemy, StatusType.Wet);
            s.Cast(Ice, enemy.Pos);
            s.World.Events.Flush();
            Assert.That(Has(enemy, StatusType.Frozen), Is.True, "풀리자마자 다시 빙결");
        }

        // #6 해동: 빙결 × 불 → 빙결 해제 + 1
        [Test]
        public void Thaw_FireOnFrozen()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            var enemy = s.Enemies[0];
            Apply(s, enemy, StatusType.Frozen);
            s.Cast(Fire, enemy.Pos);
            s.World.Events.Flush();

            Assert.That(s.Reactions, Is.EqualTo(new[] { "thaw" }));
            Assert.That(s.HitsOn(enemy), Is.EqualTo(new[] { 5, 1 }));
            Assert.That(Has(enemy, StatusType.Frozen), Is.False);
        }

        // #7 파쇄: 빙결 × 번개 → 빙결 해제 + 3 (상한 max(3, 3))
        [Test]
        public void Shatter_LightningOnFrozen()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            var enemy = s.Enemies[0];
            Apply(s, enemy, StatusType.Frozen);
            s.Cast(Lightning, enemy.Pos);
            s.World.Events.Flush();

            Assert.That(s.Reactions, Is.EqualTo(new[] { "shatter" }));
            Assert.That(s.HitsOn(enemy), Is.EqualTo(new[] { 3, 3 }));
            Assert.That(Has(enemy, StatusType.Frozen), Is.False);
        }

        // #13 결빙: 물 위 젖은 대상 × 얼음 → 빙결은 한 번(그룹), 물은 빙판 8틱으로
        [Test]
        public void FreezeWater_WetTargetOnWater_OnceAndPoolFreezes()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            var enemy = s.Enemies[0];
            s.Place(enemy.Pos, TerrainEffectType.Water);
            Wait(s, 1); // 물 위 → 젖음
            Assert.That(Has(enemy, StatusType.Wet), Is.True);

            s.Cast(Ice, enemy.Pos);
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "freeze" }), "결빙 줄은 같은 그룹이라 로그 · 상태 없이 변환만");
            Assert.That(Has(enemy, StatusType.Frozen), Is.True);
            Assert.That(s.World.Terrain.Has(enemy.Pos, TerrainEffectType.Water), Is.False);
            Assert.That(RemainingAt(s.World, enemy.Pos, TerrainEffectType.IcePatch), Is.EqualTo(7), "8틱, 시전 틱의 ③에서 1 줄어듦");
        }

        // 결빙: 젖지 않은 유닛이 물 위(놓은 직후)여도 결빙 줄이 빙결을 건다
        [Test]
        public void FreezeWater_UnitOnWater_Frozen()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            var enemy = s.Enemies[0];
            s.Place(enemy.Pos, TerrainEffectType.Water); // 아직 ③ 전 — 젖음 없음
            Assert.That(Has(enemy, StatusType.Wet), Is.False);

            s.Cast(Ice, enemy.Pos);
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "freeze_water" }));
            Assert.That(Has(enemy, StatusType.Frozen), Is.True);
            Assert.That(s.World.Terrain.Has(enemy.Pos, TerrainEffectType.IcePatch), Is.True);
        }

        // 빈 물 칸 · 오염된 물 칸에 얼음 → 빙판 (지형만 바꾸는 줄)
        [Test]
        public void FreezeWater_EmptyCells_BecomeIcePatch()
        {
            var s = Start(new GridPos(1, 2), new GridPos(9, 3));
            var water = new GridPos(4, 2);
            s.Place(water, TerrainEffectType.Water);
            s.Cast(Ice, water);
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "freeze_water" }));
            Assert.That(RemainingAt(s.World, water, TerrainEffectType.IcePatch), Is.EqualTo(7));

            var dirty = new GridPos(4, 1);
            s.Place(dirty, TerrainEffectType.ContaminatedWater);
            s.Cast(Ice, dirty);
            s.World.Events.Flush();
            Assert.That(s.Reactions, Is.EqualTo(new[] { "freeze_contaminated" }));
            Assert.That(s.World.Terrain.Has(dirty, TerrainEffectType.ContaminatedWater), Is.False);
            Assert.That(s.World.Terrain.Has(dirty, TerrainEffectType.IcePatch), Is.True);
        }

        // #16 해동: 빙판 × 불 → 물 타일 12틱
        [Test]
        public void ThawIce_FireOnIcePatch_BecomesWater()
        {
            var s = Start(new GridPos(1, 2), new GridPos(9, 3));
            var cell = new GridPos(4, 2);
            s.Place(cell, TerrainEffectType.IcePatch);
            s.Cast(Fire, cell);
            s.World.Events.Flush();

            Assert.That(s.Reactions, Is.EqualTo(new[] { "thaw_ice" }));
            Assert.That(s.World.Terrain.Has(cell, TerrainEffectType.IcePatch), Is.False);
            Assert.That(RemainingAt(s.World, cell, TerrainEffectType.Water), Is.EqualTo(11));
        }

        // 빙판 위 유닛은 매 턴 둔화 (D4 100%)
        [Test]
        public void IcePatch_SlowsUnitOnIt()
        {
            var s = Start(new GridPos(1, 2), new GridPos(5, 2));
            var enemy = s.Enemies[0];
            s.Place(enemy.Pos, TerrainEffectType.IcePatch);
            Wait(s, 1);
            Assert.That(Has(enemy, StatusType.Slow), Is.True);
        }

        [Test]
        public void Reaction_RejectsBadTargetStatusAndDuration()
        {
            var statuses = SpellTests.BalanceStatuses();
            Assert.Throws<System.ArgumentException>(() => new ReactionDef("x", ReactionTrigger.Status, StatusType.Wet, TerrainEffectType.None,
                ProjectM.Magic.Element.Ice, 0, transformDurationTicks: 8), "변환 없이 지속만");
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ReactionDef("x", ReactionTrigger.Status, StatusType.Wet, TerrainEffectType.None,
                ProjectM.Magic.Element.Ice, 0, targetStatus: StatusType.Frozen, targetStatusChancePercent: 101));
            Assert.Throws<System.ArgumentException>(() => new ReactionBook(new[]
            {
                new ReactionDef("x", ReactionTrigger.Status, StatusType.Wet, TerrainEffectType.None, ProjectM.Magic.Element.Ice, 0, targetStatus: StatusType.Frozen),
            }, new ReactionConfig(4, 3, 5), new StatusBook(new[] { new StatusDef(StatusType.Wet, 3) })), "StatusBook에 없는 대상 상태");
        }
    }
}
