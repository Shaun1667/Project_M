using System;
using System.Collections.Generic;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>맵 위 유닛 목록. 한 칸에 유닛은 하나. 순회는 항상 EntityId 오름차순 (결정론).</summary>
    public sealed class UnitRegistry
    {
        private readonly Dictionary<EntityId, Unit> _byId = new Dictionary<EntityId, Unit>();
        private readonly Dictionary<GridPos, EntityId> _byPos = new Dictionary<GridPos, EntityId>();

        public int Count => _byId.Count;

        public void Add(Unit unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (!unit.Id.IsValid) throw new ArgumentException("유효하지 않은 EntityId.", nameof(unit));
            if (_byId.ContainsKey(unit.Id)) throw new InvalidOperationException("이미 있는 유닛: " + unit.Id);
            if (_byPos.TryGetValue(unit.Pos, out var other)) throw new InvalidOperationException($"{unit.Pos}에 이미 유닛 {other}가 있다.");

            _byId.Add(unit.Id, unit);
            _byPos.Add(unit.Pos, unit.Id);
        }

        public bool Remove(EntityId id)
        {
            if (!_byId.TryGetValue(id, out var unit)) return false;
            _byId.Remove(id);
            _byPos.Remove(unit.Pos);
            return true;
        }

        public Unit Get(EntityId id)
        {
            if (!_byId.TryGetValue(id, out var unit)) throw new KeyNotFoundException("없는 유닛: " + id);
            return unit;
        }

        public bool TryGet(EntityId id, out Unit unit) => _byId.TryGetValue(id, out unit);

        public bool TryGetAt(GridPos pos, out Unit unit)
        {
            if (_byPos.TryGetValue(pos, out var id)) return _byId.TryGetValue(id, out unit);
            unit = null;
            return false;
        }

        public bool IsOccupied(GridPos pos) => _byPos.ContainsKey(pos);

        /// <summary>유닛을 옮긴다. 도착 칸이 비어 있어야 한다. 지형 규칙은 호출한 쪽(커맨드)이 검사한다.</summary>
        public void Move(EntityId id, GridPos to)
        {
            var unit = Get(id);
            if (unit.Pos == to) return;
            if (_byPos.ContainsKey(to)) throw new InvalidOperationException($"{to}에 이미 유닛이 있다.");
            _byPos.Remove(unit.Pos);
            _byPos.Add(to, id);
            unit.Pos = to;
        }

        /// <summary>EntityId 오름차순 목록 (복사본 — 순회 중 제거해도 안전).</summary>
        public List<Unit> ToSortedList()
        {
            var list = new List<Unit>(_byId.Values);
            list.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));
            return list;
        }
    }
}
