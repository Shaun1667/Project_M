using System;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Core
{
    /// <summary>유닛에 걸린 상태 이상 하나의 저장 상태.</summary>
    [Serializable]
    public readonly struct UnitStatusState
    {
        public readonly StatusType Type;
        public readonly int Remaining;

        public UnitStatusState(StatusType type, int remaining)
        {
            Type = type;
            Remaining = remaining;
        }
    }

    /// <summary>칸 하나에 놓인 지형 효과 하나의 저장 상태 (칸 인덱스 = y * MapWidth + x).</summary>
    [Serializable]
    public readonly struct TerrainCellState
    {
        public readonly int CellIndex;
        public readonly TerrainEffectType Type;
        public readonly int Remaining;

        public TerrainCellState(int cellIndex, TerrainEffectType type, int remaining)
        {
            CellIndex = cellIndex;
            Type = type;
            Remaining = remaining;
        }
    }

    /// <summary>
    /// World 전체의 저장 가능한 상태 (CLAUDE.md 핵심 설계 규칙 6). 참조 없는 값과 배열만 담는다.
    /// 시간 마법(되감기 · 기록) · 세이브 · 회귀 테스트가 같은 스냅샷을 쓴다.
    /// 필드를 추가하면 FormatVersion을 올리고 WorldSnapshotCodec과 World.Capture/Restore를 함께 고친다.
    /// </summary>
    [Serializable]
    public sealed class WorldSnapshot
    {
        /// <summary>1: 1-4 최초. 2: 유닛(Units) 추가 (1-6). 3: 탐험 기록(Explored) · 유닛 기억(LastSeen) 추가 (1-7). 4: 유닛 마나 · 주문 장착 칸 (2-1). 5: 유닛 발자국(PreviousPos) · 배회 목적지(WanderTarget). 6: 유닛 상태 이상 (2-3). 7: 지형 효과 레이어 (2-4).</summary>
        public const int CurrentFormatVersion = 7;

        public int FormatVersion = CurrentFormatVersion;

        public long CurrentTick;

        public int MapWidth;
        public int MapHeight;
        /// <summary>인덱스 = y * MapWidth + x.</summary>
        public TileType[] Cells = Array.Empty<TileType>();
        /// <summary>플레이어가 한 번이라도 본 칸. 인덱스는 Cells와 같다.</summary>
        public bool[] Explored = Array.Empty<bool>();
        /// <summary>지형 효과, 칸 인덱스 순 → 종류 순.</summary>
        public TerrainCellState[] Terrain = Array.Empty<TerrainCellState>();

        public int NextEntityId;

        public ulong RngSeed;
        /// <summary>지금까지 쓴 난수 수열만, RngStream 오름차순.</summary>
        public RngStreamState[] RngStreams = Array.Empty<RngStreamState>();

        /// <summary>EntityId 오름차순.</summary>
        public ActorState[] Actors = Array.Empty<ActorState>();

        /// <summary>맵 위 유닛, EntityId 오름차순.</summary>
        public UnitState[] Units = Array.Empty<UnitState>();
    }

    /// <summary>유닛 하나의 저장 가능한 상태 (ProjectM.Units.Unit과 같은 필드).</summary>
    [Serializable]
    public readonly struct UnitState
    {
        public readonly EntityId Id;
        public readonly Faction Faction;
        public readonly string VisualId;
        public readonly int MaxHp;
        public readonly int AttackDamage;
        public readonly int SightRange;
        public readonly int X;
        public readonly int Y;
        public readonly int Hp;
        public readonly bool HasLastSeen;
        public readonly int LastSeenX;
        public readonly int LastSeenY;
        public readonly int MaxMana;
        public readonly int Mana;
        /// <summary>주문 장착 칸. 빈 칸은 null.</summary>
        public readonly string[] SpellSlots;
        public readonly GridPos? PreviousPos;
        public readonly GridPos? WanderTarget;
        /// <summary>걸린 상태 이상, 종류 순.</summary>
        public readonly UnitStatusState[] Statuses;

        public UnitState(EntityId id, Faction faction, string visualId, int maxHp, int attackDamage, int sightRange, int x, int y, int hp,
            bool hasLastSeen, int lastSeenX, int lastSeenY, int maxMana, int mana, string[] spellSlots,
            GridPos? previousPos, GridPos? wanderTarget, UnitStatusState[] statuses)
        {
            PreviousPos = previousPos;
            Statuses = statuses ?? Array.Empty<UnitStatusState>();
            WanderTarget = wanderTarget;
            MaxMana = maxMana;
            Mana = mana;
            SpellSlots = spellSlots ?? Array.Empty<string>();
            HasLastSeen = hasLastSeen;
            LastSeenX = lastSeenX;
            LastSeenY = lastSeenY;
            Id = id;
            Faction = faction;
            VisualId = visualId;
            MaxHp = maxHp;
            AttackDamage = attackDamage;
            SightRange = sightRange;
            X = x;
            Y = y;
            Hp = hp;
        }
    }

    /// <summary>용도별 난수 수열 하나의 상태.</summary>
    [Serializable]
    public readonly struct RngStreamState
    {
        public readonly RngStream Stream;
        public readonly Pcg32State State;

        public RngStreamState(RngStream stream, Pcg32State state)
        {
            Stream = stream;
            State = state;
        }
    }
}
