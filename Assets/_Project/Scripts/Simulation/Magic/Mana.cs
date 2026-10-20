using System;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Magic
{
    /// <summary>마나 변경 지점 하나 (2-1). 주문 시전(2-2)은 TrySpend로 마나를 쓴다.</summary>
    public static class Mana
    {
        /// <summary>마나가 amount 이상이면 빼고 true. 모자라면 아무것도 바꾸지 않고 false.</summary>
        public static bool TrySpend(World world, Unit unit, int amount)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "마나 소모는 0 이상이어야 한다.");
            if (unit.Mana < amount) return false;
            if (amount == 0) return true;

            unit.Mana -= amount;
            world.Events.Publish(new ManaChangedEvent(unit.Id, unit.Mana, unit.MaxMana));
            return true;
        }

        /// <summary>최대치를 넘지 않게 회복한다. 실제로 늘어난 양을 돌려준다.</summary>
        public static int Restore(World world, Unit unit, int amount)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "회복량은 0 이상이어야 한다.");

            int gained = Math.Min(amount, unit.MaxMana - unit.Mana);
            if (gained <= 0) return 0;
            unit.Mana += gained;
            world.Events.Publish(new ManaChangedEvent(unit.Id, unit.Mana, unit.MaxMana));
            return gained;
        }
    }

    /// <summary>
    /// ⑤ 마나 회복 (GDD 3-3). 틱 번호로만 주기를 정한다 — (틱 + 1)이 주기의 배수인 틱마다, 살아 있고 마나가 있는 유닛을
    /// EntityId 순으로 회복. 상태를 갖지 않는다 (틱 번호는 스케줄러가 저장).
    /// </summary>
    public sealed class ManaRegenSystem : ITickSystem
    {
        private readonly World _world;
        private readonly MagicConfig _config;

        public ManaRegenSystem(World world, MagicConfig config)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Run(in TickContext context)
        {
            if ((context.Tick + 1) % _config.ManaRegenIntervalTicks != 0) return;
            foreach (var unit in _world.Units.ToSortedList())
            {
                if (unit.IsAlive && unit.MaxMana > 0)
                    Mana.Restore(_world, unit, _config.ManaRegenAmount);
            }
        }
    }

    /// <summary>
    /// ⑤ 체력 자동 회복 (2-6). 마나 회복 다음에 돈다. (틱 + 1)이 주기의 배수인 틱마다 살아 있는 플레이어만
    /// 최대 체력의 일정 퍼센트를 최대치를 넘지 않게 회복 (소수점은 누적 — MagicConfig.HpRegenAmount). 상태 이상 중에도 회복. 상태를 갖지 않는다.
    /// </summary>
    public sealed class HpRegenSystem : ITickSystem
    {
        private readonly World _world;
        private readonly MagicConfig _config;

        public HpRegenSystem(World world, MagicConfig config)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Run(in TickContext context)
        {
            if (_config.HpRegenIntervalTicks == 0) return;
            if ((context.Tick + 1) % _config.HpRegenIntervalTicks != 0) return;
            long count = (context.Tick + 1) / _config.HpRegenIntervalTicks; // 몇 번째 회복 주기인지 (틱 번호에서 — 저장 없음)
            foreach (var unit in _world.Units.ToSortedList())
            {
                if (unit.Faction != Faction.Player || !unit.IsAlive || unit.Hp >= unit.MaxHp) continue;
                int healed = Math.Min(_config.HpRegenAmount(unit.MaxHp, count), unit.MaxHp - unit.Hp);
                if (healed <= 0) continue;
                unit.Hp += healed;
                _world.Events.Publish(new UnitHealedEvent(unit.Id, healed, unit.Hp, unit.MaxHp));
            }
        }
    }

    /// <summary>유닛의 마나가 바뀌었다. 값은 바뀐 뒤.</summary>
    public sealed class ManaChangedEvent : ISimEvent
    {
        public EntityId Id { get; }
        public int Mana { get; }
        public int MaxMana { get; }

        public ManaChangedEvent(EntityId id, int mana, int maxMana)
        {
            Id = id;
            Mana = mana;
            MaxMana = maxMana;
        }
    }
}
