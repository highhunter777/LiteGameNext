using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;

namespace LiteGame.Boot.States
{
    /// <summary>Room：房间内等待开局；P0 占位。</summary>
    public sealed class RoomState : IAppState
    {
        public UniTask EnterAsync()
        {
            Log.Info("进入 Room（P0 占位）", "RoomState");
            return UniTask.CompletedTask;
        }

        public UniTask ExitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void Tick(float deltaSeconds)
        {
        }
    }
}
