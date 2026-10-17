using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
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

        /// <summary>인벤토리 (3-2a). 길이 = 칸 수(MagicConfig.InventorySlotCount), 빈 칸은 Empty. Items.Inventory로만 바꾼다.</summary>
        public InventoryRef[] InventorySlots { get; }

        /// <summary>
        /// 빠른 칸 (3-2a, 2-1 주문 장착 칸을 대신함). 길이 = 칸 수(MagicConfig.QuickSlotCount), 칸 번호 i는 키 i+1.
        /// 인벤토리에 있는 주문 · 아이템을 가리킨다. Items.Inventory로만 바꾼다.
        /// </summary>
        public InventoryRef[] QuickSlots { get; }

        /// <summary>빠른 칸 slot의 주문 ID. 칸 밖 · 빈 칸 · 아이템이면 null.</summary>
        public string QuickSlotSpell(int slot) =>
            slot >= 0 && slot < QuickSlots.Length && QuickSlots[slot].Kind == InventoryKind.Spell ? QuickSlots[slot].Id : null;

        /// <summary>인벤토리에 그 주문이 있는지.</summary>
        public bool HasSpell(string spellId) => spellId != null && Array.IndexOf(InventorySlots, InventoryRef.Spell(spellId)) >= 0;

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
            GridPos? lastSeenTargetPos = null, int maxMana = 0, int mana = 0, InventoryRef[] inventorySlots = null, InventoryRef[] quickSlots = null,
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
            InventorySlots = inventorySlots ?? Array.Empty<InventoryRef>();
            QuickSlots = quickSlots ?? Array.Empty<InventoryRef>();
        }

        /// <summary>
        /// 데이터에서 새 유닛: 체력 · 마나 가득. 인벤토리 inventorySize칸 · 빠른 칸 quickSlotCount칸을 만들고,
        /// 시작 주문을 인벤토리 앞쪽부터 넣고 같은 순서로 빠른 칸에 등록한다 (3-2a).
        /// </summary>
        public static Unit FromDef(EntityId id, Faction faction, UnitDef def, GridPos pos, int quickSlotCount = 0, int inventorySize = 0)
        {
            if (quickSlotCount < 0) throw new ArgumentOutOfRangeException(nameof(quickSlotCount));
            if (inventorySize < 0) throw new ArgumentOutOfRangeException(nameof(inventorySize));
            if (def.StartingSpells.Count > quickSlotCount || def.StartingSpells.Count > inventorySize)
                throw new ArgumentException($"시작 주문 {def.StartingSpells.Count}개가 빠른 칸 {quickSlotCount}개 · 인벤토리 {inventorySize}칸보다 많다.", nameof(def));

            var inventory = new InventoryRef[inventorySize];
            var quick = new InventoryRef[quickSlotCount];
            for (int i = 0; i < def.StartingSpells.Count; i++)
                inventory[i] = quick[i] = InventoryRef.Spell(def.StartingSpells[i]);

            return new Unit(id, faction, def.VisualId, def.MaxHp, def.AttackDamage, def.SightRange, pos, def.MaxHp,
                null, def.MaxMana, def.MaxMana, inventory, quick);
        }
    }
}
