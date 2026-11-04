using ProjectM.Status;
using ProjectM.Units;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 허기 · 음식 수치 (Assets/_Project/Data/Units/HungerConfig.asset, 3-14 — SPD). 기준은 BALANCE.md 18번.
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Hunger Config", fileName = "HungerConfig")]
    public sealed class HungerConfigAsset : ScriptableObject
    {
        [Tooltip("허기가 이 값 이상이면 배고픔 (알림만) — SPD 300")] [Min(1)] [SerializeField] private int _hungryAt = 300;
        [Tooltip("허기가 이 값 이상이면 굶주림 (매 턴 피해 · 자동 회복 멈춤) — SPD 450")] [Min(2)] [SerializeField] private int _starvingAt = 450;
        [Tooltip("굶주림 중 매 턴 피해")] [Min(0)] [SerializeField] private int _starveDamage = 1;
        [Tooltip("먹기 행동 비용 (턴) — SPD 3")] [Min(1)] [SerializeField] private int _eatTicks = 3;
        [Tooltip("정체불명 고기를 먹었을 때 나쁜 상태 확률 %")] [Range(0, 100)] [SerializeField] private int _riskyFoodBadPercent = 60;
        [Tooltip("정체불명 고기의 나쁜 상태 후보")] [SerializeField] private StatusType[] _riskyFoodStatuses = { StatusType.Burn, StatusType.Slow, StatusType.Poison };
        [Tooltip("적이 죽을 때 떨어뜨리는 음식 (ItemCatalog ID)")] [SerializeField] private string _enemyDropItem = "food_mystery_meat";
        [Tooltip("적이 음식을 떨어뜨릴 확률 %")] [Range(0, 100)] [SerializeField] private int _enemyDropPercent = 15;
        [Tooltip("층마다 보장하는 음식")] [SerializeField] private string _floorFoodItem = "food_ration";
        [Tooltip("층마다 확률로 더하는 음식")] [SerializeField] private string _bonusFoodItem = "food_bread";
        [Tooltip("더하는 음식 확률 %")] [Range(0, 100)] [SerializeField] private int _bonusFoodPercent = 50;
        [Tooltip("판 시작 배낭의 음식")] [SerializeField] private string _startingFood = "food_ration";

        public HungerConfig ToConfig() => new HungerConfig(_hungryAt, _starvingAt, _starveDamage, _eatTicks, _riskyFoodBadPercent, _riskyFoodStatuses,
            _enemyDropItem, _enemyDropPercent, _floorFoodItem, _bonusFoodItem, _bonusFoodPercent, _startingFood);
    }
}
