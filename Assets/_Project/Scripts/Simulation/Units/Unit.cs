using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 맵 위의 유닛 하나 (플레이어 · 적). 저장 대상 상태 — World.Units에 있고 스냅샷(UnitState)으로 그대로 옮겨진다.
    /// 위치는 UnitRegistry.Move, 체력은 Combat.ApplyDamage로만 바꾼다.
    /// </summary>
    public sealed class Unit
    {
        public EntityId Id { get; }
        public Faction Faction { get; }
        public string VisualId { get; }
        public int MaxHp { get; }
        public int AttackDamage { get; }
        public int SightRange { get; }

        public GridPos Pos { get; internal set; }
        public int Hp { get; internal set; }

        /// <summary>체력이 남아 있는지. 0이 된 유닛은 ⑥ 사망 판정 단계에서 빠질 때까지 행동 · 공격 대상이 되지 않는다.</summary>
        public bool IsAlive => Hp > 0;

        public Unit(EntityId id, Faction faction, string visualId, int maxHp, int attackDamage, int sightRange, GridPos pos, int hp)
        {
            Id = id;
            Faction = faction;
            VisualId = visualId;
            MaxHp = maxHp;
            AttackDamage = attackDamage;
            SightRange = sightRange;
            Pos = pos;
            Hp = hp;
        }

        public static Unit FromDef(EntityId id, Faction faction, UnitDef def, GridPos pos)
        {
            return new Unit(id, faction, def.VisualId, def.MaxHp, def.AttackDamage, def.SightRange, pos, def.MaxHp);
        }
    }
}
