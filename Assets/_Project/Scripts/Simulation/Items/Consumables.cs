using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Items
{
    /// <summary>
    /// 물약 · 주문서 수치 (3-9, BALANCE 14번). 값은 ConsumableConfigAsset에서 온다.
    /// 겉모습 목록(색 · 룬)은 ItemVisualCatalog 키 — 판마다 물약 · 주문서 종류에 섞어 나눠 준다 (SPD 식별).
    /// </summary>
    public sealed class ConsumableConfig
    {
        /// <summary>물약 겉모습 키 (potion_crimson …). 물약 종류보다 적으면 남는 물약은 처음부터 이름이 보인다.</summary>
        public IReadOnlyList<string> PotionLooks { get; }
        /// <summary>주문서 겉모습 키 (scroll_tyr …).</summary>
        public IReadOnlyList<string> ScrollLooks { get; }
        /// <summary>층마다 무작위로 놓는 물약 수 (무게 = ItemDef.Weight, 마지막(보스) 층 제외).</summary>
        public int RandomPotionsPerFloor { get; }
        /// <summary>층마다 무작위로 놓는 주문서 수.</summary>
        public int RandomScrollsPerFloor { get; }
        /// <summary>가스 · 불 · 냉기 물약이 퍼지는 거리 (칸, 벽을 돌아 걸음 수). 0 = 그 칸만.</summary>
        public int GasRadius { get; }
        /// <summary>정화 물약이 지형을 없애는 거리 (칸).</summary>
        public int PurityRadius { get; }
        /// <summary>던지기 사거리 (칸, 체비셰프).</summary>
        public int ThrowRange { get; }
        /// <summary>치유 물약이 푸는 상태.</summary>
        public IReadOnlyList<StatusType> HealingCures { get; }
        /// <summary>정화 물약이 없애는 지형.</summary>
        public IReadOnlyList<TerrainEffectType> PurityClears { get; }
        /// <summary>응징 (SPD): 피해 = 최대 체력 × BasePercent% + 현재 체력 × 힘 × HpPercent%, 힘 = min(PowerCap, 잃은 체력 비율 × PowerScale).</summary>
        public int RetributionBasePercent { get; }
        public float RetributionHpPercent { get; }
        public float RetributionPowerScale { get; }
        public float RetributionPowerCap { get; }

        public ConsumableConfig(IReadOnlyList<string> potionLooks, IReadOnlyList<string> scrollLooks, int randomPotionsPerFloor, int randomScrollsPerFloor,
            int gasRadius, int purityRadius, int throwRange, IReadOnlyList<StatusType> healingCures, IReadOnlyList<TerrainEffectType> purityClears,
            int retributionBasePercent, float retributionHpPercent, float retributionPowerScale, float retributionPowerCap)
        {
            if (randomPotionsPerFloor < 0) throw new ArgumentOutOfRangeException(nameof(randomPotionsPerFloor));
            if (randomScrollsPerFloor < 0) throw new ArgumentOutOfRangeException(nameof(randomScrollsPerFloor));
            if (gasRadius < 0) throw new ArgumentOutOfRangeException(nameof(gasRadius));
            if (purityRadius < 0) throw new ArgumentOutOfRangeException(nameof(purityRadius));
            if (throwRange < 1) throw new ArgumentOutOfRangeException(nameof(throwRange));
            if (retributionBasePercent < 0 || retributionHpPercent < 0 || retributionPowerScale < 0 || retributionPowerCap < 0)
                throw new ArgumentOutOfRangeException(nameof(retributionBasePercent), "응징 수치는 0 이상");
            PotionLooks = CheckLooks(potionLooks, nameof(potionLooks));
            ScrollLooks = CheckLooks(scrollLooks, nameof(scrollLooks));
            RandomPotionsPerFloor = randomPotionsPerFloor;
            RandomScrollsPerFloor = randomScrollsPerFloor;
            GasRadius = gasRadius;
            PurityRadius = purityRadius;
            ThrowRange = throwRange;
            HealingCures = healingCures ?? Array.Empty<StatusType>();
            PurityClears = purityClears ?? Array.Empty<TerrainEffectType>();
            RetributionBasePercent = retributionBasePercent;
            RetributionHpPercent = retributionHpPercent;
            RetributionPowerScale = retributionPowerScale;
            RetributionPowerCap = retributionPowerCap;
        }

        /// <summary>겉모습 없음 · 무작위 배치 없음 · 퍼짐 1 · 사거리 8 (테스트 · 에셋 없을 때).</summary>
        public static ConsumableConfig Default => new ConsumableConfig(Array.Empty<string>(), Array.Empty<string>(), 0, 0, 1, 2, 8,
            null, null, 10, 22.5f, 4.45f, 4f);

        private static IReadOnlyList<string> CheckLooks(IReadOnlyList<string> looks, string name)
        {
            if (looks == null) return Array.Empty<string>();
            var seen = new HashSet<string>();
            foreach (var look in looks)
            {
                if (string.IsNullOrEmpty(look)) throw new ArgumentException("빈 겉모습 키", name);
                if (!seen.Add(look)) throw new ArgumentException("같은 겉모습이 두 번: " + look, name);
            }
            return looks;
        }
    }

    /// <summary>물약 · 주문서 효과가 읽는 규칙 묶음 (3-9). 상태를 갖지 않는다 — 판마다 GameRoot가 만든다.</summary>
    public sealed class ConsumableRules
    {
        public ItemBook Items { get; }
        public ConsumableConfig Config { get; }
        public StatusBook Statuses { get; }
        public TerrainBook Terrains { get; }
        /// <summary>경험 물약용. null이면 경험 물약은 아무 일 없음.</summary>
        public ProgressionConfig Progression { get; }
        /// <summary>응징(보이는 적)용. null이면 응징은 아무도 맞지 않음.</summary>
        public ISightRule Sight { get; }

        public ConsumableRules(ItemBook items, ConsumableConfig config = null, StatusBook statuses = null, TerrainBook terrains = null,
            ProgressionConfig progression = null, ISightRule sight = null)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Config = config ?? ConsumableConfig.Default;
            Statuses = statuses ?? new StatusBook(Array.Empty<StatusDef>());
            Terrains = terrains ?? TerrainBook.Empty;
            Progression = progression;
            Sight = sight;
        }
    }

    /// <summary>
    /// 물약 · 주문서 식별 (3-9, 결정 2026-10-20 — SPD 그대로): 판 시작에 물약 · 주문서 종류마다 겉모습(색 · 룬)을 무작위로 정하고(RngStream.Loot),
    /// 마시거나 읽으면(던져서 효과가 보이면) 그 종류를 식별한다. 식별 주문서는 인벤토리에서 모르는 종류 하나를 식별한다.
    /// 상태는 World.Run (스냅샷 v16).
    /// </summary>
    public static class Identification
    {
        /// <summary>겉모습을 정한다 (판 시작에 한 번 — 배치 계획 모두 뒤에 불러 책 · 무기 · 소모품 계획은 그대로).</summary>
        public static void AssignLooks(World world, ItemBook items, ConsumableConfig config)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (config == null) throw new ArgumentNullException(nameof(config));
            var rng = world.Rng.Get(RngStream.Loot);
            var looks = new List<ItemLook>();
            Assign(looks, rng, items, ItemUse.Potion, config.PotionLooks);
            Assign(looks, rng, items, ItemUse.Scroll, config.ScrollLooks);
            world.Run.SetLooks(looks, Array.Empty<string>());
        }

        private static void Assign(List<ItemLook> result, IRng rng, ItemBook items, ItemUse use, IReadOnlyList<string> pool)
        {
            var shuffled = new List<string>(pool);
            for (int i = 0; i < shuffled.Count - 1; i++)
            {
                int j = rng.Range(i, shuffled.Count);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            int next = 0;
            foreach (var id in items.Ids)
            {
                if (next >= shuffled.Count) break;
                if (items.TryGet(id, out var def) && def.Use == use) result.Add(new ItemLook(id, shuffled[next++]));
            }
        }

        /// <summary>바닥 · 화면에 쓸 모습 키 — 겉모습이 있으면 겉모습, 없으면 아이템 ID.</summary>
        public static string VisualOf(World world, string itemId) => world.Run.LookOf(itemId) ?? itemId;

        /// <summary>식별한다. 처음 식별했으면 true (이벤트는 부른 쪽이 낸다).</summary>
        public static bool Identify(World world, string itemId) => world.Run.Identify(itemId);
    }

    /// <summary>소모품 사용 결과 (3-7b).</summary>
    public enum ConsumeResult
    {
        Used = 0,
        NotConsumable = 1,
        /// <summary>강화 주문서인데 든 무기가 없다 — 쓰지 않음.</summary>
        NoWeapon = 2,
        /// <summary>던질 수 없는 아이템 (3-9 — 물약만 던진다).</summary>
        NotThrowable = 3,
    }

    /// <summary>물약 · 주문서를 어떻게 썼는지 (3-9). 끝에만 추가한다.</summary>
    public enum ConsumeMode
    {
        Drink = 0,
        Read = 1,
        Throw = 2,
    }

    /// <summary>
    /// 물약 · 주문서 (3-7b → 3-9, SPD 효과). 쓰면(마시기 · 읽기 · 던지기 모두 차례 1) 인벤토리에서 하나 빠진다.
    /// 효과는 ConsumableEffect 하나로 정하고, 수치는 ConsumableConfig · 상태/지형 규칙(StatusBook · TerrainBook) 데이터에서 읽는다.
    /// 범위 물약(가스 · 불 · 냉기 · 정화)은 마시면 발밑에서, 던지면 떨어진 칸에서 깨진다 (결정 2026-10-20).
    /// </summary>
    public static class Consumables
    {
        /// <summary>쓸 수 있는지 미리 본다 (입력이 차례를 쓰기 전에 — 든 무기 없이 강화 주문서를 읽으려 하면 NoWeapon).</summary>
        public static ConsumeResult Check(ItemBook items, Unit unit, InventoryRef entry)
        {
            if (entry.Kind != InventoryKind.Item || !items.TryGet(entry.Id, out var def) || !def.IsConsumable) return ConsumeResult.NotConsumable;
            if (def.Effect == ConsumableEffect.Upgrade && unit.Weapon.IsEmpty) return ConsumeResult.NoWeapon;
            return ConsumeResult.Used;
        }

        /// <summary>던질 수 있는지 — 물약만 (3-9).</summary>
        public static bool CanThrow(ItemBook items, InventoryRef entry) =>
            entry.Kind == InventoryKind.Item && items.TryGet(entry.Id, out var def) && def.Use == ItemUse.Potion;

        /// <summary>범위로 퍼지는 효과인지 (마시면 발밑, 던지면 떨어진 칸).</summary>
        public static bool IsArea(ConsumableEffect effect) =>
            effect == ConsumableEffect.Purity || effect == ConsumableEffect.ToxicGas || effect == ConsumableEffect.LiquidFlame
            || effect == ConsumableEffect.Frost || effect == ConsumableEffect.ParalyticGas;

        public static ConsumeResult Use(World world, ItemBook items, Unit unit, int inventoryIndex) =>
            Use(world, new ConsumableRules(items), unit, inventoryIndex);

        /// <summary>인벤토리 칸 index의 물약을 마시거나 주문서를 읽는다 (틱 안 — UseConsumableCommand가 부름).</summary>
        public static ConsumeResult Use(World world, ConsumableRules rules, Unit unit, int inventoryIndex)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (inventoryIndex < 0 || inventoryIndex >= unit.InventorySlots.Length) return ConsumeResult.NotConsumable;
            var entry = unit.InventorySlots[inventoryIndex];
            var result = Check(rules.Items, unit, entry);
            if (result != ConsumeResult.Used) return result;

            rules.Items.TryGet(entry.Id, out var def);
            Inventory.RemoveAt(world, unit, inventoryIndex);
            var mode = def.Use == ItemUse.Potion ? ConsumeMode.Drink : ConsumeMode.Read;
            Announce(world, unit, def, mode, unit.Pos, identify: true);
            Apply(world, rules, unit, def, unit.Pos);
            return result;
        }

        /// <summary>
        /// 물약을 target 쪽으로 던진다 (틱 안 — ThrowItemCommand가 부름). 경로는 Throwing.Path (벽 앞 · 유닛 칸에서 멈춤).
        /// 범위 물약은 떨어진 칸에서 효과가 나고 식별된다. 그 밖의 물약은 깨져 사라진다 (효과 없음 · 식별 안 됨 — SPD).
        /// </summary>
        public static ConsumeResult Throw(World world, ConsumableRules rules, ITargetingRule targeting, Unit unit, int inventoryIndex, GridPos target)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (targeting == null) throw new ArgumentNullException(nameof(targeting));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (inventoryIndex < 0 || inventoryIndex >= unit.InventorySlots.Length) return ConsumeResult.NotConsumable;
            var entry = unit.InventorySlots[inventoryIndex];
            if (!CanThrow(rules.Items, entry)) return ConsumeResult.NotThrowable;
            var path = Throwing.Path(world, targeting, unit.Pos, target, rules.Config.ThrowRange);
            if (path.Count == 0) return ConsumeResult.NotThrowable;

            rules.Items.TryGet(entry.Id, out var def);
            Inventory.RemoveAt(world, unit, inventoryIndex);
            var landing = path[path.Count - 1];
            bool area = IsArea(def.Effect);
            Announce(world, unit, def, ConsumeMode.Throw, landing, identify: area);
            if (area) Apply(world, rules, unit, def, landing);
            return ConsumeResult.Used;
        }

        private static void Announce(World world, Unit unit, ItemDef def, ConsumeMode mode, GridPos at, bool identify)
        {
            var look = world.Run.LookOf(def.Id);
            bool wasKnown = world.Run.IsKnown(def.Id);
            if (identify) Identification.Identify(world, def.Id);
            world.Events.Publish(new ConsumableUsedEvent(unit.Id, def.Id, def.Effect, mode, at, look, wasKnown, world.Run.IsKnown(def.Id)));
        }

        private static void Apply(World world, ConsumableRules rules, Unit unit, ItemDef def, GridPos at)
        {
            var config = rules.Config;
            switch (def.Effect)
            {
                case ConsumableEffect.Strength:
                    unit.Strength += def.Amount;
                    world.Events.Publish(new StrengthChangedEvent(unit.Id, unit.Strength, def.Amount));
                    break;
                case ConsumableEffect.Upgrade:
                {
                    var held = unit.Weapon;
                    if (held.IsEmpty || !rules.Items.TryGet(held.ItemId, out var weaponItem)) break;
                    unit.Weapon = Weapons.Resolve(held.ItemId, weaponItem.Weapon, rules.Items.WeaponRules, held.Upgrade + def.Amount);
                    world.Events.Publish(new WeaponUpgradedEvent(unit.Id, unit.Weapon));
                    break;
                }
                case ConsumableEffect.Healing:
                {
                    int healed = unit.MaxHp - unit.Hp;
                    if (healed > 0)
                    {
                        unit.Hp = unit.MaxHp;
                        world.Events.Publish(new UnitHealedEvent(unit.Id, healed, unit.Hp, unit.MaxHp));
                    }
                    foreach (var status in config.HealingCures) StatusEffects.Remove(world, unit, status);
                    break;
                }
                case ConsumableEffect.Experience:
                    if (rules.Progression != null)
                        Progression.GainExp(world, unit, Math.Max(0, Progression.ExpToNextOrZero(unit, rules.Progression) - unit.Exp), rules.Progression);
                    break;
                case ConsumableEffect.Purity:
                    foreach (var cell in Area(world.Map, at, config.PurityRadius))
                        foreach (var type in config.PurityClears) TerrainEffects.Remove(world, cell, type);
                    break;
                case ConsumableEffect.ToxicGas:
                    PlaceTerrain(world, rules, at, TerrainEffectType.PoisonCloud);
                    break;
                case ConsumableEffect.LiquidFlame:
                    PlaceTerrain(world, rules, at, TerrainEffectType.FireZone);
                    break;
                case ConsumableEffect.ParalyticGas:
                    PlaceTerrain(world, rules, at, TerrainEffectType.ParalyticGas);
                    break;
                case ConsumableEffect.Frost:
                    Frost(world, rules, at);
                    break;
                case ConsumableEffect.Identify:
                    IdentifyOne(world, rules.Items, unit);
                    break;
                case ConsumableEffect.MagicMapping:
                    RevealFloor(world);
                    break;
                case ConsumableEffect.Teleportation:
                    Teleport(world, unit);
                    break;
                case ConsumableEffect.Recharging:
                    Mana.Restore(world, unit, unit.MaxMana);
                    break;
                case ConsumableEffect.Rage:
                    foreach (var other in world.Units.ToSortedList())
                        if (other.IsAlive && other.Faction != unit.Faction) other.LastSeenTargetPos = unit.Pos;
                    break;
                case ConsumableEffect.Retribution:
                    Retribution(world, rules, unit, def.Id);
                    break;
            }
        }

        /// <summary>
        /// center에서 걸음 수 radius 안의 걸을 수 있는 칸 (벽을 돌아감 — 가스가 벽 너머로 새지 않게). 순서는 걸음 수 → 찾은 순 (결정론).
        /// </summary>
        public static List<GridPos> Area(GridMap map, GridPos center, int radius)
        {
            var result = new List<GridPos>();
            if (!map.IsWalkable(center)) return result;
            var seen = new HashSet<GridPos> { center };
            result.Add(center);
            int start = 0;
            for (int step = 0; step < radius; step++)
            {
                int end = result.Count;
                for (int i = start; i < end; i++)
                    foreach (var dir in GridMovement.Directions)
                    {
                        var next = result[i] + dir;
                        if (!map.IsWalkable(next) || !GridMovement.CanStep(map, result[i], next) || !seen.Add(next)) continue;
                        result.Add(next);
                    }
                start = end;
            }
            return result;
        }

        private static void PlaceTerrain(World world, ConsumableRules rules, GridPos at, TerrainEffectType type)
        {
            if (!rules.Terrains.TryGet(type, out var def)) return;
            foreach (var cell in Area(world.Map, at, rules.Config.GasRadius)) TerrainEffects.Place(world, cell, def);
        }

        /// <summary>냉기 (SPD Frost): 범위 안 유닛 빙결 · 화염 지대 끔 · 물 → 빙판.</summary>
        private static void Frost(World world, ConsumableRules rules, GridPos at)
        {
            rules.Statuses.TryGet(StatusType.Frozen, out var frozen);
            rules.Terrains.TryGet(TerrainEffectType.IcePatch, out var ice);
            foreach (var cell in Area(world.Map, at, rules.Config.GasRadius))
            {
                TerrainEffects.Remove(world, cell, TerrainEffectType.FireZone);
                if (ice != null) TerrainEffects.Transform(world, cell, TerrainEffectType.Water, TerrainEffectType.IcePatch, ice.DurationTicks);
                if (frozen != null && world.Units.TryGetAt(cell, out var target)) StatusEffects.Apply(world, target, frozen);
            }
        }

        /// <summary>식별 주문서: 인벤토리 앞 칸부터 모르는 물약 · 주문서 한 종류를 식별한다 (SPD는 고르기 — 고르는 창은 나중에).</summary>
        private static void IdentifyOne(World world, ItemBook items, Unit unit)
        {
            foreach (var entry in unit.InventorySlots)
            {
                if (entry.Kind != InventoryKind.Item || world.Run.IsKnown(entry.Id)) continue;
                var look = world.Run.LookOf(entry.Id);
                if (!Identification.Identify(world, entry.Id)) continue;
                world.Events.Publish(new ItemIdentifiedEvent(entry.Id, look));
                return;
            }
            world.Events.Publish(new ItemIdentifiedEvent(null, null)); // 식별할 것이 없었다
        }

        /// <summary>지도 주문서: 걸을 수 있는 칸과 그 둘레 벽을 모두 탐험한 것으로 (⑥ 시야 단계가 화면에 알린다).</summary>
        private static void RevealFloor(World world)
        {
            var map = world.Map;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    var pos = new GridPos(x, y);
                    bool reveal = map.IsWalkable(pos);
                    if (!reveal)
                        foreach (var dir in GridMovement.Directions)
                            if (map.IsWalkable(pos + dir)) { reveal = true; break; }
                    if (reveal) world.Explored[y * map.Width + x] = true;
                }
        }

        /// <summary>순간이동 주문서: 비어 있는 바닥 칸(계단 제외) 중 무작위로 (RngStream.Combat).</summary>
        private static void Teleport(World world, Unit unit)
        {
            var map = world.Map;
            var cells = new List<GridPos>();
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    var pos = new GridPos(x, y);
                    if (map.Get(pos) == TileType.Floor && pos != unit.Pos && !world.Units.IsOccupied(pos)) cells.Add(pos);
                }
            if (cells.Count == 0) return;
            var to = cells[world.Rng.Get(RngStream.Combat).Range(0, cells.Count)];
            var from = unit.Pos;
            world.Units.Move(unit.Id, to);
            world.Events.Publish(new EntityMovedEvent(unit.Id, from, to));
        }

        /// <summary>응징 (SPD Retribution): 보이는 적마다 최대 체력 × Base% + 현재 체력 × 힘 × Hp% — 잃은 체력이 많을수록 셈.</summary>
        private static void Retribution(World world, ConsumableRules rules, Unit unit, string itemId)
        {
            if (rules.Sight == null || unit.MaxHp <= 0) return;
            var config = rules.Config;
            float missing = (unit.MaxHp - unit.Hp) / (float)unit.MaxHp;
            float power = Math.Min(config.RetributionPowerCap, missing * config.RetributionPowerScale);
            foreach (var target in world.Units.ToSortedList())
            {
                if (!target.IsAlive || target.Faction == unit.Faction) continue;
                if (!rules.Sight.CanSee(world.Map, unit.Pos, target.Pos, unit.SightRange)) continue;
                int damage = RetributionDamage(config, target, power);
                Combat.ApplyDamage(world, unit.Id, target, damage, DamageCause.Item(itemId));
            }
        }

        /// <summary>응징 피해 한 대상 (검산용으로 공개).</summary>
        public static int RetributionDamage(ConsumableConfig config, Unit target, float power) =>
            (int)Math.Round(target.MaxHp * config.RetributionBasePercent / 100.0 + target.Hp * power * config.RetributionHpPercent / 100.0,
                MidpointRounding.AwayFromZero);
    }

    /// <summary>던지기 조준 (3-9): 미리보기와 실제 던지기가 같은 경로 계산을 쓴다.</summary>
    public static class Throwing
    {
        /// <summary>날아가는 칸 (던진 칸 제외) — 벽 앞 · 유닛이 있는 칸에서 멈춘다 (ITargetingRule). 마지막 칸 = 떨어지는 칸.</summary>
        public static List<GridPos> Path(World world, ITargetingRule targeting, GridPos from, GridPos target, int range) =>
            from == target ? new List<GridPos>() : targeting.Trace(world.Map, from, target, range, world.Units.IsOccupied);

        /// <summary>target에 던질 수 있는지 — 대상 · 사거리 · 시야 · 막힘. 미리보기는 경로 + 떨어지는 칸 하나 (모르는 물약의 범위를 드러내지 않게).</summary>
        public static CastBlock Check(World world, ConsumableRules rules, ITargetingRule targeting, ISightRule sight, Unit thrower, InventoryRef entry,
            GridPos target, out SpellPreview preview)
        {
            preview = null;
            if (!Consumables.CanThrow(rules.Items, entry)) return CastBlock.NotOwned;
            if (target == thrower.Pos) return CastBlock.TargetIsSelf;
            if (!targeting.InRange(thrower.Pos, target, rules.Config.ThrowRange)) return CastBlock.OutOfRange;
            if (!world.Map.InBounds(target) || !sight.CanSee(world.Map, thrower.Pos, target, thrower.SightRange)) return CastBlock.NotVisible;
            var path = Path(world, targeting, thrower.Pos, target, rules.Config.ThrowRange);
            if (path.Count == 0) return CastBlock.Blocked;
            preview = new SpellPreview(path, new List<GridPos> { path[path.Count - 1] });
            return CastBlock.None;
        }
    }

    /// <summary>물약 · 주문서를 쓰는 행동 (3-7b, 차례 1). 실행 때 그 칸 내용이 바뀌었으면 차례만 쓴다.</summary>
    public sealed class UseConsumableCommand : ICommand
    {
        private readonly World _world;
        private readonly ConsumableRules _rules;
        private readonly int _inventoryIndex;
        private readonly InventoryRef _entry;

        public int CostTicks { get; }

        public UseConsumableCommand(World world, ItemBook items, int inventoryIndex, InventoryRef entry, int costTicks)
            : this(world, new ConsumableRules(items), inventoryIndex, entry, costTicks)
        {
        }

        public UseConsumableCommand(World world, ConsumableRules rules, int inventoryIndex, InventoryRef entry, int costTicks)
        {
            if (costTicks < 1) throw new ArgumentOutOfRangeException(nameof(costTicks));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _inventoryIndex = inventoryIndex;
            _entry = entry;
            CostTicks = costTicks;
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var unit) || !unit.IsAlive) return;
            if (_inventoryIndex < 0 || _inventoryIndex >= unit.InventorySlots.Length || unit.InventorySlots[_inventoryIndex] != _entry) return;
            Consumables.Use(_world, _rules, unit, _inventoryIndex);
        }
    }

    /// <summary>물약 던지기 (3-9, 차례 1). 실행 때 그 칸 내용이 바뀌었으면 차례만 쓴다.</summary>
    public sealed class ThrowItemCommand : ICommand
    {
        private readonly World _world;
        private readonly ConsumableRules _rules;
        private readonly ITargetingRule _targeting;
        private readonly int _inventoryIndex;
        private readonly InventoryRef _entry;

        public GridPos Target { get; }
        public int CostTicks { get; }

        public ThrowItemCommand(World world, ConsumableRules rules, ITargetingRule targeting, int inventoryIndex, InventoryRef entry, GridPos target, int costTicks)
        {
            if (costTicks < 1) throw new ArgumentOutOfRangeException(nameof(costTicks));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _targeting = targeting ?? throw new ArgumentNullException(nameof(targeting));
            _inventoryIndex = inventoryIndex;
            _entry = entry;
            Target = target;
            CostTicks = costTicks;
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var unit) || !unit.IsAlive) return;
            if (_inventoryIndex < 0 || _inventoryIndex >= unit.InventorySlots.Length || unit.InventorySlots[_inventoryIndex] != _entry) return;
            Consumables.Throw(_world, _rules, _targeting, unit, _inventoryIndex, Target);
        }
    }

    /// <summary>
    /// 물약 · 주문서를 썼다 (3-9). Look = 겉모습 키(없으면 null), WasKnown = 쓰기 전에 알았는지, Known = 지금 아는지 (쓰면서 식별됐으면 WasKnown false · Known true).
    /// At = 효과가 난 칸 (마시기 · 읽기 = 쓴 유닛 칸, 던지기 = 떨어진 칸).
    /// </summary>
    public sealed class ConsumableUsedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public string ItemId { get; }
        public ConsumableEffect Effect { get; }
        public ConsumeMode Mode { get; }
        public GridPos At { get; }
        public string Look { get; }
        public bool WasKnown { get; }
        public bool Known { get; }

        public ConsumableUsedEvent(EntityId unit, string itemId, ConsumableEffect effect, ConsumeMode mode, GridPos at, string look, bool wasKnown, bool known)
        {
            Unit = unit;
            ItemId = itemId;
            Effect = effect;
            Mode = mode;
            At = at;
            Look = look;
            WasKnown = wasKnown;
            Known = known;
        }
    }

    /// <summary>식별 주문서로 한 종류를 식별했다 (3-9). ItemId가 null이면 식별할 것이 없었다.</summary>
    public sealed class ItemIdentifiedEvent : ISimEvent
    {
        public string ItemId { get; }
        public string Look { get; }

        public ItemIdentifiedEvent(string itemId, string look)
        {
            ItemId = itemId;
            Look = look;
        }
    }
}
