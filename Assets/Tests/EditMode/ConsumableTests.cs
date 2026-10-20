using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// 3-9 물약 · 주문서 (결정 2026-10-20 — SPD): 판마다 겉모습 · 쓰면 식별 · 효과 15종 · 던지기(범위 물약만 효과) · 층마다 무작위 배치 · 저장 v16 · 표시.
    /// </summary>
    public class ConsumableTests
    {
        private static readonly GameConfig Config = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 6);
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10, null, accuracy: 10, evasion: 5, strength: 10);
        private static readonly UnitDef DummyDef = new UnitDef("enemy_melee", 200, 2, 6);
        private static readonly ProgressionConfig Progress = new ProgressionConfig(30, 5, 5, 5, 1, 1, 1);

        // y=5(위) → y=0(아래). x=4, y=1~3에 벽 — 가스가 벽을 돌아 퍼지는지 본다
        private static readonly string[] Rows =
        {
            "#########",
            "#.......#",
            "#...#...#",
            "#...#...#",
            "#...#...#",
            "#########",
        };

        private static readonly string[] PotionLooks = { "potion_crimson", "potion_amber", "potion_golden", "potion_jade", "potion_blue", "potion_sky", "potion_navy", "potion_magenta" };
        private static readonly string[] ScrollLooks = { "scroll_tyr", "scroll_feoh", "scroll_ur", "scroll_thorn", "scroll_rad", "scroll_gyfu", "scroll_wynn", "scroll_haegl" };

        // BALANCE 14번 무게
        internal static ItemBook Book() => new ItemBook(new[]
        {
            new ItemDef("shortsword", ItemUse.EquipWeapon, weapon: new WeaponDef(15, 3, 100, 1, 1, 12)),
            new ItemDef("potion_strength", ItemUse.Potion, amount: 1, effect: ConsumableEffect.Strength),
            new ItemDef("scroll_upgrade", ItemUse.Scroll, amount: 1, effect: ConsumableEffect.Upgrade),
            new ItemDef("potion_healing", ItemUse.Potion, effect: ConsumableEffect.Healing, weight: 3),
            new ItemDef("potion_experience", ItemUse.Potion, effect: ConsumableEffect.Experience, weight: 1),
            new ItemDef("potion_purity", ItemUse.Potion, effect: ConsumableEffect.Purity, weight: 2),
            new ItemDef("potion_toxic_gas", ItemUse.Potion, effect: ConsumableEffect.ToxicGas, weight: 3),
            new ItemDef("potion_liquid_flame", ItemUse.Potion, effect: ConsumableEffect.LiquidFlame, weight: 3),
            new ItemDef("potion_frost", ItemUse.Potion, effect: ConsumableEffect.Frost, weight: 3),
            new ItemDef("potion_paralytic_gas", ItemUse.Potion, effect: ConsumableEffect.ParalyticGas, weight: 2),
            new ItemDef("scroll_identify", ItemUse.Scroll, effect: ConsumableEffect.Identify, weight: 3),
            new ItemDef("scroll_magic_mapping", ItemUse.Scroll, effect: ConsumableEffect.MagicMapping, weight: 1),
            new ItemDef("scroll_teleportation", ItemUse.Scroll, effect: ConsumableEffect.Teleportation, weight: 1),
            new ItemDef("scroll_recharging", ItemUse.Scroll, effect: ConsumableEffect.Recharging, weight: 1),
            new ItemDef("scroll_rage", ItemUse.Scroll, effect: ConsumableEffect.Rage, weight: 1),
            new ItemDef("scroll_retribution", ItemUse.Scroll, effect: ConsumableEffect.Retribution, weight: 1),
        }, new WeaponRules(3, 2));

        internal static ConsumableConfig BalanceConfig(int potionsPerFloor = 2, int scrollsPerFloor = 2) => new ConsumableConfig(
            PotionLooks, ScrollLooks, potionsPerFloor, scrollsPerFloor, 2, 3, 8,
            new[] { StatusType.Burn, StatusType.Slow, StatusType.Poison, StatusType.Paralyze },
            new[] { TerrainEffectType.FireZone, TerrainEffectType.PoisonCloud, TerrainEffectType.ParalyticGas },
            10, 22.5f, 4.45f, 4f);

        private sealed class Idle : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;
            public ConsumableRules Rules;
            public readonly List<ConsumableUsedEvent> Used = new List<ConsumableUsedEvent>();
            public readonly List<ItemIdentifiedEvent> Identified = new List<ItemIdentifiedEvent>();
        }

        private static Session Start(GridPos? playerPos = null)
        {
            var world = new World(GridMovementTests.Parse(Rows), 7UL);
            var statuses = SpellTests.BalanceStatuses();
            var terrains = SpellTests.BalanceTerrains();
            var sight = new ShadowcastSightRule();
            var vision = world.InstallCoreSystems(sight, Magic, statuses, terrains, Progress);
            var s = new Session
            {
                World = world,
                Clock = new TurnClock(world.Scheduler, world.PlayerInput),
                Rules = new ConsumableRules(Book(), BalanceConfig(), statuses, terrains, Progress, sight),
            };
            world.Events.Subscribe<ConsumableUsedEvent>(s.Used.Add);
            world.Events.Subscribe<ItemIdentifiedEvent>(s.Identified.Add);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, playerPos ?? new GridPos(1, 1), Magic.QuickSlotCount, Magic.InventorySlotCount);
            Identification.AssignLooks(world, s.Rules.Items, s.Rules.Config);
            vision.Refresh();
            world.Events.Flush();
            return s;
        }

        private static void Give(Session s, string id) => Inventory.TryAdd(s.World, null, s.Rules.Items, s.Player, InventoryRef.Item(id));

        private static int IndexOf(Session s, string id) => System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item(id));

        private static void Use(Session s, string id)
        {
            Give(s, id);
            var entry = InventoryRef.Item(id);
            s.Clock.SubmitPlayerCommand(new UseConsumableCommand(s.World, s.Rules, IndexOf(s, id), entry, 1));
            s.World.Events.Flush();
        }

        private static void Throw(Session s, string id, GridPos target)
        {
            Give(s, id);
            var entry = InventoryRef.Item(id);
            s.Clock.SubmitPlayerCommand(new ThrowItemCommand(s.World, s.Rules, new BresenhamTargetingRule(), IndexOf(s, id), entry, target, 1));
            s.World.Events.Flush();
        }

        private static Unit Enemy(Session s, GridPos pos) => UnitSpawner.SpawnEnemy(s.World, DummyDef, pos, new Idle());

        // ── 식별 ──

        [Test]
        public void AssignLooks_EveryKindGetsDistinctLook_SameSeedSame_UnknownAtStart()
        {
            var a = Start();
            var b = Start();
            var potions = Book().Ids.Where(id => id.StartsWith("potion_")).ToList();
            var looks = potions.Select(id => a.World.Run.LookOf(id)).ToList();
            Assert.That(looks, Is.All.Not.Null);
            Assert.That(looks.Distinct().Count(), Is.EqualTo(potions.Count), "물약마다 다른 색");
            Assert.That(looks, Is.SubsetOf(PotionLooks));
            Assert.That(a.World.Run.Looks.Select(l => (l.ItemId, l.Look)), Is.EqualTo(b.World.Run.Looks.Select(l => (l.ItemId, l.Look))), "같은 시드 = 같은 겉모습");
            Assert.That(a.World.Run.LookOf("shortsword"), Is.Null, "무기는 겉모습 없음");
            Assert.That(a.World.Run.IsKnown("potion_healing"), Is.False);
            Assert.That(a.World.Run.IsKnown("shortsword"), Is.True);
        }

        [Test]
        public void Drink_IdentifiesThatKind_EventCarriesLookAndBeforeAfter()
        {
            var s = Start();
            string look = s.World.Run.LookOf("potion_healing");
            Use(s, "potion_healing");
            Assert.That(s.World.Run.IsIdentified("potion_healing"), Is.True);
            Assert.That(s.Used.Count, Is.EqualTo(1));
            var e = s.Used[0];
            Assert.That((e.ItemId, e.Look, e.Mode, e.WasKnown, e.Known), Is.EqualTo(("potion_healing", look, ConsumeMode.Drink, false, true)));
            Assert.That(IndexOf(s, "potion_healing"), Is.EqualTo(-1), "한 개 빠짐");
        }

        [Test]
        public void IdentifyScroll_IdentifiesFirstUnknownInInventory_Only()
        {
            var s = Start();
            Give(s, "potion_frost");
            Give(s, "potion_healing");
            Use(s, "scroll_identify");
            Assert.That(s.World.Run.IsIdentified("scroll_identify"), Is.True, "읽은 주문서 자신도 식별");
            Assert.That(s.World.Run.IsIdentified("potion_frost"), Is.True, "앞 칸");
            Assert.That(s.World.Run.IsIdentified("potion_healing"), Is.False, "하나만");
            Assert.That(s.Identified.Single().ItemId, Is.EqualTo("potion_frost"));
        }

        // ── 물약 효과 ──

        [Test]
        public void Healing_FullHpAndCuresPoison()
        {
            var s = Start();
            Combat.ApplyDamage(s.World, EntityId.None, s.Player, 12);
            s.Rules.Statuses.TryGet(StatusType.Poison, out var poison);
            StatusEffects.Apply(s.World, s.Player, poison);
            Use(s, "potion_healing");
            Assert.That(s.Player.Hp, Is.EqualTo(s.Player.MaxHp));
            Assert.That(s.Player.Statuses.Any(st => st.Type == StatusType.Poison), Is.False);
        }

        [Test]
        public void Experience_ReachesNextLevel()
        {
            var s = Start();
            Assert.That(s.Player.Level, Is.EqualTo(1));
            Use(s, "potion_experience");
            Assert.That(s.Player.Level, Is.EqualTo(2));
            Assert.That(s.Player.Exp, Is.EqualTo(0));
        }

        [Test]
        public void Drink_ToxicGas_CloudsAtOwnFeet()
        {
            var s = Start();
            Use(s, "potion_toxic_gas");
            Assert.That(s.World.Terrain.Has(new GridPos(1, 1), TerrainEffectType.PoisonCloud), Is.True, "발밑에서 깨짐");
            Assert.That(s.World.Terrain.Has(new GridPos(3, 3), TerrainEffectType.PoisonCloud), Is.True, "2걸음");
            Assert.That(s.World.Terrain.Has(new GridPos(4, 4), TerrainEffectType.PoisonCloud), Is.False, "3걸음 — 밖");
        }

        [Test]
        public void Area_GoesAroundWalls()
        {
            var map = GridMovementTests.Parse(Rows);
            var area = Consumables.Area(map, new GridPos(3, 2), 2);
            Assert.That(area, Has.Member(new GridPos(4, 4)), "벽 위로 돌아 2걸음");
            Assert.That(area, Has.No.Member(new GridPos(5, 2)), "벽 너머 — 돌아가면 3걸음 넘음");
            Assert.That(area, Has.No.Member(new GridPos(4, 2)), "벽 칸");
            Assert.That(Consumables.Area(map, new GridPos(3, 2), 0), Is.EqualTo(new[] { new GridPos(3, 2) }));
        }

        [Test]
        public void Throw_ToxicGas_CloudsAtLanding_Identifies()
        {
            var s = Start();
            Throw(s, "potion_toxic_gas", new GridPos(3, 2));
            Assert.That(s.World.Terrain.Has(new GridPos(3, 2), TerrainEffectType.PoisonCloud), Is.True);
            Assert.That(s.World.Terrain.Has(new GridPos(5, 2), TerrainEffectType.PoisonCloud), Is.False, "벽 너머로 새지 않음");
            Assert.That(s.World.Run.IsIdentified("potion_toxic_gas"), Is.True);
            Assert.That(s.Used.Single().Mode, Is.EqualTo(ConsumeMode.Throw));
            Assert.That(s.Used.Single().At, Is.EqualTo(new GridPos(3, 2)));
        }

        [Test]
        public void Throw_StopsAtUnit()
        {
            var s = Start();
            Enemy(s, new GridPos(2, 1));
            Throw(s, "potion_liquid_flame", new GridPos(3, 1));
            Assert.That(s.Used.Single().At, Is.EqualTo(new GridPos(2, 1)), "가는 길의 유닛 칸에서 깨짐");
            Assert.That(s.World.Terrain.Has(new GridPos(2, 1), TerrainEffectType.FireZone), Is.True);
        }

        [Test]
        public void Throw_Healing_ShattersWithoutEffect_NotIdentified()
        {
            var s = Start();
            var enemy = Enemy(s, new GridPos(3, 1));
            Combat.ApplyDamage(s.World, EntityId.None, enemy, 50);
            Throw(s, "potion_healing", new GridPos(3, 1));
            Assert.That(enemy.Hp, Is.EqualTo(150), "효과 없음");
            Assert.That(s.World.Run.IsIdentified("potion_healing"), Is.False, "SPD: 깨지기만 하면 모름");
            Assert.That(IndexOf(s, "potion_healing"), Is.EqualTo(-1), "그래도 사라짐");
        }

        [Test]
        public void Throw_ScrollIsNotThrowable()
        {
            var s = Start();
            Give(s, "scroll_rage");
            Assert.That(Consumables.CanThrow(s.Rules.Items, InventoryRef.Item("scroll_rage")), Is.False);
            Assert.That(Consumables.Throw(s.World, s.Rules, new BresenhamTargetingRule(), s.Player, IndexOf(s, "scroll_rage"), new GridPos(3, 1)),
                Is.EqualTo(ConsumeResult.NotThrowable));
            Assert.That(IndexOf(s, "scroll_rage"), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Frost_FreezesUnitsInArea_TurnsWaterToIce()
        {
            var s = Start(new GridPos(1, 4)); // 위 줄 왼쪽 — 냉기 범위(4걸음) 밖
            var enemy = Enemy(s, new GridPos(5, 4));
            s.Rules.Terrains.TryGet(TerrainEffectType.Water, out var water);
            TerrainEffects.Place(s.World, new GridPos(6, 4), water);
            Throw(s, "potion_frost", new GridPos(5, 4));
            Assert.That(enemy.Statuses.Any(st => st.Type == StatusType.Frozen), Is.True);
            Assert.That(s.Player.Statuses.Any(st => st.Type == StatusType.Frozen), Is.False, "범위 밖");
            Assert.That(s.World.Terrain.Has(new GridPos(6, 4), TerrainEffectType.IcePatch), Is.True);
            Assert.That(s.World.Terrain.Has(new GridPos(6, 4), TerrainEffectType.Water), Is.False);
        }

        [Test]
        public void ParalyticGas_PlacesGas_ThatParalyzes()
        {
            var s = Start(new GridPos(1, 4)); // 가스 범위 밖에서 던짐 (플레이어가 마비되면 차례가 넘어가며 적의 마비도 풀릴 수 있음)
            var enemy = Enemy(s, new GridPos(6, 4));
            Throw(s, "potion_paralytic_gas", new GridPos(5, 4));
            Assert.That(s.World.Terrain.Has(new GridPos(5, 4), TerrainEffectType.ParalyticGas), Is.True);
            s.Clock.SubmitPlayerCommand(new WaitCommand(1));
            s.World.Events.Flush();
            Assert.That(enemy.Statuses.Any(st => st.Type == StatusType.Paralyze), Is.True, "③ 지형 처리에서 마비");
        }

        [Test]
        public void Purity_ClearsGasAndFire_InRadius()
        {
            var s = Start();
            s.Rules.Terrains.TryGet(TerrainEffectType.PoisonCloud, out var cloud);
            s.Rules.Terrains.TryGet(TerrainEffectType.FireZone, out var fire);
            s.Rules.Terrains.TryGet(TerrainEffectType.Water, out var water);
            TerrainEffects.Place(s.World, new GridPos(2, 2), cloud);
            TerrainEffects.Place(s.World, new GridPos(3, 1), fire);
            TerrainEffects.Place(s.World, new GridPos(1, 3), water);
            Use(s, "potion_purity");
            Assert.That(s.World.Terrain.Has(new GridPos(2, 2), TerrainEffectType.PoisonCloud), Is.False);
            Assert.That(s.World.Terrain.Has(new GridPos(3, 1), TerrainEffectType.FireZone), Is.False);
            Assert.That(s.World.Terrain.Has(new GridPos(1, 3), TerrainEffectType.Water), Is.True, "물은 그대로");
        }

        // ── 주문서 효과 ──

        [Test]
        public void Recharging_FillsMana()
        {
            var s = Start();
            Mana.TrySpend(s.World, s.Player, 7);
            Use(s, "scroll_recharging");
            Assert.That(s.Player.Mana, Is.EqualTo(s.Player.MaxMana));
        }

        [Test]
        public void Teleportation_MovesToFreeFloor()
        {
            var s = Start();
            var from = s.Player.Pos;
            Use(s, "scroll_teleportation");
            Assert.That(s.Player.Pos, Is.Not.EqualTo(from));
            Assert.That(s.World.Map.Get(s.Player.Pos), Is.EqualTo(TileType.Floor));
        }

        [Test]
        public void MagicMapping_ExploresEveryFloorCell()
        {
            var s = Start();
            Use(s, "scroll_magic_mapping");
            var map = s.World.Map;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map.IsWalkable(new GridPos(x, y))) Assert.That(s.World.Explored[y * map.Width + x], Is.True, $"({x},{y})");
        }

        [Test]
        public void Rage_AllEnemiesHeadToReader()
        {
            var s = Start();
            var near = Enemy(s, new GridPos(3, 1));
            var hidden = Enemy(s, new GridPos(7, 1));
            Use(s, "scroll_rage");
            Assert.That(hidden.LastSeenTargetPos, Is.Not.Null, "보이지 않는 적도");
            Assert.That(near.LastSeenTargetPos, Is.Not.Null);
        }

        // 검산 (BALANCE 14번): 최대 20 · 현재 10 → 힘 = min(4, 0.5 × 4.45) = 2.225 → 체력 200인 적 = 200 × 10% + 200 × 2.225 × 22.5% = 120.125 → 120
        [Test]
        public void Retribution_HitsVisibleEnemiesOnly_BalanceValue()
        {
            var s = Start();
            var visible = Enemy(s, new GridPos(3, 1));
            var hidden = Enemy(s, new GridPos(7, 1));
            Combat.ApplyDamage(s.World, EntityId.None, s.Player, 10);
            Use(s, "scroll_retribution");
            Assert.That(visible.Hp, Is.EqualTo(200 - 120));
            Assert.That(hidden.Hp, Is.EqualTo(200), "벽 너머 — 안 보임");
            Assert.That(s.World.Run.Kills, Is.EqualTo(0));
        }

        [Test]
        public void Retribution_FullHp_OnlyBasePart()
        {
            var config = BalanceConfig();
            var world = new World(GridMovementTests.Parse(Rows), 1UL);
            var target = UnitSpawner.SpawnEnemy(world, DummyDef, new GridPos(2, 2), new Idle());
            Assert.That(Consumables.RetributionDamage(config, target, 0f), Is.EqualTo(20), "200 × 10%");
            Assert.That(Consumables.RetributionDamage(config, target, 4f), Is.EqualTo(200), "20 + 200 × 4 × 22.5% = 200");
        }

        // ── 배치 ──

        [Test]
        public void PlanRandomConsumables_PerFloorCounts_NoWeightZero_Deterministic()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var a = new World(GridMovementTests.Parse("###", "#.#", "###"), seed);
                var b = new World(GridMovementTests.Parse("###", "#.#", "###"), seed);
                Loot.PlanRandomConsumables(a, Book(), BalanceConfig(), 5);
                Loot.PlanRandomConsumables(b, Book(), BalanceConfig(), 5);
                var plan = a.Run.PendingFloorItems;
                Assert.That(b.Run.PendingFloorItems, Is.EqualTo(plan), "같은 시드 = 같은 계획");
                for (int floor = 1; floor <= 4; floor++)
                {
                    Assert.That(plan.Count(p => p.Floor == floor && p.ItemId.StartsWith("potion_")), Is.EqualTo(2), $"시드 {seed} {floor}층 물약");
                    Assert.That(plan.Count(p => p.Floor == floor && p.ItemId.StartsWith("scroll_")), Is.EqualTo(2), $"시드 {seed} {floor}층 주문서");
                }
                Assert.That(plan.Any(p => p.Floor == 5), Is.False, "보스 층 제외");
                Assert.That(plan.Any(p => p.ItemId == "potion_strength" || p.ItemId == "scroll_upgrade"), Is.False, "무게 0은 보장 수로만");
            }
        }

        [Test]
        public void PlanRandomConsumables_AfterExistingPlan_KeepsIt()
        {
            var loot = new LootConfig(2, 3, 0, 3, 2);
            var a = new World(GridMovementTests.Parse("###", "#.#", "###"), 9UL);
            var b = new World(GridMovementTests.Parse("###", "#.#", "###"), 9UL);
            Loot.PlanConsumables(a, Book(), loot, 5);
            Loot.PlanConsumables(b, Book(), loot, 5);
            var before = a.Run.PendingFloorItems.ToList();
            Loot.PlanRandomConsumables(b, Book(), BalanceConfig(), 5);
            Assert.That(b.Run.PendingFloorItems.Take(before.Count), Is.EqualTo(before), "앞 계획은 그대로, 뒤에 붙음");
        }

        [Test]
        public void FloorItemPlaced_UsesLook_NotItemId()
        {
            var s = Start();
            var e = new FloorItemPlacedEvent(new GridPos(2, 2), "potion_frost", Identification.VisualOf(s.World, "potion_frost"));
            Assert.That(e.VisualId, Is.EqualTo(s.World.Run.LookOf("potion_frost")));
            Assert.That(new FloorItemPlacedEvent(new GridPos(2, 2), "shortsword").VisualId, Is.EqualTo("shortsword"));
        }

        // ── 저장 · 표시 ──

        [Test]
        public void Snapshot_RoundTripsLooksAndIdentified()
        {
            var s = Start();
            Use(s, "potion_healing");
            var restored = World.Restore(WorldSnapshotCodec.Decode(WorldSnapshotCodec.Encode(s.World.Capture())), (w, id) => new Idle());
            Assert.That(restored.Run.Looks.Select(l => (l.ItemId, l.Look)), Is.EqualTo(s.World.Run.Looks.Select(l => (l.ItemId, l.Look))));
            Assert.That(restored.Run.Identified, Is.EqualTo(new[] { "potion_healing" }));
            Assert.That(WorldSnapshot.CurrentFormatVersion, Is.EqualTo(16));
        }

        [Test]
        public void HudText_UnknownShowsLookName_ThenRealName()
        {
            var s = Start();
            var text = new HudText(itemNames: new Dictionary<string, string> { { "potion_healing", "치유 물약" } }) { Run = s.World.Run };
            string look = s.World.Run.LookOf("potion_healing");
            Assert.That(text.Item("potion_healing"), Is.EqualTo(HudText.LookName(look)));
            Assert.That(text.Item("potion_healing"), Does.EndWith("물약"));
            Assert.That(text.ConsumableUsed("potion_healing", look, ConsumeMode.Drink, false, true),
                Is.EqualTo($"{HudText.Eul(HudText.LookName(look))} 마셨다 — 치유 물약이었다"));
            Use(s, "potion_healing");
            Assert.That(text.Item("potion_healing"), Is.EqualTo("치유 물약"));
            Assert.That(text.ConsumableUsed("potion_healing", look, ConsumeMode.Throw, true, true), Is.EqualTo("치유 물약을 던졌다 — 깨졌다"));
            Assert.That(HudText.LookName("scroll_tyr"), Is.EqualTo("티르 룬 주문서"));
        }

        [Test]
        public void InventoryMenu_ThrowOnlyForPotions()
        {
            var s = Start();
            Give(s, "potion_frost");
            Give(s, "scroll_rage");
            var menu = new InventoryMenu { CanThrow = entry => Consumables.CanThrow(s.Rules.Items, entry) };
            menu.Open();
            menu.Confirm(s.World, s.Player); // 0번 칸 = 물약
            Assert.That(menu.Options, Has.Member(InventoryMenuOption.Throw));
            menu.SelectMenu(menu.Options.ToList().IndexOf(InventoryMenuOption.Throw));
            var action = menu.Confirm(s.World, s.Player);
            Assert.That((action.Kind, action.Entry), Is.EqualTo((InventoryActionKind.ThrowItem, InventoryRef.Item("potion_frost"))));
            menu.Open();
            menu.Select(1, s.Player.InventorySlots.Length);
            menu.Confirm(s.World, s.Player);
            Assert.That(menu.Options, Has.No.Member(InventoryMenuOption.Throw), "주문서");
        }

        // 검산: 에셋이 BALANCE 7 · 14번과 같은지
        [Test]
        public void ProjectAssets_MatchBalanceTable()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConsumableConfigAsset>("Assets/_Project/Data/Items/ConsumableConfig.asset");
            Assert.That(config, Is.Not.Null, "ConsumableConfig.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var c = config.ToConfig();
            Assert.That((c.PotionLooks.Count, c.ScrollLooks.Count), Is.EqualTo((12, 12)));
            Assert.That((c.RandomPotionsPerFloor, c.RandomScrollsPerFloor, c.GasRadius, c.PurityRadius, c.ThrowRange), Is.EqualTo((2, 2, 2, 3, 8)));
            Assert.That((c.RetributionBasePercent, c.RetributionHpPercent, c.RetributionPowerScale, c.RetributionPowerCap), Is.EqualTo((10, 22.5f, 4.45f, 4f)));
            Assert.That(c.HealingCures, Is.EquivalentTo(new[] { StatusType.Burn, StatusType.Slow, StatusType.Poison, StatusType.Paralyze }));
            Assert.That(c.PurityClears, Is.EquivalentTo(new[] { TerrainEffectType.FireZone, TerrainEffectType.PoisonCloud, TerrainEffectType.ParalyticGas }));

            var book = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            var expected = Book();
            foreach (var id in expected.Ids)
            {
                if (!expected.TryGet(id, out var e) || !e.IsConsumable) continue;
                Assert.That(book.TryGet(id, out var a), Is.True, id);
                Assert.That((a.Use, a.Effect, a.Weight, a.Amount), Is.EqualTo((e.Use, e.Effect, e.Weight, e.Amount)), id);
            }
        }
    }
}
