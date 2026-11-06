using System;
using Cysharp.Threading.Tasks;
using LiteGame.Core.Lua;
using LiteGame.Core.Logging;

namespace LiteGame.Boot.States
{
    /// <summary>Boot：初始化 Lua 环境；失败由 AppFlow 转到 Error。</summary>
    public sealed class BootState : IAppState
    {
        private readonly LuaHost _lua;

        public BootState(LuaHost lua)
        {
            _lua = lua ?? throw new ArgumentNullException(nameof(lua));
        }

        public UniTask EnterAsync()
        {
            if (!_lua.Start())
            {
                throw new InvalidOperationException("Lua 启动失败，详见日志");
            }

            Log.Info("进入 Boot", "BootState");
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
