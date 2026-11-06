using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;

namespace LiteGame.Boot.States
{
    /// <summary>Battle：一局对战；P0 占位。</summary>
    public sealed class BattleState : IAppState
    {
        public UniTask EnterAsync()
        {
            Log.Info("进入 Battle（P0 占位）", LogTag.Flow);
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
