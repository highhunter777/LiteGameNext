using System;
using System.Collections.Generic;

namespace LiteGame.Core.Commands
{
    /// <summary>字符串指令表：名字 → 处理委托；无撤销、审计、排队与业务类型依赖。
    /// 只做 GM/开发指令的装配与派发；业务动作是注入依赖上的用例方法，不是命令对象。</summary>
    public sealed class CommandTable
    {
        private static readonly char[] _separators = { ' ', '\t', '\r', '\n' };

        private readonly Dictionary<string, Action<string[]>> _commands =
            new Dictionary<string, Action<string[]>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>名首尾空白裁掉、不区分大小写；空名/含空白/重名 fail-fast。</summary>
        public void Register(string name, Action<string[]> handler)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var key = name.Trim();
            if (key.Length == 0)
            {
                throw new ArgumentException("指令名不能为空", nameof(name));
            }

            if (key.IndexOfAny(_separators) >= 0)
            {
                throw new ArgumentException("指令名不能含空白：" + key, nameof(name));
            }

            if (_commands.ContainsKey(key))
            {
                throw new InvalidOperationException("指令已注册：" + key);
            }

            _commands.Add(key, handler);
        }

        /// <summary>执行一行（首词为指令名，其余按空白切分）；未知指令与处理失败返回 false 与原因。</summary>
        public bool TryExecute(string line, out string error)
        {
            error = null;
            var text = line?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                error = "空指令";
                return false;
            }

            var tokens = text.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
            if (!_commands.TryGetValue(tokens[0], out var handler))
            {
                error = "未知指令：" + tokens[0];
                return false;
            }

            try
            {
                handler(Args(tokens));
                return true;
            }
            catch (Exception exception)
            {
                error = exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        public IReadOnlyCollection<string> Names => _commands.Keys;

        private static string[] Args(string[] tokens)
        {
            if (tokens.Length <= 1)
            {
                return Array.Empty<string>();
            }

            var args = new string[tokens.Length - 1];
            Array.Copy(tokens, 1, args, 0, args.Length);
            return args;
        }
    }
}
