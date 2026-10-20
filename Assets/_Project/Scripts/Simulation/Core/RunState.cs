using ProjectM.Status;

namespace ProjectM.Core
{
    /// <summary>피해의 출처 종류 (3-4b — 사망 원인). 끝에만 추가한다.</summary>
    public enum DamageKind
    {
        Unknown = 0,
        /// <summary>근접 공격 — Attacker가 공격자.</summary>
        Attack = 1,
        /// <summary>주문 직접 피해 — Id = 주문 ID (융합이면 융합 ID).</summary>
        Spell = 2,
        /// <summary>반응 (추가 피해 · 폭발 · 전이) — Id = 반응 ID.</summary>
        Reaction = 3,
        /// <summary>상태 이상의 틱당 피해 — Status.</summary>
        Status = 4,
        /// <summary>지형 효과의 틱당 피해 — Terrain.</summary>
        Terrain = 5,
        /// <summary>물약 · 주문서 (3-9 응징 등) — Id = 아이템 ID.</summary>
        Item = 6,
    }

    /// <summary>피해 하나의 출처 (3-4b). 값 타입 — 이벤트 · 저장에 그대로 쓴다.</summary>
    public readonly struct DamageCause
    {
        public readonly DamageKind Kind;
        public readonly string Id;
        public readonly StatusType Status;
        public readonly TerrainEffectType Terrain;

        public DamageCause(DamageKind kind, string id = null, StatusType status = StatusType.None, TerrainEffectType terrain = TerrainEffectType.None)
        {
            Kind = kind;
            Id = id ?? string.Empty;
            Status = status;
            Terrain = terrain;
        }

        public static DamageCause Attack => new DamageCause(DamageKind.Attack);
        public static DamageCause Spell(string id) => new DamageCause(DamageKind.Spell, id);
        public static DamageCause Reaction(string id) => new DamageCause(DamageKind.Reaction, id);
        public static DamageCause FromStatus(StatusType type) => new DamageCause(DamageKind.Status, status: type);
        public static DamageCause FromTerrain(TerrainEffectType type) => new DamageCause(DamageKind.Terrain, terrain: type);
        public static DamageCause Item(string id) => new DamageCause(DamageKind.Item, id);
    }

    /// <summary>한 판의 결과 (3-4b). 끝에만 추가한다.</summary>
    public enum RunOutcome
    {
        /// <summary>진행 중.</summary>
        None = 0,
        /// <summary>보스 처치.</summary>
        Victory = 1,
        /// <summary>플레이어 사망.</summary>
        Defeat = 2,
    }

    /// <summary>
    /// 한 판 기록 (3-4b): 처치 수 · 결과 · 보스 ID · 플레이어가 마지막으로 받은 피해(사망 원인). World에 두고 스냅샷 v10에 저장한다.
    /// 도달 층은 World.Floor · StoredFloors에서, 턴 수는 Scheduler.CurrentTick에서 계산하므로 따로 두지 않는다.
    /// </summary>
    public sealed class RunState
    {
        /// <summary>죽은 적 수 (보스 포함, 플레이어가 잡지 않은 것도 — 폭발 · 지형 포함).</summary>
        public int Kills { get; internal set; }
        public RunOutcome Outcome { get; internal set; }
        /// <summary>보스 층에 놓인 보스 (없으면 None). 이 유닛이 죽으면 승리.</summary>
        public EntityId BossId { get; internal set; }
        /// <summary>플레이어가 마지막으로 받은 피해의 출처 · 공격자 (사망 원인).</summary>
        public DamageCause LastPlayerDamage { get; internal set; }
        public EntityId LastPlayerAttacker { get; internal set; }

        public void SetBoss(EntityId id) => BossId = id;

        private readonly System.Collections.Generic.List<PendingFloorItem> _pendingFloorItems = new System.Collections.Generic.List<PendingFloorItem>();
        /// <summary>아직 가 보지 않은 층에 놓을 아이템 (3-5 마법서 배치 계획), 층 → 나눠 준 순서. 처음 가는 층에서 꺼내 놓는다.</summary>
        public System.Collections.Generic.IReadOnlyList<PendingFloorItem> PendingFloorItems => _pendingFloorItems;

        internal void SetPendingFloorItems(System.Collections.Generic.IEnumerable<PendingFloorItem> items)
        {
            _pendingFloorItems.Clear();
            _pendingFloorItems.AddRange(items);
        }

        /// <summary>계획 끝에 붙인다 (3-7a 무기 — 책 계획 뒤).</summary>
        internal void AddPendingFloorItems(System.Collections.Generic.IEnumerable<PendingFloorItem> items) => _pendingFloorItems.AddRange(items);

        // ── 식별 (3-9, SPD): 물약 · 주문서는 판마다 겉모습(색 · 룬)이 섞이고, 써 보거나 식별 주문서를 읽기 전에는 무엇인지 모른다 ──
        private readonly System.Collections.Generic.List<ItemLook> _looks = new System.Collections.Generic.List<ItemLook>();
        private readonly System.Collections.Generic.List<string> _identified = new System.Collections.Generic.List<string>();

        /// <summary>아이템 ID → 겉모습 (정한 순서 = ItemBook 순서).</summary>
        public System.Collections.Generic.IReadOnlyList<ItemLook> Looks => _looks;
        /// <summary>식별한 아이템 ID (식별한 순서).</summary>
        public System.Collections.Generic.IReadOnlyList<string> Identified => _identified;

        /// <summary>그 아이템의 겉모습 키. 겉모습이 없는 아이템(무기 · 책)이면 null.</summary>
        public string LookOf(string itemId)
        {
            foreach (var look in _looks)
                if (look.ItemId == itemId) return look.Look;
            return null;
        }

        public bool IsIdentified(string itemId) => _identified.Contains(itemId);

        /// <summary>무엇인지 아는지 — 겉모습이 없는 아이템은 늘 안다.</summary>
        public bool IsKnown(string itemId) => LookOf(itemId) == null || IsIdentified(itemId);

        internal void SetLooks(System.Collections.Generic.IEnumerable<ItemLook> looks, System.Collections.Generic.IEnumerable<string> identified)
        {
            _looks.Clear();
            _looks.AddRange(looks);
            _identified.Clear();
            _identified.AddRange(identified);
        }

        /// <summary>식별한다. 겉모습이 있고 처음 식별했으면 true.</summary>
        internal bool Identify(string itemId)
        {
            if (LookOf(itemId) == null || _identified.Contains(itemId)) return false;
            _identified.Add(itemId);
            return true;
        }

        /// <summary>그 층 몫을 꺼낸다 (계획에서 빠짐).</summary>
        internal System.Collections.Generic.List<string> TakePendingFloorItems(int floor)
        {
            var taken = new System.Collections.Generic.List<string>();
            for (int i = 0; i < _pendingFloorItems.Count; i++)
            {
                if (_pendingFloorItems[i].Floor != floor) continue;
                taken.Add(_pendingFloorItems[i].ItemId);
                _pendingFloorItems.RemoveAt(i--);
            }
            return taken;
        }
    }

    /// <summary>판마다 정한 물약 · 주문서의 겉모습 하나 (3-9 식별): 아이템 ID → 모습 키 (ItemVisualCatalog, 예: potion_crimson). 스냅샷 v16.</summary>
    [System.Serializable]
    public readonly struct ItemLook
    {
        public readonly string ItemId;
        public readonly string Look;

        public ItemLook(string itemId, string look)
        {
            ItemId = itemId;
            Look = look;
        }
    }

    /// <summary>아직 놓지 않은 층 아이템 하나 (3-5): 층 번호 + 아이템 ID. 스냅샷 v11에 저장.</summary>
    [System.Serializable]
    public readonly struct PendingFloorItem
    {
        public readonly int Floor;
        public readonly string ItemId;

        public PendingFloorItem(int floor, string itemId)
        {
            Floor = floor;
            ItemId = itemId;
        }
    }
}
