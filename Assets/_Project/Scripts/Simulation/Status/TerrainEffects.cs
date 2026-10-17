using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Status
{
    /// <summary>지형 효과 종류 (2-4). 끝에만 추가한다 (에셋이 정수로 저장). None = 없음.</summary>
    public enum TerrainEffectType
    {
        None = 0,
        FireZone = 1,
        PoisonCloud = 2,
        /// <summary>물 타일 (3-2): 물 주문 3×3, 위 유닛 젖음.</summary>
        Water = 3,
        /// <summary>오염된 물 (3-2): 물 타일 × 독 · 물 + 독 융합, 위 유닛 젖음 + 매 틱 피해.</summary>
        ContaminatedWater = 4,
        /// <summary>빙판 (3-3a): 물 타일 × 얼음(결빙) · 얼음 + 물 융합, 위 유닛 둔화. 불을 맞으면 물 타일로 (해동).</summary>
        IcePatch = 5,
    }

    /// <summary>
    /// 지형 효과 하나의 규칙 데이터 (2-4, Q8 임시값). 값은 TerrainCatalogAsset에서 온다 — 기준은 BALANCE.md 7번.
    /// 효과는 데이터 필드로만: 그 칸에 있는 유닛에게 ③ 단계마다 상태 이상을 걸고 피해를 준다.
    /// </summary>
    public sealed class TerrainDef
    {
        public TerrainEffectType Type { get; }
        /// <summary>남아 있는 ③ 처리 횟수 (틱). 생긴 틱의 ③부터 센다.</summary>
        public int DurationTicks { get; }
        /// <summary>그 칸에 있는 유닛에게 매 틱 거는 상태 (StatusBook). None이면 없음.</summary>
        public StatusType AppliesStatus { get; }
        /// <summary>그 칸에 있는 유닛이 매 틱 받는 피해 (3-2 오염된 물). 상태를 건 뒤에 준다.</summary>
        public int DamagePerTick { get; }
        /// <summary>이 지형이 있는 칸에는 놓이지 않는다 (3-2: 화염 지대 위에는 물 타일이 깔리지 않음). None이면 없음.</summary>
        public TerrainEffectType BlockedBy { get; }

        public TerrainDef(TerrainEffectType type, int durationTicks, StatusType appliesStatus = StatusType.None,
            int damagePerTick = 0, TerrainEffectType blockedBy = TerrainEffectType.None)
        {
            if (type == TerrainEffectType.None) throw new ArgumentException("None은 지형 효과가 아니다.", nameof(type));
            if (durationTicks < 1) throw new ArgumentOutOfRangeException(nameof(durationTicks), durationTicks, "지속은 1틱 이상이어야 한다.");
            if (damagePerTick < 0) throw new ArgumentOutOfRangeException(nameof(damagePerTick), damagePerTick, "틱당 피해는 0 이상이어야 한다.");
            if (blockedBy == type) throw new ArgumentException("자기 자신에게 막힐 수 없다.", nameof(blockedBy));
            Type = type;
            DurationTicks = durationTicks;
            AppliesStatus = appliesStatus;
            DamagePerTick = damagePerTick;
            BlockedBy = blockedBy;
        }
    }

    /// <summary>지형 효과 종류 → 규칙. 같은 종류가 두 번 나오거나, 거는 상태가 StatusBook에 없으면 예외.</summary>
    public sealed class TerrainBook
    {
        private readonly Dictionary<TerrainEffectType, TerrainDef> _defs = new Dictionary<TerrainEffectType, TerrainDef>();

        public TerrainBook(IEnumerable<TerrainDef> defs, StatusBook statuses)
        {
            if (defs == null) throw new ArgumentNullException(nameof(defs));
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            foreach (var def in defs)
            {
                if (def == null) throw new ArgumentException("null 지형 효과", nameof(defs));
                if (_defs.ContainsKey(def.Type)) throw new ArgumentException("같은 지형 효과가 두 번: " + def.Type, nameof(defs));
                if (def.AppliesStatus != StatusType.None && !statuses.TryGet(def.AppliesStatus, out _))
                    throw new ArgumentException($"지형 {def.Type}의 상태 {def.AppliesStatus}가 StatusBook에 없다.", nameof(defs));
                _defs.Add(def.Type, def);
            }
        }

        public static TerrainBook Empty => new TerrainBook(Array.Empty<TerrainDef>(), new StatusBook(Array.Empty<StatusDef>()));

        public int Count => _defs.Count;

        public bool TryGet(TerrainEffectType type, out TerrainDef def) => _defs.TryGetValue(type, out def);
    }

    /// <summary>칸 하나에 놓인 지형 효과 하나 (저장 대상).</summary>
    public sealed class TerrainInstance
    {
        public TerrainEffectType Type { get; }
        public int Remaining { get; internal set; }

        public TerrainInstance(TerrainEffectType type, int remaining)
        {
            Type = type;
            Remaining = remaining;
        }
    }

    /// <summary>
    /// 지형 효과 레이어 (2-4, 결정: 한 칸에 여러 효과 공존, 같은 종류는 지속 갱신). 기본 지형(GridMap) 위에 따로 둔다.
    /// 칸 인덱스(y * Width + x) → 효과 목록(종류 순). 순회는 칸 인덱스 순 (결정론). World 상태 — 스냅샷 대상.
    /// </summary>
    public sealed class TerrainLayer
    {
        private readonly SortedDictionary<int, List<TerrainInstance>> _cells = new SortedDictionary<int, List<TerrainInstance>>();

        public int Width { get; }
        public int Height { get; }

        public TerrainLayer(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int CellCount => _cells.Count;

        /// <summary>그 칸의 효과 (종류 순). 없으면 빈 목록.</summary>
        public IReadOnlyList<TerrainInstance> At(GridPos pos)
        {
            return _cells.TryGetValue(Index(pos), out var list) ? list : (IReadOnlyList<TerrainInstance>)Array.Empty<TerrainInstance>();
        }

        public bool Has(GridPos pos, TerrainEffectType type)
        {
            return _cells.TryGetValue(Index(pos), out var list) && list.Exists(t => t.Type == type);
        }

        /// <summary>놓는다. 같은 종류가 있으면 남은 틱을 더 긴 쪽으로. 놓인 뒤의 남은 틱.</summary>
        internal int Place(GridPos pos, TerrainEffectType type, int duration)
        {
            int index = Index(pos);
            if (!_cells.TryGetValue(index, out var list))
            {
                list = new List<TerrainInstance>();
                _cells.Add(index, list);
            }
            var existing = list.Find(t => t.Type == type);
            if (existing != null)
            {
                existing.Remaining = Math.Max(existing.Remaining, duration);
                return existing.Remaining;
            }
            int at = list.FindIndex(t => t.Type > type);
            var instance = new TerrainInstance(type, duration);
            if (at < 0) list.Add(instance);
            else list.Insert(at, instance);
            return duration;
        }

        /// <summary>칸 인덱스 순 목록 (복사본 — 순회 중 바꿔도 안전).</summary>
        public List<KeyValuePair<int, List<TerrainInstance>>> ToSortedList() => new List<KeyValuePair<int, List<TerrainInstance>>>(_cells);

        /// <summary>그 칸의 그 종류를 없앤다. 없앴으면 true.</summary>
        internal bool Remove(GridPos pos, TerrainEffectType type)
        {
            int index = Index(pos);
            if (!_cells.TryGetValue(index, out var list)) return false;
            int at = list.FindIndex(t => t.Type == type);
            if (at < 0) return false;
            list.RemoveAt(at);
            RemoveEmpty(index);
            return true;
        }

        internal void RemoveEmpty(int index)
        {
            if (_cells.TryGetValue(index, out var list) && list.Count == 0) _cells.Remove(index);
        }

        public GridPos PosOf(int index) => new GridPos(index % Width, index / Width);

        private int Index(GridPos pos)
        {
            if (pos.X < 0 || pos.Y < 0 || pos.X >= Width || pos.Y >= Height)
                throw new ArgumentOutOfRangeException(nameof(pos), pos, "맵 밖 칸");
            return pos.Y * Width + pos.X;
        }
    }

    /// <summary>지형 효과를 놓는 곳은 여기 하나. 걸을 수 없는 칸(벽)에는 놓지 않는다.</summary>
    public static class TerrainEffects
    {
        /// <param name="durationTicks">0이면 규칙의 지속(def.DurationTicks). 3-2b 융합은 자기 지속을 넘긴다.</param>
        public static void Place(World world, GridPos pos, TerrainDef def, int durationTicks = 0)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (!world.Map.IsWalkable(pos)) return;
            if (def.BlockedBy != TerrainEffectType.None && world.Terrain.Has(pos, def.BlockedBy)) return;
            int remaining = world.Terrain.Place(pos, def.Type, durationTicks > 0 ? durationTicks : def.DurationTicks);
            world.Events.Publish(new TerrainChangedEvent(pos, def.Type, remaining));
        }

        /// <summary>
        /// 그 칸의 from을 to로 바꾼다 — durationTicks가 0이면 남은 틱 유지 (3-2 오염: 물 → 오염된 물), 아니면 그 지속
        /// (3-3a 결빙: 물 → 빙판 8틱, 해동: 빙판 → 물 12틱). to가 이미 있으면 남은 틱이 긴 쪽. from이 없으면 아무 일 없음.
        /// </summary>
        public static bool Transform(World world, GridPos pos, TerrainEffectType from, TerrainEffectType to, int durationTicks = 0)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (to == TerrainEffectType.None || from == to) throw new ArgumentException("바꿀 지형이 잘못됐다.", nameof(to));
            if (durationTicks < 0) throw new ArgumentOutOfRangeException(nameof(durationTicks));
            if (!world.Map.InBounds(pos)) return false;
            int remaining = -1;
            foreach (var t in world.Terrain.At(pos))
                if (t.Type == from) remaining = t.Remaining;
            if (remaining < 0) return false;
            if (durationTicks > 0) remaining = durationTicks;

            world.Terrain.Remove(pos, from);
            world.Events.Publish(new TerrainChangedEvent(pos, from, 0));
            int placed = world.Terrain.Place(pos, to, remaining);
            world.Events.Publish(new TerrainChangedEvent(pos, to, placed));
            return true;
        }

        /// <summary>지형 효과를 없앤다 (반응의 트리거 소모 등). 없으면 아무 일 없음.</summary>
        public static bool Remove(World world, GridPos pos, TerrainEffectType type)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (!world.Map.InBounds(pos) || !world.Terrain.Remove(pos, type)) return false;
            world.Events.Publish(new TerrainChangedEvent(pos, type, 0));
            return true;
        }
    }

    /// <summary>
    /// ③ 지형 처리 (GDD 3-3): 칸 인덱스 순 · 종류 순으로, 그 칸 유닛에게 상태를 걸고(지속 갱신) → 틱당 피해(3-2) → 남은 틱 1 감소 → 0이면 사라짐.
    /// 확산은 없다 (2-4). 상태를 갖지 않는다.
    /// </summary>
    public sealed class TerrainSystem : ITickSystem
    {
        private readonly World _world;
        private readonly TerrainBook _terrains;
        private readonly StatusBook _statuses;

        public TerrainSystem(World world, TerrainBook terrains, StatusBook statuses)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _terrains = terrains ?? throw new ArgumentNullException(nameof(terrains));
            _statuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
        }

        public void Run(in TickContext context)
        {
            foreach (var pair in _world.Terrain.ToSortedList())
            {
                var pos = _world.Terrain.PosOf(pair.Key);
                var list = pair.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var effect = list[i];
                    if (_terrains.TryGet(effect.Type, out var def) && _world.Units.TryGetAt(pos, out var unit) && unit.IsAlive)
                    {
                        if (def.AppliesStatus != StatusType.None && _statuses.TryGet(def.AppliesStatus, out var status))
                            StatusEffects.Apply(_world, unit, status);
                        if (def.DamagePerTick > 0)
                            Combat.ApplyDamage(_world, EntityId.None, unit, def.DamagePerTick);
                    }

                    effect.Remaining--;
                    _world.Events.Publish(new TerrainChangedEvent(pos, effect.Type, effect.Remaining));
                    if (effect.Remaining <= 0)
                    {
                        list.RemoveAt(i);
                        i--;
                    }
                }
                _world.Terrain.RemoveEmpty(pair.Key);
            }
        }
    }

    /// <summary>칸의 지형 효과가 바뀌었다 (놓임 · 갱신 · 1 줄어듦). Remaining 0 = 사라짐.</summary>
    public sealed class TerrainChangedEvent : ISimEvent
    {
        public GridPos Pos { get; }
        public TerrainEffectType Type { get; }
        public int Remaining { get; }

        public TerrainChangedEvent(GridPos pos, TerrainEffectType type, int remaining)
        {
            Pos = pos;
            Type = type;
            Remaining = remaining;
        }
    }
}
