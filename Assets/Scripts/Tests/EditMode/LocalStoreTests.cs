using System;
using LiteGame.Core.Storage;
using NUnit.Framework;

namespace LiteGame.Tests.EditMode
{
    /// <summary>LocalStore 的键校验与读写往返。</summary>
    public sealed class LocalStoreTests
    {
        private const string IntKey = "Tests.LocalStore.Int";
        private const string BoolKey = "Tests.LocalStore.Bool";
        private const string StringKey = "Tests.LocalStore.String";

        [TearDown]
        public void TearDown()
        {
            var store = new LocalStore();
            store.Delete(IntKey);
            store.Delete(BoolKey);
            store.Delete(StringKey);
        }

        [Test]
        public void GetInt_NullOrEmptyKey_Throws()
        {
            var store = new LocalStore();

            Assert.Throws<ArgumentException>(() => store.GetInt(null));
            Assert.Throws<ArgumentException>(() => store.GetInt(string.Empty));
        }

        [Test]
        public void SetString_NullValue_Throws()
        {
            var store = new LocalStore();

            Assert.Throws<ArgumentNullException>(() => store.SetString(StringKey, null));
        }

        [Test]
        public void SetAndGet_RoundTripsValues()
        {
            var store = new LocalStore();
            store.SetInt(IntKey, 42);
            store.SetBool(BoolKey, true);
            store.SetString(StringKey, "hello");

            Assert.AreEqual(42, store.GetInt(IntKey));
            Assert.IsTrue(store.GetBool(BoolKey));
            Assert.AreEqual("hello", store.GetString(StringKey));
            Assert.IsTrue(store.Has(IntKey));
            Assert.IsFalse(store.Has("Tests.LocalStore.Missing"));
        }
    }
}
