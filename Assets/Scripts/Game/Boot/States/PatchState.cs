using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;

namespace LiteGame.Boot.States
{
    /// <summary>Patch：内容补丁；P0 未接内容管线，只占位。</summary>
    public sealed class PatchState : IAppState
    {
        public UniTask EnterAsync()
        {
            Log.Info("进入 Patch（P0 占位）", "PatchState");
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
