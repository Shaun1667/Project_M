using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;

namespace ProjectM.Units
{
    /// <summary>허기 수치 (3-14, SPD). 값은 HungerConfigAsset에서 — 기준은 BALANCE.md 18번.</summary>
    public sealed class HungerConfig
    {
        /// <summary>허기가 이 값 이상이면 배고픔 (알림만).</summary>
        public int HungryAt { get; }
        /// <summary>허기가 이 값 이상이면 굶주림 (매 틱 피해 · 자동 회복 멈춤). 허기는 여기서 더 오르지 않는다.</summary>
        public int StarvingAt { get; }
        /// <summary>굶주림 중 매 틱 피해.</summary>
        public int StarveDamage { get; }
        /// <summary>먹기 행동 비용 (틱, SPD 3).</summary>
        public int EatTicks { get; }
        /// <summary>위험한 음식(정체불명 고기)을 먹었을 때 나쁜 상태가 걸릴 확률 (%) · 걸리는 상태 후보.</summary>
        public int RiskyFoodBadPercent { get; }
        public IReadOnlyList<StatusType> RiskyFoodStatuses { get; }
        /// <summary>적이 죽을 때 떨어뜨리는 음식 · 확률 (%). 비면 없음.</summary>
        public string EnemyDropItem { get; }
        public int EnemyDropPercent { get; }
        /// <summary>층마다 보장하는 음식 · 확률로 더하는 음식 (3-14 배치).</summary>
        public string FloorFoodItem { get; }
        public string BonusFoodItem { get; }
        public int BonusFoodPercent { get; }
        /// <summary>판 시작 배낭의 음식 (비면 없음).</summary>
        public string StartingFood { get; }

        public HungerConfig(int hungryAt, int starvingAt, int starveDamage, int eatTicks, int riskyFoodBadPercent, IReadOnlyList<StatusType> riskyFoodStatuses,
            string enemyDropItem = null, int enemyDropPercent = 0, string floorFoodItem = null, string bonusFoodItem = null, int bonusFoodPercent = 0,
            string startingFood = null)
        {
            if (hungryAt < 1 || starvingAt <= hungryAt) throw new ArgumentOutOfRangeException(nameof(starvingAt), "1 ≤ 배고픔 < 굶주림");
            if (starveDamage < 0 || eatTicks < 1) throw new ArgumentOutOfRangeException(nameof(eatTicks));
            if (riskyFoodBadPercent < 0 || riskyFoodBadPercent > 100 || enemyDropPercent < 0 || enemyDropPercent > 100 || bonusFoodPercent < 0 || bonusFoodPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(riskyFoodBadPercent), "확률은 0 ~ 100");
            HungryAt = hungryAt;
            StarvingAt = starvingAt;
            StarveDamage = starveDamage;
            EatTicks = eatTicks;
            RiskyFoodBadPercent = riskyFoodBadPercent;
            RiskyFoodStatuses = riskyFoodStatuses ?? Array.Empty<StatusType>();
            EnemyDropItem = string.IsNullOrEmpty(enemyDropItem) ? null : enemyDropItem;
            EnemyDropPercent = enemyDropPercent;
            FloorFoodItem = string.IsNullOrEmpty(floorFoodItem) ? null : floorFoodItem;
            BonusFoodItem = string.IsNullOrEmpty(bonusFoodItem) ? null : bonusFoodItem;
            BonusFoodPercent = bonusFoodPercent;
            StartingFood = string.IsNullOrEmpty(startingFood) ? null : startingFood;
        }
    }

    /// <summary>허기 단계 (3-14). 끝에만 추가한다.</summary>
    public enum HungerState
    {
        None = 0,
        Hungry = 1,
        Starving = 2,
    }

    /// <summary>허기 · 먹기 · 음식 떨어뜨리기 · 요리는 여기 하나 (3-14, SPD).</summary>
    public static class Hunger
    {
        public static HungerState StateOf(HungerConfig config, int hunger) =>
            hunger >= config.StarvingAt ? HungerState.Starving : hunger >= config.HungryAt ? HungerState.Hungry : HungerState.None;

        /// <summary>
        /// 먹는다 (틱 안 — EatCommand): 한 개를 빼고 허기 −Food(0 아래로 안 감). 위험한 음식이면 RiskyFoodBadPercent로 후보 중 하나 (RngStream.Combat).
        /// </summary>
        public static bool Eat(World world, ItemBook items, StatusBook statuses, HungerConfig config, Unit unit, int inventoryIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= unit.InventorySlots.Length) return false;
            var entry = unit.InventorySlots[inventoryIndex];
            if (entry.Kind != InventoryKind.Item || !items.TryGet(entry.Id, out var def) || def.Use != ItemUse.Food) return false;
            Inventory.RemoveAt(world, unit, inventoryIndex);
            var before = StateOf(config, unit.Hunger);
            unit.Hunger = Math.Max(0, unit.Hunger - def.Food);
            world.Events.Publish(new FoodEatenEvent(unit.Id, def.Id));
            Publish(world, config, unit, before);
            if (def.Risky && config.RiskyFoodStatuses.Count > 0 && world.Rng.Get(RngStream.Combat).Range(0, 100) < config.RiskyFoodBadPercent)
            {
                var type = config.RiskyFoodStatuses[world.Rng.Get(RngStream.Combat).Range(0, config.RiskyFoodStatuses.Count)];
                if (statuses != null && statuses.TryGet(type, out var status)) StatusEffects.Apply(world, unit, status);
            }
            return true;
        }

        internal static void Publish(World world, HungerConfig config, Unit unit, HungerState before)
        {
            var after = StateOf(config, unit.Hunger);
            if (after != before) world.Events.Publish(new HungerChangedEvent(unit.Id, after));
        }

        /// <summary>
        /// 원소가 닿은 칸의 바닥 음식이 바뀐다 (3-14 요리 — 결정 2026-11-04): 불 = ItemDef.FireTransformsTo(구운 고기), 얼음 = IceTransformsTo(얼린 고기).
        /// 주문 · 함정 효과가 부른다 (World.OnCellsHit).
        /// </summary>
        public static void TransformFloorItems(World world, ItemBook items, Element element, IReadOnlyList<GridPos> cells)
        {
            if (element != Element.Fire && element != Element.Ice) return;
            foreach (var cell in cells)
            {
                foreach (var id in world.FloorItems.At(cell))
                {
                    if (!items.TryGet(id, out var def)) continue;
                    string to = element == Element.Fire ? def.FireTransformsTo : def.IceTransformsTo;
                    if (to == null || !items.TryGet(to, out _)) continue;
                    world.FloorItems.Remove(cell, id);
                    world.Events.Publish(new FloorItemRemovedEvent(cell, id));
                    world.FloorItems.Add(cell, to);
                    world.Events.Publish(new FloorItemPlacedEvent(cell, to, to));
                    world.Events.Publish(new FoodCookedEvent(cell, id, to));
                }
            }
        }

        /// <summary>적이 죽으면 EnemyDropPercent로 그 칸에 음식 (3-14, SPD 쥐 고기 — RngStream.Loot). DeathCheck가 부른다 (World.OnUnitDied).</summary>
        public static void DropFood(World world, HungerConfig config, Unit dead)
        {
            if (config.EnemyDropItem == null || dead.Faction != Faction.Enemy) return;
            if (world.Rng.Get(RngStream.Loot).Range(0, 100) >= config.EnemyDropPercent) return;
            world.FloorItems.Add(dead.Pos, config.EnemyDropItem);
            world.Events.Publish(new FloorItemPlacedEvent(dead.Pos, config.EnemyDropItem, config.EnemyDropItem));
        }
    }

    /// <summary>⑤ 허기 (3-14): 살아 있는 플레이어 허기 +1 (굶주림 값에서 멈춤), 굶주림이면 매 틱 피해. 상태를 갖지 않는다.</summary>
    public sealed class HungerSystem : ITickSystem
    {
        private readonly World _world;
        private readonly HungerConfig _config;

        public HungerSystem(World world, HungerConfig config)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Run(in TickContext context)
        {
            if (!PlayerActions.TryGetPlayer(_world, out var player) || !player.IsAlive || player.TimeFrozenTicks > 0) return; // 3-28c 회중시계: 허기 멈춤
            var before = Hunger.StateOf(_config, player.Hunger);
            if (player.Hunger < _config.StarvingAt) player.Hunger++;
            Hunger.Publish(_world, _config, player, before);
            if (Hunger.StateOf(_config, player.Hunger) == HungerState.Starving && _config.StarveDamage > 0)
                Combat.ApplyDamage(_world, EntityId.None, player, _config.StarveDamage, DamageCause.Hunger);
        }
    }

    /// <summary>먹기 (3-14): 비용 EatTicks (SPD 3). 실행 때 그 칸 내용이 바뀌었으면 차례만 쓴다.</summary>
    public sealed class EatCommand : ICommand
    {
        private readonly World _world;
        private readonly ItemBook _items;
        private readonly StatusBook _statuses;
        private readonly HungerConfig _config;
        private readonly int _inventoryIndex;
        private readonly InventoryRef _entry;

        public int CostTicks => _config.EatTicks;

        public EatCommand(World world, ItemBook items, StatusBook statuses, HungerConfig config, int inventoryIndex, InventoryRef entry)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _statuses = statuses;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _inventoryIndex = inventoryIndex;
            _entry = entry;
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var unit) || !unit.IsAlive) return;
            if (_inventoryIndex < 0 || _inventoryIndex >= unit.InventorySlots.Length || unit.InventorySlots[_inventoryIndex] != _entry) return;
            Hunger.Eat(_world, _items, _statuses, _config, unit, _inventoryIndex);
        }
    }

    /// <summary>허기 단계가 바뀌었다 (로그 · HUD).</summary>
    public sealed class HungerChangedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public HungerState State { get; }

        public HungerChangedEvent(EntityId unit, HungerState state)
        {
            Unit = unit;
            State = state;
        }
    }

    /// <summary>먹었다 (로그).</summary>
    public sealed class FoodEatenEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public string ItemId { get; }

        public FoodEatenEvent(EntityId unit, string itemId)
        {
            Unit = unit;
            ItemId = itemId;
        }
    }

    /// <summary>바닥 음식이 원소로 바뀌었다 (로그).</summary>
    public sealed class FoodCookedEvent : ISimEvent
    {
        public GridPos Pos { get; }
        public string From { get; }
        public string To { get; }

        public FoodCookedEvent(GridPos pos, string from, string to)
        {
            Pos = pos;
            From = from;
            To = to;
        }
    }
}
