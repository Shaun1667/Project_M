using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>
    /// 화난 상인 규칙 연결 (3-29). World.SetShopRules로 넣는다 — 규칙 연결이라 저장하지 않는다 (화난 상태 = 상인의 진영 · 능력치 · 배낭, 스냅샷 그대로).
    /// </summary>
    public sealed class ShopRules
    {
        public ShopConfig Config { get; }
        public ZoneBook Zones { get; }
        /// <summary>화난 상인 AI (ShopkeeperAi).</summary>
        public ICommandSource AngryAi { get; }
        public ItemBook Items { get; }

        public ShopRules(ShopConfig config, ZoneBook zones, ICommandSource angryAi, ItemBook items)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Zones = zones;
            AngryAi = angryAi ?? throw new ArgumentNullException(nameof(angryAi));
        }

        public bool IsKeeper(Unit unit) => unit != null && unit.VisualId == Config.KeeperVisualId;
    }

    /// <summary>
    /// 화난 상인 (3-29, 결정 2026-11-05 — 사용자): 플레이어가 상인을 주문 · 던지기(반응 포함)로 다치게 하면 그 판 상점은 끝 —
    /// 상인은 적이 되고(체력 = 그 구역 보스 체력 × AngryHpPercent, 공격 · 명중 · 회피 = 보스), 가격표 물건을 모두 챙겨 쓰며 싸운다(ShopkeeperAi).
    /// 쓰러뜨리면 남은 물건이 모두 그 칸에 떨어진다. 근접(부딪힘)은 그대로 팔기.
    /// </summary>
    public static class AngryShopkeeper
    {
        /// <summary>Combat.ApplyDamage가 부른다: 중립 상인을 플레이어가 주문 · 던지기 · 반응으로 쳤으면 화낸다. 화냈으면 true.</summary>
        public static bool OnDamaged(World world, EntityId attacker, Unit target, DamageCause cause)
        {
            var rules = world.ShopRules;
            if (rules == null || target.Faction != Faction.Neutral || !rules.IsKeeper(target)) return false;
            if (!attacker.IsValid || attacker != world.Scheduler.PlayerId) return false;
            if (cause.Kind != DamageKind.Spell && cause.Kind != DamageKind.Item && cause.Kind != DamageKind.Reaction) return false;
            Anger(world, rules, target);
            return true;
        }

        /// <summary>상인을 적으로: 능력치 · 가게 물건 챙기기(가격표 뗌) · AI 바꾸기 · ShopkeeperAngeredEvent.</summary>
        public static void Anger(World world, ShopRules rules, Unit keeper)
        {
            if (world == null || rules == null || keeper == null || keeper.Faction != Faction.Neutral) return;
            keeper.Faction = Faction.Enemy;
            var boss = rules.Zones?.ZoneOf(world.Floor).Boss;
            if (boss != null)
            {
                keeper.MaxHp = Math.Max(1, (int)((long)boss.MaxHp * rules.Config.AngryHpPercent / 100));
                keeper.AttackDamage = boss.AttackDamage;
                keeper.Accuracy = boss.Accuracy;
                keeper.Evasion = boss.Evasion;
            }
            keeper.Hp = keeper.MaxHp;

            int taken = 0;
            foreach (var item in new List<FloorItem>(world.FloorItems.All))
            {
                if (item.Price <= 0) continue;
                world.FloorItems.Remove(item.Pos, item.ItemId);
                world.Events.Publish(new FloorItemRemovedEvent(item.Pos, item.ItemId));
                if (Inventory.TryAdd(world, null, rules.Items, keeper, InventoryRef.Item(item.ItemId)) >= 0) taken++;
                else // 칸이 없으면(옛 저장의 상인) 가격표만 떼고 바닥에 둔다
                {
                    world.FloorItems.Add(item.Pos, item.ItemId);
                    world.Events.Publish(new FloorItemPlacedEvent(item.Pos, item.ItemId, Identification.VisualOf(world, item.ItemId)));
                }
            }
            world.Scheduler.Unregister(keeper.Id);
            world.Scheduler.RegisterActor(keeper.Id, rules.AngryAi);
            world.Events.Publish(new ShopkeeperAngeredEvent(keeper.Id, taken, keeper.Hp, keeper.MaxHp));
        }

        /// <summary>화난 상인이 죽었다 (GameRoot 사망 규칙이 부른다): 남은 물건을 모두 그 칸에.</summary>
        public static void DropStock(World world, Unit dead)
        {
            var rules = world?.ShopRules;
            if (rules == null || dead == null || !rules.IsKeeper(dead)) return;
            foreach (var entry in dead.InventorySlots)
            {
                if (entry.IsEmpty || entry.Kind != InventoryKind.Item) continue;
                for (int i = 0; i < Math.Max(1, entry.Count); i++)
                {
                    world.FloorItems.Add(dead.Pos, entry.Id);
                    world.Events.Publish(new FloorItemPlacedEvent(dead.Pos, entry.Id, Identification.VisualOf(world, entry.Id)));
                }
            }
        }

        /// <summary>상인이 던질 수 있는 해로운 물건인지 (물약: 독가스 · 액체 화염 · 냉기 · 마비 가스, 던지는 무기).</summary>
        public static bool IsWeapon(ItemBook items, InventoryRef entry)
        {
            if (entry.IsEmpty || entry.Kind != InventoryKind.Item || !items.TryGet(entry.Id, out var def)) return false;
            if (def.Use == ItemUse.Thrown) return true;
            return def.Use == ItemUse.Potion && (def.Effect == ConsumableEffect.ToxicGas || def.Effect == ConsumableEffect.LiquidFlame
                || def.Effect == ConsumableEffect.Frost || def.Effect == ConsumableEffect.ParalyticGas);
        }
    }

    /// <summary>
    /// 화난 상인 AI (3-29): 체력이 HealBelowPercent 아래면 치유 물약을 마신다 → ItemUsePercent 확률로 보이는 플레이어에게 해로운 물약 · 던지는 무기를 던진다
    /// (판정은 플레이어 던지기와 같은 Throwing.Check) → 아니면 쫓아와 근접 (MeleeChaserAi). 난수 RngStream.Combat. 상태를 갖지 않는다.
    /// </summary>
    public sealed class ShopkeeperAi : ICommandSource
    {
        private readonly World _world;
        private readonly GameConfig _config;
        private readonly ConsumableRules _rules;
        private readonly ITargetingRule _targeting;
        private readonly ISightRule _sight;
        private readonly ShopConfig _shop;
        private readonly ICommandSource _chase;

        public ShopkeeperAi(World world, GameConfig config, ConsumableRules rules, ITargetingRule targeting, ISightRule sight, ShopConfig shop, ICommandSource chase)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _targeting = targeting ?? throw new ArgumentNullException(nameof(targeting));
            _sight = sight ?? throw new ArgumentNullException(nameof(sight));
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _chase = chase ?? throw new ArgumentNullException(nameof(chase));
        }

        public ICommand NextCommand(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var self)) return new WaitCommand(_config.DefaultActionCostTicks);
            var slots = self.InventorySlots;
            if (self.Hp * 100 < self.MaxHp * _shop.HealBelowPercent)
                for (int i = 0; i < slots.Length; i++)
                    if (!slots[i].IsEmpty && _rules.Items.TryGet(slots[i].Id, out var def) && def.Use == ItemUse.Potion && def.Effect == ConsumableEffect.Healing)
                        return new UseConsumableCommand(_world, _rules, i, slots[i], _config.DefaultActionCostTicks);

            if (PlayerActions.TryGetPlayer(_world, out var player) && player.IsAlive
                && _world.Rng.Get(RngStream.Combat).Range(0, 100) < _shop.ItemUsePercent)
                for (int i = 0; i < slots.Length; i++)
                {
                    if (!AngryShopkeeper.IsWeapon(_rules.Items, slots[i])) continue;
                    if (Throwing.Check(_world, _rules, _targeting, _sight, self, slots[i], player.Pos, out _) != CastBlock.None) break;
                    return new ThrowItemCommand(_world, _rules, _targeting, i, slots[i], player.Pos, _config.DefaultActionCostTicks);
                }
            return _chase.NextCommand(context);
        }
    }

    /// <summary>상인이 화났다 (3-29 — HUD 로그 · 안내). Taken = 챙긴 물건 수.</summary>
    public sealed class ShopkeeperAngeredEvent : ISimEvent
    {
        public EntityId Keeper { get; }
        public int Taken { get; }
        public int Hp { get; }
        public int MaxHp { get; }

        public ShopkeeperAngeredEvent(EntityId keeper, int taken, int hp, int maxHp)
        {
            Keeper = keeper;
            Taken = taken;
            Hp = hp;
            MaxHp = maxHp;
        }
    }
}
