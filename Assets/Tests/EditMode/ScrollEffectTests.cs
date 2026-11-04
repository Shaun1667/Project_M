using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Tests
{
    /// <summary>3-13b 주문서 4종 (SPD — 결정 2026-11-04): 분신(아군) · 공포(도망) · 자장가(잠, 맞으면 깸) · 변환(고른 것을 같은 쓰임 다른 것으로).</summary>
    public class ScrollEffectTests
    {
        private static readonly GameConfig StepConfig = new GameConfig(1, 1f);
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();
        private static readonly UnitDef PlayerDef = new UnitDef("player", 20, 3, 8, 10);
        private static readonly UnitDef EnemyDef = new UnitDef("enemy_melee", 30, 2, 6);

        private static readonly ItemBook Items = new ItemBook(new[]
        {
            new ItemDef("scroll_mirror_image", ItemUse.Scroll, effect: ConsumableEffect.MirrorImage, weight: 2),
            new ItemDef("scroll_terror", ItemUse.Scroll, effect: ConsumableEffect.Terror, weight: 1),
            new ItemDef("scroll_lullaby", ItemUse.Scroll, effect: ConsumableEffect.Lullaby, weight: 1),
            new ItemDef("scroll_transmutation", ItemUse.Scroll, effect: ConsumableEffect.Transmutation, weight: 1),
            new ItemDef("potion_healing", ItemUse.Potion, effect: ConsumableEffect.Healing, weight: 3),
            new ItemDef("potion_frost", ItemUse.Potion, effect: ConsumableEffect.Frost, weight: 3),
            new ItemDef("potion_strength", ItemUse.Potion, amount: 1, effect: ConsumableEffect.Strength),
            new ItemDef("shortsword", ItemUse.EquipWeapon, weapon: new WeaponDef(15, 3, 100, 1, 1, 12)),
            new ItemDef("hand_axe", ItemUse.EquipWeapon, weapon: new WeaponDef(12, 4, 90, 1, 1, 12)),
        }, new WeaponRules(3, 2));

        private sealed class Session
        {
            public World World;
            public TurnClock Clock;
            public Unit Player;
            public StatusBook Statuses;
            public ConsumableRules Rules;
            public MeleeChaserAi EnemyAi;

            public int Give(string id) => Inventory.TryAdd(World, null, Items, Player, InventoryRef.Item(id));

            public void Read(string id, InventoryRef pick = default)
            {
                int index = System.Array.IndexOf(Player.InventorySlots, InventoryRef.Item(id));
                Assert.That(Consumables.Use(World, Rules, Player, index, UpgradeTarget.Auto, pick), Is.EqualTo(ConsumeResult.Used), id);
                World.Events.Flush();
            }

            public void Wait(int turns = 1)
            {
                for (int i = 0; i < turns; i++) Clock.SubmitPlayerCommand(PlayerActions.Wait(StepConfig));
                World.Events.Flush();
            }
        }

        private static Session Start(params GridPos[] enemies)
        {
            var world = new World(GridMovementTests.Parse("############", "#..........#", "#..........#", "#..........#", "############"), 5UL);
            var s = new Session { World = world, Clock = new TurnClock(world.Scheduler, world.PlayerInput), Statuses = SpellTests.BalanceStatuses() };
            world.InstallCoreSystems(Sight, Magic, s.Statuses, SpellTests.BalanceTerrains());
            s.Rules = new ConsumableRules(Items, ConsumableConfig.Default, s.Statuses, SpellTests.BalanceTerrains(), null, Sight, new AllyAi(world, StepConfig, Sight));
            s.EnemyAi = new MeleeChaserAi(world, StepConfig, Sight, s.Statuses);
            s.Player = UnitSpawner.SpawnPlayer(world, PlayerDef, new GridPos(2, 2), Magic.QuickSlotCount);
            foreach (var p in enemies) UnitSpawner.SpawnEnemy(world, EnemyDef, p, s.EnemyAi);
            world.Events.Flush();
            return s;
        }

        private static Unit Enemy(Session s)
        {
            foreach (var u in s.World.Units.ToSortedList()) if (u.Faction == Faction.Enemy) return u;
            return null;
        }

        [Test]
        public void Factions_AllyIsNotHostileToPlayer()
        {
            Assert.That(Factions.AreHostile(Faction.Player, Faction.Ally), Is.False);
            Assert.That(Factions.AreHostile(Faction.Enemy, Faction.Ally), Is.True);
            Assert.That(Factions.AreHostile(Faction.Enemy, Faction.Player), Is.True);
            Assert.That(Factions.AreHostile(Faction.Enemy, Faction.Enemy), Is.False);
        }

        // 분신: 둘레에 아군 2명 (체력 1, 내 모습 · 공격), 적을 공격하고 적도 분신을 친다
        [Test]
        public void MirrorImage_SpawnsAlliesThatFight()
        {
            var s = Start(new GridPos(6, 2));
            s.Give("scroll_mirror_image");
            s.Read("scroll_mirror_image");
            var allies = s.World.Units.ToSortedList().FindAll(u => u.Faction == Faction.Ally);
            Assert.That(allies.Count, Is.EqualTo(2));
            Assert.That(allies[0].MaxHp, Is.EqualTo(1));
            Assert.That(allies[0].VisualId, Is.EqualTo(s.Player.VisualId));
            Assert.That(allies[0].AttackDamage, Is.EqualTo(s.Player.AttackDamage));

            var enemy = Enemy(s);
            int before = enemy.Hp;
            s.Wait(6);
            bool allyFought = enemy.Hp < before || s.World.Units.ToSortedList().FindAll(u => u.Faction == Faction.Ally).Count < 2;
            Assert.That(allyFought, Is.True, "분신이 적과 싸움 (적이 맞았거나 분신이 쓰러짐)");
        }

        // 플레이어는 분신을 공격하지 못한다 (부딪히면 막힘)
        [Test]
        public void Player_CannotAttackAlly()
        {
            var s = Start();
            UnitSpawner.SpawnAlly(s.World, new UnitDef("player", 1, 3, 8), new GridPos(3, 2), new AllyAi(s.World, StepConfig, Sight));
            Assert.That(PlayerActions.TryStep(s.World, StepConfig, new GridPos(1, 0), out _), Is.False);
        }

        // 공포: 보이는 적이 멀어지고 공격하지 않는다
        [Test]
        public void Terror_EnemyFlees()
        {
            var s = Start(new GridPos(4, 2));
            s.Give("scroll_terror");
            s.Read("scroll_terror");
            var enemy = Enemy(s);
            Assert.That(enemy.HasStatus(StatusType.Terrified), Is.True);
            int distance = GridMovement.Chebyshev(enemy.Pos, s.Player.Pos);
            s.Wait(2);
            Assert.That(GridMovement.Chebyshev(enemy.Pos, s.Player.Pos), Is.GreaterThan(distance));
            Assert.That(s.Player.Hp, Is.EqualTo(PlayerDef.MaxHp));
        }

        // 자장가: 보이는 적이 잠들어 행동하지 않고, 맞으면 깬다
        [Test]
        public void Lullaby_SleepsUntilDamaged()
        {
            var s = Start(new GridPos(3, 2));
            s.Give("scroll_lullaby");
            s.Read("scroll_lullaby");
            var enemy = Enemy(s);
            Assert.That(enemy.HasStatus(StatusType.Asleep), Is.True);
            s.Wait(3);
            Assert.That(s.Player.Hp, Is.EqualTo(PlayerDef.MaxHp), "잠든 적은 공격 안 함");
            Combat.ApplyDamage(s.World, s.Player.Id, enemy, 1);
            Assert.That(enemy.HasStatus(StatusType.Asleep), Is.False, "맞으면 깸");
        }

        // 변환: 고른 무기 → 다른 무기 (강화 유지), 물약 → 힘이 아닌 다른 물약
        [Test]
        public void Transmutation_ChangesPickedItemWithinKind()
        {
            var s = Start();
            Inventory.TryAdd(s.World, null, Items, s.Player, InventoryRef.Item("shortsword", 2));
            s.Give("potion_healing");
            s.Give("scroll_transmutation");
            s.Give("scroll_transmutation");
            s.Read("scroll_transmutation", InventoryRef.Item("shortsword", 2));
            Assert.That(System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("hand_axe", 2)), Is.GreaterThanOrEqualTo(0), "강화 유지");
            Assert.That(System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("shortsword", 2)), Is.LessThan(0));

            s.Read("scroll_transmutation", InventoryRef.Item("potion_healing"));
            Assert.That(System.Array.IndexOf(s.Player.InventorySlots, InventoryRef.Item("potion_frost")), Is.GreaterThanOrEqualTo(0), "힘 물약은 결과에서 빠짐");
        }

        [Test]
        public void TransmutationPickFilter()
        {
            var s = Start();
            var filter = Consumables.PickFilter(s.World, Items, InventoryRef.Item("scroll_transmutation"));
            Assert.That(filter(InventoryRef.Item("shortsword")), Is.True);
            Assert.That(filter(InventoryRef.Item("potion_healing")), Is.True);
            Assert.That(filter(InventoryRef.Item("scroll_transmutation")), Is.False, "자기 자신");
            Assert.That(filter(InventoryRef.Spell("fire_basic")), Is.False);
        }
    }
}
