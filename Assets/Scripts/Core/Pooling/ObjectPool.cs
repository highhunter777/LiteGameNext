using System;
using System.Collections.Generic;

namespace LiteGame.Core.Pooling
{
    /// <summary>最小对象池：栈式复用 + 工厂/取用/归还/丢弃回调；无统计、淘汰策略与接口层。
    /// 非线程安全；回调同步执行且不得抛异常，池只保证自身状态一致。</summary>
    public sealed class ObjectPool<T> : IDisposable where T : class
    {
        private readonly Func<T> _create;
        private readonly Action<T> _onRent;
        private readonly Action<T> _onReturn;
        private readonly Action<T> _onDiscard;
        private readonly Stack<T> _idle;
        private readonly HashSet<T> _rented;

        private bool _disposed;

        /// <summary>maxIdle 为闲置上限；超出的归还件交给 onDiscard 丢弃。</summary>
        public ObjectPool(Func<T> create, Action<T> onRent = null, Action<T> onReturn = null,
            Action<T> onDiscard = null, int maxIdle = 64)
        {
            if (create == null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            if (maxIdle < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxIdle));
            }

            _create = create;
            _onRent = onRent;
            _onReturn = onReturn;
            _onDiscard = onDiscard;
            _idle = new Stack<T>(maxIdle);
            _rented = new HashSet<T>(ReferenceComparer.Instance);
            MaxIdle = maxIdle;
        }

        public int MaxIdle { get; }

        public int IdleCount => _idle.Count;

        /// <summary>取件：命中复用，未命中调用工厂；计入在租集合后执行 onRent。工厂返回 null 视为缺陷。</summary>
        public T Rent()
        {
            ThrowIfDisposed();

            var item = _idle.Count > 0 ? _idle.Pop() : Create();
            _rented.Add(item);
            _onRent?.Invoke(item);
            return item;
        }

        /// <summary>把闲置件补足到 count 个（已有不重建），不执行 onRent；count 超限即抛。</summary>
        public void Prewarm(int count)
        {
            ThrowIfDisposed();

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count > MaxIdle)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "count 超过 MaxIdle");
            }

            while (_idle.Count < count)
            {
                _idle.Push(Create());
            }
        }

        /// <summary>查询实例是否在本池租出中；归还后为 false（给表现层断言用，null 视为 false）。</summary>
        public bool IsRented(T item)
        {
            ThrowIfDisposed();
            return item != null && _rented.Contains(item);
        }

        /// <summary>归还：未租出/已归还/重复归还一律抛；满员交给 onDiscard。</summary>
        public void Return(T item)
        {
            ThrowIfDisposed();

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!_rented.Remove(item))
            {
                throw new InvalidOperationException("归还了未租出或已归还的对象");
            }

            if (_idle.Count >= MaxIdle)
            {
                _onDiscard?.Invoke(item);
                return;
            }

            _onReturn?.Invoke(item);
            _idle.Push(item);
        }

        /// <summary>幂等；丢弃全部闲置件（回调异常会传播，池已置为已释放）。之后再访问抛。</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            while (_idle.Count > 0)
            {
                // 先出栈再回调：空条件调用会跳过参数求值，写成 ?.Invoke(_idle.Pop()) 会死循环。
                var item = _idle.Pop();
                _onDiscard?.Invoke(item);
            }
        }

        /// <summary>引用语义比较：T 覆写 Equals 也不影响“同一实例”判定。</summary>
        private sealed class ReferenceComparer : IEqualityComparer<T>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

        private T Create()
        {
            return _create() ?? throw new InvalidOperationException("池工厂返回 null");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ObjectPool<T>));
            }
        }
    }
}
