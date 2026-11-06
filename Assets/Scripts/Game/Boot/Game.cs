using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteGame.Boot.States;
using LiteGame.Core.Config;
using LiteGame.Core.Events;
using LiteGame.Core.Host;
using LiteGame.Core.Logging;
using LiteGame.Core.Lua;
using LiteGame.Core.Storage;
using LiteGame.Core.Timing;
using LiteGame.Ui;

namespace LiteGame.Boot
{
    /// <summary>组合根：唯一 new 业务件的地方；持有长期服务与 Tick 序。</summary>
    public sealed class Game : IGame
    {
        private bool _started;
        private bool _disposed;
        private AppState _pendingState;

        private Game(GameEntry host, GameClock clock, FixedStep step, EventBus events, LocalStore store,
            LuaHost lua, UiHost ui, AppFlow flow)
        {
            Host = host;
            Clock = clock;
            Step = step;
            Events = events;
            Store = store;
            Lua = lua;
            Ui = ui;
            Flow = flow;
        }

        public GameEntry Host { get; }

        public GameClock Clock { get; }

        public FixedStep Step { get; }

        public EventBus Events { get; }

        public LocalStore Store { get; }

        public LuaHost Lua { get; }

        public UiHost Ui { get; }

        public AppFlow Flow { get; }

        /// <summary>装全套依赖并建状态表；工厂缝由 BootEntry 调用。</summary>
        public static Game Build(GameEntry host, LaunchConfig config)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var lua = new LuaHost(config.LuaRoot);
            var configHost = new ConfigHost(ReadConfigBytes);
            var ui = UiHost.Create(host.UiRootPrefab);
            ui.Initialize(lua);

            Game game = null;
            Action<AppState> request = state => game?.RequestState(state);
            var flow = new AppFlow(new Dictionary<AppState, IAppState>
            {
                { AppState.Boot, new BootState(lua, ui, request, configHost) },
                { AppState.Patch, new PatchState() },
                { AppState.Login, new LoginState() },
                { AppState.Lobby, new LobbyState(ui) },
                { AppState.Room, new RoomState() },
                { AppState.Battle, new BattleState() },
                { AppState.Result, new ResultState() },
                { AppState.Error, new ErrorState(ui, request) },
            });

            game = new Game(host, new GameClock(), new FixedStep(), new EventBus(), new LocalStore(), lua, ui, flow);
            return game;
        }

        /// <summary>P1 从工程内读表（编辑期路径）；Player 内容管线在 P3 接管。</summary>
        private static byte[] ReadConfigBytes(string location)
        {
            var relative = location.StartsWith("Assets/", StringComparison.Ordinal) ? location.Substring(7) : location;
            var path = Path.Combine(UnityEngine.Application.dataPath, relative);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("配置表缺失：" + location, path);
            }

            return File.ReadAllBytes(path);
        }

        public async UniTask StartAsync(CancellationToken token)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Game));
            }

            if (_started)
            {
                throw new InvalidOperationException("Game 已启动");
            }

            token.ThrowIfCancellationRequested();
            _started = true;
            Log.Info("启动", LogTag.Game);
            await Flow.EnterAsync(AppState.Boot);
        }

        /// <summary>P0 只有时钟、流程与 UI；战斗/网络分步在 P1 按 01 §5 插入。</summary>
        public void Tick(float deltaSeconds)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Game));
            }

            Clock.Tick(deltaSeconds);
            Flow.Tick(deltaSeconds);
            Ui.Tick(deltaSeconds);
            ApplyPendingState();
        }

        /// <summary>状态/页面的切态请求；由 Tick 统一交给 AppFlow（唯一写者）。</summary>
        /// <summary>返回键：只退页面栈；空栈或已释放时无操作。</summary>
        public void HandleBack()
        {
            if (_disposed || Ui.Screens == null)
            {
                return;
            }

            Ui.Screens.BackAsync().Forget(exception => Log.Error(exception, LogTag.Ui));
        }

        public void RequestState(AppState next)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Game));
            }

            if (next == AppState.None)
            {
                return;
            }

            if (!Flow.Contains(next))
            {
                throw new ArgumentOutOfRangeException(nameof(next), next, "状态未注册");
            }

            _pendingState = next;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            // 先退状态（Exit 目前同步完成）再拆 UI/Lua；跨帧 Exit 由 Forget 记日志。
            Flow.ShutdownAsync().Forget(exception => Log.Error(exception, LogTag.Flow));
            Ui.Dispose();
            Lua.Dispose();
        }

        private void ApplyPendingState()
        {
            if (_pendingState == AppState.None || Flow.IsSwitching)
            {
                return;
            }

            var next = _pendingState;
            _pendingState = AppState.None;
            Flow.EnterAsync(next).Forget(exception => Log.Error(exception, LogTag.Flow));
        }
    }
}
