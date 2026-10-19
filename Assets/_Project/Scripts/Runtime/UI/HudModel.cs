using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.UI
{
    /// <summary>
    /// HUD가 보일 값과 전투 로그를 이벤트로만 모은다 (2-5). 엔진 타입을 쓰지 않아 테스트에서 그대로 돌린다.
    /// 화면 그리기는 HudPresenter. 로그는 최근 N줄, 같은 상태의 지속 갱신(이미 걸려 있는데 다시 걸림)은 남기지 않는다.
    /// </summary>
    public sealed class HudModel
    {
        private readonly int _maxLogLines;
        private readonly List<string> _log = new List<string>();
        private readonly SortedDictionary<StatusType, int> _playerStatuses = new SortedDictionary<StatusType, int>();
        private readonly Dictionary<EntityId, HashSet<StatusType>> _activeStatuses = new Dictionary<EntityId, HashSet<StatusType>>();
        private SimEventBus _bus;
        private EntityId _playerId;
        private InventoryRef[] _quickSlots = new InventoryRef[0];
        private int[] _slotCosts = new int[0];
        private SpellBook _spellBook;
        private string _lastPickupFail; // 3-5: 같은 칸 · 같은 아이템 "가득 참"은 한 번만

        public HudModel(int maxLogLines, HudText text = null)
        {
            if (maxLogLines < 1) throw new ArgumentOutOfRangeException(nameof(maxLogLines));
            _maxLogLines = maxLogLines;
            Text = text ?? new HudText();
        }

        public HudText Text { get; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public int Mana { get; private set; }
        public int MaxMana { get; private set; }
        /// <summary>레벨 · 지금 레벨 경험치 · 다음 레벨까지 필요량 (3-6). ExpToNext 0 = 최대 레벨 또는 경험치 없음.</summary>
        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public int ExpToNext { get; private set; }
        /// <summary>든 무기 (3-7a) — Empty = 맨손 (피해 = UnarmedDamage).</summary>
        public EquippedWeapon Weapon { get; private set; }
        public int UnarmedDamage { get; private set; }
        public bool Dead { get; private set; }
        /// <summary>빠른 칸 (3-2a). 빈 칸은 Empty.</summary>
        public IReadOnlyList<InventoryRef> QuickSlots => _quickSlots;
        /// <summary>칸마다 마나 비용, 주문이 아니면(빈 칸 · 아이템) -1.</summary>
        public IReadOnlyList<int> SlotCosts => _slotCosts;
        /// <summary>플레이어 상태 → 남은 틱 (종류 순).</summary>
        public IReadOnlyDictionary<StatusType, int> PlayerStatuses => _playerStatuses;
        /// <summary>오래된 줄이 앞, 새 줄이 뒤.</summary>
        public IReadOnlyList<string> Log => _log;

        /// <summary>값이 바뀌면 (이벤트 하나마다) 알린다.</summary>
        public event Action Changed;

        /// <param name="progression">3-6: 첫 경험치 막대 (다음 레벨 필요량). null이면 막대 비움.</param>
        public void Bind(SimEventBus bus, Unit player, SpellBook spellBook = null, ProgressionConfig progression = null)
        {
            Unbind();
            _bus = bus;
            _playerId = player.Id;
            Level = player.Level;
            Exp = player.Exp;
            Weapon = player.Weapon;
            UnarmedDamage = player.AttackDamage;
            ExpToNext = progression != null ? Progression.ExpToNextOrZero(player, progression) : 0;
            Hp = player.Hp;
            MaxHp = player.MaxHp;
            Mana = player.Mana;
            MaxMana = player.MaxMana;
            Dead = false;
            _spellBook = spellBook;
            SetSlots(player.QuickSlots);
            _log.Clear();
            _playerStatuses.Clear();
            _activeStatuses.Clear();
            foreach (var s in player.Statuses)
            {
                _playerStatuses[s.Type] = s.Remaining;
                Active(_playerId).Add(s.Type);
            }

            _bus.Subscribe<UnitDamagedEvent>(OnDamaged);
            _bus.Subscribe<UnitHealedEvent>(OnHealed);
            _bus.Subscribe<UnitDiedEvent>(OnDied);
            _bus.Subscribe<ManaChangedEvent>(OnManaChanged);
            _bus.Subscribe<SpellCastEvent>(OnSpellCast);
            _bus.Subscribe<StatusChangedEvent>(OnStatusChanged);
            _bus.Subscribe<QuickSlotsChangedEvent>(OnSlotsChanged);
            _bus.Subscribe<ReactionTriggeredEvent>(OnReaction);
            _bus.Subscribe<ProjectM.Dungeon.FloorChangedEvent>(OnFloorChanged);
            _bus.Subscribe<ItemPickedUpEvent>(OnPickedUp);
            _bus.Subscribe<ItemPickupFailedEvent>(OnPickupFailed);
            _bus.Subscribe<SpellLearnedEvent>(OnSpellLearned);
            _bus.Subscribe<EntityMovedEvent>(OnMoved);
            _bus.Subscribe<AttackMissedEvent>(OnMissed);
            _bus.Subscribe<ExpGainedEvent>(OnExpGained);
            _bus.Subscribe<LevelUpEvent>(OnLevelUp);
            _bus.Subscribe<WeaponEquippedEvent>(OnWeaponEquipped);
            _lastPickupFail = null;
            Changed?.Invoke();
        }

        // 마법서 (3-5): "불꽃 화살 마법서을(를) 주웠다" · "불꽃 화살을(를) 배웠다 → [1]"
        private void OnPickedUp(ItemPickedUpEvent e)
        {
            if (e.Unit != _playerId) return;
            _lastPickupFail = null;
            AddLog(Text.PickedUp(e.ItemId));
            Changed?.Invoke();
        }

        private void OnPickupFailed(ItemPickupFailedEvent e)
        {
            if (e.Unit != _playerId) return;
            string key = $"{e.Pos}:{e.ItemId}";
            if (key == _lastPickupFail) return; // 서 있는 동안 틱마다 오므로 한 번만
            _lastPickupFail = key;
            AddLog(Text.PickupFull(e.ItemId));
            Changed?.Invoke();
        }

        private void OnSpellLearned(SpellLearnedEvent e)
        {
            if (e.Unit != _playerId) return;
            AddLog(Text.Learned(e.SpellId, e.QuickSlot));
            Changed?.Invoke();
        }

        // 3-7 명중 · 회피: 플레이어가 끼인 빗나감만
        private void OnMissed(AttackMissedEvent e)
        {
            if (e.Attacker == _playerId) AddLog(HudText.PlayerMissed(e.Target));
            else if (e.Target == _playerId) AddLog(HudText.EnemyMissed(e.Attacker));
            else return;
            Changed?.Invoke();
        }

        // 3-6 경험치 · 레벨업: 레벨업 줄이 경험치 줄보다 먼저 온다 (레벨업 처리 뒤 경험치 이벤트)
        private void OnExpGained(ExpGainedEvent e)
        {
            if (e.Unit != _playerId) return;
            Level = e.Level;
            Exp = e.Exp;
            ExpToNext = e.ExpToNext;
            AddLog(HudText.ExpGained(e.Amount));
            Changed?.Invoke();
        }

        private void OnLevelUp(LevelUpEvent e)
        {
            if (e.Unit != _playerId) return;
            Level = e.Level;
            Hp = e.Hp;
            MaxHp = e.MaxHp;
            Mana = e.Mana;
            MaxMana = e.MaxMana;
            AddLog(HudText.LevelUp(e.Level, e.MaxHp, e.MaxMana));
            Changed?.Invoke();
        }

        // 3-7a: "소검을 들었다"
        private void OnWeaponEquipped(WeaponEquippedEvent e)
        {
            if (e.Unit != _playerId) return;
            Weapon = e.Weapon;
            AddLog(Text.Equipped(e.Weapon.ItemId));
            Changed?.Invoke();
        }

        private void OnMoved(EntityMovedEvent e)
        {
            if (e.Id == _playerId && e.From != e.To) _lastPickupFail = null; // 다른 칸으로 가면 다시 알릴 수 있게
        }

        // 층 이동 로그 (3-4a): "2층으로 내려왔다" · "1층으로 올라왔다"
        private void OnFloorChanged(ProjectM.Dungeon.FloorChangedEvent e)
        {
            AddLog(HudText.FloorChanged(e.From, e.To));
            Changed?.Invoke();
        }

        private void SetSlots(InventoryRef[] slots)
        {
            _quickSlots = (InventoryRef[])slots.Clone();
            _slotCosts = new int[_quickSlots.Length];
            for (int i = 0; i < _quickSlots.Length; i++)
                _slotCosts[i] = _spellBook != null && _quickSlots[i].Kind == InventoryKind.Spell && _spellBook.TryGet(_quickSlots[i].Id, out var spell) ? spell.ManaCost : -1;
        }

        private void OnSlotsChanged(QuickSlotsChangedEvent e)
        {
            if (e.Id != _playerId) return;
            SetSlots(e.Slots);
            Changed?.Invoke();
        }

        // 반응 로그 (3-1): "독화! 적 #3" · 빈 칸의 지형 반응은 이름만
        private void OnReaction(ReactionTriggeredEvent e)
        {
            AddLog(e.Target.IsValid ? $"{Text.Reaction(e.ReactionId)}! {Who(e.Target)}" : $"{Text.Reaction(e.ReactionId)}!");
            Changed?.Invoke();
        }

        public void Unbind()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<UnitDamagedEvent>(OnDamaged);
            _bus.Unsubscribe<UnitHealedEvent>(OnHealed);
            _bus.Unsubscribe<UnitDiedEvent>(OnDied);
            _bus.Unsubscribe<ManaChangedEvent>(OnManaChanged);
            _bus.Unsubscribe<SpellCastEvent>(OnSpellCast);
            _bus.Unsubscribe<StatusChangedEvent>(OnStatusChanged);
            _bus.Unsubscribe<QuickSlotsChangedEvent>(OnSlotsChanged);
            _bus.Unsubscribe<ReactionTriggeredEvent>(OnReaction);
            _bus.Unsubscribe<ProjectM.Dungeon.FloorChangedEvent>(OnFloorChanged);
            _bus.Unsubscribe<ItemPickedUpEvent>(OnPickedUp);
            _bus.Unsubscribe<ItemPickupFailedEvent>(OnPickupFailed);
            _bus.Unsubscribe<SpellLearnedEvent>(OnSpellLearned);
            _bus.Unsubscribe<EntityMovedEvent>(OnMoved);
            _bus.Unsubscribe<AttackMissedEvent>(OnMissed);
            _bus.Unsubscribe<ExpGainedEvent>(OnExpGained);
            _bus.Unsubscribe<LevelUpEvent>(OnLevelUp);
            _bus.Unsubscribe<WeaponEquippedEvent>(OnWeaponEquipped);
            _bus = null;
        }

        private HashSet<StatusType> Active(EntityId id)
        {
            if (!_activeStatuses.TryGetValue(id, out var set))
            {
                set = new HashSet<StatusType>();
                _activeStatuses[id] = set;
            }
            return set;
        }

        private string Who(EntityId id) => id == _playerId ? "나" : HudText.Enemy(id);

        // 2-6 자동 회복: HP 막대만 갱신, 로그는 남기지 않음 (10틱마다 한 줄씩 쌓이면 전투 로그가 묻힘)
        private void OnHealed(UnitHealedEvent e)
        {
            if (e.Target == _playerId)
            {
                Hp = e.Hp;
                MaxHp = e.MaxHp;
            }
        }

        private void OnDamaged(UnitDamagedEvent e)
        {
            bool fromStatus = !e.Attacker.IsValid; // 상태 이상(화상 · 중독) · 지형 피해
            if (e.Target == _playerId)
            {
                Hp = e.Hp;
                MaxHp = e.MaxHp;
                AddLog(fromStatus
                    ? $"나: 지속 피해 {e.Amount} (HP {e.Hp}/{e.MaxHp})"
                    : $"{HudText.Enemy(e.Attacker)}의 공격: 피해 {e.Amount} (HP {e.Hp}/{e.MaxHp})");
            }
            else if (e.Attacker == _playerId)
            {
                AddLog($"{HudText.Enemy(e.Target)}에게 피해 {e.Amount} ({e.Hp}/{e.MaxHp})");
            }
            else if (fromStatus)
            {
                AddLog($"{HudText.Enemy(e.Target)}: 지속 피해 {e.Amount} ({e.Hp}/{e.MaxHp})");
            }
            Changed?.Invoke();
        }

        private void OnStatusChanged(StatusChangedEvent e)
        {
            var active = Active(e.Id);
            bool wasActive = active.Contains(e.Type);
            if (e.Remaining > 0) active.Add(e.Type);
            else active.Remove(e.Type);

            if (e.Id == _playerId)
            {
                if (e.Remaining > 0) _playerStatuses[e.Type] = e.Remaining;
                else _playerStatuses.Remove(e.Type);
            }

            if (e.Applied && !wasActive) AddLog($"{Who(e.Id)}: {HudText.Status(e.Type)} {e.Remaining}턴");
            else if (!e.Applied && e.Remaining <= 0) AddLog($"{Who(e.Id)}: {HudText.Status(e.Type)} 풀림");
            Changed?.Invoke();
        }

        private void OnSpellCast(SpellCastEvent e)
        {
            if (e.Caster != _playerId) return;
            AddLog($"{Text.Spell(e.SpellId)} 시전");
            Changed?.Invoke();
        }

        private void OnManaChanged(ManaChangedEvent e)
        {
            if (e.Id != _playerId) return;
            Mana = e.Mana;
            MaxMana = e.MaxMana;
            Changed?.Invoke();
        }

        private void OnDied(UnitDiedEvent e)
        {
            _activeStatuses.Remove(e.Id);
            if (e.Id == _playerId)
            {
                Dead = true;
                Hp = 0;
                _playerStatuses.Clear();
                AddLog("쓰러졌다.");
            }
            else
            {
                AddLog($"{HudText.Enemy(e.Id)} 쓰러짐");
            }
            Changed?.Invoke();
        }

        private void AddLog(string line)
        {
            _log.Add(line);
            if (_log.Count > _maxLogLines) _log.RemoveRange(0, _log.Count - _maxLogLines);
        }
    }
}
