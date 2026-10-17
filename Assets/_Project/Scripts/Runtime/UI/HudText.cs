using System.Collections.Generic;
using ProjectM.Magic;
using ProjectM.Status;
using EntityId = ProjectM.Core.EntityId;

namespace ProjectM.UI
{
    /// <summary>
    /// HUD · 로그 · 조준 안내의 한글 문구를 한곳에 모은다 (2-5). 규칙에는 쓰지 않는다.
    /// 주문 이름은 SpellCatalog의 DisplayName(데이터)에서 받고, 없으면 ID를 그대로 보인다.
    /// </summary>
    public sealed class HudText
    {
        private readonly IReadOnlyDictionary<string, string> _spellNames;
        private readonly IReadOnlyDictionary<string, string> _reactionNames;

        /// <param name="reactionNames">반응 ID → 로그 이름 (ReactionCatalog의 DisplayName, 3-1). 없으면 ID.</param>
        public HudText(IReadOnlyDictionary<string, string> spellNames = null, IReadOnlyDictionary<string, string> reactionNames = null)
        {
            _spellNames = spellNames ?? new Dictionary<string, string>();
            _reactionNames = reactionNames ?? new Dictionary<string, string>();
        }

        public string Spell(string id)
        {
            if (string.IsNullOrEmpty(id)) return "-";
            return _spellNames.TryGetValue(id, out var name) ? name : id;
        }

        public string Reaction(string id)
        {
            if (string.IsNullOrEmpty(id)) return "-";
            return _reactionNames.TryGetValue(id, out var name) ? name : id;
        }

        public static string Status(StatusType type)
        {
            switch (type)
            {
                case StatusType.Burn: return "화상";
                case StatusType.Slow: return "둔화";
                case StatusType.Poison: return "중독";
                case StatusType.Paralyze: return "마비";
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
                default: return block.ToString();
            }
        }

        // ── 조준 안내 ──
        public string AimHint(int slot, string spellId, int manaCost) =>
            $"[{slot + 1}] {Spell(spellId)} ({manaCost} MP): Space/클릭 시전 · Tab 다음 대상 · Esc 취소";

        public static string EmptySlot(int slot) => $"[{slot + 1}] 빈 칸";

        public string NotEnoughMana(int slot, string spellId, int mana, int cost) =>
            $"[{slot + 1}] {Spell(spellId)}: 마나 부족 ({mana}/{cost})";

        public const string DeadHint = "쓰러졌다 — R 키로 새 판";
    }
}
