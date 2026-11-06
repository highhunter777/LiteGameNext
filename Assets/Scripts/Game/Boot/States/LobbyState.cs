using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;

namespace LiteGame.Boot.States
{
    /// <summary>Lobby：大厅（房间列表/创房）；P0 占位。</summary>
    public sealed class LobbyState : IAppState
    {
        public UniTask EnterAsync()
        {
            Log.Info("进入 Lobby（P0 占位）", "LobbyState");
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
