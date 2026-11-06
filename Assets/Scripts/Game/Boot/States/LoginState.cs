using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;

namespace LiteGame.Boot.States
{
    /// <summary>Login：账号登录；P0 无网络，只占位。</summary>
    public sealed class LoginState : IAppState
    {
        public UniTask EnterAsync()
        {
            Log.Info("进入 Login（P0 占位）", "LoginState");
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
