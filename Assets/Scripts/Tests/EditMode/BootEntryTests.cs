using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteGame.Core.Host;
using LiteGame.Core.Logging;
using NUnit.Framework;
using UnityEngine;

namespace LiteGame.Tests.EditMode
{
    /// <summary>BootEntry 缝的契约：未注册/重复注册/工厂返回 null 都必须 fail-fast。</summary>
    public sealed class BootEntryTests
    {
        private GameObject _host;

        [SetUp]
        public void SetUp()
        {
            BootEntry.ResetForTests();
            _host = new GameObject("BootEntryTestHost");
            _host.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            BootEntry.ResetForTests();
            if (_host != null)
            {
                UnityEngine.Object.DestroyImmediate(_host);
            }
        }

        [Test]
        public void Create_WithoutRegister_Throws()
        {
            var entry = _host.AddComponent<GameEntry>();
            var config = new LaunchConfig(LogLevel.Info, "C:/lua");

            Assert.Throws<InvalidOperationException>(() => BootEntry.Create(entry, config));
        }

        [Test]
        public void Create_NullArguments_Throw()
        {
            var config = new LaunchConfig(LogLevel.Info, "C:/lua");
            var entry = _host.AddComponent<GameEntry>();

            Assert.Throws<ArgumentNullException>(() => BootEntry.Create(null, config));
            Assert.Throws<ArgumentNullException>(() => BootEntry.Create(entry, null));
        }

        [Test]
        public void Register_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BootEntry.Register(null));
        }

        [Test]
        public void Register_Twice_Throws()
        {
            BootEntry.Register((host, config) => new FakeGame());

            Assert.Throws<InvalidOperationException>(() => BootEntry.Register((host, config) => new FakeGame()));
        }

        [Test]
        public void Register_FactoryReturningNull_Throws()
        {
            BootEntry.Register((host, config) => null);
            var entry = _host.AddComponent<GameEntry>();
            var config = new LaunchConfig(LogLevel.Info, "C:/lua");

            Assert.Throws<InvalidOperationException>(() => BootEntry.Create(entry, config));
        }

        [Test]
        public void Register_ThenCreate_ReturnsFactoryResult()
        {
            var expected = new FakeGame();
            BootEntry.Register((host, config) => expected);
            var entry = _host.AddComponent<GameEntry>();
            var config = new LaunchConfig(LogLevel.Info, "C:/lua");

            var created = BootEntry.Create(entry, config);

            Assert.AreSame(expected, created);
        }

        private sealed class FakeGame : IGame
        {
            public UniTask StartAsync(CancellationToken token)
            {
                return UniTask.CompletedTask;
            }

            public void Tick(float deltaSeconds)
            {
            }

            public void HandleBack()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
