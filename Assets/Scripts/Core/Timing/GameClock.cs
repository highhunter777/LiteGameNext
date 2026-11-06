namespace LiteGame.Core.Timing
{
    /// <summary>帧时间累加器：dt 由调用方喂入，不直接读 Unity Time（可在 dotnet 侧单测）。</summary>
    public sealed class GameClock
    {
        /// <summary>本次帧间隔（秒），负数按 0 处理。</summary>
        public float Delta { get; private set; }

        /// <summary>累计运行时间（秒）。</summary>
        public double Elapsed { get; private set; }

        /// <summary>累计帧号。</summary>
        public int Frame { get; private set; }

        public void Tick(float deltaSeconds)
        {
            Delta = deltaSeconds > 0f ? deltaSeconds : 0f;
            Elapsed += Delta;
            Frame++;
        }

        public void Reset()
        {
            Delta = 0f;
            Elapsed = 0d;
            Frame = 0;
        }
    }
}
