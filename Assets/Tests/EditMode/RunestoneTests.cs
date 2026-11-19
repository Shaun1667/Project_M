using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>
    /// 3-53 룬석 방 (SPD Runestone — 결정 2026-11-19): 룬석 = 던지면 떨어진 칸 3×3에 SpellId 주문 효과(함정처럼, 시전자 = 던진 유닛) · 사라짐 · 겹침.
    /// 4종(폭발 · 번개 · 잠 · 공포), 룬석 방에 2~3개 (BALANCE.md 16번).
    /// </summary>
    public class RunestoneTests
    {
        private static readonly MagicConfig Magic = new MagicConfig(3, 1, 4);
        private static readonly ISightRule Sight = new ShadowcastSightRule();

        private static readonly ItemBook Items = new ItemBook(new[]
        {
            new ItemDef("runestone_blast", ItemUse.Runestone, spellId: "rune_blast"),
            new ItemDef("runestone_sleep", ItemUse.Runestone, spellId: "rune_sleep"),
        });

        private sealed class WaitAi : ICommandSource
        {
            public ICommand NextCommand(in TickContext context) => new WaitCommand(1);
        }

        private static (World World, Unit Player, Unit Enemy, Unit Other, ConsumableRules Rules) Start()
        {
            var world = new World(GridMovementTests.Parse("##########", "#........#", "#........#", "##########"), 5UL);
            var statuses = SpellTests.BalanceStatuses();
            var terrains = SpellTests.BalanceTerrains();
            world.InstallCoreSystems(Sight, Magic, statuses, terrains);
            var spells = new SpellBook(new[]
            {
                new SpellDef("rune_blast", Element.Fire, SpellShape.Burst, 1, 6, 0, radius: 1, appliesStatus: StatusType.Burn, statusChancePercent: 100),
                new SpellDef("rune_sleep", Element.Neutral, SpellShape.Burst, 1, 0, 0, radius: 1, appliesStatus: StatusType.Asleep, statusChancePercent: 100),
            }, statuses, terrains);
            var player = UnitSpawner.SpawnPlayer(world, new UnitDef("player", 20, 3, 8, 10), new GridPos(1, 1), Magic.QuickSlotCount);
            var enemy = UnitSpawner.SpawnEnemy(world, new UnitDef("enemy_melee", 30, 1, 6), new GridPos(6, 1), new WaitAi());
            var other = UnitSpawner.SpawnEnemy(world, new UnitDef("enemy_melee", 30, 1, 6), new GridPos(7, 2), new WaitAi()); // 3×3 안 (대각선)
            world.Events.Flush();
            return (world, player, enemy, other, new ConsumableRules(Items, statuses: statuses, terrains: terrains, spells: spells));
        }

        // 폭발의 룬석: 떨어진 칸 3×3 안 적 모두 피해 6 + 화상, 룬석은 사라짐(바닥에 안 남음), 한 개만 씀
        [Test]
        public void Blast_HitsAreaAndVanishes()
        {
            var (world, player, enemy, other, rules) = Start();
            var casts = new List<SpellCastEvent>();
            world.Events.Subscribe<SpellCastEvent>(casts.Add);
            int index = Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("runestone_blast").WithCount(2));
            Assert.That(Consumables.Throw(world, rules, new BresenhamTargetingRule(), player, index, enemy.Pos), Is.EqualTo(ConsumeResult.Used));
            world.Events.Flush();
            Assert.That(enemy.Hp, Is.EqualTo(24));
            Assert.That(other.Hp, Is.EqualTo(24), "3×3 안");
            Assert.That(enemy.HasStatus(StatusType.Burn), Is.True);
            Assert.That(world.FloorItems.ItemsAt(enemy.Pos), Is.Empty, "사라짐");
            Assert.That(player.InventorySlots[index].Count, Is.EqualTo(1), "하나만");
            Assert.That(casts.Count == 1 && casts[0].Caster == player.Id && casts[0].SpellId == "rune_blast", Is.True, "화면 · 로그용 시전 이벤트 (시전자 = 던진 유닛)");
        }

        // 잠의 룬석: 피해 없이 3×3 안 적이 잠듦
        [Test]
        public void Sleep_PutsAreaToSleep()
        {
            var (world, player, enemy, other, rules) = Start();
            int index = Inventory.TryAdd(world, null, Items, player, InventoryRef.Item("runestone_sleep"));
            Consumables.Throw(world, rules, new BresenhamTargetingRule(), player, index, enemy.Pos);
            Assert.That(enemy.HasStatus(StatusType.Asleep) && other.HasStatus(StatusType.Asleep), Is.True);
            Assert.That(enemy.Hp, Is.EqualTo(30));
        }

        // 룬석은 던질 수 있고 겹치며, 주문이 없는 룬석 줄은 만들 수 없다
        [Test]
        public void ItemRules()
        {
            Assert.That(Consumables.CanThrow(Items, InventoryRef.Item("runestone_blast")), Is.True);
            Assert.That(Inventory.IsStackable(Items, InventoryRef.Item("runestone_blast")), Is.True);
            Assert.Throws<System.ArgumentException>(() => new ItemDef("runestone_x", ItemUse.Runestone));
        }

        // 룬석 방: 입구 = 문, 방 안에 룬석 2~3개 (같은 종류 겹침 가능), 그 밖의 물건 없음
        [Test]
        public void Room_HasTwoToThreeRunestones()
        {
            int carved = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var (world, _, room) = SpecialRoomTests.CarveFor(seed, SpecialRoomType.Runestone);
                if (room == null) continue;
                carved++;
                Assert.That(world.Map.Get(room.Entrance), Is.EqualTo(TileType.Door), $"시드 {seed}");
                int stones = 0;
                foreach (var c in room.Cells)
                    foreach (var item in world.FloorItems.ItemsAt(c))
                    {
                        Assert.That(item.ItemId.StartsWith("runestone_"), Is.True, $"시드 {seed}: {item.ItemId}");
                        stones++;
                    }
                Assert.That(stones, Is.InRange(2, 3), $"시드 {seed}");
            }
            Assert.That(carved, Is.GreaterThan(0));
        }

        // 검산: FloorDecorConfig 룬석 방 · 룬석 아이템 · 룬 주문 = BALANCE.md 16번
        [Test]
        public void ProjectAsset_MatchesBalanceTable()
        {
            var c = AssetDatabase.LoadAssetAtPath<FloorDecorConfigAsset>("Assets/_Project/Data/Dungeon/FloorDecorConfig.asset").ToConfig().SpecialRooms;
            Assert.That(c.RunestoneItems, Is.EqualTo(new[] { "runestone_blast", "runestone_shock", "runestone_sleep", "runestone_fear" }));
            Assert.That((c.RunestoneMin, c.RunestoneMax), Is.EqualTo((2, 3)));
            var statuses = AssetDatabase.LoadAssetAtPath<StatusCatalogAsset>("Assets/_Project/Data/Statuses/StatusCatalog.asset").ToBook();
            var terrains = AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>("Assets/_Project/Data/Terrains/TerrainCatalog.asset").ToBook(statuses);
            var spells = AssetDatabase.LoadAssetAtPath<SpellCatalogAsset>("Assets/_Project/Data/Spells/SpellCatalog.asset").ToBook(statuses, terrains);
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>("Assets/_Project/Data/Items/ItemCatalog.asset").ToBook();
            var expected = new (string Item, string Spell, Element Element, int Damage, StatusType Status)[]
            {
                ("runestone_blast", "rune_blast", Element.Fire, 6, StatusType.Burn),
                ("runestone_shock", "rune_shock", Element.Lightning, 5, StatusType.None),
                ("runestone_sleep", "rune_sleep", Element.Neutral, 0, StatusType.Asleep),
                ("runestone_fear", "rune_fear", Element.Neutral, 0, StatusType.Terrified),
            };
            foreach (var e in expected)
            {
                Assert.That(items.TryGet(e.Item, out var def) && def.Use == ItemUse.Runestone && def.SpellId == e.Spell, Is.True, e.Item);
                Assert.That(spells.TryGet(e.Spell, out var s), Is.True, e.Spell);
                Assert.That((s.Element, s.Shape, s.Radius, s.Damage, s.ManaCost, s.AppliesStatus), Is.EqualTo((e.Element, SpellShape.Burst, 1, e.Damage, 0, e.Status)), e.Spell);
            }
        }
    }
}
