using System;
using LiteGame.Core.Logging;

namespace LiteGame.Core.Lua
{
    /// <summary>Lua 回调保护：null 视为"页面已关"返回 false；异常转日志不抛出。</summary>
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
