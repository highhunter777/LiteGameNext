using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;
using Luban;

namespace LiteGame.Core.Config
{
    /// <summary>配置装载：注入字节读取 → 建 cfg.Tables → 算玩法摘要；失败抛，由 AppFlow 转 Error。</summary>
    public sealed class ConfigHost
    {
        private readonly Func<string, byte[]> _read;
        private cfg.Tables _tables;
        private uint _digest;

        public ConfigHost(Func<string, byte[]> read)
        {
            _read = read ?? throw new ArgumentNullException(nameof(read));
        }

        public bool IsLoaded => _tables != null;

        /// <summary>未加载时访问抛 InvalidOperationException（与 LuaHost.Env 同判据）。</summary>
        public cfg.Tables Tables => _tables ?? throw new InvalidOperationException("ConfigHost 未加载");

        public uint Digest => _tables != null ? _digest : throw new InvalidOperationException("ConfigHost 未加载");

        /// <summary>逐表读取建表；幂等。读取/解析失败带 location 抛出。</summary>
        public UniTask LoadAsync(CancellationToken token)
        {
            if (_tables != null)
            {
                return UniTask.CompletedTask;
            }

            token.ThrowIfCancellationRequested();
            var bytes = new Dictionary<string, byte[]>(ConfigTableLayout.Files.Length, StringComparer.Ordinal);
            foreach (var file in ConfigTableLayout.Files)
            {
                token.ThrowIfCancellationRequested();
                var location = ConfigTableLayout.TablePath(file);
                byte[] data;
                try
                {
                    data = _read(location);
                }
                catch (Exception exception) when (!(exception is OperationCanceledException))
                {
                    throw new InvalidOperationException("配置表读取失败：" + location, exception);
                }

                if (data == null || data.Length == 0)
                {
                    throw new InvalidOperationException("配置表为空：" + location);
                }

                bytes[ConfigTableLayout.TableName(file)] = data;
            }

            var tables = new cfg.Tables(name => bytes.TryGetValue(name, out var data)
                ? new ByteBuf(data)
                : throw new InvalidOperationException("Luban 请求了未声明的表：" + name));
            var digest = ConfigDigest.Compute(bytes);

            _tables = tables;
            _digest = digest;
            Log.Info("配置装载完成：" + bytes.Count + " 张表，digest=" + digest.ToString("x8"), LogTag.Config);
            return UniTask.CompletedTask;
        }
    }
}
