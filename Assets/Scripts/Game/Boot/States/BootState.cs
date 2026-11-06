using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteGame.Core.Config;
using LiteGame.Core.Logging;
using LiteGame.Core.Lua;
using LiteGame.Facade;
using LiteGame.Ui;

namespace LiteGame.Boot.States
{
    /// <summary>Boot：装载配置 → 起 Lua → 装 Bridge → 显示首屏 → 交给 Lobby；失败由 AppFlow 转到 Error。</summary>
    public sealed class BootState : IAppState
    {
        private readonly LuaHost _lua;
        private readonly UiHost _ui;
        private readonly Action<AppState> _request;
        private readonly ConfigHost _config;

        public BootState(LuaHost lua, UiHost ui, Action<AppState> request, ConfigHost config)
        {
            _lua = lua ?? throw new ArgumentNullException(nameof(lua));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async UniTask EnterAsync()
        {
            await _config.LoadAsync(CancellationToken.None);
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
