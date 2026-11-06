using System;
using System.Collections.Generic;
using LiteGame.Core.Logging;

namespace LiteGame.Core.Events
{
    /// <summary>强类型事件总线：实例持有、零反射、仅主线程。
    /// 派发期间增删订阅安全：新增只影响下一次派发，退订立即生效（未轮到的不再调用）。
    /// 单个处理器的异常被隔离并记日志，不中断其余处理器。</summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, object> _channels = new Dictionary<Type, object>();

        /// <summary>订阅；Dispose 即退订，可重复 Dispose。</summary>
        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return GetOrCreate<T>().Subscribe(handler);
        }

        /// <summary>派发；无订阅者时零分配。</summary>
        public void Publish<T>(T signal)
        {
            if (_channels.TryGetValue(typeof(T), out var channel))
            {
                ((Channel<T>)channel).Publish(signal);
            }
        }

        /// <summary>清空全部订阅；派发中调用同样安全。</summary>
        public void Clear()
        {
            foreach (var pair in _channels)
            {
                ((IChannel)pair.Value).Clear();
            }
        }

        private Channel<T> GetOrCreate<T>()
        {
            if (_channels.TryGetValue(typeof(T), out var channel))
            {
                return (Channel<T>)channel;
            }

            var created = new Channel<T>();
            _channels.Add(typeof(T), created);
            return created;
        }

        private interface IChannel
        {
            void Clear();
        }

        private sealed class Channel<T> : IChannel
        {
            private readonly List<Slot> _slots = new List<Slot>(4);
            private int _publishing;

            public IDisposable Subscribe(Action<T> handler)
            {
                var slot = new Slot(handler);
                _slots.Add(slot);
                return new Subscription(this, slot);
            }

            public void Publish(T signal)
            {
                var count = _slots.Count;
                if (count == 0)
                {
                    return;
                }

                _publishing++;
                try
                {
                    // Clear 期间 List 不再被整体清空，Count 保持不变；
                    // i < _slots.Count 仅作防御。
                    for (var i = 0; i < count && i < _slots.Count; i++)
                    {
                        var slot = _slots[i];
                        if (!slot.Active)
                        {
                            continue;
                        }

                        try
                        {
                            slot.Handler(signal);
                        }
                        catch (Exception exception)
                        {
                            Log.Error(exception, LogTag.EventBus, typeof(T).Name);
                        }
                    }
                }
                finally
                {
                    _publishing--;
                    if (_publishing == 0)
                    {
                        Sweep();
                    }
                }
            }

            public void Clear()
            {
                for (var i = 0; i < _slots.Count; i++)
                {
                    _slots[i].Active = false;
                }

                // 非派发期可直接清空；派发期留给 Sweep 按 Active 移除，
                // 这样 Clear 之后同周期新订阅的 Slot（Active=true）会被保留。
                if (_publishing == 0)
                {
                    _slots.Clear();
                }
            }

            private void Unsubscribe(Slot slot)
            {
                if (!slot.Active)
                {
                    return;
                }

                slot.Active = false;
                if (_publishing == 0)
                {
                    Sweep();
                }
            }

            private void Sweep()
            {
                // 只移除失效 Slot；派发中 Clear 后新订阅的 Active=true 会留下。
                for (var i = _slots.Count - 1; i >= 0; i--)
                {
                    if (!_slots[i].Active)
                    {
                        _slots.RemoveAt(i);
                    }
                }
            }

            private sealed class Slot
            {
                public Slot(Action<T> handler)
                {
                    Handler = handler;
                    Active = true;
                }

                public Action<T> Handler { get; }

                public bool Active { get; set; }
            }

            private sealed class Subscription : IDisposable
            {
                private readonly Slot _slot;
                private Channel<T> _channel;

                public Subscription(Channel<T> channel, Slot slot)
                {
                    _channel = channel;
                    _slot = slot;
                }

                public void Dispose()
                {
                    if (_channel == null)
                    {
                        return;
                    }

                    _channel.Unsubscribe(_slot);
                    _channel = null;
                }
            }
        }
    }
}
