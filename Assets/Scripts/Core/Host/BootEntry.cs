using System;
using UnityEngine;

namespace LiteGame.Core.Host
{
    /// <summary>AOT → 业务程序集 的唯一缝：业务侧注册工厂，宿主侧创建。</summary>
    public static class BootEntry
    {
        private static Func<GameEntry, LaunchConfig, IGame> _factory;

        /// <summary>业务侧注册一次；重复注册抛异常（防两套组合根）。</summary>
        public static void Register(Func<GameEntry, LaunchConfig, IGame> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (_factory != null)
            {
                throw new InvalidOperationException("业务程序集已注册");
            }

            _factory = factory;
        }

        /// <summary>未注册或工厂返回 null 均 fail-fast，不进入半初始化状态。</summary>
        public static IGame Create(GameEntry entry, LaunchConfig config)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (_factory == null)
            {
                throw new InvalidOperationException("业务程序集未注册：GameBootstrap 未执行");
            }

            return _factory(entry, config) ?? throw new InvalidOperationException("工厂返回 null");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForEditorReload()
        {
            _factory = null;
        }

        internal static void ResetForTests()
        {
            _factory = null;
        }
    }
}
