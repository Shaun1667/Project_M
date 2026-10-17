using ProjectM.Magic;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 마나 회복 · 빠른 칸 · 인벤토리 칸(3-2a) · 체력 자동 회복(2-6) 수치 에셋 (Assets/_Project/Data/Magic/MagicConfig.asset). 기준은 BALANCE.md 4번 (Q1 · Q2 임시값).
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Magic Config", fileName = "MagicConfig")]
    public sealed class MagicConfigAsset : ScriptableObject
    {
        [Tooltip("마나 회복 주기 (틱)")]
        [Min(1)] [SerializeField] private int _manaRegenIntervalTicks = 3;
        [Tooltip("한 번에 회복하는 마나")]
        [Min(0)] [SerializeField] private int _manaRegenAmount = 1;
        [Tooltip("빠른 칸 수 (키 1 ~ N, 3-2a — 예전 주문 장착 칸)")]
        [Range(1, 9)] [SerializeField] private int _spellSlotCount = 6;
        [Tooltip("인벤토리 칸 수 (3-2a, 고정 칸)")]
        [Min(1)] [SerializeField] private int _inventorySlotCount = 20;
        [Tooltip("플레이어 체력 자동 회복 주기 (틱). 0이면 회복 없음")]
        [Min(0)] [SerializeField] private int _hpRegenIntervalTicks = 10;
        [Tooltip("한 번에 회복하는 체력 = 최대 체력의 퍼센트 (올림 · 최소 1)")]
        [Range(0, 100)] [SerializeField] private int _hpRegenPercent = 1;

        public MagicConfig ToConfig() => new MagicConfig(_manaRegenIntervalTicks, _manaRegenAmount, _spellSlotCount,
            _hpRegenIntervalTicks, _hpRegenPercent, _inventorySlotCount);
    }
}
