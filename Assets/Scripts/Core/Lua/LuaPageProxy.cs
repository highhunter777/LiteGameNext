using System;
using System.Collections.Generic;
using DG.Tweening;
using LiteGame.Core.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace LiteGame.Core.Lua
{
    /// <summary>注入 Lua 页的 self.ui：按 key 找控件，缺件/类型不符只告警一次并按 no-op 降级。
    /// 控件解析由宿主注入委托；动效走 DOTween（独立更新，不受 timeScale 影响）。</summary>
    public sealed partial class LuaPageProxy
    {
        private readonly Func<string, Component> _resolve;
        private readonly Func<string, string> _textOf;
        private readonly string _page;
        private readonly HashSet<string> _warned = new HashSet<string>(StringComparer.Ordinal);

        public LuaPageProxy(Func<string, Component> resolve, string page, Func<string, string> textOf = null)
        {
            _resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));
            _page = page ?? string.Empty;
            _textOf = textOf ?? (key => key);
        }

        public void SetText(string id, string value)
        {
            var text = Find<Text>(id);
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        public void SetTextKey(string id, string key)
        {
            SetText(id, _textOf(key));
        }

        /// <summary>最多两个参数（旧页面实际用量，03 §3.5）。</summary>
        public void SetTextKeyArgs(string id, string key, string arg0, string arg1)
        {
            SetText(id, string.Format(_textOf(key), arg0, arg1));
        }

        public void SetVisible(string id, bool visible)
        {
            var component = Resolve(id);
            if (component == null)
            {
                return;
            }

            component.gameObject.SetActive(visible);
        }

        public void SetInteractable(string id, bool value)
        {
            var selectable = Find<Selectable>(id);
            if (selectable != null)
            {
                selectable.interactable = value;
            }
        }

        public void OnButton(string id, Action callback)
        {
            var button = Find<Button>(id);
            if (button != null && callback != null)
            {
                button.onClick.AddListener(() => callback());
            }
        }

        /// <summary>闪一下：要求节点上有 CanvasGroup（没有则告警一次）；重复调用先杀掉旧动画。</summary>
        public void Flash(string id)
        {
            var component = Resolve(id);
            var group = component == null ? null : component as CanvasGroup ?? component.GetComponent<CanvasGroup>();
            if (group == null)
            {
                Warn(id, "Flash 缺 CanvasGroup");
                return;
            }

            DOTween.Kill(group);
            group.alpha = 1f;
            // CanvasGroup.DOFade 在 DOTween.Modules（需额外 asmdef 引用），这里用核心 API 等价实现。
            DOTween.To(() => group.alpha, value => group.alpha = value, 0.35f, 0.09f)
                .SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        public void Pulse(string id)
        {
            var component = Resolve(id);
            if (component == null)
            {
                return;
            }

            var target = component.transform;
            DOTween.Kill(target);
            var scale = target.localScale;
            target.DOScale(scale * 1.12f, 0.1f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        private Component Resolve(string id)
        {
            var component = string.IsNullOrEmpty(id) ? null : _resolve(id);
            if (component == null)
            {
                Warn(id, "缺件");
            }

            return component;
        }

        private T Find<T>(string id) where T : Component
        {
            var component = _resolve(string.IsNullOrEmpty(id) ? string.Empty : id);
            if (component is T typed)
            {
                return typed;
            }

            Warn(id, component == null ? "缺件" : "类型不符");
            return null;
        }

        /// <summary>同一页面同一 key 只刷一次，避免每帧告警。</summary>
        private void Warn(string key, string what)
        {
            if (_warned.Add((key ?? string.Empty) + "|" + what))
            {
                Log.Warning(what + "：" + _page + "/" + key, LogTag.Lua);
            }
        }
    }
}
