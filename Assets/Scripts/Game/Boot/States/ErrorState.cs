using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;

namespace LiteGame.Boot.States
{
    /// <summary>Error：确定性错误态；只记录，不再自行切换。</summary>
    public sealed class ErrorState : IAppState
    {
        public UniTask EnterAsync()
        {
            Log.Error("进入 Error：流程已停在此态", "ErrorState");
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
