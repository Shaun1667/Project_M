using System;
using System.Collections.Generic;

namespace ProjectM.Core
{
    /// <summary>플레이어 입력 큐. 입력 어댑터가 Enqueue하고, 플레이어 차례에 하나씩 꺼낸다.</summary>
    public sealed class PlayerCommandSource : ICommandSource
    {
        private readonly Queue<ICommand> _queue = new Queue<ICommand>();

        public bool HasCommand => _queue.Count > 0;

        public void Enqueue(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _queue.Enqueue(command);
        }

        public void Clear() => _queue.Clear();

        public ICommand NextCommand(in TickContext context)
        {
            if (_queue.Count == 0)
                throw new InvalidOperationException("플레이어 입력 없이 플레이어 차례가 진행됐다.");
            return _queue.Dequeue();
        }
    }
}
