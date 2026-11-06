using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;

namespace LiteGame.Boot
{
    /// <summary>应用状态机：切换的唯一写者。重复进入同态幂等；切换中重入 fail-fast。</summary>
    public sealed class AppFlow
    {
        private readonly IReadOnlyDictionary<AppState, IAppState> _states;
        private IAppState _current;
        private bool _switching;

        public AppFlow(IReadOnlyDictionary<AppState, IAppState> states)
        {
            if (states == null)
            {
                throw new ArgumentNullException(nameof(states));
            }

            if (states.Count == 0)
            {
                throw new ArgumentException("状态表不能为空", nameof(states));
            }

            _states = states;
        }

        public AppState Current { get; private set; } = AppState.None;

        /// <summary>切到目标状态；入口异常会转到 Error（Error 自身再失败则只记日志）。</summary>
        public UniTask EnterAsync(AppState next)
        {
            if (_switching)
            {
                throw new InvalidOperationException("AppFlow 正在切换，禁止重入");
            }

            return SwitchAsync(next);
        }

        public void Tick(float deltaSeconds)
        {
            _current?.Tick(deltaSeconds);
        }

        /// <summary>关停：退出当前状态并复位为 None。</summary>
        public async UniTask ShutdownAsync()
        {
            if (_current == null)
            {
                return;
            }

            var state = _current;
            _current = null;
            Current = AppState.None;
            await state.ExitAsync();
        }

        private async UniTask SwitchAsync(AppState next)
        {
            if (next == Current)
            {
                Log.Debug("已在 " + next + "，跳过", "AppFlow");
                return;
            }

            if (!_states.TryGetValue(next, out var state))
            {
                throw new ArgumentOutOfRangeException(nameof(next), next, "状态未注册");
            }

            _switching = true;
            try
            {
                if (_current != null)
                {
                    await _current.ExitAsync();
                }

                _current = state;
                Current = next;
                await state.EnterAsync();
            }
            catch (Exception exception)
            {
                Log.Error(exception, "AppFlow");
                if (next != AppState.Error)
                {
                    _switching = false;
                    await SwitchAsync(AppState.Error);
                }
            }
            finally
            {
                _switching = false;
            }
        }
    }
}
