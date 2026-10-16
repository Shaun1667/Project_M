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
        [Tooltip("상대를 알아채는 거리 (칸, 대각선 포함). 1-7 전까지는 벽을 무시한다")]
        [Min(1)] [SerializeField] private int _sightRange = 6;

        public UnitDef ToDef() => new UnitDef(_visualId, _maxHp, _attackDamage, _sightRange);
    }
}
