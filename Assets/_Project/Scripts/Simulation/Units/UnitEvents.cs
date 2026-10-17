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
        /// <summary>피해 출처 (3-4b).</summary>
        public DamageCause Cause { get; }

        public UnitDamagedEvent(EntityId target, EntityId attacker, int amount, int hp, int maxHp, DamageCause cause = default)
        {
            Cause = cause;
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

    /// <summary>한 판이 끝났다 (3-4b): 보스 처치 = 승리, 플레이어 사망 = 패배. ⑥ 사망 판정에서 한 번.</summary>
    public sealed class RunEndedEvent : ISimEvent
    {
        public RunOutcome Outcome { get; }

        public RunEndedEvent(RunOutcome outcome)
        {
            Outcome = outcome;
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
