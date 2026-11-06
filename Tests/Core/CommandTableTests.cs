using System;
using LiteGame.Core.Commands;
using Xunit;

namespace LiteGame.Tests.Core
{
    /// <summary>CommandTable 的注册、解析、大小写与失败契约（xUnit，不依赖 Unity）。</summary>
    public sealed class CommandTableTests
    {
        [Fact]
        public void Register_ThenExecute_PassesArguments()
        {
            var table = new CommandTable();
            string[] received = null;
            table.Register("log", args => received = args);

            var ok = table.TryExecute("log a b", out var error);

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal(new[] { "a", "b" }, received);
        }

        [Fact]
        public void Execute_MatchesNameIgnoreCase()
        {
            var table = new CommandTable();
            var called = false;
            table.Register("spawn", _ => called = true);

            Assert.True(table.TryExecute("SPAWN", out _));
            Assert.True(called);
        }

        [Fact]
        public void Execute_TrimsLineAndCollapsesWhitespace()
        {
            var table = new CommandTable();
            string[] received = null;
            table.Register("log", args => received = args);

            var ok = table.TryExecute("  log\t a \r\n b  ", out _);

            Assert.True(ok);
            Assert.Equal(new[] { "a", "b" }, received);
        }

        [Fact]
        public void Register_TrimsName()
        {
            var table = new CommandTable();
            var called = false;
            table.Register("  log  ", _ => called = true);

            Assert.True(table.TryExecute("log", out _));
            Assert.True(called);
        }

        [Fact]
        public void Register_NameWithWhitespace_Throws()
        {
            var table = new CommandTable();

            Assert.Throws<ArgumentException>(() => table.Register("a b", _ => { }));
        }

        [Fact]
        public void Register_NullOrEmptyName_Throws()
        {
            var table = new CommandTable();

            Assert.Throws<ArgumentNullException>(() => table.Register(null, _ => { }));
            Assert.Throws<ArgumentException>(() => table.Register(string.Empty, _ => { }));
            Assert.Throws<ArgumentException>(() => table.Register("   ", _ => { }));
        }

        [Fact]
        public void Register_NullHandler_Throws()
        {
            var table = new CommandTable();

            Assert.Throws<ArgumentNullException>(() => table.Register("x", null));
        }

        [Fact]
        public void Register_Duplicate_Throws()
        {
            var table = new CommandTable();
            table.Register("x", _ => { });

            Assert.Throws<InvalidOperationException>(() => table.Register(" x ", _ => { }));
        }

        [Fact]
        public void TryExecute_UnknownCommand_ReturnsFalse()
        {
            var table = new CommandTable();

            var ok = table.TryExecute("nope", out var error);

            Assert.False(ok);
            Assert.Contains("nope", error);
        }

        [Fact]
        public void TryExecute_EmptyLine_ReturnsFalse()
        {
            var table = new CommandTable();

            Assert.False(table.TryExecute("  ", out var error));
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void TryExecute_HandlerThrows_ReturnsFalseWithDiagnostics()
        {
            var table = new CommandTable();
            table.Register("boom", _ => throw new InvalidOperationException("炸了"));

            var ok = table.TryExecute("boom", out var error);

            Assert.False(ok);
            Assert.Contains("InvalidOperationException", error);
            Assert.Contains("炸了", error);
        }

        [Fact]
        public void Names_ListsRegisteredCommands()
        {
            var table = new CommandTable();
            table.Register("a", _ => { });
            table.Register("b", _ => { });

            Assert.Equal(2, table.Names.Count);
            Assert.Contains("a", table.Names);
            Assert.Contains("b", table.Names);
        }
    }
}
