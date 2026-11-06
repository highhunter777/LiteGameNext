namespace LiteGame.Core.Logging
{
    /// <summary>日志级别；低于 Configure 设定值的直接丢弃。</summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }
}
