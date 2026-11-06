using System;
using LiteGame.Core.Lua;
using UnityEngine;

namespace LiteGame.Ui
{
    /// <summary>UIRoot 宿主：实例化常驻根、装配页面目录与 ScreenManager；不直接开关页面。
    /// Awake 只建目录，页面栈在 Initialize 时装配（Lua 页需要 LuaHost）。</summary>
    public sealed class UiHost : MonoBehaviour, IDisposable
    {
        [SerializeField] private Transform _screenRoot;
        [SerializeField] private ScreenCatalog.Entry[] _screens;

        private ScreenCatalog _catalog;
        private bool _disposed;

        public ScreenManager Screens { get; private set; }

        private void Awake()
        {
            if (_screenRoot == null)
            {
                throw new InvalidOperationException("UIRoot 缺少 Canvas_Screen");
            }

            _catalog = new ScreenCatalog(_screens ?? Array.Empty<ScreenCatalog.Entry>());
        }

        /// <summary>装配页面栈；由组合根在创建 LuaHost 之后调一次。</summary>
        public void Initialize(LuaHost lua)
        {
            if (Screens != null)
            {
                throw new InvalidOperationException("UiHost 已初始化");
            }

            if (_catalog == null)
            {
                throw new InvalidOperationException("UiHost 未完成 Awake");
            }

            Screens = new ScreenManager(_catalog, _screenRoot, lua);
        }

        /// <summary>实例化并常驻 UIRoot 预制体；缺 UiHost 组件或未完成 Awake 即 fail-fast。</summary>
        public static UiHost Create(GameObject uiRootPrefab)
        {
            if (uiRootPrefab == null)
            {
                throw new ArgumentNullException(nameof(uiRootPrefab));
            }

            var instance = Instantiate(uiRootPrefab);
            instance.name = uiRootPrefab.name;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(instance);
            }

            if (!instance.TryGetComponent(out UiHost host))
            {
                Destroy(instance);
                throw new InvalidOperationException("UIRoot 预制体缺少 UiHost：" + uiRootPrefab.name);
            }

            if (host._catalog == null)
            {
                Destroy(instance);
                throw new InvalidOperationException("UIRoot 未完成初始化（根节点未激活或缺 Canvas_Screen）：" + uiRootPrefab.name);
            }

            return host;
        }

        public void Tick(float deltaSeconds)
        {
            if (!_disposed && Screens != null)
            {
                Screens.Tick(deltaSeconds);
            }
        }

        /// <summary>幂等；销毁常驻根。</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
