using System;
using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;
using LiteGame.Ui;

namespace LiteGame.Boot.States
{
    /// <summary>Lobby：显示 Lua 页（P0 由 Page_Lobby 渲染 hello）；退出清页面栈。</summary>
    public sealed class LobbyState : IAppState
    {
        private readonly UiHost _ui;

        public LobbyState(UiHost ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public async UniTask EnterAsync()
        {
            await _ui.Screens.ShowAsync(ScreenId.Lobby);
            Log.Info("进入 Lobby", LogTag.Flow);
        }

        public UniTask ExitAsync()
        {
            return _ui.Screens.CloseAllAsync();
        }

        public void Tick(float deltaSeconds)
        {
        }
    }
}
