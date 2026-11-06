using System;
using LiteGame.Core.Timing;
using Xunit;

namespace LiteGame.Tests.Core
{
    /// <summary>FixedStep 的定步、追帧上限与插值契约（原 EditMode 用例迁入）。</summary>
    public sealed class FixedStepTests
    {
        [Fact]
        public void Constructor_NonPositiveStep_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedStep(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedStep(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedStep(1f / 60f, 0));
        }

        [Fact]
        public void Advance_BelowOneStep_ReturnsZeroAndKeepsAlpha()
        {
            var step = new FixedStep(1f / 60f);

            var steps = step.Advance(0.01f);

            Assert.Equal(0, steps);
            Assert.Equal(0.6f, step.Alpha, 4);
        }

        [Fact]
        public void Advance_AccumulatesAcrossCalls_UntilOneStep()
        {
            var step = new FixedStep(1f / 60f);

            Assert.Equal(0, step.Advance(0.01f));
            Assert.Equal(1, step.Advance(0.01f));
            Assert.Equal(1, step.Advance(0.02f));
        }

        [Fact]
        public void Advance_HugeDelta_ClampsToMaxStepsAndDropsBacklog()
        {
            var step = new FixedStep(1f / 60f, maxStepsPerTick: 5);

            var steps = step.Advance(10f);

            Assert.Equal(5, steps);
            Assert.Equal(0f, step.Alpha, 4);
        }

        [Fact]
        public void Advance_NegativeDelta_IsTreatedAsZero()
        {
            var step = new FixedStep(1f / 60f);

            Assert.Equal(0, step.Advance(-1f));
            Assert.Equal(0f, step.Alpha, 4);
        }
    }
}
