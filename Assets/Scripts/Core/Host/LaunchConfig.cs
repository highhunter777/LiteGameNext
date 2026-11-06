using System;
using LiteGame.Core.Assets;
using LiteGame.Core.Logging;

namespace LiteGame.Core.Host
{
    /// <summary>启动参数：命令行/环境给出的运行期配置，构造后只读。</summary>
    public sealed class LaunchConfig
    {
        private const string LuaRootArg = "-luaRoot";
        private const string MetaUrlArg = "-metaUrl";
        private const string CdnRootArg = "-cdnRoot";

        public LaunchConfig(
            LogLevel logLevel,
            string luaRoot,
            string metaServerUrl = "",
            string cdnRoot = "",
            AssetMode assetMode = AssetMode.EditorSimulator)
        {
            if (string.IsNullOrEmpty(luaRoot))
            {
                throw new ArgumentException("luaRoot 不能为空", nameof(luaRoot));
            }

            LogLevel = logLevel;
            LuaRoot = luaRoot;
            MetaServerUrl = metaServerUrl ?? string.Empty;
            CdnRoot = cdnRoot ?? string.Empty;
            AssetMode = assetMode;
        }

        public LogLevel LogLevel { get; }

        public string LuaRoot { get; }

        /// <summary>为空表示离线（不走网络）。</summary>
        public string MetaServerUrl { get; }

        /// <summary>内容 CDN 根；P0 未接内容管线，仅保留参数。</summary>
        public string CdnRoot { get; }

        /// <summary>编辑器用模拟模式，Player 默认离线；联机模式由后续版本策略切。</summary>
        public AssetMode AssetMode { get; }

        /// <summary>从命令行解析；开关形如 `-luaRoot &lt;path&gt;`，缺省用 defaultLuaRoot。</summary>
        public static LaunchConfig Load(string[] args, string defaultLuaRoot, LogLevel logLevel = LogLevel.Info)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (string.IsNullOrEmpty(defaultLuaRoot))
            {
                throw new ArgumentException("defaultLuaRoot 不能为空", nameof(defaultLuaRoot));
            }

            return new LaunchConfig(
                logLevel,
                Read(args, LuaRootArg, defaultLuaRoot),
                Read(args, MetaUrlArg, string.Empty),
                Read(args, CdnRootArg, string.Empty),
                UnityEngine.Application.isEditor ? AssetMode.EditorSimulator : AssetMode.Offline);
        }

        private static string Read(string[] args, string name, string fallback)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }
    }
}
