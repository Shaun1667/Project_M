using ProjectM.Core;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// GameConfig의 데이터 에셋. 수치를 바꿀 때는 이 에셋(Assets/_Project/Data/Config/GameConfig.asset)만 고친다.
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이며, 기준은 BALANCE.md 1번이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Game Config", fileName = "GameConfig")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        [Tooltip("행동 비용을 따로 지정하지 않은 행동이 쓰는 틱 수 (GDD 2-4)")]
        [Min(1)]
        [SerializeField] private int _defaultActionCostTicks = 1;

        [Tooltip("제한해제(실시간) 모드에서 1틱의 길이, 초 (GDD 3-1)")]
        [Min(0.01f)]
        [SerializeField] private float _realtimeTickIntervalSeconds = 1f;

        public GameConfig ToConfig()
        {
            return new GameConfig(_defaultActionCostTicks, _realtimeTickIntervalSeconds);
        }
    }
}
