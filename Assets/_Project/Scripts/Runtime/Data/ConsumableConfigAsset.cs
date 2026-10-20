using ProjectM.Items;
using ProjectM.Status;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 물약 · 주문서 수치 (Assets/_Project/Data/Items/ConsumableConfig.asset, 3-9). 기준은 BALANCE.md 14번.
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Consumable Config", fileName = "ConsumableConfig")]
    public sealed class ConsumableConfigAsset : ScriptableObject
    {
        [Header("식별 (SPD) — 판마다 종류에 섞어 나눠 주는 겉모습 (ItemVisualCatalog 키, 메뉴 ProjectM/Setup/Consumables)")]
        [SerializeField] private string[] _potionLooks =
        {
            "potion_crimson", "potion_amber", "potion_golden", "potion_jade", "potion_blue", "potion_sky",
            "potion_navy", "potion_magenta", "potion_bistre", "potion_charcoal", "potion_silver", "potion_ivory",
        };
        [SerializeField] private string[] _scrollLooks =
        {
            "scroll_tyr", "scroll_feoh", "scroll_ur", "scroll_thorn", "scroll_rad", "scroll_gyfu",
            "scroll_wynn", "scroll_haegl", "scroll_is", "scroll_sigel", "scroll_beorc", "scroll_oethel",
        };

        [Header("배치 — 층마다 무작위 (무게 = ItemCatalog Weight, 마지막 층 제외)")]
        [Min(0)] [SerializeField] private int _randomPotionsPerFloor = 2;
        [Min(0)] [SerializeField] private int _randomScrollsPerFloor = 2;

        [Header("효과")]
        [Tooltip("가스 · 불 · 냉기 물약이 퍼지는 걸음 수 (벽을 돌아감)")] [Min(0)] [SerializeField] private int _gasRadius = 2;
        [Tooltip("정화 물약 걸음 수")] [Min(0)] [SerializeField] private int _purityRadius = 3;
        [Tooltip("던지기 사거리 (칸)")] [Min(1)] [SerializeField] private int _throwRange = 8;
        [Tooltip("치유 물약이 푸는 상태")] [SerializeField] private StatusType[] _healingCures = { StatusType.Burn, StatusType.Slow, StatusType.Poison, StatusType.Paralyze };
        [Tooltip("정화 물약이 없애는 지형")] [SerializeField] private TerrainEffectType[] _purityClears = { TerrainEffectType.FireZone, TerrainEffectType.PoisonCloud, TerrainEffectType.ParalyticGas };

        [Header("응징 (SPD): 최대 체력 × Base% + 현재 체력 × 힘 × Hp%, 힘 = min(Cap, 잃은 체력 비율 × Scale)")]
        [Min(0)] [SerializeField] private int _retributionBasePercent = 10;
        [Min(0)] [SerializeField] private float _retributionHpPercent = 22.5f;
        [Min(0)] [SerializeField] private float _retributionPowerScale = 4.45f;
        [Min(0)] [SerializeField] private float _retributionPowerCap = 4f;

        public ConsumableConfig ToConfig() => new ConsumableConfig(_potionLooks, _scrollLooks, _randomPotionsPerFloor, _randomScrollsPerFloor,
            _gasRadius, _purityRadius, _throwRange, _healingCures, _purityClears,
            _retributionBasePercent, _retributionHpPercent, _retributionPowerScale, _retributionPowerCap);
    }
}
