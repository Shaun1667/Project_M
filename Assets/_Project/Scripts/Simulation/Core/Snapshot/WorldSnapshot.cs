using System;
using ProjectM.Items;
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
        /// <summary>1: 1-4 최초. 2: 유닛(Units) 추가 (1-6). 3: 탐험 기록(Explored) · 유닛 기억(LastSeen) 추가 (1-7). 4: 유닛 마나 · 주문 장착 칸 (2-1). 5: 유닛 발자국(PreviousPos) · 배회 목적지(WanderTarget). 6: 유닛 상태 이상 (2-3). 7: 지형 효과 레이어 (2-4). 8: 주문 장착 칸 → 인벤토리 · 빠른 칸 (3-2a). 9: 층 번호 · 떠난 층들 (3-4a). 10: 한 판 기록 — 처치 수 · 결과 · 보스 · 사망 원인 (3-4b).</summary>
        public const int CurrentFormatVersion = 10;

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

        /// <summary>지금 층 번호 (1부터, 3-4a).</summary>
        public int Floor = 1;
        /// <summary>떠난 층들 (멈춘 채 보존, 3-4a), 층 번호 오름차순.</summary>
        public StoredFloorState[] StoredFloors = Array.Empty<StoredFloorState>();

        // 한 판 기록 (3-4b, RunState)
        public int Kills;
        public RunOutcome Outcome;
        public EntityId BossId;
        public DamageCause LastPlayerDamage;
        public EntityId LastPlayerAttacker;
    }

    /// <summary>
    /// 떠난 층 하나 (3-4a). 떠난 순간 그대로 멈춰 있다가 돌아오면 이어서 진행한다 (결정 2026-10-18).
    /// 행위자의 ReadyTick은 떠날 때의 현재 틱 기준 차이(ReadyTick − CurrentTick)로 저장한다.
    /// </summary>
    [Serializable]
    public sealed class StoredFloorState
    {
        public int Floor;
        public int MapWidth;
        public int MapHeight;
        public TileType[] Cells = Array.Empty<TileType>();
        public bool[] Explored = Array.Empty<bool>();
        public TerrainCellState[] Terrain = Array.Empty<TerrainCellState>();
        /// <summary>그 층의 유닛 (플레이어 제외), EntityId 오름차순.</summary>
        public UnitState[] Units = Array.Empty<UnitState>();
        /// <summary>그 층의 행위자, ReadyTick = 떠날 때 현재 틱과의 차이. EntityId 오름차순.</summary>
        public ActorState[] Actors = Array.Empty<ActorState>();
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
        /// <summary>인벤토리 칸 (3-2a). 빈 칸은 Empty.</summary>
        public readonly InventoryRef[] InventorySlots;
        /// <summary>빠른 칸 (3-2a). 빈 칸은 Empty.</summary>
        public readonly InventoryRef[] QuickSlots;
        public readonly GridPos? PreviousPos;
        public readonly GridPos? WanderTarget;
        /// <summary>걸린 상태 이상, 종류 순.</summary>
        public readonly UnitStatusState[] Statuses;

        public UnitState(EntityId id, Faction faction, string visualId, int maxHp, int attackDamage, int sightRange, int x, int y, int hp,
            bool hasLastSeen, int lastSeenX, int lastSeenY, int maxMana, int mana, InventoryRef[] inventorySlots, InventoryRef[] quickSlots,
            GridPos? previousPos, GridPos? wanderTarget, UnitStatusState[] statuses)
        {
            PreviousPos = previousPos;
            Statuses = statuses ?? Array.Empty<UnitStatusState>();
            WanderTarget = wanderTarget;
            MaxMana = maxMana;
            Mana = mana;
            InventorySlots = inventorySlots ?? Array.Empty<InventoryRef>();
            QuickSlots = quickSlots ?? Array.Empty<InventoryRef>();
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
