using System;
using Cysharp.Threading.Tasks;
using LiteGame.Facade;
using LiteGame.Core.Logging;
using LiteGame.Core.Lua;
using LiteGame.Ui;

namespace LiteGame.Boot.States
{
    /// <summary>Boot：起 Lua、装 Bridge、显示首屏，再把流程交给 Lobby；失败由 AppFlow 转到 Error。</summary>
    public sealed class BootState : IAppState
    {
        private readonly LuaHost _lua;
        private readonly UiHost _ui;
        private readonly Action<AppState> _request;

        public BootState(LuaHost lua, UiHost ui, Action<AppState> request)
        {
            _lua = lua ?? throw new ArgumentNullException(nameof(lua));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public async UniTask EnterAsync()
        {
            if (!_lua.IsRunning && !_lua.Start())
            {
                throw new InvalidOperationException("Lua 启动失败，详见日志");
            }

            new Bridge(_lua.Env, _ui.Screens).Install();
            await _ui.Screens.ShowAsync(ScreenId.Loading);
            Log.Info("进入 Boot", LogTag.Flow);

            // 经 Game.RequestState 排队到下一帧，避免在切换中重入 AppFlow。
            _request(AppState.Lobby);
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
