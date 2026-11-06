using System;
using LiteGame.Core.Lua;
using UnityEngine;

namespace LiteGame.Ui
{
    /// <summary>Lua 页宿主：把 ScreenBase 生命周期转给 Core 的 LuaPage；每开一次新建（池化留 P1）。</summary>
    public sealed class LuaScreen : ScreenBase
    {
        [SerializeField] private string _script;

        private LuaHost _lua;
        private LuaPage _page;

        /// <summary>页面名（与 ScreenCatalog 的第二字段同源，建页时校验一致）。</summary>
        public string Script => _script;

        /// <summary>由 ScreenManager 在建页时注入；缺 LuaHost 即 fail-fast。</summary>
        public void Initialize(LuaHost lua)
        {
            _lua = lua ?? throw new ArgumentNullException(nameof(lua));
        }

        protected override void OnShow()
        {
            if (_page == null)
            {
                _page = LuaPage.Create(_lua, _script, new LuaPageProxy(ResolveControl, _script));
                if (_page == null)
                {
                    return;
                }
            }

            _page.Show();
        }

        protected override void OnReveal()
        {
            _page?.Reveal();
        }

        protected override void OnHide()
        {
            _page?.Hide();
            _page = null;
        }

        protected override void OnUpdate(float deltaSeconds)
        {
            _page?.Update(deltaSeconds);
        }

        private Component ResolveControl(string key)
        {
            return Binder != null && Binder.TryGet<Component>(key, out var component) ? component : null;
        }
    }
}
