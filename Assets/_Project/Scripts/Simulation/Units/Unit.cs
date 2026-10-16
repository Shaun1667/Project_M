using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;

namespace ProjectM.Units
{
    /// <summary>
    /// 맵 위의 유닛 하나 (플레이어 · 적). 저장 대상 상태 — World.Units에 있고 스냅샷(UnitState)으로 그대로 옮겨진다.
    /// 위치는 UnitRegistry.Move, 체력은 Combat.ApplyDamage, 마나는 Mana.TrySpend / ManaRegenSystem으로만 바꾼다.
    /// </summary>
    public sealed class Unit
    {
        public EntityId Id { get; }
        public Faction Faction { get; }
        public string VisualId { get; }
        public int MaxHp { get; }
        public int AttackDamage { get; }
        public int SightRange { get; }
        public int MaxMana { get; }

        public GridPos Pos { get; internal set; }
        public int Hp { get; internal set; }
        public int Mana { get; internal set; }

        /// <summary>
        /// 주문 장착 칸 (2-1). 길이 = 칸 수(MagicConfig.SpellSlotCount), 빈 칸은 null. 칸 번호 i는 키 i+1.
        /// </summary>
        public string[] SpellSlots { get; }

        /// <summary>AI 기억 (1-7): 마지막으로 상대(플레이어)를 본 칸. 없으면 null. AI가 쓰고 스냅샷에 저장된다.</summary>
        public GridPos? LastSeenTargetPos { get; set; }

        /// <summary>마지막 이동 직전 칸 (발자국). 한 번도 안 움직였으면 null. UnitRegistry.Move가 쓴다.</summary>
        public GridPos? PreviousPos { get; internal set; }

        /// <summary>AI 배회 목적지 (2026-10-16). 플레이어를 못 봤고 기억도 없을 때 가는 칸. 없으면 null.</summary>
        public GridPos? WanderTarget { get; set; }

        /// <summary>걸려 있는 상태 이상 (2-3). 종류 순으로 정렬, 종류마다 하나. StatusEffects로만 바꾼다.</summary>
        public List<StatusInstance> Statuses { get; } = new List<StatusInstance>();

        public bool HasStatus(StatusType type) => Statuses.Exists(s => s.Type == type);

        /// <summary>체력이 남아 있는지. 0이 된 유닛은 ⑥ 사망 판정 단계에서 빠질 때까지 행동 · 공격 대상이 되지 않는다.</summary>
        public bool IsAlive => Hp > 0;

        public Unit(EntityId id, Faction faction, string visualId, int maxHp, int attackDamage, int sightRange, GridPos pos, int hp,
            GridPos? lastSeenTargetPos = null, int maxMana = 0, int mana = 0, string[] spellSlots = null,
            GridPos? previousPos = null, GridPos? wanderTarget = null)
        {
            PreviousPos = previousPos;
            WanderTarget = wanderTarget;
            Id = id;
            Faction = faction;
            VisualId = visualId;
            MaxHp = maxHp;
            AttackDamage = attackDamage;
            SightRange = sightRange;
            Pos = pos;
            Hp = hp;
            LastSeenTargetPos = lastSeenTargetPos;
            MaxMana = maxMana;
            Mana = mana;
            SpellSlots = spellSlots ?? Array.Empty<string>();
        }

        /// <summary>
        /// 데이터에서 새 유닛: 체력 · 마나 가득. 장착 칸 spellSlotCount개를 시작 주문으로 앞쪽부터 채운다.
        /// </summary>
        public static Unit FromDef(EntityId id, Faction faction, UnitDef def, GridPos pos, int spellSlotCount = 0)
        {
            if (spellSlotCount < 0) throw new ArgumentOutOfRangeException(nameof(spellSlotCount));
            if (def.StartingSpells.Count > spellSlotCount)
                throw new ArgumentException($"시작 주문 {def.StartingSpells.Count}개가 장착 칸 {spellSlotCount}개보다 많다.", nameof(def));

            var slots = new string[spellSlotCount];
            for (int i = 0; i < def.StartingSpells.Count; i++)
                slots[i] = def.StartingSpells[i];

            return new Unit(id, faction, def.VisualId, def.MaxHp, def.AttackDamage, def.SightRange, pos, def.MaxHp,
                null, def.MaxMana, def.MaxMana, slots);
        }
    }
}
