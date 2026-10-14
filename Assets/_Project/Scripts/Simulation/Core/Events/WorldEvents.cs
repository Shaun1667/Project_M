using System;
using System.Collections.Generic;

namespace ProjectM.Core
{
    /// <summary>층 전체 지형이 새로 정해졌다 (층 시작 · 불러오기). 칸 배열은 복사본이다.</summary>
    public sealed class MapLoadedEvent : ISimEvent
    {
        public int Width { get; }
        public int Height { get; }
        /// <summary>인덱스 = y * Width + x.</summary>
        public IReadOnlyList<TileType> Cells { get; }

        public MapLoadedEvent(GridMap map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            Width = map.Width;
            Height = map.Height;
            Cells = map.CopyCells();
        }
    }

    /// <summary>칸 하나의 기본 지형이 바뀌었다.</summary>
    public sealed class TileChangedEvent : ISimEvent
    {
        public GridPos Pos { get; }
        public TileType Type { get; }

        public TileChangedEvent(GridPos pos, TileType type)
        {
            Pos = pos;
            Type = type;
        }
    }

    /// <summary>엔티티가 나타났다. VisualId로 표현 레이어가 모습을 고른다 (데이터는 비주얼을 ID로만 참조).</summary>
    public sealed class EntitySpawnedEvent : ISimEvent
    {
        public EntityId Id { get; }
        public GridPos Pos { get; }
        public string VisualId { get; }

        public EntitySpawnedEvent(EntityId id, GridPos pos, string visualId)
        {
            if (!id.IsValid) throw new ArgumentException("유효하지 않은 EntityId.", nameof(id));
            if (string.IsNullOrEmpty(visualId)) throw new ArgumentException("VisualId가 비어 있다.", nameof(visualId));
            Id = id;
            Pos = pos;
            VisualId = visualId;
        }
    }

    /// <summary>엔티티가 다른 칸으로 옮겨졌다.</summary>
    public sealed class EntityMovedEvent : ISimEvent
    {
        public EntityId Id { get; }
        public GridPos From { get; }
        public GridPos To { get; }

        public EntityMovedEvent(EntityId id, GridPos from, GridPos to)
        {
            Id = id;
            From = from;
            To = to;
        }
    }

    /// <summary>엔티티가 사라졌다 (사망 · 퇴장).</summary>
    public sealed class EntityRemovedEvent : ISimEvent
    {
        public EntityId Id { get; }

        public EntityRemovedEvent(EntityId id)
        {
            Id = id;
        }
    }
}
