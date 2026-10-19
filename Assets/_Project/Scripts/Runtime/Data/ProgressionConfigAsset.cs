using ProjectM.Units;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 경험치 · 레벨업 수치 (Assets/_Project/Data/Units/ProgressionConfig.asset, 3-6 — SPD 참고). 기준은 BALANCE.md 12번.
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Progression Config", fileName = "ProgressionConfig")]
    public sealed class ProgressionConfigAsset : ScriptableObject
    {
        [Tooltip("최대 레벨 (SPD 30). 도달하면 경험치를 더 쌓지 않는다")]
        [Min(1)] [SerializeField] private int _maxLevel = 30;
        [Tooltip("다음 레벨 필요 경험치 = 기본 + 레벨 × 레벨당 (SPD 5 + 5 × 레벨)")]
        [Min(0)] [SerializeField] private int _expBase = 5;
        [Min(0)] [SerializeField] private int _expPerLevel = 5;
        [Header("레벨업마다")]
        [Tooltip("최대 체력 증가 — 현재 체력도 같이 오름 (SPD +5)")]
        [Min(0)] [SerializeField] private int _hpPerLevel = 5;
        [Tooltip("최대 마나 증가 — 현재 마나도 같이 오름")]
        [Min(0)] [SerializeField] private int _manaPerLevel = 1;
        [Tooltip("명중 증가 (SPD +1)")]
        [Min(0)] [SerializeField] private int _accuracyPerLevel = 1;
        [Tooltip("회피 증가 (SPD +1)")]
        [Min(0)] [SerializeField] private int _evasionPerLevel = 1;

        public ProgressionConfig ToConfig() =>
            new ProgressionConfig(_maxLevel, _expBase, _expPerLevel, _hpPerLevel, _manaPerLevel, _accuracyPerLevel, _evasionPerLevel);
    }
}
