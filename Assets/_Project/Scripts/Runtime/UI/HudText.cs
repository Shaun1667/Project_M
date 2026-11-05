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
            if (id.StartsWith(ProjectM.Units.GhostBoss.SpellPrefix)) return Spell(id.Substring(ProjectM.Units.GhostBoss.SpellPrefix.Length)); // 3-23b 그림자 보스 주문 = 원래 이름
            return _fusionNames.TryGetValue(id, out var fusion) ? fusion : id;
        }

        public string Reaction(string id)
        {
            if (string.IsNullOrEmpty(id)) return "-";
            return _reactionNames.TryGetValue(id, out var name) ? name : id;
        }

        /// <summary>이번 판 기록 (3-9 식별) — 있으면 모르는 물약 · 주문서는 겉모습 이름("진홍 물약")으로. GameRoot가 판마다 넣는다.</summary>
        public RunState Run { get; set; }

        /// <summary>그림을 고를 키 (P-3 ⓒ) — 모르든 알든 물약 · 주문서는 겉모습 키(그림은 식별돼도 같은 병), 나머지는 아이템 ID.</summary>
        public string VisualOf(string id) => Run?.LookOf(id) ?? id;

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
                case StatusType.Haste: return "신속";
                case StatusType.Invisible: return "투명";
                case StatusType.Levitating: return "부양";
                case StatusType.MindVision: return "정신 시야";
                case StatusType.Asleep: return "잠";
                case StatusType.Terrified: return "공포";
                default: return type.ToString();
            }
        }

        public static string Enemy(EntityId id) => $"{EnemyName?.Invoke(id) ?? "적"} {id}";

        /// <summary>3-17b 적 이름 (엔티티 → 종류 이름, HudModel이 생성 이벤트로 채움). null이면 모두 "적".</summary>
        public static System.Func<EntityId, string> EnemyName { get; set; }

        /// <summary>대상 고르기 창 제목 (3-10b): "식별 주문서 — 대상 고르기 (Esc 취소)".</summary>
        public string PickTitle(InventoryRef source) => $"{Entry(source)} — 대상 고르기  <size=70%><color=#AAAAAA>Esc 취소</color></size>";

        /// <summary>배낭 창 제목 (3-10b, SPD).</summary>
        public const string BackpackTitle = "배낭  <size=70%><color=#AAAAAA>I · F 열기 · Esc 닫기</color></size>";

        /// <summary>장비 5칸 이름 (3-10b — 결정 2026-10-20 · 3-28a 2026-11-05: 무기 · 갑옷 · 유물 · 유물 · 반지 — 유물 2칸, 반지는 틀만). 유물 칸 = 2 · 3번.</summary>
        public static readonly string[] EquipSlotNames = { "무기", "갑옷", "유물", "유물", "반지" };
        public const int FirstRelicSlot = 2;

        // ── 유물 (3-28a) ──
        public string RelicSameKind(string id) => $"{Item(id)}: 같은 유물은 하나만 낄 수 있다";
        public string RelicEquipped(string id, string previous) => previous == null ? $"{Item(id)}을(를) 꼈다" : $"{Item(id)}을(를) 꼈다 ({Item(previous)} → 배낭)";

        // ── 시간 유물 (3-28c) ──
        public string TimeRelicNotWorn(string id) => $"{Item(id)}: 유물 칸에 껴야 쓸 수 있다";
        public string TimeRelicEmpty(string id, int chargeTicks) => $"{Item(id)}: 게이지가 비었다 ({chargeTicks}턴마다 1칸)";
        public const string TimeRelicBusy = "이미 시간 유물 효과가 이어지고 있다";
        public static string TimeKindDesc(ProjectM.Items.TimeRelicKind kind, int gauge, int power)
        {
            switch (kind)
            {
                case ProjectM.Items.TimeRelicKind.PocketWatch: return $"내 시간을 멈춘다 — 최대 {gauge}턴 (그동안 행동 못 함 · 무적, 세계는 흐름)";
                case ProjectM.Items.TimeRelicKind.TimeMachine: return $"세계를 되돌린다 — 최대 {gauge}턴 전 (배낭 · 바닥 아이템은 지금 그대로)";
                case ProjectM.Items.TimeRelicKind.Accelerator: return $"시간을 가속 — {gauge}턴 동안 한 턴에 {power}번 행동";
                case ProjectM.Items.TimeRelicKind.Stopwatch: return $"나를 뺀 모든 것의 시간을 멈춘다 — 최대 {gauge}번 행동";
                default: return string.Empty;
            }
        }
        public string TimeRelicUsed(string id, ProjectM.Items.TimeRelicKind kind, int amount, int power)
        {
            switch (kind)
            {
                case ProjectM.Items.TimeRelicKind.PocketWatch: return $"{Item(id)}: 내 시간이 멈췄다 ({amount}턴)";
                case ProjectM.Items.TimeRelicKind.Accelerator: return $"{Item(id)}: 시간이 빨라졌다 ({amount}턴 동안 한 턴에 {power}번)";
                case ProjectM.Items.TimeRelicKind.Stopwatch: return $"{Item(id)}: 세계가 멈췄다 (행동 {amount}번)";
                default: return $"{Item(id)}을(를) 썼다";
            }
        }
        public static string TimeRelicEnded(ProjectM.Items.TimeRelicKind kind) =>
            kind == ProjectM.Items.TimeRelicKind.PocketWatch ? "멈춘 시간이 다시 흐른다"
            : kind == ProjectM.Items.TimeRelicKind.Accelerator ? "가속이 끝났다"
            : kind == ProjectM.Items.TimeRelicKind.Stopwatch ? "세계가 다시 움직인다" : string.Empty;
        public static string TimeMachineTitle(int charge) => $"타임머신 — 몇 턴 전으로? (게이지 {charge})";
        public static string TimeMachineTurns(int turns) => $"{turns}턴 전";
        public const string TimeMachineDetail = "위치 · 체력 · 상태 · 적이 그때로\n배낭 · 바닥 아이템은 지금 그대로";
        public const string TimeMachineNoHistory = "되돌아갈 기록이 없다 (층에 들어온 뒤의 턴만)";
        public static string TimeMachineRewound(int turns) => $"시간을 {turns}턴 되돌렸다";
        public static string TimeRelicGauge(int charge, int max) => $"게이지 {charge}/{max}";

        // ── 마법 제작대 · 에테르 (3-28b) ──
        public static string EtherLabel(int ether) => ether > 0 ? $"<color=#B39DDB>에테르 {ether}</color>" : string.Empty;
        public static string WorkbenchTitle(int ether) => $"마법 제작대 — 에테르 {ether}";
        public const string WorkbenchHint = "← → 고르기 · Enter/클릭 확정 · Esc 뒤로";
        public const string WorkbenchCraft = "유물 만들기";
        public const string WorkbenchUpgrade = "유물 강화";
        public const string WorkbenchDisassemble = "유물 분해";
        // 카드 폭에 맞춰 줄을 직접 나눔 (2026-11-05 플레이 확인 — 저절로 줄바꿈되면 "만든 / 다"처럼 끊김)
        public static string WorkbenchCraftDetail(int cost) => $"유물 목록에서\n골라 만든다\n\n에테르 {cost}";
        public static string WorkbenchUpgradeDetail(System.Collections.Generic.IReadOnlyList<int> costs) =>
            $"낀 유물이나\n배낭 유물을 강화\n(최대 +{costs.Count})\n\n에테르 {string.Join(" / ", costs)}";
        public static string WorkbenchDisassembleDetail(int gain) => $"배낭 유물을 없애고\n에테르를 얻는다\n(강화는 사라짐)\n\n에테르 +{gain}";
        public static string EtherCost(int cost) => $"에테르 {cost}";
        public static string EtherGain(int gain) => $"에테르 +{gain}";
        public const string RelicWornMark = " (낀 것)";
        public const string RelicMaxUpgrade = "최대 강화";
        public const string NoRelics = "강화할 유물이 없다";
        public const string NoRelicsInBackpack = "배낭에 분해할 유물이 없다";
        public static string WorkbenchFailed(ProjectM.Items.WorkbenchResult result) =>
            result == ProjectM.Items.WorkbenchResult.NotEnoughEther ? "에테르가 모자라다"
            : result == ProjectM.Items.WorkbenchResult.BackpackFull ? "배낭이 가득 찼다"
            : result == ProjectM.Items.WorkbenchResult.MaxUpgrade ? "이미 최대로 강화했다"
            : "할 수 없다";
        public string WorkbenchUsed(ProjectM.Items.WorkbenchAction action, string id, int upgrade, int ether) =>
            action == ProjectM.Items.WorkbenchAction.Craft ? $"{Item(id)}을(를) 만들었다 (에테르 {ether})"
            : action == ProjectM.Items.WorkbenchAction.Upgrade ? $"강화: {Item(id)}+{upgrade} (에테르 {ether})"
            : $"{Item(id)}{(upgrade > 0 ? "+" + upgrade : string.Empty)}을(를) 분해했다 (에테르 {ether})";

        /// <summary>유물 효과 한 줄 (3-28a, 데이터 → 글 — 유물마다 글을 따로 두지 않음).</summary>
        public static string RelicEffect(TalentEffectDef e, int points)
        {
            int v = e.ValueAt(points);
            string target = e.AppliesTo("__none__", ProjectM.Magic.Element.Fire) ? (e.AppliesTo("__none__", ProjectM.Magic.Element.Neutral) ? "모든 주문" : "속성 주문") : "주문";
            switch (e.Effect)
            {
                case TalentEffect.SpellDamage: return $"{target} 피해 {Signed(v)}";
                case TalentEffect.MaxHp: return $"최대 체력 {Signed(v)}";
                case TalentEffect.ManaRegenInterval: return $"마나 회복 간격 {Signed(v)}턴";
                case TalentEffect.HitManaChance: return $"근접 · {target}이 맞으면 {v}% 확률로 마나 +1";
                case TalentEffect.UnseenDamagePercent: return $"나를 못 본 적에게 {target} 피해 +{v}%";
                case TalentEffect.MeleeDamage: return $"근접 피해 {Signed(v)}";
                default: return $"{e.Effect} {Signed(v)}";
            }
        }

        private static string Signed(int v) => v >= 0 ? "+" + v : v.ToString();

        /// <summary>배낭 설명 줄 (3-10b): 고른 칸의 종류와 쓰는 법. 모르는 물약 · 주문서는 "정체를 모름".</summary>
        public static string ItemInfo(HudText text, ItemBook items, InventoryRef entry)
        {
            if (entry.IsEmpty) return string.Empty;
            if (entry.Kind == InventoryKind.Spell) return $"{text.Entry(entry)} — 주문. 사용하면 조준해 시전";
            if (items == null || !items.TryGet(entry.Id, out var def)) return text.Entry(entry);
            string count = entry.Count > 1 ? $" ({entry.Count}개)" : string.Empty;
            bool known = text.Run == null || text.Run.IsKnown(entry.Id);
            switch (def.Use)
            {
                case ItemUse.EquipWeapon: return $"{text.Entry(entry)} — 근접 무기. 사용 = 들기";
                case ItemUse.EquipArmor: return $"{text.Entry(entry)} — 갑옷. 사용 = 입기";
                case ItemUse.Potion: return $"{text.Entry(entry)}{count} — 물약. 마시기 · 던지기" + (known ? string.Empty : " · 정체를 모름");
                case ItemUse.Scroll: return $"{text.Entry(entry)}{count} — 주문서. 읽기" + (known ? string.Empty : " · 정체를 모름");
                case ItemUse.LearnSpell: return $"{text.Entry(entry)} — 마법서. 읽으면 주문을 배운다";
                case ItemUse.Fusion: return $"{text.Entry(entry)} — 마법 융합기. 주문 두 개를 합쳐 한 번에 시전";
                case ItemUse.Seed: return $"{text.Entry(entry)}{count} — 씨앗. 사용 = 발밑에 심기 · 던지기 = 그 칸에 심기 → {Trap(def.PlantTrap)} (밟으면 발동)";
                case ItemUse.Thrown: return $"{text.Entry(entry)}{count} — 던지는 무기. 던지기 = 피해 {def.ThrowMinDamage}~{def.ThrowMaxDamage}" + (def.BreakPercent > 0 ? $" · {def.BreakPercent}%로 부러짐" : " · 다시 주울 수 있음");
                case ItemUse.Key: return $"{text.Entry(entry)}{count} — 열쇠. 잠긴 문에 부딪히면 열린다";
                case ItemUse.Food: return $"{text.Entry(entry)}{count} — 음식. 먹으면 허기 −{def.Food}" + (def.Risky ? " · 탈이 날 수 있음 (불에 구우면 안전)" : string.Empty);
                case ItemUse.Relic:
                {
                    if (def.TimeKind != ProjectM.Items.TimeRelicKind.None) // 3-28c 시간 유물
                        return $"{text.Entry(entry)} — 시간 유물. 유물 칸에 끼고 빠른 칸으로 쓴다\n"
                            + TimeKindDesc(def.TimeKind, ProjectM.Items.TimeRelics.MaxCharge(items, entry), ProjectM.Items.TimeRelics.Power(items, entry));
                    var lines = new System.Collections.Generic.List<string>();
                    foreach (var e in def.RelicEffects) lines.Add(RelicEffect(e, ProjectM.Items.Relics.PointsOf(entry)));
                    return $"{text.Entry(entry)} — 유물. 사용 = 끼기 (유물 칸 2 · 같은 종류는 하나)\n" + string.Join(" · ", lines);
                }
                default: return text.Entry(entry);
            }
        }

        public static string Planted(ProjectM.Dungeon.TrapType plant) => $"{Trap(plant)}을(를) 심었다";
        public string ThrownBroke(string id) => $"{Item(id)}이(가) 부러졌다";

        /// <summary>연금 솥 (3-16c).</summary>
        public static string BrewTitle(int picked) => $"연금 솥 — 씨앗 {picked + 1}/3 고르기  <size=70%><color=#AAAAAA>Esc 그만두기</color></size>";
        public const string NotEnoughSeeds = "씨앗이 3개 있어야 한다";
        /// <summary>3-17d 패턴 보스.</summary>
        public static string BossCharging(EntityId boss) => $"{Enemy(boss)}이(가) 힘을 모은다! 붉은 칸에서 벗어나라";
        public static string BossTeleported(EntityId boss) => $"{Enemy(boss)}이(가) 순간이동했다";
        public static string BossSummoned(EntityId boss, int count) => $"{Enemy(boss)}이(가) 부하 {count}을(를) 불러냈다";

        /// <summary>3-17a 중간 보스.</summary>
        public const string StairsUnsealed = "보스를 쓰러뜨렸다 — 내려가는 계단이 열렸다";
        public string Brewed(string id) => $"연금 솥에서 {Item(id)}이(가) 만들어졌다";

        /// <summary>상점 (3-15c).</summary>
        public const string SellTitle = "팔기 — 고른 물건을 값의 절반에  <size=70%><color=#AAAAAA>Esc 닫기</color></size>";
        public const string NothingToSell = "팔 물건이 없다";
        // 3-29 화난 상인
        public const string KeeperName = "상인";
        public static string ShopkeeperAngered(int taken) => taken > 0 ? $"상인이 분노했다! 가게 물건 {taken}개를 챙겨 덤벼든다" : "상인이 분노했다!";
        public string Bought(string id, int price) => $"샀다: {Item(id)} (금화 {price})";
        public string Sold(string id, int price) => $"팔았다: {Item(id)} (금화 {price})";
        public string CannotAfford(string id, int price) => $"{Item(id)} — 금화 {price}개가 필요하다";

        /// <summary>잠긴 문 (3-15b).</summary>
        public const string DoorLocked = "잠겨 있다 — 열쇠가 필요하다";
        public const string DoorUnlocked = "열쇠로 문을 열었다";

        /// <summary>금화 (3-15a): HUD 표시 · 주웠을 때 로그.</summary>
        public static string GoldLabel(int gold) => $"<color=#FFD54F>금화 {gold}</color>";
        public static string GoldPicked(int amount) => $"금화 {amount}개를 주웠다";

        /// <summary>허기 단계 로그 · HUD 표시 (3-14).</summary>
        public static string HungerLog(HungerState state) =>
            state == HungerState.Starving ? "굶주리고 있다! 무언가 먹어야 한다" : state == HungerState.Hungry ? "배가 고프다" : "배가 부르다";
        public static string HungerLabel(HungerState state) =>
            state == HungerState.Starving ? "<color=#FF5050>굶주림</color>" : state == HungerState.Hungry ? "<color=#FFC46B>배고픔</color>" : string.Empty;
        public string Eaten(string itemId) => $"{Item(itemId)}을(를) 먹었다";
        public string Cooked(string from, string to) => $"{Item(from)} → {Item(to)}";

        /// <summary>변환 로그 (3-13b): "변환: 소검 → 손도끼" · 바꿀 것 없음.</summary>
        public string Transmuted(string from, string to) =>
            from == null ? "변환할 것을 고르지 않았다" : to == null ? $"{Item(from)}: 바꿀 것이 없다" : $"변환: {Item(from)} → {Item(to)}";

        /// <summary>함정 이름 (3-12a).</summary>
        public static string Trap(ProjectM.Dungeon.TrapType type)
        {
            switch (type)
            {
                case ProjectM.Dungeon.TrapType.Fire: return "화염 함정";
                case ProjectM.Dungeon.TrapType.ToxicGas: return "독가스 함정";
                case ProjectM.Dungeon.TrapType.ParalyticGas: return "마비 가스 함정";
                case ProjectM.Dungeon.TrapType.Flood: return "물 함정";
                case ProjectM.Dungeon.TrapType.Shock: return "번개 함정";
                case ProjectM.Dungeon.TrapType.Teleport: return "순간이동 함정";
                case ProjectM.Dungeon.TrapType.Alarm: return "경보 함정";
                case ProjectM.Dungeon.TrapType.Firebloom: return "화염꽃";
                case ProjectM.Dungeon.TrapType.Icecap: return "얼음버섯";
                case ProjectM.Dungeon.TrapType.Sorrowmoss: return "독이끼";
                case ProjectM.Dungeon.TrapType.Sungrass: return "햇볕풀";
                case ProjectM.Dungeon.TrapType.Fadeleaf: return "사라짐잎";
                default: return type.ToString();
            }
        }

        /// <summary>함정 발동 로그 (3-12a): "나: 화염 함정 발동!"</summary>
        public static string TrapTriggered(string who, ProjectM.Dungeon.TrapType type) => $"{who}: {Trap(type)} 발동!";

        /// <summary>찾기 행동 로그 (3-12b): 아무것도 못 찾았을 때만 (찾은 것은 각각 로그).</summary>
        public const string SearchFoundNothing = "주변을 살폈지만 아무것도 없다";
        public const string HiddenDoorFound = "숨겨진 문을 찾았다";

        // ── 정보 모드 (3-24, SPD — E) ──
        public const string ExamineHint = "정보: 방향키 · 마우스로 칸 고르기 · E 한 번 더 = 주변 8칸 찾기 (1턴) · Esc/우클릭 닫기";
        public const string CellUnknown = "알 수 없는 곳";

        // ── 타이틀 · 모드 · 시드 · 저장 · 실시간 (3-27) ──
        public const string GameTitle = "MoaDungeon"; // 2026-11-05 사용자: 타이틀 이름 (~~Project M~~)
        /// <summary>디버그 순간이동 칸 고르기 안내 (2026-11-05).</summary>
        public const string DebugTeleportPick = "[디버그] 순간이동할 칸을 누르세요 (Esc · 오른쪽 클릭 = 취소)";
        public const string GameSubtitle = "원소와 시간의 던전";
        public static string TitleItemName(TitleItem item) =>
            item == TitleItem.Continue ? "이어하기" : item == TitleItem.NewGame ? "게임 시작" : item == TitleItem.HallOfFame ? "명예의 전당" : "종료";

        // 3-33 명예의 전당
        public const string HallOfFameTitle = "명예의 전당 — 클리어 기록 (높은 점수 순)";
        public const string HallOfFameEmpty = "<color=#888888>아직 클리어 기록이 없습니다.\n26층 시간의 모래시계를 손에 넣으면 여기에 남습니다.</color>";
        public const string HallOfFameHint = "Enter · Esc · 클릭 뒤로";
        public const string HallOfFameBack = "뒤로";
        /// <summary>명예의 전당에 한 번에 보이는 기록 수.</summary>
        public const int HallOfFameShown = 10;

        /// <summary>기록 한 줄 (두 줄 — 위: 순위 · 점수 · 캐릭터 · 레벨 · 턴 · 처치 · 모드, 아래: 날짜 · 시드 · 유물).</summary>
        public static string HallOfFameLine(int rank, ProjectM.Dungeon.HallOfFameRecord r)
        {
            string who = string.IsNullOrEmpty(r.Form) ? r.Character : $"{r.Character} ({r.Form})";
            string relics = r.Relics.Count > 0 ? string.Join(", ", r.Relics) : "없음";
            return $"<color=#FFD54F>{rank}위</color>  <b>{N0(r.Score)}점</b>  {who} · Lv {r.Level} · {N0(r.Turns)}턴 · 처치 {r.Kills} · {ModeName(r.Mode)}\n"
                + $"<size=80%><color=#AAAAAA>      {r.Date} · 시드 {(long)r.Seed} · 유물: {relics}</color></size>";
        }

        /// <summary>결과 화면 · 로그: 명예의 전당에 오른 순위 (0 = 순위 밖).</summary>
        public static string HallOfFameRanked(int rank, int score) =>
            rank > 0 ? $"명예의 전당 {rank}위에 올랐다! ({N0(score)}점)" : $"명예의 전당 순위 밖 ({N0(score)}점)";
        private static string N0(long n) => n.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        public const string TitleHint = "↑ ↓ 고르기 · Enter/클릭";
        public const string NewGameTitle = "게임 시작 — 모드 고르기";
        public const string ModeTurnName = "턴제";
        public const string ModeTurnDesc = "내가 행동해야 시간이 흐른다\n\n천천히 생각하며 한 수씩";
        public const string ModeRealName = "실시간";
        public const string ModeRealDesc = "1초마다 시간이 흐른다\n\n내 행동은 기다림 없이 바로 —\n원하는 만큼 움직이고 쏜다\nP 일시정지";
        public static string ModeName(RunMode mode) => mode == RunMode.RealTime ? ModeRealName : ModeTurnName;
        public const string SeedLabel = "시드";
        public const string SeedPlaceholder = "비우면 무작위 (숫자 또는 글자)";
        public const string StartButton = "시작";
        public const string NewGameHint = "← → 모드 · 글자 입력 = 시드 · Backspace 지우기 · Enter 시작 · Esc 뒤로";
        public const string Paused = "일시정지 — P로 계속";

        // ── 저장 슬롯 (3-30) ──
        public const string SlotsTitle = "저장 슬롯 — 고르기";
        public const string SlotHint = "↑ ↓ 고르기 · Enter/클릭 · Esc 뒤로";
        public const string SlotChoiceHint = "← → 고르기 · Enter/클릭 · Esc 뒤로";
        public const string SlotEmpty = "비어 있음 — 새 게임";
        public static string SlotLabel(int slot, bool filled, string label) => $"슬롯 {slot}   " + (filled ? label : $"<color=#888888>{SlotEmpty}</color>");
        public const string SlotStart = "시작";
        public const string SlotDelete = "데이터 삭제";
        public const string SlotBack = "뒤로";
        public static string SlotDeleteQuestion(int slot) => $"슬롯 {slot}의 데이터를 지울까요?\n<size=70%><color=#AAAAAA>되돌릴 수 없습니다</color></size>";
        public const string SlotDeleteYes = "삭제";
        public const string SlotDeleteNo = "취소";
        public const string SlotUnreadable = "<color=#FF8A65>읽을 수 없는 저장 (버전이 다르거나 깨짐)</color>";

        /// <summary>슬롯 한 줄: "루멘 · Lv 5 · 7층 · 턴제".</summary>
        public static string SlotLine(string character, int level, int floor, RunMode mode) => $"{character} · Lv {level} · {floor}층 · {ModeName(mode)}";

        /// <summary>찬 슬롯 요약 (3-30 — 캐릭터 · 능력치 · 현재 층 · 장비 · 유물 · 재화 · 진행).</summary>
        public string SlotDetail(string character, string formName, int level, int exp, int hp, int maxHp, int mana, int maxMana, int strength, int accuracy, int evasion,
            string weapon, string armor, System.Collections.Generic.IReadOnlyList<string> relics, int floor, RunMode mode, long seed, int gold, int ether, int kills, long turns)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"<b>{character}</b>{(string.IsNullOrEmpty(formName) ? string.Empty : "  <color=#AAAAAA>" + formName + "</color>")}\n");
            sb.Append($"Lv {level}  (경험치 {exp})    HP {hp}/{maxHp}    MP {mana}/{maxMana}\n");
            sb.Append($"힘 {strength}    명중 {accuracy}    회피 {evasion}\n");
            sb.Append($"무기: {weapon ?? NoEquipment}    갑옷: {armor ?? NoEquipment}\n");
            sb.Append($"유물: {(relics != null && relics.Count > 0 ? string.Join(" · ", relics) : NoEquipment)}\n\n");
            sb.Append($"<b>{floor}층</b>    {ModeName(mode)}    {turns}턴    처치 {kills}\n");
            sb.Append($"금화 {gold}    에테르 {ether}    <color=#888888>시드 {seed}</color>");
            return sb.ToString();
        }
        public const string RealTimeStarted = "실시간 모드 — 1초마다 시간이 흐른다 (P 일시정지)";
        public const string RunContinued = "저장한 곳에서 이어한다";

        // ── 공격 대상 표시 (3-25, SPD) ──
        public const string AttackIndicatorHint = "Q 공격 · Tab 다음 적";
        public static string AttackTargetName(Unit target) => $"{Enemy(target.Id)}  HP {target.Hp}/{target.MaxHp}";

        public static string Tile(TileType type)
        {
            switch (type)
            {
                case TileType.Floor: return "바닥";
                case TileType.Wall:
                case TileType.HiddenDoor: return "벽"; // 숨겨진 문은 찾기 전까지 벽
                case TileType.StairsDown: return "내려가는 계단";
                case TileType.StairsUp: return "올라가는 계단";
                case TileType.Grass: return "풀";
                case TileType.TallGrass: return "키 큰 풀";
                case TileType.Door: return "닫힌 문";
                case TileType.OpenDoor: return "열린 문";
                case TileType.LockedDoor: return "잠긴 문";
                case TileType.AlchemyPot: return "연금 솥";
                case TileType.SealedStairs: return "봉인된 계단";
                case TileType.ArtifactBench: return "마법 제작대";
                default: return CellUnknown;
            }
        }

        /// <summary>
        /// 정보 모드 칸 설명 (3-24): 유닛(이름 · 체력 · 상태) · 지형 효과(남은 턴) — 지금 보이는 칸만, 함정(드러난 것) · 바닥 아이템 · 칸 종류 — 가 본 칸.
        /// 한 줄에 하나, 위에서부터 유닛 → 지형 효과 → 함정 → 아이템 → 칸.
        /// </summary>
        public string CellInfo(World world, GridPos pos, bool explored, bool visible)
        {
            if (world == null || !world.Map.InBounds(pos) || !explored) return CellUnknown;
            var lines = new List<string>();
            if (visible && world.Units.TryGetAt(pos, out var unit) && unit.IsAlive) lines.Add(UnitInfo(world, unit));
            if (visible)
                foreach (var t in world.Terrain.At(pos))
                    lines.Add(t.Remaining == TerrainLayer.Permanent ? Terrain(t.Type) : $"{Terrain(t.Type)} ({t.Remaining}턴)");
            if (world.Traps.TryGet(pos, out var trap) && !trap.Hidden) lines.Add($"<color=#FF8A65>{Trap(trap.Type)}</color>");
            foreach (var item in world.FloorItems.ItemsAt(pos))
            {
                string name = item.ItemId == Loot.GoldItemId ? $"금화 {item.Quantity}개" : item.Quantity > 1 ? $"{Item(item.ItemId)} ×{item.Quantity}" : Item(item.ItemId);
                lines.Add(item.Price > 0 ? $"{name} — 금화 {item.Price}" : name);
            }
            lines.Add($"<color=#AAAAAA>{Tile(world.Map.Get(pos))}</color>");
            return string.Join("\n", lines);
        }

        private static string UnitInfo(World world, Unit unit)
        {
            string name = unit.Id == world.Scheduler.PlayerId ? "나"
                : unit.Faction == Faction.Ally ? "아군"
                : unit.Faction == Faction.Neutral ? "상인"
                : Enemy(unit.Id);
            var sb = new System.Text.StringBuilder($"<b>{name}</b>  HP {unit.Hp}/{unit.MaxHp}");
            foreach (var s in unit.Statuses) sb.Append($" · {Status(s.Type)} {s.Remaining}턴");
            return sb.ToString();
        }

        /// <summary>함정 발견 로그 (3-12b): "함정 발견: 화염 함정"</summary>
        public static string TrapFound(ProjectM.Dungeon.TrapType type) => $"함정 발견: {Trap(type)}";

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
            $"{SlotPrefix(slot)}{Spell(spellId)} ({manaCost} MP): {(slot >= 0 ? $"{slot + 1} 다시 · " : string.Empty)}Space/클릭 시전 · Tab 다음 대상 · Esc 취소"; // 3-26

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

        public const string DeadHint = "쓰러졌다 — R 키로 타이틀";

        // ── 캐릭터 선택 (2026-10-21) ──
        public const string CharacterSelectTitle = "캐릭터 선택";
        public const string CharacterSelectHint = "← → 고르기 · Enter/클릭 시작";
        // 3-21b 직업 형태 선택 창 · 로그
        public static string FormSelectTitle(string characterName) => string.IsNullOrEmpty(characterName) ? "형태를 고르세요" : $"{characterName} — 형태를 고르세요"; // 2026-11-05: 캐릭터마다 (~~"빛의 창 — …" 고정~~)
        public const string FormSelectHint = "← → 고르기 · Enter/클릭 확정 (한 번만 고를 수 있음)";

        // 3-23b 지난 회차 플레이어 보스
        public static string GhostName(string characterName) => $"{characterName}의 그림자";
        public const string GhostDefeated = "그림자를 쓰러뜨렸다 — 지난 회차의 기록이 사라졌다";

        // 3-21c 특성 창 · 로그
        public static string TalentSelectTitle(int tier, int unspent) => $"특성 — {tier}단계 (남은 점수 {unspent})";
        public const string TalentSelectHint = "← → 고르기 · Enter/클릭 1점 찍기 · Esc 닫기";
        public const string NoTalentPoints = "특성 점수가 없다 (레벨이 오르면 1점)";
        public const string TalentMaxed = "그 특성은 이미 최대 점수다";
        public static string TalentPointsReady(int points) => $"특성 점수 {points}점 — T로 찍기";

        /// <summary>특성 ID → 표시 이름 (ClassCatalog, 3-21c). 없으면 ID.</summary>
        public IReadOnlyDictionary<string, string> TalentNames { get; set; }

        public string TalentSpent(string talentId, int points, int maxPoints)
        {
            string name = TalentNames != null && talentId != null && TalentNames.TryGetValue(talentId, out var n) ? n : talentId;
            return $"특성: {name} {points}/{maxPoints}";
        }

        /// <summary>형태 ID → 표시 이름 (ClassCatalog, 3-21b). 없으면 ID.</summary>
        public IReadOnlyDictionary<string, string> FormNames { get; set; }

        /// <summary>형태를 골랐다 — 배운 주문 이름들 (3-21b).</summary>
        public string FormChosen(string formId, IReadOnlyList<string> learned)
        {
            string name = FormNames != null && formId != null && FormNames.TryGetValue(formId, out var n) ? n : formId;
            if (learned == null || learned.Count == 0) return $"{Eul(name)} 골랐다";
            var spells = new List<string>();
            foreach (var id in learned) spells.Add(Spell(id));
            return $"{Eul(name)} 골랐다 — 새 기술: {string.Join(", ", spells)}";
        }

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
        public static string StrengthLost(int strength) => $"힘이 내려갔다 (힘 {strength})"; // 디버그 창 힘 − (2026-11-05)
        public string Upgraded(EquippedWeapon weapon) => $"강화: {WeaponName(weapon)} ({weapon.MinDamage}~{weapon.MaxDamage}, 요구 힘 {weapon.StrengthRequirement})";
        public const string NoWeaponToUpgrade = "강화할 장비가 없다 — 무기를 들거나 갑옷을 입어야 한다";

        // ── 3-10a 갑옷 ──
        public string Worn(string itemId) => $"{Eul(Item(itemId))} 입었다";
        /// <summary>갑옷 이름 + 강화 ("가죽 갑옷+2").</summary>
        public string ArmorName(EquippedArmor armor) => armor.Upgrade > 0 ? $"{Item(armor.ItemId)}+{armor.Upgrade}" : Item(armor.ItemId);
        /// <summary>HUD 갑옷 줄: "갑옷: 천 갑옷 방어 0~1 · 힘 10/10", 맨몸이면 "갑옷: 없음".</summary>
        public string ArmorLabel(EquippedArmor armor, int strength) => armor.IsEmpty
            ? "갑옷: 없음"
            : $"갑옷: {ArmorName(armor)} 방어 {armor.ShownMin}~{armor.ShownMax} · 힘 {strength}/{armor.StrengthRequirement}";
        public string ArmorUpgraded(EquippedArmor armor) => $"강화: {ArmorName(armor)} (방어 {armor.ShownMin}~{armor.ShownMax}, 요구 힘 {armor.StrengthRequirement})";

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
        public const string ResultRestart = "R 키로 타이틀"; // 3-27: 판이 끝나면 타이틀 (모드 · 시드를 다시 고름)
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
                case DamageKind.Hunger: return "굶주림";
                default: return "알 수 없음";
            }
        }

        public static string StairsHint(bool down) => down ? "내려가는 계단 — Enter/이 칸 클릭으로 내려가기" : "올라가는 계단 — Enter/이 칸 클릭으로 올라가기";
    }
}
