using System;
using System.Collections.Generic;

namespace LiteGame.Core.Events
{
    /// <summary>强类型事件总线：实例持有、零反射；回调内不要增删订阅。</summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        /// <summary>订阅；Dispose 即退订。</summary>
        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>(4);
                _handlers.Add(typeof(T), list);
            }

            list.Add(handler);
            return new Subscription(list, handler);
        }

        /// <summary>派发；无订阅者时为零开销。</summary>
        public void Publish<T>(T signal)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                return;
            }

            var count = list.Count;
            for (var i = 0; i < count && i < list.Count; i++)
            {
                if (list[i] is Action<T> handler)
                {
                    handler(signal);
                }
            }
        }

        public void Clear()
        {
            _handlers.Clear();
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Delegate _handler;
            private List<Delegate> _list;

            public Subscription(List<Delegate> list, Delegate handler)
            {
                _list = list;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_list == null)
                {
                    return;
                }

                _list.Remove(_handler);
                _list = null;
            }
        }
    }
}
