using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>유닛이 피해를 입었다. Hp는 피해 뒤 값.</summary>
    public sealed class UnitDamagedEvent : ISimEvent
    {
        public EntityId Target { get; }
        public EntityId Attacker { get; }
        public int Amount { get; }
        public int Hp { get; }
        public int MaxHp { get; }

        public UnitDamagedEvent(EntityId target, EntityId attacker, int amount, int hp, int maxHp)
        {
            Target = target;
            Attacker = attacker;
            Amount = amount;
            Hp = hp;
            MaxHp = maxHp;
        }
    }

    /// <summary>유닛이 체력을 회복했다 (2-6 자동 회복). Hp는 회복 뒤 값.</summary>
    public sealed class UnitHealedEvent : ISimEvent
    {
        public EntityId Target { get; }
        public int Amount { get; }
        public int Hp { get; }
        public int MaxHp { get; }

        public UnitHealedEvent(EntityId target, int amount, int hp, int maxHp)
        {
            Target = target;
            Amount = amount;
            Hp = hp;
            MaxHp = maxHp;
        }
    }

    /// <summary>유닛이 죽었다 (⑥ 사망 판정). 같은 틱에 EntityRemovedEvent가 뒤따른다.</summary>
    public sealed class UnitDiedEvent : ISimEvent
    {
        public EntityId Id { get; }
        public Faction Faction { get; }

        public UnitDiedEvent(EntityId id, Faction faction)
        {
            Id = id;
            Faction = faction;
        }
    }
}
