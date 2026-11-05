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
    /// 3-21c 특성 (Docs/직업_루멘.md 4번, 결정 2026-11-05 — SPD식 단계): 레벨별 점수 · 단계 · 형태 제한 · 최대 점수 ·
    /// 효과(주문 값 — SpellModifiers 한 곳 · 처치 마나 · 기습 피해 · 명중 · 최대 체력) · 스냅샷 v25 · 에셋 값 (BALANCE.md 21번).
    /// </summary>
    public class TalentTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly ProgressionConfig Progress = new ProgressionConfig(30, 5, 5, 5, 1, 1, 1);
        private static readonly ITargetingRule Targeting = new BresenhamTargetingRule();
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("hero_blond", 20, 3, 8, 30,
            new[] { "light_spear_thrust", "light_spear_throw" }, accuracy: 10, evasion: 5);

        private sealed class Idle : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static readonly string[] All = { "light_spear_thrust", "light_spear_throw", "light_spear_flurry", "light_spear_pierce",
            "light_spear_charge", "light_spear_lance", "light_spear_volley", "light_spear_throw_plus" };

        private static SpellBook Book() => new SpellBook(new[]
        {
            new SpellDef("light_spear_thrust", Element.Neutral, SpellShape.Beam, 2, 4, 2, boostWeaponId: "spear", boostDamagePercent: 150, boostRange: 1),
            new SpellDef("light_spear_throw", Element.Neutral, SpellShape.Bolt, 5, 4, 3),
            new SpellDef("light_spear_flurry", Element.Neutral, SpellShape.Bolt, 1, 2, 3, hitCount: 3),
            new SpellDef("light_spear_pierce", Element.Neutral, SpellShape.Beam, 3, 5, 3),
            new SpellDef("light_spear_charge", Element.Neutral, SpellShape.Dash, 4, 6, 4),
            new SpellDef("light_spear_lance", Element.Neutral, SpellShape.Beam, 7, 5, 4),
            new SpellDef("light_spear_volley", Element.Neutral, SpellShape.Multi, 6, 4, 5, maxTargets: 3),
            new SpellDef("light_spear_throw_plus", Element.Neutral, SpellShape.Bolt, 7, 4, 2),
        }, SpellTests.BalanceStatuses(), SpellTests.BalanceTerrains());

        private static TalentDef T(string id, int tier, string form, int max, params TalentEffectDef[] effects) => new TalentDef(id, tier, max, effects, form);
        private static TalentEffectDef E(TalentEffect effect, int[] values, string[] spells = null, string weapon = null, StatusType status = StatusType.None) =>
            new TalentEffectDef(effect, values, spells, weapon, status);

        // BALANCE.md 21번 특성 표 (12개) — 선택 층 1 (테스트 세계는 1층)
        private static ClassBook Classes() => new ClassBook(new[]
        {
            new ClassDef("lumen", 1, new[] { new ClassFormDef("melee"), new ClassFormDef("ranged") },
                new[] { new TalentTier(2, 6), new TalentTier(7, 12) },
                new[]
                {
                    T("light_mastery", 1, null, 2, E(TalentEffect.SpellDamage, new[] { 1, 2 }, All)),
                    T("mana_cycle", 1, null, 2, E(TalentEffect.SpellKillMana, new[] { 1, 2 }, All)),
                    T("spear_handling", 1, null, 2, E(TalentEffect.WeaponAccuracy, new[] { 2, 4 }, weapon: "spear")),
                    T("light_blessing", 1, null, 2, E(TalentEffect.MaxHp, new[] { 3, 6 })),
                    T("endless_thrust", 2, "melee", 3, E(TalentEffect.SpellHitCount, new[] { 1, 2, 3 }, new[] { "light_spear_flurry" })),
                    T("dash", 2, "melee", 3, E(TalentEffect.SpellRange, new[] { 1, 2, 2 }, new[] { "light_spear_charge" }),
                        E(TalentEffect.SpellStatusChance, new[] { 0, 0, 100 }, new[] { "light_spear_charge" }, status: StatusType.Slow)),
                    T("light_rain", 2, "ranged", 3, E(TalentEffect.SpellMaxTargets, new[] { 1, 2, 3 }, new[] { "light_spear_volley" })),
                    T("light_hand", 2, "ranged", 3, E(TalentEffect.SpellManaCost, new[] { -1, -1, -1 }, new[] { "light_spear_throw_plus" }),
                        E(TalentEffect.SpellRange, new[] { 0, 1, 2 }, new[] { "light_spear_throw_plus" })),
                    T("shadow_hunt", 2, "ranged", 3, E(TalentEffect.UnseenDamagePercent, new[] { 25, 50, 75 }, All)),
                }),
        });

        private sealed class Session
        {
            public World World;
            public SpellBook Book;
            public ClassBook Classes;
            public TurnClock Clock;
            public Unit Player;
        }

        private static Session Start(int level = 1)
        {
            var grid = GridMovementTests.Parse(
                "############",
                "#..........#",
                "#..........#",
                "#..........#",
                "############");
            var world = new World(grid, 4UL);
            var s = new Session { World = world, Book = Book(), Classes = Classes(), Clock = new TurnClock(world.Scheduler, world.PlayerInput) };
            var vision = world.InstallCoreSystems(Sight, Magic, s.Book.Statuses, s.Book.Terrains);
            world.SetClassRules(s.Classes);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(1, 2), Magic.QuickSlotCount, Magic.InventorySlotCount);
            ClassForms.SetClass(world, "lumen");
            int exp = 0;
            for (int l = 1; l < level; l++) exp += 5 + 5 * l; // BALANCE 12번: 다음 레벨 = 5 + 5 × 레벨
            if (exp > 0) Progression.GainExp(world, s.Player, exp, Progress);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static SpellDef Effective(Session s, string id)
        {
            s.Book.TryGet(id, out var def);
            return SpellModifiers.For(s.World, s.Player, def);
        }

        // ── 점수 ──

        // 레벨업 1번 = 1점: 1단계 레벨 2~6 (5점), 2단계 7~12 (6점)
        [Test]
        public void Points_ComeFromLevelRanges()
        {
            var s1 = Start(1);
            Assert.That((Talents.Unspent(s1.World, s1.Classes, s1.Player, 1), Talents.Unspent(s1.World, s1.Classes, s1.Player, 2)), Is.EqualTo((0, 0)));
            var s3 = Start(3);
            Assert.That(s3.Player.Level, Is.EqualTo(3));
            Assert.That((Talents.Unspent(s3.World, s3.Classes, s3.Player, 1), Talents.Unspent(s3.World, s3.Classes, s3.Player, 2)), Is.EqualTo((2, 0)));
            var s8 = Start(8);
            Assert.That((Talents.Unspent(s8.World, s8.Classes, s8.Player, 1), Talents.Unspent(s8.World, s8.Classes, s8.Player, 2)), Is.EqualTo((5, 2)));
            Assert.That(new TalentTier(2, 6).PointsAt(30), Is.EqualTo(5));
        }

        // 점수가 없으면 못 찍음 · 최대 체력 특성은 바로 오름(+3 · +6) · 최대 점수 넘으면 못 찍음 · 점수가 줄어듦
        [Test]
        public void Spend_MaxHp_NoPoints_Maxed()
        {
            var low = Start(1);
            Assert.That(Talents.Spend(low.World, low.Classes, low.Player, "light_blessing"), Is.EqualTo(SpendTalentResult.NoPoints));
            var s = Start(4); // 1단계 3점
            int maxHp = s.Player.MaxHp, hp = s.Player.Hp;
            var spent = new List<TalentSpentEvent>();
            s.World.Events.Subscribe<TalentSpentEvent>(spent.Add);
            Assert.That(Talents.Spend(s.World, s.Classes, s.Player, "light_blessing"), Is.EqualTo(SpendTalentResult.Spent));
            Assert.That((s.Player.MaxHp, s.Player.Hp), Is.EqualTo((maxHp + 3, hp + 3)));
            Assert.That(Talents.Spend(s.World, s.Classes, s.Player, "light_blessing"), Is.EqualTo(SpendTalentResult.Spent));
            Assert.That(s.Player.MaxHp, Is.EqualTo(maxHp + 6), "2점 = 누적 6");
            Assert.That(Talents.Spend(s.World, s.Classes, s.Player, "light_blessing"), Is.EqualTo(SpendTalentResult.Maxed));
            Assert.That(Talents.Unspent(s.World, s.Classes, s.Player, 1), Is.EqualTo(1));
            Assert.That(Talents.Spend(s.World, s.Classes, s.Player, "nope"), Is.EqualTo(SpendTalentResult.UnknownTalent));
            s.World.Events.Flush();
            Assert.That(spent.ConvertAll(e => (e.TalentId, e.Points, e.MaxPoints)), Is.EqualTo(new[] { ("light_blessing", 1, 2), ("light_blessing", 2, 2) }));
        }

        // 2단계: 형태를 고르기 전 · 다른 형태 특성은 못 찍음, 형태를 고르면 그 형태 특성만
        [Test]
        public void TierTwo_NeedsChosenForm()
        {
            var s = Start(8);
            Assert.That(Talents.Spend(s.World, s.Classes, s.Player, "endless_thrust"), Is.EqualTo(SpendTalentResult.WrongForm), "형태 전");
            ClassForms.Choose(s.World, s.Classes, s.Book, s.Player, "ranged");
            Assert.That(Talents.Spend(s.World, s.Classes, s.Player, "endless_thrust"), Is.EqualTo(SpendTalentResult.WrongForm), "근접형 특성");
            Assert.That(Talents.Spend(s.World, s.Classes, s.Player, "light_rain"), Is.EqualTo(SpendTalentResult.Spent));
            Assert.That(Talents.Unspent(s.World, s.Classes, s.Player, 2), Is.EqualTo(1));
            var ids = Talents.Available(s.World, s.Classes).ConvertAll(t => t.Id);
            Assert.That(ids, Is.EqualTo(new[] { "light_mastery", "mana_cycle", "spear_handling", "light_blessing", "light_rain", "light_hand", "shadow_hunt" }));
        }

        // ── 효과 ──

        // 주문 값 = 특성 더하기 → 창 강화 (미리보기 · 시전 공용 SpellModifiers)
        [Test]
        public void SpellEffects_AddThenWeaponBoost()
        {
            var s = Start(12);
            ClassForms.Choose(s.World, s.Classes, s.Book, s.Player, "melee");
            Assert.That(Effective(s, "light_spear_thrust").Damage, Is.EqualTo(4), "특성 없음 = 그대로");
            Talents.Spend(s.World, s.Classes, s.Player, "light_mastery");
            Assert.That(Effective(s, "light_spear_thrust").Damage, Is.EqualTo(5));
            Talents.Spend(s.World, s.Classes, s.Player, "endless_thrust");
            Assert.That(Effective(s, "light_spear_flurry").HitCount, Is.EqualTo(4));
            for (int i = 0; i < 3; i++) Talents.Spend(s.World, s.Classes, s.Player, "dash");
            var charge = Effective(s, "light_spear_charge");
            Assert.That((charge.Range, charge.AppliesStatus, charge.StatusChancePercent), Is.EqualTo((6, StatusType.Slow, 100)), "질주 3점: 거리 +2 · 둔화");

            Inventory.TryAdd(s.World, s.Book, WeaponTests.Book(), s.Player, InventoryRef.Item("spear"));
            Weapons.Equip(s.World, WeaponTests.Book(), s.Player, System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("spear")));
            var thrust = Effective(s, "light_spear_thrust");
            Assert.That((thrust.Damage, thrust.Range), Is.EqualTo((7, 3)), "(4 + 1) × 150% = 7, 사거리 2 + 1");
        }

        // 원거리형: 빛의 비 대상 +1 · 가벼운 손 마나 2 → 1 · 사거리 +1 (2점)
        [Test]
        public void RangedTalents_TargetsManaRange()
        {
            var s = Start(12);
            ClassForms.Choose(s.World, s.Classes, s.Book, s.Player, "ranged");
            Talents.Spend(s.World, s.Classes, s.Player, "light_rain");
            Assert.That(Effective(s, "light_spear_volley").MaxTargets, Is.EqualTo(4));
            Talents.Spend(s.World, s.Classes, s.Player, "light_hand");
            Assert.That((Effective(s, "light_spear_throw_plus").ManaCost, Effective(s, "light_spear_throw_plus").Range), Is.EqualTo((1, 7)), "1점: 마나만");
            Talents.Spend(s.World, s.Classes, s.Player, "light_hand");
            Assert.That((Effective(s, "light_spear_throw_plus").ManaCost, Effective(s, "light_spear_throw_plus").Range), Is.EqualTo((1, 8)));
        }

        // 마나 순환: 빛의 창으로 적을 쓰러뜨리면 마나 +1 (던지기 3 − 1 = 2만 줄어듦)
        [Test]
        public void ManaCycle_RestoresOnKill()
        {
            var s = Start(2);
            Talents.Spend(s.World, s.Classes, s.Player, "mana_cycle");
            var enemy = UnitSpawner.SpawnEnemy(s.World, new UnitDef("enemy_melee", 4, 2, 6), new GridPos(4, 2), new Idle());
            s.World.Events.Flush();
            int mana = s.Player.Mana;
            var block = SpellCasting.TryCreateCastSpell(s.World, Config, s.Book, Targeting, Sight, "light_spear_throw", enemy.Pos, out var cmd);
            Assert.That(block, Is.EqualTo(CastBlock.None));
            s.Clock.SubmitPlayerCommand(cmd);
            s.World.Events.Flush();
            Assert.That(enemy.IsAlive, Is.False);
            Assert.That(s.Player.Mana, Is.EqualTo(mana - 3 + 1));
        }

        // 그림자 사냥: 나를 못 본 적(기습 판정)에게 피해 × 1.25 (4 → 5)
        [Test]
        public void ShadowHunt_BonusOnUnawareEnemy()
        {
            var s = Start(12);
            ClassForms.Choose(s.World, s.Classes, s.Book, s.Player, "ranged");
            Talents.Spend(s.World, s.Classes, s.Player, "shadow_hunt");
            var enemy = UnitSpawner.SpawnEnemy(s.World, new UnitDef("enemy_melee", 50, 2, 6), new GridPos(5, 2), new Idle());
            s.World.Events.Flush();
            Assert.That(enemy.LastSeenTargetPos.HasValue, Is.False, "가만히 있는 적 = 플레이어를 기억하지 않음");
            var block = SpellCasting.TryCreateCastSpell(s.World, Config, s.Book, Targeting, Sight, "light_spear_throw", enemy.Pos, out var cmd); // 테스트 형태 표는 배우는 주문이 없어 던지기 그대로
            Assert.That(block, Is.EqualTo(CastBlock.None));
            s.Clock.SubmitPlayerCommand(cmd);
            s.World.Events.Flush();
            Assert.That(enemy.Hp, Is.EqualTo(50 - 5));
        }

        // 창 다루기: 창을 들 때만 명중 +2 / +4, 플레이어 진영만
        [Test]
        public void SpearHandling_AccuracyOnlyWithSpear()
        {
            var s = Start(3);
            Talents.Spend(s.World, s.Classes, s.Player, "spear_handling");
            Assert.That(Talents.AccuracyBonus(s.World, s.Player), Is.EqualTo(0), "맨손");
            Inventory.TryAdd(s.World, s.Book, WeaponTests.Book(), s.Player, InventoryRef.Item("spear"));
            Weapons.Equip(s.World, WeaponTests.Book(), s.Player, System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("spear")));
            Assert.That(Talents.AccuracyBonus(s.World, s.Player), Is.EqualTo(2));
            Talents.Spend(s.World, s.Classes, s.Player, "spear_handling");
            Assert.That(Talents.AccuracyBonus(s.World, s.Player), Is.EqualTo(4));
        }

        // 스냅샷 v25: 찍은 점수 왕복 (처음 찍은 순서)
        [Test]
        public void Snapshot_RoundTripsTalents()
        {
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.GreaterThanOrEqualTo(25), "3-21c (정확한 값은 SiaTests — 26)");
            var s = Start(4);
            Talents.Spend(s.World, s.Classes, s.Player, "mana_cycle");
            Talents.Spend(s.World, s.Classes, s.Player, "light_mastery");
            Talents.Spend(s.World, s.Classes, s.Player, "mana_cycle");
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(s.World.Capture())), (w, id) => null);
            Assert.That(new List<TalentPoint>(restored.Run.Talents).ConvertAll(t => (t.TalentId, t.Points)),
                Is.EqualTo(new[] { ("mana_cycle", 2), ("light_mastery", 1) }));
            ClassForms.SetClass(restored, "lumen");
            Assert.That(restored.Run.Talents, Is.Empty, "새 판(직업 정하기)이면 지움");
        }

        // ── 검산 ──

        // BALANCE.md 21번 특성 표 = ClassCatalog 루멘 줄
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ClassCatalogAsset>("Assets/_Project/Data/Units/ClassCatalog.asset");
            Assert.That(catalog.ToBook().TryGet("lumen", out var lumen), Is.True);
            Assert.That(lumen.Tiers.Count, Is.EqualTo(2));
            Assert.That((lumen.Tiers[0].FirstLevel, lumen.Tiers[0].LastLevel, lumen.Tiers[1].FirstLevel, lumen.Tiers[1].LastLevel), Is.EqualTo((2, 6, 7, 12)));
            string Row(TalentDef t)
            {
                var parts = new List<string>();
                foreach (var e in t.Effects) parts.Add($"{e.Effect}[{string.Join(",", e.Values)}]{e.WeaponId}{(e.Status != StatusType.None ? e.Status.ToString() : "")}");
                return $"{t.Id}:{t.Tier}:{t.FormId}:{t.MaxPoints}:{string.Join(" ", parts)}";
            }
            var rows = new List<string>();
            foreach (var t in lumen.Talents) rows.Add(Row(t));
            Assert.That(rows, Is.EqualTo(new[]
            {
                "light_mastery:1::2:SpellDamage[1,2]",
                "mana_cycle:1::2:SpellKillMana[1,2]",
                "spear_handling:1::2:WeaponAccuracy[2,4]spear",
                "light_blessing:1::2:MaxHp[3,6]",
                "endless_thrust:2:melee:3:SpellHitCount[1,2,3]",
                "long_blade:2:melee:3:SpellRange[1,2,3]",
                "dash:2:melee:3:SpellRange[1,2,2] SpellStatusChance[0,0,100]Slow",
                "spear_momentum:2:melee:3:HitManaChance[20,40,60]",
                "light_rain:2:ranged:3:SpellMaxTargets[1,2,3]",
                "piercing_light:2:ranged:3:SpellDamage[1,2,3]",
                "light_hand:2:ranged:3:SpellManaCost[-1,-1,-1] SpellRange[0,1,2]",
                "shadow_hunt:2:ranged:3:UnseenDamagePercent[25,50,75]",
            }));
            Assert.That(lumen.TalentOf("light_mastery").Effects[0].SpellIds, Is.EqualTo(All), "빛의 숙련 = 빛의 창 기술 8개");
        }
    }
}
