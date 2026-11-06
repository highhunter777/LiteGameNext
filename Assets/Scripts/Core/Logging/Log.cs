using System;

namespace LiteGame.Core.Logging
{
    /// <summary>全仓唯一日志门面；启动时 Configure 一次，之后只读。</summary>
    public static class Log
    {
        private static LogLevel _level = LogLevel.Info;

        /// <summary>设置级别；发布包由 LaunchConfig 传入。</summary>
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

            var text = tag == null ? message : "[" + tag + "] " + message;
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
