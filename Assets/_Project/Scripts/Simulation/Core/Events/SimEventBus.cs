using System;
using System.Collections.Generic;

namespace ProjectM.Core
{
    /// <summary>
    /// 시뮬레이션 이벤트 버스. Publish는 쌓아 두기만 하고, Flush가 발행 순서대로 구독자에게 보낸다.
    /// 틱 안에서는 ⑦ 이벤트 발행 단계(EventFlushSystem)가 Flush한다 — 한 틱의 결과를 표현 레이어가 한 번에 받는다.
    /// </summary>
    public sealed class SimEventBus
    {
        private sealed class Handler
        {
            public readonly Delegate Original;
            public readonly Action<ISimEvent> Invoke;

            public Handler(Delegate original, Action<ISimEvent> invoke)
            {
                Original = original;
                Invoke = invoke;
            }
        }

        private readonly Dictionary<Type, List<Handler>> _handlers = new Dictionary<Type, List<Handler>>();
        private List<ISimEvent> _pending = new List<ISimEvent>();
        private List<ISimEvent> _flushing = new List<ISimEvent>();
        private bool _isFlushing;

        public int PendingCount => _pending.Count;

        public void Publish(ISimEvent simEvent)
        {
            if (simEvent == null) throw new ArgumentNullException(nameof(simEvent));
            _pending.Add(simEvent);
        }

        public void Subscribe<T>(Action<T> handler) where T : ISimEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Handler>();
                _handlers.Add(typeof(T), list);
            }
            list.Add(new Handler(handler, e => handler((T)e)));
        }

        public void Unsubscribe<T>(Action<T> handler) where T : ISimEvent
        {
            if (handler == null || !_handlers.TryGetValue(typeof(T), out var list)) return;
            int index = list.FindIndex(h => h.Original.Equals(handler));
            if (index >= 0) list.RemoveAt(index);
        }

        /// <summary>
        /// 쌓인 이벤트를 보낸다. 구독자가 Flush 도중 Publish한 이벤트는 다음 Flush로 넘어간다.
        /// </summary>
        public void Flush()
        {
            if (_isFlushing) throw new InvalidOperationException("Flush 도중에 Flush를 다시 부를 수 없다.");
            if (_pending.Count == 0) return;

            (_pending, _flushing) = (_flushing, _pending);
            _isFlushing = true;
            try
            {
                for (int i = 0; i < _flushing.Count; i++)
                    Dispatch(_flushing[i]);
            }
            finally
            {
                _flushing.Clear();
                _isFlushing = false;
            }
        }

        private void Dispatch(ISimEvent simEvent)
        {
            if (!_handlers.TryGetValue(simEvent.GetType(), out var list) || list.Count == 0) return;

            // 처리 중 구독 해제에 안전하도록 복사본으로 돈다.
            var snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i].Invoke(simEvent);
        }
    }
}
