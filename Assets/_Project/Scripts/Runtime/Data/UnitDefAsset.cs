using ProjectM.Units;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 유닛 종류 수치 에셋 (Assets/_Project/Data/Units/*.asset). 기준은 BALANCE.md 3번 (Q3 임시값).
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Unit Def", fileName = "UnitDef")]
    public sealed class UnitDefAsset : ScriptableObject
    {
        [Tooltip("모습 ID — EntityVisualCatalog의 항목")]
        [SerializeField] private string _visualId = "unit";
        [Min(1)] [SerializeField] private int _maxHp = 10;
        [Tooltip("근접 공격 한 번의 고정 피해")]
        [Min(0)] [SerializeField] private int _attackDamage = 1;
        [Tooltip("시야 반지름 (칸). 원형 범위, 벽에 가려진다")]
        [Min(1)] [SerializeField] private int _sightRange = 6;
        [Tooltip("최대 마나 (BALANCE 4번). 0이면 마나 없음")]
        [Min(0)] [SerializeField] private int _maxMana;
        [Tooltip("시작할 때 장착 칸 1번부터 채우는 주문 ID (주문 데이터는 2-2)")]
        [SerializeField] private string[] _startingSpells = new string[0];
        [Header("명중 · 회피 (3-7, SPD) · 경험치 (3-6)")]
        [Tooltip("명중 — 근접 공격 굴림 0~명중 (SPD attackSkill)")]
        [Min(0)] [SerializeField] private int _accuracy;
        [Tooltip("회피 — 막기 굴림 0~회피, 명중 굴림 ≥ 회피 굴림이면 맞음. 0이면 항상 맞음 (SPD defenseSkill)")]
        [Min(0)] [SerializeField] private int _evasion;
        [Tooltip("처치 경험치 (SPD EXP). 플레이어는 0")]
        [Min(0)] [SerializeField] private int _expReward;
        [Tooltip("플레이어 레벨이 이 값 이하일 때만 경험치를 준다 (SPD maxLvl, 반복 사냥 방지)")]
        [Min(0)] [SerializeField] private int _expMaxLevel;

        [Tooltip("힘 (3-7b, SPD — 플레이어 10). 무기 요구 힘과 비교. 적은 0")]
        [Min(0)] [SerializeField] private int _strength;

        public UnitDef ToDef() => new UnitDef(_visualId, _maxHp, _attackDamage, _sightRange, _maxMana, _startingSpells,
            _accuracy, _evasion, _expReward, _expMaxLevel, _strength);
    }
}
