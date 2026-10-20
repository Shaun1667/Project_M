using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using EntityId = ProjectM.Core.EntityId;

namespace ProjectM.UI
{
    /// <summary>
    /// HUD · 로그 · 조준 · 인벤토리 · 융합 안내의 한글 문구를 한곳에 모은다 (2-5, 3-2a, 3-2b). 규칙에는 쓰지 않는다.
    /// 주문 · 반응 · 아이템 이름은 각 Catalog의 DisplayName(데이터)에서 받고, 없으면 ID를 그대로 보인다.
    /// </summary>
    public sealed class HudText
    {
        private readonly IReadOnlyDictionary<string, string> _spellNames;
        private readonly IReadOnlyDictionary<string, string> _reactionNames;
        private readonly IReadOnlyDictionary<string, string> _itemNames;
        private readonly IReadOnlyDictionary<string, string> _fusionNames;

        /// <param name="reactionNames">반응 ID → 로그 이름 (ReactionCatalog의 DisplayName, 3-1). 없으면 ID.</param>
        /// <param name="itemNames">아이템 ID → 이름 (ItemCatalog의 DisplayName, 3-2a). 없으면 ID.</param>
        /// <param name="fusionNames">융합 ID → 이름 (FusionCatalog의 DisplayName, 3-2b). 융합 주문의 ID가 곧 융합 ID라 Spell()에서 함께 찾는다.</param>
        public HudText(IReadOnlyDictionary<string, string> spellNames = null, IReadOnlyDictionary<string, string> reactionNames = null,
            IReadOnlyDictionary<string, string> itemNames = null, IReadOnlyDictionary<string, string> fusionNames = null)
        {
            _spellNames = spellNames ?? new Dictionary<string, string>();
            _reactionNames = reactionNames ?? new Dictionary<string, string>();
            _itemNames = itemNames ?? new Dictionary<string, string>();
            _fusionNames = fusionNames ?? new Dictionary<string, string>();
        }

        /// <summary>주문 이름 — 융합 주문(3-2b)도 여기서 (로그 "용암 분출 시전").</summary>
        public string Spell(string id)
        {
            if (string.IsNullOrEmpty(id)) return "-";
            if (_spellNames.TryGetValue(id, out var name)) return name;
            return _fusionNames.TryGetValue(id, out var fusion) ? fusion : id;
        }

        public string Reaction(string id)
        {
            if (string.IsNullOrEmpty(id)) return "-";
            return _reactionNames.TryGetValue(id, out var name) ? name : id;
        }

        /// <summary>이번 판 기록 (3-9 식별) — 있으면 모르는 물약 · 주문서는 겉모습 이름("진홍 물약")으로. GameRoot가 판마다 넣는다.</summary>
        public RunState Run { get; set; }

        public string Item(string id)
        {
            if (string.IsNullOrEmpty(id)) return "-";
            if (Run != null && !Run.IsKnown(id)) return LookName(Run.LookOf(id));
            return KnownItem(id);
        }

        /// <summary>식별과 상관없는 진짜 이름 (데이터의 DisplayName).</summary>
        public string KnownItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return "-";
            return _itemNames.TryGetValue(id, out var name) ? name : id;
        }

        // ── 3-9 물약 · 주문서 겉모습 이름 (ConsumableArtSetup의 색 · 룬 키) ──
        private static readonly Dictionary<string, string> LookNames = new Dictionary<string, string>
        {
            { "potion_crimson", "진홍 물약" }, { "potion_amber", "호박색 물약" }, { "potion_golden", "황금 물약" }, { "potion_jade", "비취 물약" },
            { "potion_blue", "파란 물약" }, { "potion_sky", "하늘색 물약" }, { "potion_navy", "남색 물약" }, { "potion_magenta", "자홍 물약" },
            { "potion_bistre", "갈색 물약" }, { "potion_charcoal", "숯빛 물약" }, { "potion_silver", "은빛 물약" }, { "potion_ivory", "상아색 물약" },
            { "scroll_tyr", "티르 룬 주문서" }, { "scroll_feoh", "페오 룬 주문서" }, { "scroll_ur", "우르 룬 주문서" }, { "scroll_thorn", "소른 룬 주문서" },
            { "scroll_rad", "라드 룬 주문서" }, { "scroll_gyfu", "기푸 룬 주문서" }, { "scroll_wynn", "윈 룬 주문서" }, { "scroll_haegl", "하걸 룬 주문서" },
            { "scroll_is", "이스 룬 주문서" }, { "scroll_sigel", "시겔 룬 주문서" }, { "scroll_beorc", "베오르크 룬 주문서" }, { "scroll_oethel", "오셀 룬 주문서" },
        };

        /// <summary>겉모습 키 → 이름 ("진홍 물약"). 모르는 키면 키 그대로.</summary>
        public static string LookName(string look)
        {
            if (string.IsNullOrEmpty(look)) return "-";
            return LookNames.TryGetValue(look, out var name) ? name : look;
        }

        /// <summary>인벤토리 · 빠른 칸 항목 이름. 빈 칸은 "-".</summary>
        public string Entry(InventoryRef entry)
        {
            switch (entry.Kind)
            {
                case InventoryKind.Spell: return Spell(entry.Id);
                case InventoryKind.Item: return entry.Upgrade > 0 ? $"{Item(entry.Id)}+{entry.Upgrade}" : Item(entry.Id); // 3-7b 강화
                default: return "-";
            }
        }

        public static string Status(StatusType type)
        {
            switch (type)
            {
                case StatusType.Burn: return "화상";
                case StatusType.Slow: return "둔화";
                case StatusType.Poison: return "중독";
                case StatusType.Paralyze: return "마비";
                case StatusType.Wet: return "젖음";
                case StatusType.Frozen: return "빙결";
                default: return type.ToString();
            }
        }

        public static string Enemy(EntityId id) => $"적 {id}";

        // 3-6 · 3-7: 명중 · 경험치 · 레벨
        public static string PlayerMissed(EntityId target) => $"{Enemy(target)}에게 빗나감";
        public static string EnemyMissed(EntityId attacker) => $"{Enemy(attacker)}의 공격: 빗나감";
        public static string ExpGained(int amount) => $"경험치 +{amount}";
        public static string LevelUp(int level, int maxHp, int maxMana) => $"레벨 {level}! 최대 HP {maxHp} · MP {maxMana}";
        /// <summary>경험치 막대 글자. expToNext 0 = 최대 레벨.</summary>
        public static string ExpLabel(int level, int exp, int expToNext) =>
            expToNext > 0 ? $"Lv {level}  EXP {exp}/{expToNext}" : $"Lv {level} (최대)";

        public static string CastBlockReason(CastBlock block)
        {
            switch (block)
            {
                case CastBlock.NotEnoughMana: return "마나 부족";
                case CastBlock.OutOfRange: return "사거리 밖";
                case CastBlock.NotVisible: return "보이지 않는 칸";
                case CastBlock.TargetIsSelf: return "대상을 고르세요";
                case CastBlock.Blocked: return "막힘";
                case CastBlock.EmptySlot: return "빈 칸";
                case CastBlock.UnknownSpell: return "알 수 없는 주문";
                case CastBlock.NotASpell: return "주문이 아님";
                case CastBlock.NotOwned: return "가지고 있지 않은 주문";
                case CastBlock.NoFusionDevice: return "마법 융합기가 없다";
                case CastBlock.NoFusionRecipe: return "융합할 수 없는 조합";
                case CastBlock.SameSpell: return "같은 주문끼리는 융합하지 않는다";
                default: return block.ToString();
            }
        }

        // ── 조준 안내 ── slot = 빠른 칸 번호, -1 = 인벤토리에서 바로 씀
        public string AimHint(int slot, string spellId, int manaCost) =>
            $"{SlotPrefix(slot)}{Spell(spellId)} ({manaCost} MP): Space/클릭 시전 · Tab 다음 대상 · Esc 취소";

        public static string EmptySlot(int slot) => $"[{slot + 1}] 빈 칸";

        public string NotEnoughMana(int slot, string spellId, int mana, int cost) =>
            $"{SlotPrefix(slot)}{Spell(spellId)}: 마나 부족 ({mana}/{cost})";

        private static string SlotPrefix(int slot) => slot >= 0 ? $"[{slot + 1}] " : string.Empty;

        // ── 인벤토리 안내 (3-2a) ──
        public const string InventoryHint = "인벤토리: 방향키 · 마우스로 고르기 · Enter/클릭 메뉴 · 숫자 키 = 그 칸에 등록 · I/Esc 닫기";
        public static string AssignHint(int slot) => $"[{slot + 1}] 빈 칸 — 등록할 항목을 고르세요 (Enter/클릭 · Esc 취소)";
        public string ChooseSlotHint(InventoryRef entry) => $"{Entry(entry)}: 등록할 빠른 칸 번호(1~)를 누르거나 칸을 클릭 (Esc 취소)";
        public string Assigned(InventoryRef entry, int slot) => $"{Entry(entry)} → [{slot + 1}] 등록";
        public string Unassigned(InventoryRef entry, int slot) => $"{Entry(entry)}: [{slot + 1}] 등록 해제";
        public string ItemNotUsable(InventoryRef entry) => $"{Entry(entry)}: 쓸 수 없는 아이템";

        // ── 마법 융합 (3-2b) ──
        public const string FusionPickHint = "마법 융합: 주문 두 개를 고르세요 (Enter/클릭 · 같은 칸 다시 = 취소 · Esc 뒤로)";
        public string FusionSecondHint(InventoryRef first) => $"마법 융합: {Entry(first)} + ? — 두 번째 주문을 고르세요 (같은 칸 다시 = 취소 · Esc 뒤로)";
        /// <summary>인벤토리 창 제목. first가 비면 아직 하나도 안 고름.</summary>
        public string FusionPickTitle(InventoryRef first) => first.IsEmpty ? "마법 융합 — 주문 두 개 고르기" : $"마법 융합 — {Entry(first)} + ?";
        public string FusionAimHint(string firstId, string secondId, string fusionId, int manaCost) =>
            $"{Spell(fusionId)} ({Spell(firstId)} + {Spell(secondId)}, {manaCost} MP): Space/클릭 시전 · Tab 다음 대상 · Esc 취소";
        public string FusionBlocked(string firstId, string secondId, CastBlock block) => $"{Spell(firstId)} + {Spell(secondId)}: {CastBlockReason(block)}";

        public const string MenuUse = "사용";
        public const string MenuAssign = "등록하기";
        public const string MenuUnassign = "등록 해제";
        public const string MenuClose = "닫기";
        public const string MenuThrow = "던지기";

        public const string DeadHint = "쓰러졌다 — R 키로 새 판";

        // ── 마법서 (3-5) ──
        public string PickedUp(string itemId) => $"{Eul(Item(itemId))} 주웠다";
        // 3-7a 무기
        public string Equipped(string itemId) => $"{Eul(Item(itemId))} 들었다";
        /// <summary>무기 이름 + 강화 ("소검+2"), 강화 0이면 이름만.</summary>
        public string WeaponName(EquippedWeapon weapon) =>
            weapon.Upgrade > 0 ? $"{Item(weapon.ItemId)}+{weapon.Upgrade}" : Item(weapon.ItemId);

        /// <summary>
        /// HUD 무기 줄: "무기: 소검 1~5 · 힘 10/12" (요구 힘이 있으면 힘/요구), 맨손이면 "무기: 맨손 3".
        /// </summary>
        public string WeaponLabel(EquippedWeapon weapon, int unarmedDamage, int strength = 0) => weapon.IsEmpty
            ? $"무기: 맨손 {unarmedDamage}"
            : weapon.StrengthRequirement > 0
                ? $"무기: {WeaponName(weapon)} {weapon.MinDamage}~{weapon.MaxDamage} · 힘 {strength}/{weapon.StrengthRequirement}{DefenseSuffix(weapon)}"
                : $"무기: {WeaponName(weapon)} {weapon.MinDamage}~{weapon.MaxDamage}{DefenseSuffix(weapon)}";

        /// <summary>방어가 있으면 " · 방어 0~2" (3-7c).</summary>
        private static string DefenseSuffix(EquippedWeapon weapon) => weapon.Defense > 0 ? $" · 방어 0~{weapon.Defense}" : string.Empty;

        // 3-7b 힘 · 강화
        public static string StrengthGained(int strength) => $"힘이 올랐다 (힘 {strength})";
        public string Upgraded(EquippedWeapon weapon) => $"강화: {WeaponName(weapon)} ({weapon.MinDamage}~{weapon.MaxDamage}, 요구 힘 {weapon.StrengthRequirement})";
        public const string NoWeaponToUpgrade = "든 무기가 없다 — 강화 주문서를 쓸 수 없다";

        // ── 3-9 물약 · 주문서 ──
        /// <summary>쓴 한 줄: "진홍 물약을 마셨다 — 치유 물약이었다". 쓰기 전 몰랐으면 겉모습 이름, 쓰면서 식별됐으면 정체를 붙인다.</summary>
        public string ConsumableUsed(string itemId, string look, ConsumeMode mode, bool wasKnown, bool known)
        {
            string name = wasKnown ? KnownItem(itemId) : LookName(look);
            string verb = mode == ConsumeMode.Drink ? "마셨다" : mode == ConsumeMode.Read ? "읽었다" : "던졌다 — 깨졌다";
            string reveal = !wasKnown && known ? $" — {KnownItem(itemId)}{Ida(KnownItem(itemId))}" : string.Empty;
            return $"{Eul(name)} {verb}{reveal}";
        }

        /// <summary>효과 한 줄 (힘 · 강화 · 경험 · 식별은 각자 이벤트가 따로 적으므로 null).</summary>
        public static string ConsumableEffectText(ConsumableEffect effect)
        {
            switch (effect)
            {
                case ConsumableEffect.Healing: return "상처가 아물었다";
                case ConsumableEffect.Purity: return "주변 공기가 맑아졌다";
                case ConsumableEffect.ToxicGas: return "독가스가 퍼졌다";
                case ConsumableEffect.LiquidFlame: return "불길이 치솟았다";
                case ConsumableEffect.Frost: return "냉기가 퍼졌다";
                case ConsumableEffect.ParalyticGas: return "마비 가스가 퍼졌다";
                case ConsumableEffect.MagicMapping: return "이 층의 지도가 떠올랐다";
                case ConsumableEffect.Teleportation: return "순간이동했다";
                case ConsumableEffect.Recharging: return "마나가 가득 찼다";
                case ConsumableEffect.Rage: return "격노의 외침 — 적들이 몰려온다";
                case ConsumableEffect.Retribution: return "응징의 빛이 터졌다";
                default: return null;
            }
        }

        public string Identified(string itemId, string look) =>
            string.IsNullOrEmpty(itemId) ? "식별할 것이 없다" : $"{LookName(look)}: {KnownItem(itemId)}{Ida(KnownItem(itemId))}";

        public string ThrowAimHint(InventoryRef entry) => $"{Entry(entry)} 던지기: Space/클릭 던지기 · Tab 다음 대상 · Esc 취소";
        public string NotThrowable(InventoryRef entry) => $"{Entry(entry)}: 던질 수 없다";

        /// <summary>서술격 조사: 받침 있으면 "이었다", 없으면 "였다".</summary>
        public static string Ida(string word)
        {
            if (string.IsNullOrEmpty(word)) return "이었다";
            char last = word[word.Length - 1];
            if (last < '가' || last > '힣') return "이었다";
            return (last - 0xAC00) % 28 != 0 ? "이었다" : "였다";
        }
        // 3-7c 기습 · 방어
        public static string Surprise(EntityId target) => $"기습! {Enemy(target)}";
        public static string Blocked(int amount) => $"방어: {amount} 막음";
        public string PickupFull(string itemId) => $"인벤토리가 가득 차 {Eul(Item(itemId))} 줍지 못했다";
        public string Learned(string spellId, int quickSlot) =>
            quickSlot >= 0 ? $"{Eul(Spell(spellId))} 배웠다 → [{quickSlot + 1}]" : $"{Eul(Spell(spellId))} 배웠다 (빠른 칸이 가득 차 등록 안 함)";

        /// <summary>목적격 조사: 받침 있으면 "을", 없으면 "를" (한글이 아니면 "을(를)").</summary>
        public static string Eul(string word)
        {
            if (string.IsNullOrEmpty(word)) return word;
            char last = word[word.Length - 1];
            if (last < '가' || last > '힣') return word + "을(를)";
            return word + ((last - 0xAC00) % 28 != 0 ? "을" : "를");
        }
        public string AlreadyKnown(string spellId) => $"이미 알고 있는 주문: {Spell(spellId)}";

        // ── 층 (3-4a) ──
        public static string FloorChanged(int from, int to) => to > from ? $"{to}층으로 내려왔다" : $"{to}층으로 올라왔다";
        // ── 결과 화면 (3-4b) ──
        public static string ResultTitle(RunOutcome outcome) => outcome == RunOutcome.Victory ? "승리 — 보스를 쓰러뜨렸다" : "패배 — 쓰러졌다";
        public const string ResultRestart = "R 키로 새 판";
        public const string NoEquipment = "없음";

        public static string Terrain(TerrainEffectType type)
        {
            switch (type)
            {
                case TerrainEffectType.FireZone: return "화염 지대";
                case TerrainEffectType.PoisonCloud: return "독구름";
                case TerrainEffectType.Water: return "물 웅덩이";
                case TerrainEffectType.ContaminatedWater: return "오염된 물";
                case TerrainEffectType.IcePatch: return "빙판";
                case TerrainEffectType.ParalyticGas: return "마비 가스";
                default: return type.ToString();
            }
        }

        /// <summary>사망 원인 한 줄: 마지막으로 받은 피해의 출처.</summary>
        public string DeathCause(DamageCause cause, EntityId attacker, EntityId bossId, EntityId playerId)
        {
            switch (cause.Kind)
            {
                case DamageKind.Attack: return bossId.IsValid && attacker == bossId ? "보스의 공격" : $"{Enemy(attacker)}의 공격";
                case DamageKind.Spell: return attacker == playerId ? $"내 {Spell(cause.Id)}" : Spell(cause.Id);
                case DamageKind.Reaction: return attacker == playerId ? $"{Reaction(cause.Id)} (내 주문의 반응)" : Reaction(cause.Id);
                case DamageKind.Status: return Status(cause.Status);
                case DamageKind.Terrain: return Terrain(cause.Terrain);
                case DamageKind.Item: return KnownItem(cause.Id);
                default: return "알 수 없음";
            }
        }

        public static string StairsHint(bool down) => down ? "내려가는 계단 — Enter/이 칸 클릭으로 내려가기" : "올라가는 계단 — Enter/이 칸 클릭으로 올라가기";
    }
}
