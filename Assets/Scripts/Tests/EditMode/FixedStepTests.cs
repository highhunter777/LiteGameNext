using System;
using LiteGame.Core.Timing;
using NUnit.Framework;

namespace LiteGame.Tests.EditMode
{
    /// <summary>FixedStep 的定步、追帧上限与插值契约。</summary>
    public sealed class FixedStepTests
    {
        [Test]
        public void Constructor_NonPositiveStep_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedStep(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedStep(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedStep(1f / 60f, 0));
        }

        [Test]
        public void Advance_BelowOneStep_ReturnsZeroAndKeepsAlpha()
        {
            var step = new FixedStep(1f / 60f);

            var steps = step.Advance(0.01f);

            Assert.AreEqual(0, steps);
            Assert.That(step.Alpha, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void Advance_AccumulatesAcrossCalls_UntilOneStep()
        {
            var step = new FixedStep(1f / 60f);

            Assert.AreEqual(0, step.Advance(0.01f));
            Assert.AreEqual(1, step.Advance(0.01f));
            Assert.AreEqual(1, step.Advance(0.02f));
        }

        [Test]
        public void Advance_HugeDelta_ClampsToMaxStepsAndDropsBacklog()
        {
            var step = new FixedStep(1f / 60f, maxStepsPerTick: 5);

            var steps = step.Advance(10f);

            Assert.AreEqual(5, steps);
            Assert.AreEqual(0f, step.Alpha);
        }

        [Test]
        public void Advance_NegativeDelta_IsTreatedAsZero()
        {
            var step = new FixedStep(1f / 60f);

            Assert.AreEqual(0, step.Advance(-1f));
            Assert.AreEqual(0f, step.Alpha);
        }
    }
}
