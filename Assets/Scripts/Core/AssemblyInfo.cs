using System.Runtime.CompilerServices;

// Core 暴露内部重置点给 EditMode 测试（静态工厂缝的用例需要复位）
[assembly: InternalsVisibleTo("LiteGame.Tests.EditMode")]
