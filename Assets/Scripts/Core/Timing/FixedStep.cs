using System;

namespace LiteGame.Core.Timing
{
    /// <summary>固定步长累加器：逻辑 60 Hz，落后过多时丢弃积累而不是无限追帧。</summary>
    public sealed class FixedStep
    {
        private float _accumulator;

        public FixedStep(float stepSeconds = 1f / 60f, int maxStepsPerTick = 5)
        {
            if (stepSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(stepSeconds));
            }

            if (maxStepsPerTick < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStepsPerTick));
            }

            StepSeconds = stepSeconds;
            MaxStepsPerTick = maxStepsPerTick;
        }

        /// <summary>单步时长（秒）。</summary>
        public float StepSeconds { get; }

        /// <summary>单帧最多执行的步数。</summary>
        public int MaxStepsPerTick { get; }

        /// <summary>渲染插值系数，取值 [0, 1)。</summary>
        public float Alpha { get; private set; }

        /// <summary>推进一帧，返回本次应执行的定步数。</summary>
        public int Advance(float deltaSeconds)
        {
            _accumulator += deltaSeconds > 0f ? deltaSeconds : 0f;
            var steps = (int)(_accumulator / StepSeconds);
            if (steps > MaxStepsPerTick)
            {
                steps = MaxStepsPerTick;
                _accumulator = 0f;
            }
            else
            {
                _accumulator -= steps * StepSeconds;
            }

            Alpha = _accumulator / StepSeconds;
            return steps;
        }

        public void Reset()
        {
            _accumulator = 0f;
            Alpha = 0f;
        }
    }
}
