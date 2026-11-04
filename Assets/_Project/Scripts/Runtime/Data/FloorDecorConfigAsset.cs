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

        public FloorDecorConfig ToConfig() => new FloorDecorConfig(_grassPatchCount, _grassPatchMinSize, _grassPatchMaxSize, _tallGrassPercent,
            _puddleCount, _puddleMinSize, _puddleMaxSize, _doorPercent);
    }
}
