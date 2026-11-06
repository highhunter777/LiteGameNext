using System;
using LiteGame.Core.Pooling;
using Xunit;

namespace LiteGame.Tests.Core
{
    /// <summary>ObjectPool 的复用、上限、预热、回调与释放契约（xUnit，不依赖 Unity）。</summary>
    public sealed class ObjectPoolTests
    {
        [Fact]
        public void Rent_Empty_CreatesInstance()
        {
            var created = 0;
            var pool = new ObjectPool<object>(() =>
            {
                created++;
                return new object();
            });

            var item = pool.Rent();

            Assert.NotNull(item);
            Assert.Equal(1, created);
            Assert.Equal(0, pool.IdleCount);
            pool.Dispose();
        }

        [Fact]
        public void Return_ThenRent_ReusesSameInstance()
        {
            var pool = new ObjectPool<object>(() => new object());
            var first = pool.Rent();

            pool.Return(first);
            var second = pool.Rent();

            Assert.Same(first, second);
            pool.Dispose();
        }

        [Fact]
        public void Return_BeyondMaxIdle_DiscardsOverflow()
        {
            var discarded = 0;
            var pool = new ObjectPool<object>(() => new object(), onDiscard: _ => discarded++, maxIdle: 1);
            var first = pool.Rent();
            var second = pool.Rent();

            pool.Return(first);
            pool.Return(second);

            Assert.Equal(1, pool.IdleCount);
            Assert.Equal(1, discarded);
            pool.Dispose();
        }

        [Fact]
        public void RentAndReturn_InvokeCallbacks()
        {
            var rented = 0;
            var returned = 0;
            var pool = new ObjectPool<object>(() => new object(), onRent: _ => rented++, onReturn: _ => returned++);

            pool.Return(pool.Rent());

            Assert.Equal(1, rented);
            Assert.Equal(1, returned);
            pool.Dispose();
        }

        [Fact]
        public void Prewarm_FillsToRequestedIdleCount()
        {
            var created = 0;
            var pool = new ObjectPool<object>(() =>
            {
                created++;
                return new object();
            }, maxIdle: 3);

            pool.Prewarm(2);
            pool.Prewarm(2);
            pool.Prewarm(3);

            Assert.Equal(3, created);
            Assert.Equal(3, pool.IdleCount);
            pool.Dispose();
        }

        [Fact]
        public void Prewarm_AboveMaxIdle_Throws()
        {
            var pool = new ObjectPool<object>(() => new object(), maxIdle: 2);

            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Prewarm(3));
            pool.Dispose();
        }

        [Fact]
        public void Prewarm_Negative_Throws()
        {
            var pool = new ObjectPool<object>(() => new object());

            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Prewarm(-1));
            pool.Dispose();
        }

        [Fact]
        public void Rent_FactoryReturnsNull_Throws()
        {
            var pool = new ObjectPool<object>(() => null);

            Assert.Throws<InvalidOperationException>(() => pool.Rent());
            pool.Dispose();
        }

        [Fact]
        public void Return_Null_Throws()
        {
            var pool = new ObjectPool<object>(() => new object());

            Assert.Throws<ArgumentNullException>(() => pool.Return(null));
            pool.Dispose();
        }

        [Fact]
        public void Return_NeverRented_Throws()
        {
            var pool = new ObjectPool<object>(() => new object());

            Assert.Throws<InvalidOperationException>(() => pool.Return(new object()));
            pool.Dispose();
        }

        [Fact]
        public void Return_Twice_Throws()
        {
            var pool = new ObjectPool<object>(() => new object());
            var item = pool.Rent();
            pool.Return(item);

            Assert.Throws<InvalidOperationException>(() => pool.Return(item));
            pool.Dispose();
        }

        [Fact]
        public void IsRented_ReflectsRentAndReturn()
        {
            var pool = new ObjectPool<object>(() => new object());
            var item = pool.Rent();

            Assert.True(pool.IsRented(item));

            pool.Return(item);

            Assert.False(pool.IsRented(item));
            pool.Dispose();
        }

        [Fact]
        public void IsRented_NeverRentedOrNull_IsFalse()
        {
            var pool = new ObjectPool<object>(() => new object());

            Assert.False(pool.IsRented(new object()));
            Assert.False(pool.IsRented(null));
            pool.Dispose();
        }

        [Fact]
        public void Ctor_InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectPool<object>(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectPool<object>(() => new object(), maxIdle: 0));
        }

        [Fact]
        public void Dispose_DiscardsIdle_AndBlocksAccess()
        {
            var discarded = 0;
            var pool = new ObjectPool<object>(() => new object(), onDiscard: _ => discarded++);
            pool.Return(pool.Rent());

            pool.Dispose();
            pool.Dispose();

            Assert.Equal(1, discarded);
            Assert.Throws<ObjectDisposedException>(() => pool.Rent());
            Assert.Throws<ObjectDisposedException>(() => pool.Return(new object()));
            Assert.Throws<ObjectDisposedException>(() => pool.Prewarm(1));
            Assert.Throws<ObjectDisposedException>(() => pool.IsRented(new object()));
        }
    }
}
