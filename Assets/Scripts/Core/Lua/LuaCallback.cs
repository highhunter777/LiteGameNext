using System;
using LiteGame.Core.Logging;

namespace LiteGame.Core.Lua
{
    /// <summary>Lua 回调保护：异常转日志并返回 false，不把 Lua 错误抛进调用方。</summary>
    public static class LuaCallback
    {
        public static bool Invoke(Action callback, string context)
        {
            if (callback == null)
            {
                return false;
            }

            try
            {
                callback();
                return true;
            }
            catch (Exception exception)
            {
                Log.Error(exception, context);
                return false;
            }
        }
    }
}
