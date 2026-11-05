using ProjectM.Dungeon;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 층 꾸미기 수치 (Assets/_Project/Data/Dungeon/FloorDecorConfig.asset, 3-11a — 풀 · 물 웅덩이, 3-11c — 문). 기준은 BALANCE.md 16번.
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Floor Decor Config", fileName = "FloorDecorConfig")]
    public sealed class FloorDecorConfigAsset : ScriptableObject
    {
        [Tooltip("층마다 풀 덩어리 수")] [Min(0)] [SerializeField] private int _grassPatchCount = 6;
        [Tooltip("풀 덩어리 최소 칸 수")] [Min(1)] [SerializeField] private int _grassPatchMinSize = 4;
        [Tooltip("풀 덩어리 최대 칸 수")] [Min(1)] [SerializeField] private int _grassPatchMaxSize = 14;
        [Tooltip("풀 칸 중 높은 풀(시야 가림) 확률 %")] [Range(0, 100)] [SerializeField] private int _tallGrassPercent = 50;
        [Tooltip("층마다 물 웅덩이 수 (사라지지 않는 물 지형)")] [Min(0)] [SerializeField] private int _puddleCount = 4;
        [Tooltip("웅덩이 최소 칸 수")] [Min(1)] [SerializeField] private int _puddleMinSize = 3;
        [Tooltip("웅덩이 최대 칸 수")] [Min(1)] [SerializeField] private int _puddleMaxSize = 9;
        [Tooltip("방 입구(복도가 방에 들어가는 한 칸 폭 길목)마다 문을 놓을 확률 % (3-11c)")] [Range(0, 100)] [SerializeField] private int _doorPercent = 70;
        [Tooltip("놓은 문이 숨겨진 문일 확률 % (3-12b) — 막으면 층이 끊기는 자리는 보통 문")] [Range(0, 100)] [SerializeField] private int _hiddenDoorPercent = 15;

        [Header("특수 방 (3-15b — BALANCE 16번)")]
        [Tooltip("층마다 특수 방이 생길 확률 % (생기면 하나)")] [Range(0, 100)] [SerializeField] private int _specialRoomPercent = 70;
        [Tooltip("종류 무게: 보물 방 · 무기고 · 도서관 · 정원")] [SerializeField] private int[] _specialRoomWeights = { 3, 2, 2, 2 };
        [Tooltip("방 안쪽 크기 (한 변) 최소 · 최대")] [Min(2)] [SerializeField] private int _specialRoomMinSize = 3;
        [Min(2)] [SerializeField] private int _specialRoomMaxSize = 5;
        [Tooltip("보물 방 아이템 (겹치지 않게 고름)")] [SerializeField] private string[] _vaultItems = { "scroll_upgrade", "potion_strength", "potion_experience", "armor_mail", "spear" };
        [Min(0)] [SerializeField] private int _vaultItemCount = 2;
        [SerializeField] private string[] _armoryItems = { "shortsword", "hand_axe", "spear", "quarterstaff", "dirk", "sickle", "knuckle", "armor_leather", "armor_mail" };

        /// <summary>에디터 Setup용 (3-22a 너클): 무기고 후보에 없으면 더한다. 더했으면 true.</summary>
        public bool AddArmoryItemIfMissing(string itemId)
        {
            if (System.Array.IndexOf(_armoryItems, itemId) >= 0) return false;
            var list = new System.Collections.Generic.List<string>(_armoryItems) { itemId };
            _armoryItems = list.ToArray();
            return true;
        }
        [Min(0)] [SerializeField] private int _armoryItemCount = 2;
        [SerializeField] private string[] _libraryItems = { "scroll_identify", "scroll_magic_mapping", "scroll_teleportation", "scroll_recharging", "scroll_mirror_image", "scroll_lullaby", "scroll_terror", "scroll_transmutation" };
        [Min(0)] [SerializeField] private int _libraryItemCount = 3;
        [SerializeField] private string[] _gardenItems = { "food_bread", "food_ration" };
        [Min(0)] [SerializeField] private int _gardenItemCount = 2;
        [Tooltip("보물 방 열쇠 (ItemCatalog, ItemUse.Key)")] [SerializeField] private string _keyItem = "key_iron";
        [Tooltip("연구실 무게 (3-16c)")] [Min(0)] [SerializeField] private int _laboratoryWeight = 2;
        [SerializeField] private string[] _labItems = { "seed_firebloom", "seed_icecap", "seed_sorrowmoss", "seed_sungrass", "seed_fadeleaf" };
        [Min(0)] [SerializeField] private int _labItemCount = 2;

        [Header("상점 (3-15c — BALANCE 16번)")]
        [Tooltip("상점이 있는 첫 층")] [Min(1)] [SerializeField] private int _shopFloor = 3;
        [Tooltip("상점이 더 있는 층 (2026-11-05 사용자 — 구역 시작층 6 · 11 · 16 · 21층)")] [SerializeField] private int[] _moreShopFloors = { 6, 11, 16, 21 };
        [SerializeField] private string[] _shopStock = { "potion_healing", "potion_frost", "scroll_identify", "scroll_magic_mapping", "scroll_teleportation", "scroll_recharging",
            "food_ration", "food_bread", "armor_leather", "shortsword", "hand_axe", "key_iron" };
        [Tooltip("파는 물건 수")] [Min(0)] [SerializeField] private int _shopStockCount = 6;
        [Tooltip("사는 값 = 아이템 값 × %")] [Min(1)] [SerializeField] private int _shopBuyPercent = 100;
        [Tooltip("파는 값 = 아이템 값 × %")] [Min(0)] [SerializeField] private int _shopSellPercent = 50;
        [SerializeField] private string _shopkeeperVisualId = "shopkeeper";
        [Min(1)] [SerializeField] private int _shopkeeperHp = 999;
        [Tooltip("3-29 화난 상인 체력 = 그 구역 보스 체력 × %")] [Min(1)] [SerializeField] private int _angryKeeperHpPercent = 150;
        [Tooltip("3-29 화난 상인이 차례마다 물건(해로운 물약 · 던지는 무기)을 던질 확률 %")] [Range(0, 100)] [SerializeField] private int _angryKeeperItemPercent = 35;
        [Tooltip("3-29 화난 상인이 치유 물약을 마시는 체력 % (이 아래)")] [Range(0, 100)] [SerializeField] private int _angryKeeperHealBelowPercent = 50;

        public ShopConfig ToShop() => new ShopConfig(_shopFloor, _shopStock, _shopStockCount, _shopBuyPercent, _shopSellPercent, _shopkeeperVisualId, _shopkeeperHp,
            _angryKeeperHpPercent, _angryKeeperItemPercent, _angryKeeperHealBelowPercent, _moreShopFloors);

        [Header("작업실 · 마법 제작대 (3-28b — BALANCE 24번)")]
        [Tooltip("유물 하나 만들기 (에테르)")] [Min(0)] [SerializeField] private int _relicCraftCost = 12;
        [Tooltip("유물 하나 분해 (에테르, 강화와 무관)")] [Min(0)] [SerializeField] private int _relicDisassembleGain = 6;
        [Tooltip("강화 비용 — +1, +2, +3 … (길이 = 최대 강화)")] [SerializeField] private int[] _relicUpgradeCosts = { 6, 9, 12 };
        [Tooltip("작업실 방 안쪽 크기 (최소 · 최대)")] [Min(2)] [SerializeField] private int _workshopMinSize = 3;
        [Min(2)] [SerializeField] private int _workshopMaxSize = 4;

        public ProjectM.Items.WorkbenchConfig ToWorkbench() =>
            new ProjectM.Items.WorkbenchConfig(_relicCraftCost, _relicDisassembleGain, _relicUpgradeCosts ?? new int[0], _workshopMinSize, _workshopMaxSize);

        public FloorDecorConfig ToConfig() => new FloorDecorConfig(_grassPatchCount, _grassPatchMinSize, _grassPatchMaxSize, _tallGrassPercent,
            _puddleCount, _puddleMinSize, _puddleMaxSize, _doorPercent, _hiddenDoorPercent,
            new SpecialRoomConfig(_specialRoomPercent, WithLab(), _specialRoomMinSize, _specialRoomMaxSize, _vaultItems, _vaultItemCount,
                _armoryItems, _armoryItemCount, _libraryItems, _libraryItemCount, _gardenItems, _gardenItemCount, _keyItem, _labItems, _labItemCount));

        /// <summary>보물 · 무기고 · 도서관 · 정원 무게 + 연구실 무게 (3-16c — 예전 에셋의 4칸 배열을 그대로 쓰려고 따로 둠).</summary>
        private int[] WithLab()
        {
            var w = new int[5];
            for (int i = 0; i < 4 && i < _specialRoomWeights.Length; i++) w[i] = _specialRoomWeights[i];
            w[4] = _laboratoryWeight;
            return w;
        }
    }
}
