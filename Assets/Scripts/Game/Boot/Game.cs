using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteGame.Boot.States;
using LiteGame.Core.Events;
using LiteGame.Core.Host;
using LiteGame.Core.Logging;
using LiteGame.Core.Lua;
using LiteGame.Core.Storage;
using LiteGame.Core.Timing;

namespace LiteGame.Boot
{
    /// <summary>组合根：唯一 new 业务件的地方；持有长期服务与 Tick 序。</summary>
    public sealed class Game : IGame
    {
        public readonly GameEntry Host;
        public readonly GameClock Clock;
        public readonly FixedStep Step;
        public readonly EventBus Events;
        public readonly LocalStore Store;
        public readonly LuaHost Lua;
        public readonly AppFlow Flow;

        private bool _started;
        private bool _disposed;

        private Game(GameEntry host, GameClock clock, FixedStep step, EventBus events, LocalStore store, LuaHost lua, AppFlow flow)
        {
            Host = host;
            Clock = clock;
            Step = step;
            Events = events;
            Store = store;
            Lua = lua;
            Flow = flow;
        }

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
            var flow = new AppFlow(new Dictionary<AppState, IAppState>
            {
                { AppState.Boot, new BootState(lua) },
                { AppState.Patch, new PatchState() },
                { AppState.Login, new LoginState() },
                { AppState.Lobby, new LobbyState() },
                { AppState.Room, new RoomState() },
                { AppState.Battle, new BattleState() },
                { AppState.Result, new ResultState() },
                { AppState.Error, new ErrorState() },
            });

            return new Game(host, new GameClock(), new FixedStep(), new EventBus(), new LocalStore(), lua, flow);
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

            _started = true;
            token.ThrowIfCancellationRequested();
            Log.Info("启动", "Game");
            await Flow.EnterAsync(AppState.Boot);
        }

        /// <summary>P0 只有时钟与流程；战斗/网络分步在 P1 按 01 §5 插入。</summary>
        public void Tick(float deltaSeconds)
        {
            if (_disposed)
            {
                return;
            }

            Clock.Tick(deltaSeconds);
            Flow.Tick(deltaSeconds);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Lua.Dispose();
        }
    }
}
