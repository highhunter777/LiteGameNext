using System;
using LiteGame.Core.Logging;
using LiteGame.Ui;
using XLua;

namespace LiteGame.Facade
{
    /// <summary>Lua 门面装配：只把 C# 能力注册成表，不做缓存、不持有业务状态（03 §3）。
    /// P0 只装 ui 一张表，其余 7 张随各自消费者补。</summary>
    public sealed partial class Bridge
    {
        private readonly LuaEnv _env;
        private readonly ScreenManager _screens;

        public Bridge(LuaEnv env, ScreenManager screens)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _screens = screens ?? throw new ArgumentNullException(nameof(screens));
        }

        /// <summary>装表到全局 Bridge；重复调用覆盖同名表（Boot 重入不炸）。</summary>
        public void Install()
        {
            var root = _env.NewTable();
            InstallUi(root);
            _env.Global.Set("Bridge", root);
        }
    }
}
