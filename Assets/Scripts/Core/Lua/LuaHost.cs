using System;
using System.IO;
using LiteGame.Core.Logging;
using XLua;

namespace LiteGame.Core.Lua
{
    /// <summary>Lua 环境宿主：根目录装载、入口执行、整包重载。仅主线程。
    /// 释放后可再次 Start（会新建环境）。</summary>
    public sealed class LuaHost : IDisposable
    {
        private const string MainScript = "main";

        private readonly string _root;
        private LuaEnv _env;

        public LuaHost(string luaRoot)
        {
            if (string.IsNullOrEmpty(luaRoot))
            {
                throw new ArgumentException("luaRoot 不能为空", nameof(luaRoot));
            }

            _root = luaRoot;
        }

        public bool IsRunning => _env != null;

        /// <summary>未启动或已释放时访问抛 InvalidOperationException。</summary>
        public LuaEnv Env
        {
            get
            {
                if (_env == null)
                {
                    throw new InvalidOperationException("LuaHost 未启动或已释放");
                }

                return _env;
            }
        }

        /// <summary>创建环境并执行 main.lua；失败记日志、清理并返回 false。</summary>
        public bool Start()
        {
            if (_env != null)
            {
                throw new InvalidOperationException("LuaHost 已启动，先用 Dispose 释放");
            }

            _env = new LuaEnv();
            _env.AddLoader(LoadChunk);
            try
            {
                _env.DoString("require('" + MainScript + "')", "main.lua");
                return true;
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Lua");
                Dispose();
                return false;
            }
        }

        public void Dispose()
        {
            _env?.Dispose();
            _env = null;
        }

        private byte[] LoadChunk(ref string filepath)
        {
            var path = Path.Combine(_root, filepath.Replace('.', '/') + ".lua");
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }
}
