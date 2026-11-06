using System;
using System.Text.RegularExpressions;
using LiteGame.Core.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LiteGame.Tests.EditMode
{
    /// <summary>EventBus 的派发期行为契约：增删订阅、嵌套派发、异常隔离。</summary>
    public sealed class EventBusTests
    {
        [Test]
        public void UnsubscribeInsideHandler_SkipsRemainingHandlers()
        {
            var bus = new EventBus();
            var calls = 0;
            IDisposable second = null;
            bus.Subscribe<int>(_ =>
            {
                calls++;
                second.Dispose();
            });
            second = bus.Subscribe<int>(_ => calls += 10);
            bus.Publish(1);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void SubscribeInsideHandler_NotCalledByCurrentPublish()
        {
            var bus = new EventBus();
            var calls = 0;
            bus.Subscribe<int>(_ =>
            {
                calls++;
                bus.Subscribe<int>(__ => calls += 100);
            });
            bus.Publish(1);

            Assert.AreEqual(1, calls);

            bus.Publish(1);
            Assert.AreEqual(102, calls);
        }

        [Test]
        public void PublishInsideHandler_ReentrantDispatchCompletes()
        {
            var bus = new EventBus();
            var calls = 0;
            bus.Subscribe<int>(value =>
            {
                calls++;
                if (value < 2)
                {
                    bus.Publish(value + 1);
                }
            });
            bus.Publish(1);

            Assert.AreEqual(2, calls);
        }

        [Test]
        public void ThrowingHandler_DoesNotStopRemainingHandlers()
        {
            var bus = new EventBus();
            var calls = 0;
            LogAssert.Expect(LogType.Error, new Regex("EventBus/Int32"));
            bus.Subscribe<int>(_ => throw new InvalidOperationException("boom"));
            bus.Subscribe<int>(_ => calls++);
            bus.Publish(1);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void ClearInsideHandler_StopsRemainingHandlers()
        {
            var bus = new EventBus();
            var calls = 0;
            bus.Subscribe<int>(_ =>
            {
                calls++;
                bus.Clear();
            });
            bus.Subscribe<int>(_ => calls += 10);
            bus.Publish(1);

            Assert.AreEqual(1, calls);
        }
    }
}
