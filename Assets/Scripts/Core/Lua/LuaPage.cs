using System;
using LiteGame.Core.Logging;
using XLua;

namespace LiteGame.Core.Lua
{
    /// <summary>Lua 页实例：require + new + 生命周期转发与错误隔离；OnUpdate 在此按 10 Hz 收口。
    /// 首次失败即标记 broken：后续入口只记一次日志、不抛、不刷屏（03 §4）。</summary>
    public sealed class LuaPage
    {
        private const float UpdateStep = 0.1f;

        private readonly string _script;
        private readonly LuaPageProxy _proxy;
        private LuaTable _table;
        private bool _broken;
        private float _accumulator;

        private LuaPage(string script, LuaPageProxy proxy, LuaTable table)
        {
            _script = script;
            _proxy = proxy;
            _table = table;
        }

        /// <summary>失败返回 null（已记日志）：页面降级但不拖垮流程。</summary>
        public static LuaPage Create(LuaHost host, string script, LuaPageProxy proxy)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (string.IsNullOrEmpty(script))
            {
                throw new ArgumentException("script 不能为空", nameof(script));
            }

            try
            {
                var module = host.Env.DoString("return require('" + script + "')", script)[0] as LuaTable;
                if (module == null)
                {
                    Log.Error("Lua 页 require 返回空：" + script, LogTag.Lua);
                    return null;
                }

                var factory = module.Get<LuaFunction>("new");
                var created = factory?.Call(module, proxy);
                var table = created != null && created.Length > 0 ? created[0] as LuaTable : null;
                if (table == null)
                {
                    Log.Error("Lua 页构造失败：" + script, LogTag.Lua);
                    return null;
                }

                return new LuaPage(script, proxy, table);
            }
            catch (Exception exception)
            {
                Log.Error(exception, LogTag.Lua, script);
                return null;
            }
        }

        public void Show()
        {
            Call("OnShow");
        }

        public void Reveal()
        {
            Call("OnReveal");
        }

        public void Hide()
        {
            Call("OnHide");
            _table?.Dispose();
            _table = null;
        }

        /// <summary>代理的特效每帧推进；页面 OnUpdate 按 10 Hz 派发。</summary>
        public void Update(float deltaSeconds)
        {
            if (_broken || _table == null)
            {
                return;
            }

            _proxy.Tick(deltaSeconds);
            _accumulator += deltaSeconds;
            if (_accumulator < UpdateStep)
            {
                return;
            }

            var step = _accumulator;
            _accumulator = 0f;
            Call("OnUpdate", step);
        }

        private void Call(string method, params object[] args)
        {
            if (_broken || _table == null)
            {
                return;
            }

            try
            {
                var function = _table.Get<LuaFunction>(method);
                if (function == null)
                {
                    return;
                }

                var callArgs = new object[args.Length + 1];
                callArgs[0] = _table;
                Array.Copy(args, 0, callArgs, 1, args.Length);
                function.Call(callArgs);
            }
            catch (Exception exception)
            {
                _broken = true;
                Log.Error(exception, LogTag.Lua, _script + "." + method);
            }
        }
    }
}
