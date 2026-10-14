using System;

namespace ProjectM.Core
{
    /// <summary>
    /// EntityId 발급기. 1부터 차례로 발급하고 재사용하지 않는다.
    /// NextValue를 스냅샷에 저장했다가 같은 값으로 다시 만들면 발급이 이어진다.
    /// </summary>
    public sealed class EntityIdAllocator
    {
        public int NextValue { get; private set; }

        public EntityIdAllocator() : this(1)
        {
        }

        public EntityIdAllocator(int nextValue)
        {
            if (nextValue < 1) throw new ArgumentOutOfRangeException(nameof(nextValue), nextValue, "다음 ID는 1 이상이어야 한다.");
            NextValue = nextValue;
        }

        public EntityId Allocate()
        {
            if (NextValue == int.MaxValue) throw new InvalidOperationException("EntityId가 소진됐다.");
            return new EntityId(NextValue++);
        }
    }
}
