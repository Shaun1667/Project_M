using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>함정 종류 (3-12a). 끝에만 추가한다 (에셋 · 스냅샷이 정수로 저장). None = 없음.</summary>
    public enum TrapType
    {
        None = 0,
        /// <summary>화염 함정: 3×3 불 피해 + 화상 + 화염 지대 (주문 trap_fire).</summary>
        Fire = 1,
        /// <summary>독가스 함정: 3×3 중독 + 독구름 (trap_toxic).</summary>
        ToxicGas = 2,
        /// <summary>마비 가스 함정: 3×3 마비 가스 (trap_paralytic).</summary>
        ParalyticGas = 3,
        /// <summary>물 함정: 3×3 젖음 + 물 타일 (trap_flood).</summary>
        Flood = 4,
        /// <summary>번개 함정: 3×3 번개 피해 + 마비 확률 — 젖은 대상 · 물이면 감전 (trap_shock).</summary>
        Shock = 5,
        /// <summary>순간이동 함정: 밟은 유닛을 층의 무작위 빈 칸으로.</summary>
        Teleport = 6,
        /// <summary>경보 함정: 층의 모든 적이 함정 칸을 안다 (몰려옴).</summary>
        Alarm = 7,
    }

    /// <summary>주문으로 나타낼 수 없는 함정 효과 (3-12a).</summary>
    public enum TrapSpecial
    {
        None = 0,
        Teleport = 1,
        Alarm = 2,
    }

    /// <summary>함정 한 종류 (3-12a). 값은 TrapCatalogAsset에서 — 기준은 BALANCE.md 17번.</summary>
    public sealed class TrapDef
    {
        public TrapType Type { get; }
        /// <summary>효과 주문 (SpellCatalog, 함정 칸 중심으로 시전자 없이). 비면 주문 효과 없음.</summary>
        public string SpellId { get; }
        public TrapSpecial Special { get; }
        /// <summary>배치 무게 (클수록 자주).</summary>
        public int Weight { get; }

        public TrapDef(TrapType type, string spellId, TrapSpecial special, int weight)
        {
            if (type == TrapType.None) throw new ArgumentException("None은 함정이 아니다.", nameof(type));
            if (weight < 1) throw new ArgumentOutOfRangeException(nameof(weight));
            if (string.IsNullOrEmpty(spellId) && special == TrapSpecial.None) throw new ArgumentException("효과 없는 함정: " + type);
            Type = type;
            SpellId = string.IsNullOrEmpty(spellId) ? null : spellId;
            Special = special;
            Weight = weight;
        }
    }

    /// <summary>함정 표 + 배치 수치 (3-12a). 효과 주문이 SpellBook에 없으면 예외.</summary>
    public sealed class TrapBook
    {
        private readonly List<TrapDef> _defs = new List<TrapDef>();

        /// <summary>층마다 함정 수.</summary>
        public int TrapsPerFloor { get; }
        /// <summary>숨겨진 채 놓일 확률 (%).</summary>
        public int HiddenPercent { get; }
        /// <summary>층 시작 칸에서 최소 거리 (칸, 대각선 포함).</summary>
        public int MinDistanceFromStart { get; }
        public IReadOnlyList<TrapDef> All => _defs;

        public TrapBook(IEnumerable<TrapDef> defs, SpellBook spells, int trapsPerFloor, int hiddenPercent, int minDistanceFromStart)
        {
            if (defs == null) throw new ArgumentNullException(nameof(defs));
            if (trapsPerFloor < 0) throw new ArgumentOutOfRangeException(nameof(trapsPerFloor));
            if (hiddenPercent < 0 || hiddenPercent > 100) throw new ArgumentOutOfRangeException(nameof(hiddenPercent));
            if (minDistanceFromStart < 0) throw new ArgumentOutOfRangeException(nameof(minDistanceFromStart));
            foreach (var d in defs)
            {
                if (d == null) throw new ArgumentException("null 함정", nameof(defs));
                if (_defs.Exists(x => x.Type == d.Type)) throw new ArgumentException("같은 함정이 두 번: " + d.Type, nameof(defs));
                if (d.SpellId != null && (spells == null || !spells.TryGet(d.SpellId, out _)))
                    throw new ArgumentException($"함정 {d.Type}의 주문 {d.SpellId}가 SpellBook에 없다.", nameof(defs));
                _defs.Add(d);
            }
            TrapsPerFloor = trapsPerFloor;
            HiddenPercent = hiddenPercent;
            MinDistanceFromStart = minDistanceFromStart;
        }

        public bool TryGet(TrapType type, out TrapDef def)
        {
            def = _defs.Find(d => d.Type == type);
            return def != null;
        }

        /// <summary>무게로 하나 고른다 (표 순서 고정 — 결정론).</summary>
        public TrapDef Pick(IRng rng)
        {
            int total = 0;
            foreach (var d in _defs) total += d.Weight;
            int roll = rng.Range(0, total);
            foreach (var d in _defs)
            {
                if (roll < d.Weight) return d;
                roll -= d.Weight;
            }
            return _defs[_defs.Count - 1];
        }
    }

    /// <summary>칸 하나의 함정 (저장 대상).</summary>
    public sealed class TrapInstance
    {
        public TrapType Type { get; }
        /// <summary>아직 플레이어가 모름 (화면에 안 보임).</summary>
        public bool Hidden { get; internal set; }

        public TrapInstance(TrapType type, bool hidden)
        {
            Type = type;
            Hidden = hidden;
        }
    }

    /// <summary>함정 레이어 (3-12a): 칸 인덱스 → 함정 하나. 지금 층의 것 — World 상태, 스냅샷 대상.</summary>
    public sealed class TrapLayer
    {
        private readonly SortedDictionary<int, TrapInstance> _cells = new SortedDictionary<int, TrapInstance>();

        public int Width { get; }
        public int Height { get; }

        public TrapLayer(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Count => _cells.Count;

        public bool TryGet(GridPos pos, out TrapInstance trap) => _cells.TryGetValue(Index(pos), out trap);

        public bool Has(GridPos pos) => InBounds(pos) && _cells.ContainsKey(Index(pos));

        /// <summary>칸 인덱스 순 목록 (복사본).</summary>
        public List<KeyValuePair<int, TrapInstance>> ToSortedList() => new List<KeyValuePair<int, TrapInstance>>(_cells);

        public GridPos PosOf(int index) => new GridPos(index % Width, index / Width);

        internal void Set(GridPos pos, TrapInstance trap) => _cells[Index(pos)] = trap;

        internal bool Remove(GridPos pos) => _cells.Remove(Index(pos));

        private bool InBounds(GridPos pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height;

        private int Index(GridPos pos)
        {
            if (!InBounds(pos)) throw new ArgumentOutOfRangeException(nameof(pos), pos, "맵 밖 칸");
            return pos.Y * Width + pos.X;
        }
    }

    /// <summary>
    /// 함정을 놓고 · 드러내고 · 발동하는 곳은 여기 하나 (3-12a). 화면에는 드러난 함정만 TrapChangedEvent로 알린다.
    /// </summary>
    public static class Traps
    {
        /// <summary>
        /// 층 꾸미기 끝에 함정을 놓는다: 맨땅(바닥 · 풀) 중 지형 효과 · 함정이 없고 시작 칸에서 MinDistanceFromStart 이상인 칸 (y → x 순 목록에서 Decor 난수로).
        /// </summary>
        public static void PlaceForFloor(World world, GridPos start, TrapBook book, IRng rng)
        {
            if (book == null || book.All.Count == 0 || book.TrapsPerFloor == 0) return;
            var map = world.Map;
            var cells = new List<GridPos>();
            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var pos = new GridPos(x, y);
                if (GridMap.IsGroundType(map.Get(pos)) && world.Terrain.At(pos).Count == 0 && GridMovement.Chebyshev(pos, start) >= book.MinDistanceFromStart)
                    cells.Add(pos);
            }
            for (int i = 0; i < book.TrapsPerFloor && cells.Count > 0; i++)
            {
                int pick = rng.Range(0, cells.Count);
                var pos = cells[pick];
                cells.RemoveAt(pick);
                var def = book.Pick(rng);
                Place(world, pos, def.Type, rng.Range(0, 100) < book.HiddenPercent);
            }
        }

        public static void Place(World world, GridPos pos, TrapType type, bool hidden)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (type == TrapType.None) throw new ArgumentException("None은 함정이 아니다.", nameof(type));
            world.Traps.Set(pos, new TrapInstance(type, hidden));
            if (!hidden) world.Events.Publish(new TrapChangedEvent(pos, type, true));
        }

        /// <summary>플레이어가 아는(드러난) 함정이 있는 칸인지 — 클릭 이동 경로가 피한다.</summary>
        public static bool IsKnown(World world, GridPos pos) => world.Traps.Has(pos) && world.Traps.TryGet(pos, out var trap) && !trap.Hidden;

        /// <summary>숨겨진 함정을 드러낸다 (3-12b 찾기). 드러냈으면 true.</summary>
        public static bool Reveal(World world, GridPos pos)
        {
            if (!world.Traps.TryGet(pos, out var trap) || !trap.Hidden) return false;
            trap.Hidden = false;
            world.Events.Publish(new TrapChangedEvent(pos, trap.Type, true));
            world.Events.Publish(new TrapRevealedEvent(pos, trap.Type));
            return true;
        }

        /// <summary>
        /// unit이 밟은 함정을 발동한다: 함정은 사라지고(한 번만), 효과 주문(함정 칸 중심, 시전자 없음) → 특수 효과(순간이동 · 경보) 순.
        /// </summary>
        public static void Trigger(World world, TrapBook traps, SpellBook spells, Unit unit)
        {
            var pos = unit.Pos;
            if (!world.Traps.TryGet(pos, out var trap)) return;
            world.Traps.Remove(pos);
            world.Events.Publish(new TrapChangedEvent(pos, trap.Type, false));
            world.Events.Publish(new TrapTriggeredEvent(pos, trap.Type, unit.Id));
            if (!traps.TryGet(trap.Type, out var def)) return;

            if (def.SpellId != null && spells.TryGet(def.SpellId, out var spell))
                SpellCasting.ApplyEffect(world, spells, EntityId.None, spell, SpellCasting.PreviewAt(world, spell, pos));
            switch (def.Special)
            {
                case TrapSpecial.Teleport:
                    if (unit.IsAlive) Consumables.Teleport(world, unit);
                    break;
                case TrapSpecial.Alarm:
                    foreach (var other in world.Units.ToSortedList())
                        if (other.IsAlive && other.Faction != Faction.Player) other.LastSeenTargetPos = pos;
                    break;
            }
        }
    }

    /// <summary>
    /// ⑤ 함정 발동 (3-12a, 결정 2026-11-04 — 적도 밟으면 발동): 함정 칸에 선 살아 있는 유닛마다 (EntityId 순) 그 함정을 발동한다.
    /// 숨겨진 함정도 발동한다. 상태를 갖지 않는다.
    /// </summary>
    public sealed class TrapSystem : ITickSystem
    {
        private readonly World _world;
        private readonly TrapBook _traps;
        private readonly SpellBook _spells;

        public TrapSystem(World world, TrapBook traps, SpellBook spells)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _traps = traps ?? throw new ArgumentNullException(nameof(traps));
            _spells = spells ?? throw new ArgumentNullException(nameof(spells));
        }

        public void Run(in TickContext context)
        {
            if (_world.Traps.Count == 0) return;
            foreach (var unit in _world.Units.ToSortedList())
                if (unit.IsAlive && _world.Map.InBounds(unit.Pos) && _world.Traps.Has(unit.Pos)
                    && !ProjectM.Status.StatusEffects.Any(_spells.Statuses, unit, d => d.Levitates)) // 3-13a 부양: 함정 위를 떠서 지나감
                    Traps.Trigger(_world, _traps, _spells, unit);
        }
    }

    /// <summary>화면에 보이는 함정이 바뀌었다 (드러남 · 놓임 = Visible, 발동해 사라짐 = !Visible). 숨겨진 함정은 알리지 않는다.</summary>
    public sealed class TrapChangedEvent : ISimEvent
    {
        public GridPos Pos { get; }
        public TrapType Type { get; }
        public bool Visible { get; }

        public TrapChangedEvent(GridPos pos, TrapType type, bool visible)
        {
            Pos = pos;
            Type = type;
            Visible = visible;
        }
    }

    /// <summary>함정이 발동했다 (로그 · 연출). Unit = 밟은 유닛.</summary>
    public sealed class TrapTriggeredEvent : ISimEvent
    {
        public GridPos Pos { get; }
        public TrapType Type { get; }
        public EntityId Unit { get; }

        public TrapTriggeredEvent(GridPos pos, TrapType type, EntityId unit)
        {
            Pos = pos;
            Type = type;
            Unit = unit;
        }
    }

    /// <summary>숨겨진 함정을 찾았다 (로그, 3-12b).</summary>
    public sealed class TrapRevealedEvent : ISimEvent
    {
        public GridPos Pos { get; }
        public TrapType Type { get; }

        public TrapRevealedEvent(GridPos pos, TrapType type)
        {
            Pos = pos;
            Type = type;
        }
    }
}
