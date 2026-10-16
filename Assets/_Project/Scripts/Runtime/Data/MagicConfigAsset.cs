using ProjectM.Magic;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 마나 회복 · 주문 장착 칸 수치 에셋 (Assets/_Project/Data/Magic/MagicConfig.asset). 기준은 BALANCE.md 4번 (Q1 · Q2 임시값).
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Magic Config", fileName = "MagicConfig")]
    public sealed class MagicConfigAsset : ScriptableObject
    {
        [Tooltip("마나 회복 주기 (틱)")]
        [Min(1)] [SerializeField] private int _manaRegenIntervalTicks = 3;
        [Tooltip("한 번에 회복하는 마나")]
        [Min(0)] [SerializeField] private int _manaRegenAmount = 1;
        [Tooltip("주문 장착 칸 수 (키 1 ~ N)")]
        [Range(1, 9)] [SerializeField] private int _spellSlotCount = 4;

        public MagicConfig ToConfig() => new MagicConfig(_manaRegenIntervalTicks, _manaRegenAmount, _spellSlotCount);
    }
}
