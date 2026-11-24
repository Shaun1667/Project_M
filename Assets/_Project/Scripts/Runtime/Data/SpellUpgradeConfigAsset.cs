using System.Collections.Generic;
using ProjectM.Magic;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 마법 강화 수치 (Assets/_Project/Data/Magic/SpellUpgradeConfig.asset, 3-55 — 강화 주문서로 배운 주문 강화). 기준은 BALANCE.md 26번.
    /// 필드 초기값은 새 에셋을 만들 때의 시작값일 뿐이다. 새 강화 종류 = SpellUpgradeKind 끝에 추가 + 표 한 줄 + SpellUpgrades.Fits · ApplyTo 한 줄.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Spell Upgrade Config", fileName = "SpellUpgradeConfig")]
    public sealed class SpellUpgradeConfigAsset : ScriptableObject
    {
        [System.Serializable]
        public sealed class Row
        {
            public SpellUpgradeKind Kind;
            [Tooltip("단계별 누적 값 (I · II · III …). 길이 = 최대 단계. 마나 절약은 줄이는 양")] public int[] Values;
        }

        [Tooltip("주문 하나에 강화 주문서를 쓸 수 있는 횟수 (모든 종류 단계 합)")] [Min(1)] [SerializeField] private int _maxPerSpell = 5;
        [Tooltip("한 번에 보여 주는 강화 카드 수")] [Min(1)] [SerializeField] private int _choiceCount = 3;
        [Tooltip("지속 강화 값 1당 지형 지속 틱 (상태는 1틱)")] [Min(0)] [SerializeField] private int _terrainTicksPerLevel = 2;
        [SerializeField] private List<Row> _upgrades = new List<Row>
        {
            new Row { Kind = SpellUpgradeKind.Damage, Values = new[] { 1, 2, 3 } },
            new Row { Kind = SpellUpgradeKind.Area, Values = new[] { 1, 2, 3 } },
            new Row { Kind = SpellUpgradeKind.StatusChance, Values = new[] { 15, 30, 45 } },
            new Row { Kind = SpellUpgradeKind.Duration, Values = new[] { 1, 2, 3 } },
            new Row { Kind = SpellUpgradeKind.ManaCost, Values = new[] { 1, 2, 3 } },
            new Row { Kind = SpellUpgradeKind.HitCount, Values = new[] { 1, 2, 3 } },
        };

        public SpellUpgradeConfig ToConfig()
        {
            var defs = new List<SpellUpgradeDef>();
            foreach (var row in _upgrades) defs.Add(new SpellUpgradeDef(row.Kind, row.Values));
            return new SpellUpgradeConfig(_maxPerSpell, _choiceCount, _terrainTicksPerLevel, defs);
        }
    }
}
