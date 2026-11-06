using System;
using LiteGame.Core.Host;
using LiteGame.Core.Logging;
using NUnit.Framework;

namespace LiteGame.Tests.EditMode
{
    /// <summary>LaunchConfig 的参数校验与命令行解析。</summary>
    public sealed class LaunchConfigTests
    {
        [Test]
        public void Constructor_EmptyLuaRoot_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LaunchConfig(LogLevel.Info, null));
            Assert.Throws<ArgumentException>(() => new LaunchConfig(LogLevel.Info, string.Empty));
        }

        [Test]
        public void Load_NullArgs_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => LaunchConfig.Load(null, "C:/lua"));
        }

        [Test]
        public void Load_EmptyDefaultLuaRoot_Throws()
        {
            Assert.Throws<ArgumentException>(() => LaunchConfig.Load(new string[0], string.Empty));
        }

        [Test]
        public void Load_ParsesCommandLineOverrides()
        {
            var args = new[]
            {
                "-luaRoot", "D:/lua",
                "-metaUrl", "http://meta",
                "-cdnRoot", "http://cdn",
            };

            var config = LaunchConfig.Load(args, "C:/default");

            Assert.AreEqual("D:/lua", config.LuaRoot);
            Assert.AreEqual("http://meta", config.MetaServerUrl);
            Assert.AreEqual("http://cdn", config.CdnRoot);
        }

        [Test]
        public void Load_MissingArgument_UsesFallback()
        {
            var config = LaunchConfig.Load(new[] { "-luaRoot" }, "C:/default");

            Assert.AreEqual("C:/default", config.LuaRoot);
            Assert.AreEqual(string.Empty, config.MetaServerUrl);
        }
    }
}
