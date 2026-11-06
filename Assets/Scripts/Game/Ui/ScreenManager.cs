using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;
using LiteGame.Core.Lua;
using UnityEngine;

namespace LiteGame.Ui
{
    /// <summary>页面开关唯一写者：全屏单栈顶、栈深上限；页面自身不得调用本类。
    /// 同一时刻只允许一个页面操作，重入（如 OnShow 里再开页）直接丢弃并告警。</summary>
    public sealed class ScreenManager
    {
        public const int MaxDepth = 4;

        private readonly ScreenCatalog _catalog;
        private readonly Transform _parent;
        private readonly LuaHost _lua;
        private readonly List<Entry> _stack = new List<Entry>(MaxDepth);
        private bool _busy;

        public ScreenManager(ScreenCatalog catalog, Transform parent, LuaHost lua = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _lua = lua;
        }

        public ScreenBase Current => _stack.Count == 0 ? null : _stack[_stack.Count - 1].Screen;

        public ScreenId CurrentId => _stack.Count == 0 ? ScreenId.None : _stack[_stack.Count - 1].Id;

        public int Depth => _stack.Count;

        /// <summary>是否有页面操作在途；Lua 侧据此禁用按钮。</summary>
        public bool IsBusy => _busy;

        public bool IsOpen(ScreenId id)
        {
            for (var i = 0; i < _stack.Count; i++)
            {
                if (_stack[i].Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>压栈显示；同顶重复请求去重（连点），超过 MaxDepth 抛，操作中重入丢弃。</summary>
        public UniTask ShowAsync(ScreenId id)
        {
            if (!Begin("Show " + id))
            {
                return UniTask.CompletedTask;
            }

            try
            {
                if (CurrentId == id)
                {
                    Log.Debug("已在栈顶，去重：" + id, LogTag.Ui);
                    return UniTask.CompletedTask;
                }

                if (_stack.Count >= MaxDepth)
                {
                    throw new InvalidOperationException("页面栈已到上限 " + MaxDepth);
                }

                var screen = Create(id);
                Current?.Cover();
                _stack.Add(new Entry(id, screen));
                screen.Show();
            }
            finally
            {
                _busy = false;
            }

            return UniTask.CompletedTask;
        }

        /// <summary>替换栈顶；空栈等同 ShowAsync，同顶去重。</summary>
        public UniTask ReplaceAsync(ScreenId id)
        {
            if (!Begin("Replace " + id))
            {
                return UniTask.CompletedTask;
            }

            try
            {
                if (CurrentId == id)
                {
                    Log.Debug("已在栈顶，去重：" + id, LogTag.Ui);
                    return UniTask.CompletedTask;
                }

                var screen = Create(id);
                if (_stack.Count > 0)
                {
                    var top = _stack[_stack.Count - 1];
                    _stack.RemoveAt(_stack.Count - 1);
                    top.Screen.Hide();
                    Release(top.Screen);
                }

                _stack.Add(new Entry(id, screen));
                screen.Show();
            }
            finally
            {
                _busy = false;
            }

            return UniTask.CompletedTask;
        }

        /// <summary>退栈；空栈 no-op，返回后露出上一页。</summary>
        public UniTask BackAsync()
        {
            if (!Begin("Back"))
            {
                return UniTask.CompletedTask;
            }

            try
            {
                if (_stack.Count == 0)
                {
                    return UniTask.CompletedTask;
                }

                var top = _stack[_stack.Count - 1];
                _stack.RemoveAt(_stack.Count - 1);
                top.Screen.Hide();
                Release(top.Screen);
                Current?.Reveal();
            }
            finally
            {
                _busy = false;
            }

            return UniTask.CompletedTask;
        }

        /// <summary>清空全部页面；空栈 no-op。</summary>
        public UniTask CloseAllAsync()
        {
            if (!Begin("CloseAll"))
            {
                return UniTask.CompletedTask;
            }

            try
            {
                for (var i = _stack.Count - 1; i >= 0; i--)
                {
                    var entry = _stack[i];
                    entry.Screen.Hide();
                    Release(entry.Screen);
                }

                _stack.Clear();
            }
            finally
            {
                _busy = false;
            }

            return UniTask.CompletedTask;
        }

        /// <summary>Lua 页面名入口：未登记时由 Bridge 校验，这里按 id 处理。</summary>
        public UniTask ShowPageAsync(string page)
        {
            return ShowAsync(_catalog.IdOfPage(page));
        }

        public UniTask ReplacePageAsync(string page)
        {
            return ReplaceAsync(_catalog.IdOfPage(page));
        }

        public bool IsPageOpen(string page)
        {
            return _catalog.TryIdOfPage(page, out var id) && IsOpen(id);
        }

        public bool TryPageId(string page, out ScreenId id)
        {
            return _catalog.TryIdOfPage(page, out id);
        }

        public void Tick(float deltaSeconds)
        {
            Current?.Tick(deltaSeconds);
        }

        /// <summary>重入即丢弃：一次点击只应产生一次页面操作，重入基本都是内容错误。</summary>
        private bool Begin(string operation)
        {
            if (_busy)
            {
                Log.Warning("页面操作重入，已丢弃：" + operation, LogTag.Ui);
                return false;
            }

            _busy = true;
            return true;
        }

        private ScreenBase Create(ScreenId id)
        {
            var prefab = _catalog.PrefabOf(id);
            var instance = UnityEngine.Object.Instantiate(prefab, _parent, false);
            instance.name = prefab.name;

            if (!instance.TryGetComponent(out ScreenBase screen))
            {
                Release(instance);
                throw new InvalidOperationException("预制体根节点缺少 ScreenBase：" + prefab.name);
            }

            if (screen is LuaScreen luaScreen)
            {
                if (_lua == null)
                {
                    Release(instance);
                    throw new InvalidOperationException("Lua 页面需要 LuaHost：" + id);
                }

                luaScreen.Initialize(_lua);
                var page = _catalog.PageOf(id);
                if (!string.IsNullOrEmpty(page) && !string.Equals(page, luaScreen.Script, StringComparison.Ordinal))
                {
                    Release(instance);
                    throw new InvalidOperationException("目录页面名与预制体 script 不一致：" + id);
                }
            }

            return screen;
        }

        private static void Release(ScreenBase screen)
        {
            if (screen != null)
            {
                Release(screen.gameObject);
            }
        }

        private static void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(instance);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private readonly struct Entry
        {
            public Entry(ScreenId id, ScreenBase screen)
            {
                Id = id;
                Screen = screen;
            }

            public ScreenId Id { get; }

            public ScreenBase Screen { get; }
        }
    }
}