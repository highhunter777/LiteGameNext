using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using LiteGame.Boot;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LiteGame.Tests.EditMode
{
    /// <summary>AppFlow 的切换契约：幂等、未知态、失败转 Error、先退出后进入、切换中重入。</summary>
    public sealed class AppFlowTests
    {
        private FakeState _boot;
        private FakeState _login;
        private FakeState _error;
        private AppFlow _flow;

        [SetUp]
        public void SetUp()
        {
            _boot = new FakeState();
            _login = new FakeState();
            _error = new FakeState();
            _flow = new AppFlow(new Dictionary<AppState, IAppState>
            {
                { AppState.Boot, _boot },
                { AppState.Login, _login },
                { AppState.Error, _error },
            });
        }

        [Test]
        public void Enter_SameStateTwice_DoesNotReenter()
        {
            _flow.EnterAsync(AppState.Boot).GetAwaiter().GetResult();
            _flow.EnterAsync(AppState.Boot).GetAwaiter().GetResult();

            Assert.AreEqual(1, _boot.Enters);
            Assert.AreEqual(AppState.Boot, _flow.Current);
        }

        [Test]
        public void Enter_UnknownState_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _flow.EnterAsync(AppState.Room).GetAwaiter().GetResult());
        }

        [Test]
        public void Enter_StateThrows_TransitionsToError()
        {
            _boot.ThrowOnEnter = true;
            LogAssert.Expect(LogType.Error, new Regex(@"\[Flow\]"));

            _flow.EnterAsync(AppState.Boot).GetAwaiter().GetResult();

            Assert.AreEqual(AppState.Error, _flow.Current);
            Assert.AreEqual(1, _error.Enters);
        }

        [Test]
        public void Enter_NextState_ExitsPrevious()
        {
            _flow.EnterAsync(AppState.Boot).GetAwaiter().GetResult();
            _flow.EnterAsync(AppState.Login).GetAwaiter().GetResult();

            Assert.AreEqual(1, _boot.Exits);
            Assert.AreEqual(1, _login.Enters);
        }

        [Test]
        public void Enter_WhileSwitching_Throws()
        {
            var guardObserved = false;
            _boot.OnEnter = () =>
            {
                try
                {
                    _flow.EnterAsync(AppState.Login).GetAwaiter().GetResult();
                }
                catch (InvalidOperationException)
                {
                    guardObserved = true;
                }
            };

            _flow.EnterAsync(AppState.Boot).GetAwaiter().GetResult();

            Assert.IsTrue(guardObserved);
        }

        [Test]
        public void Contains_ReflectsRegisteredStates()
        {
            Assert.IsTrue(_flow.Contains(AppState.Boot));
            Assert.IsFalse(_flow.Contains(AppState.Room));
        }

        [Test]
        public void Constructor_CopiesStates()
        {
            var states = new Dictionary<AppState, IAppState>
            {
                { AppState.Boot, new FakeState() },
            };
            var flow = new AppFlow(states);
            states.Clear();

            Assert.IsTrue(flow.Contains(AppState.Boot));
        }

        [Test]
        public void Shutdown_ExitsCurrentAndResets()
        {
            _flow.EnterAsync(AppState.Boot).GetAwaiter().GetResult();
            _flow.ShutdownAsync().GetAwaiter().GetResult();

            Assert.AreEqual(1, _boot.Exits);
            Assert.AreEqual(AppState.None, _flow.Current);
        }

        private sealed class FakeState : IAppState
        {
            public int Enters { get; private set; }

            public int Exits { get; private set; }

            public bool ThrowOnEnter { get; set; }

            public Action OnEnter { get; set; }

            public UniTask EnterAsync()
            {
                Enters++;
                OnEnter?.Invoke();
                if (ThrowOnEnter)
                {
                    throw new InvalidOperationException("boom");
                }

                return UniTask.CompletedTask;
            }

            public UniTask ExitAsync()
            {
                Exits++;
                return UniTask.CompletedTask;
            }

            public void Tick(float deltaSeconds)
            {
            }
        }
    }
}
