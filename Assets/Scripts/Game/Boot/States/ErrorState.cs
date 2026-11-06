using System;
using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;
using LiteGame.Ui;
using LiteGame.Ui.Screens;

namespace LiteGame.Boot.States
{
    /// <summary>Error：确定性错误态；展示错误页，返回请求交给组合根处理。</summary>
    public sealed class ErrorState : IAppState
    {
        private readonly UiHost _ui;
        private readonly Action<AppState> _request;

        public ErrorState(UiHost ui, Action<AppState> request)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public async UniTask EnterAsync()
        {
            Log.Error("进入 Error：流程已停在此态", LogTag.Flow);
            await _ui.Screens.ShowAsync(ScreenId.Error);

            if (_ui.Screens.Current is ErrorScreen screen)
            {
                screen.BackRequested = () => _request(AppState.Boot);
            }
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
