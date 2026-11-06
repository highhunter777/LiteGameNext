using System;

namespace LiteGame.Core.Logging
{
    /// <summary>全仓唯一日志门面；启动期 Configure 一次。任何情况下不抛异常。</summary>
    public static class Log
    {
        private static LogLevel _level = LogLevel.Info;

        /// <summary>设置级别；仅启动期调用，运行期不再变更。</summary>
        public static void Configure(LogLevel level)
        {
            _level = level;
        }

        public static void Debug(string message, LogTag tag)
        {
            Write(LogLevel.Debug, message, tag);
        }

        public static void Info(string message, LogTag tag)
        {
            Write(LogLevel.Info, message, tag);
        }

        public static void Warning(string message, LogTag tag)
        {
            Write(LogLevel.Warning, message, tag);
        }

        public static void Error(string message, LogTag tag)
        {
            Write(LogLevel.Error, message, tag);
        }

        /// <summary>异常按错误级记录；detail 打印在异常栈之前（页面名/信号类型）。</summary>
        public static void Error(Exception exception, LogTag tag, string detail = null)
        {
            var stack = exception == null ? "null exception" : exception.ToString();
            Write(LogLevel.Error, string.IsNullOrEmpty(detail) ? stack : detail + "\n" + stack, tag);
        }

        private static void Write(LogLevel level, string message, LogTag tag)
        {
            if (level < _level)
            {
                return;
            }

            // None 是"未分类"占位值；输出成 Unclassified 便于在控制台一眼看出漏传标签。
            var name = tag == LogTag.None ? "Unclassified" : tag.ToString();
            var text = "[" + name + "] " + (message ?? "(null)");
            switch (level)
            {
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(text);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(text);
                    break;
                default:
                    UnityEngine.Debug.Log(text);
                    break;
            }
        }
    }
}
