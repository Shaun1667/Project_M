using System;
using System.Collections.Generic;

namespace ProjectM.Magic
{
    /// <summary>기본 속성 7개 (CLAUDE.md 규칙 11). 끝에만 추가한다 (에셋이 정수로 저장).</summary>
    public enum Element
    {
        Fire = 0,
        Ice = 1,
        Lightning = 2,
        Poison = 3,
        Earth = 4,
        Wind = 5,
        Water = 6,
    }

    /// <summary>주문이 맞히는 모양 (2-2). 끝에만 추가한다.</summary>
    public enum SpellShape
    {
        /// <summary>단일 투사체: 조준선을 따라 날아가 처음 만난 유닛 하나.</summary>
        Bolt = 0,
        /// <summary>직선 관통: 조준 방향으로 사거리 끝(또는 벽)까지, 선 위의 모든 유닛.</summary>
        Beam = 1,
        /// <summary>범위 폭발: 투사체가 멈춘 칸 중심, 반지름(체비셰프) 안의 모든 유닛 — 시전자 포함.</summary>
        Burst = 2,
    }

    /// <summary>
    /// 주문 하나의 데이터 (2-2, Q4 임시값). 값은 SpellCatalogAsset에서 온다 — 기준은 BALANCE.md 5번.
    /// 상태 이상 · 지형 태그(2-3 · 2-4)는 이 데이터에 필드로 추가한다 — 마법끼리의 조합은 하드코딩하지 않는다 (규칙 9).
    /// </summary>
    public sealed class SpellDef
    {
        public string Id { get; }
        public Element Element { get; }
        public SpellShape Shape { get; }
        /// <summary>사거리 (체비셰프, ITargetingRule.InRange).</summary>
        public int Range { get; }
        /// <summary>맞은 유닛 하나당 고정 피해 (항상 명중 — 결정 2026-10-16).</summary>
        public int Damage { get; }
        public int ManaCost { get; }
        /// <summary>Burst 반지름. 다른 모양은 0.</summary>
        public int Radius { get; }

        public SpellDef(string id, Element element, SpellShape shape, int range, int damage, int manaCost, int radius = 0)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("주문 ID가 비어 있다.", nameof(id));
            if (range < 1) throw new ArgumentOutOfRangeException(nameof(range), range, "사거리는 1 이상이어야 한다.");
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage), damage, "피해는 0 이상이어야 한다.");
            if (manaCost < 0) throw new ArgumentOutOfRangeException(nameof(manaCost), manaCost, "마나 소모는 0 이상이어야 한다.");
            if (shape == SpellShape.Burst ? radius < 1 : radius != 0)
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "범위 폭발만 반지름 1 이상, 나머지는 0.");

            Id = id;
            Element = element;
            Shape = shape;
            Range = range;
            Damage = damage;
            ManaCost = manaCost;
            Radius = radius;
        }
    }

    /// <summary>주문 ID → 데이터. 같은 ID가 두 번 나오면 예외.</summary>
    public sealed class SpellBook
    {
        private readonly Dictionary<string, SpellDef> _spells = new Dictionary<string, SpellDef>();

        public SpellBook(IEnumerable<SpellDef> spells)
        {
            if (spells == null) throw new ArgumentNullException(nameof(spells));
            foreach (var spell in spells)
            {
                if (spell == null) throw new ArgumentException("null 주문", nameof(spells));
                if (_spells.ContainsKey(spell.Id)) throw new ArgumentException("같은 주문 ID가 두 번: " + spell.Id, nameof(spells));
                _spells.Add(spell.Id, spell);
            }
        }

        public int Count => _spells.Count;

        public bool TryGet(string id, out SpellDef spell)
        {
            spell = null;
            return id != null && _spells.TryGetValue(id, out spell);
        }
    }
}
