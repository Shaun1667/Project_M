using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Magic
{
    /// <summary>
    /// 융합 하나 = "속성 A + 속성 B → 결과" 규칙 한 줄 (3-2b, REACTIONS.md 5번). 값은 FusionCatalogAsset — BALANCE.md 10번.
    /// 순서는 상관없다 (불 + 물 = 물 + 불). 피해 · 모양 · 사거리 · 마나는 두 주문에서 계산하고(FusionBook.Build), 여기에는 결과만 둔다.
    /// </summary>
    public sealed class FusionDef
    {
        public string Id { get; }
        public Element ElementA { get; }
        public Element ElementB { get; }
        /// <summary>남기는 지형 (None = 없음). 지속은 TerrainDurationTicks (0이면 지형 규칙의 지속).</summary>
        public TerrainEffectType LeavesTerrain { get; }
        public int TerrainDurationTicks { get; }
        /// <summary>맞은 유닛에 거는 상태 (None = 없음 — 3-2b 결정: 두 주문의 상태는 걸지 않는다).</summary>
        public StatusType AppliesStatus { get; }
        public int StatusChancePercent { get; }

        public FusionDef(string id, Element elementA, Element elementB, TerrainEffectType leavesTerrain = TerrainEffectType.None,
            int terrainDurationTicks = 0, StatusType appliesStatus = StatusType.None, int statusChancePercent = 100)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("융합 ID가 비어 있다.", nameof(id));
            if (elementA == elementB) throw new ArgumentException("같은 속성끼리는 융합하지 않는다: " + id, nameof(elementB));
            if (terrainDurationTicks < 0) throw new ArgumentOutOfRangeException(nameof(terrainDurationTicks));
            if (statusChancePercent < 0 || statusChancePercent > 100) throw new ArgumentOutOfRangeException(nameof(statusChancePercent));
            Id = id;
            ElementA = elementA;
            ElementB = elementB;
            LeavesTerrain = leavesTerrain;
            TerrainDurationTicks = terrainDurationTicks;
            AppliesStatus = appliesStatus;
            StatusChancePercent = statusChancePercent;
        }

        public bool Matches(Element a, Element b) => (a == ElementA && b == ElementB) || (a == ElementB && b == ElementA);
    }

    /// <summary>
    /// 융합 규칙 표 + 융합 주문 만들기 (3-2b). 같은 속성 조합은 한 줄만. 남기는 지형 · 거는 상태가 규칙에 없으면 예외.
    /// 융합 주문 (REACTIONS.md 5번 F2 · F3 · 3-2b 결정):
    /// - 마나 = 두 주문 합, 행동 1회 (규칙 10 예외), 피해 = 두 주문 피해 합
    /// - 모양 = 두 주문 중 큰 쪽 (Bolt &lt; Beam &lt; Burst, 같으면 첫 주문), 사거리 · 반지름 = 그 주문 것
    /// - 반응을 일으키지 않는다 (F4). 상태 · 지형은 융합 표의 것만.
    /// </summary>
    public sealed class FusionBook
    {
        private readonly List<FusionDef> _fusions = new List<FusionDef>();

        public int Count => _fusions.Count;
        public IReadOnlyList<FusionDef> All => _fusions;

        public FusionBook(IEnumerable<FusionDef> fusions, StatusBook statuses, TerrainBook terrains)
        {
            if (fusions == null) throw new ArgumentNullException(nameof(fusions));
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            if (terrains == null) throw new ArgumentNullException(nameof(terrains));
            foreach (var f in fusions)
            {
                if (f == null) throw new ArgumentException("null 융합", nameof(fusions));
                if (_fusions.Exists(x => x.Id == f.Id)) throw new ArgumentException("같은 융합 ID가 두 번: " + f.Id, nameof(fusions));
                if (_fusions.Exists(x => x.Matches(f.ElementA, f.ElementB))) throw new ArgumentException("같은 속성 조합이 두 번: " + f.Id, nameof(fusions));
                if (f.LeavesTerrain != TerrainEffectType.None && !terrains.TryGet(f.LeavesTerrain, out _))
                    throw new ArgumentException($"융합 {f.Id}의 지형 {f.LeavesTerrain}가 TerrainBook에 없다.", nameof(fusions));
                if (f.AppliesStatus != StatusType.None && !statuses.TryGet(f.AppliesStatus, out _))
                    throw new ArgumentException($"융합 {f.Id}의 상태 {f.AppliesStatus}가 StatusBook에 없다.", nameof(fusions));
                _fusions.Add(f);
            }
        }

        public static FusionBook Empty => new FusionBook(Array.Empty<FusionDef>(), new StatusBook(Array.Empty<StatusDef>()), TerrainBook.Empty);

        public FusionDef Find(Element a, Element b) => _fusions.Find(f => f.Matches(a, b));

        /// <summary>두 주문으로 융합 주문을 만든다. 조합이 없으면 null.</summary>
        public SpellDef Build(SpellDef first, SpellDef second)
        {
            if (first == null || second == null) return null;
            var fusion = Find(first.Element, second.Element);
            if (fusion == null) return null;
            var shaper = second.Shape > first.Shape ? second : first;
            return new SpellDef(fusion.Id, shaper.Element, shaper.Shape, shaper.Range, first.Damage + second.Damage, first.ManaCost + second.ManaCost,
                shaper.Radius, fusion.AppliesStatus, fusion.StatusChancePercent, fusion.LeavesTerrain, fusion.TerrainDurationTicks,
                triggersReactions: false);
        }
    }

    /// <summary>마법 융합기로 두 주문을 한 번에 쓰기 (3-2b). 플레이어 전용 (F5: 적은 쓰지 않는다).</summary>
    public static class FusionCasting
    {
        /// <summary>인벤토리에 마법 융합기(ItemUse.Fusion)가 있는지.</summary>
        public static bool HasFusionDevice(Unit unit, ItemBook items)
        {
            if (unit == null || items == null) return false;
            foreach (var entry in unit.InventorySlots)
                if (entry.Kind == InventoryKind.Item && items.TryGet(entry.Id, out var def) && def.Use == ItemUse.Fusion) return true;
            return false;
        }

        /// <summary>두 주문(인벤토리에 있어야 함)의 융합을 target에 쓸 수 있는지. 쓸 수 있으면 융합 주문 · 미리보기.</summary>
        public static CastBlock Check(World world, SpellBook spells, FusionBook fusions, ItemBook items, ITargetingRule targeting, ISightRule sight,
            Unit caster, string firstId, string secondId, GridPos target, out SpellDef fused, out SpellPreview preview)
        {
            fused = null;
            preview = null;
            if (caster == null || firstId == null || secondId == null) return CastBlock.EmptySlot;
            if (!HasFusionDevice(caster, items)) return CastBlock.NoFusionDevice;
            if (firstId == secondId) return CastBlock.SameSpell;
            if (!caster.HasSpell(firstId) || !caster.HasSpell(secondId)) return CastBlock.NotOwned;
            if (!spells.TryGet(firstId, out var first) || !spells.TryGet(secondId, out var second)) return CastBlock.UnknownSpell;
            fused = fusions.Build(first, second);
            if (fused == null) return CastBlock.NoFusionRecipe;
            return SpellCasting.CheckDef(world, targeting, sight, caster, fused, target, out preview);
        }

        /// <summary>Check를 통과하면 시전 커맨드 (행동 비용 = 기본 1회 — F2).</summary>
        public static CastBlock TryCreateCast(World world, GameConfig config, SpellBook spells, FusionBook fusions, ItemBook items,
            ITargetingRule targeting, ISightRule sight, string firstId, string secondId, GridPos target, out ICommand command)
        {
            command = null;
            if (!PlayerActions.TryGetPlayer(world, out var player) || !player.IsAlive) return CastBlock.EmptySlot;
            var block = Check(world, spells, fusions, items, targeting, sight, player, firstId, secondId, target, out var fused, out _);
            if (block != CastBlock.None) return block;
            command = new CastSpellCommand(world, spells, targeting, fused, target, config.DefaultActionCostTicks);
            return CastBlock.None;
        }
    }
}
