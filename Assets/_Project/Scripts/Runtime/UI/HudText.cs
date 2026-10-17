using System.Collections.Generic;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
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

        public string Item(string id)
        {
            if (string.IsNullOrEmpty(id)) return "-";
            return _itemNames.TryGetValue(id, out var name) ? name : id;
        }

        /// <summary>인벤토리 · 빠른 칸 항목 이름. 빈 칸은 "-".</summary>
        public string Entry(InventoryRef entry)
        {
            switch (entry.Kind)
            {
                case InventoryKind.Spell: return Spell(entry.Id);
                case InventoryKind.Item: return Item(entry.Id);
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
                default: return type.ToString();
            }
        }

        public static string Enemy(EntityId id) => $"적 {id}";

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

        public const string DeadHint = "쓰러졌다 — R 키로 새 판";
    }
}
