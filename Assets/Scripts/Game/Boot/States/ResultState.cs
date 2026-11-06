using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;

namespace LiteGame.Boot.States
{
    /// <summary>Result：结算展示；P0 占位。</summary>
    public sealed class ResultState : IAppState
    {
        public UniTask EnterAsync()
        {
            Log.Info("进入 Result（P0 占位）", LogTag.Flow);
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
