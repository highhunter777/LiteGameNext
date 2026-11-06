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

        public static void Debug(string message, string tag = null)
        {
            Write(LogLevel.Debug, message, tag);
        }

        public static void Info(string message, string tag = null)
        {
            Write(LogLevel.Info, message, tag);
        }

        public static void Warn(string message, string tag = null)
        {
            Write(LogLevel.Warn, message, tag);
        }

        public static void Error(string message, string tag = null)
        {
            Write(LogLevel.Error, message, tag);
        }

        public static void Error(Exception exception, string tag = null)
        {
            Write(LogLevel.Error, exception == null ? "null exception" : exception.ToString(), tag);
        }

        private static void Write(LogLevel level, string message, string tag)
        {
            if (level < _level)
            {
                return;
            }

            var text = message ?? "(null)";
            if (!string.IsNullOrEmpty(tag))
            {
                text = "[" + tag + "] " + text;
            }

            switch (level)
            {
                case LogLevel.Warn:
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
