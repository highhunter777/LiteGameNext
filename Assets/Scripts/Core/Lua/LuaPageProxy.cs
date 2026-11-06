using System;
using System.Collections.Generic;
using LiteGame.Core.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace LiteGame.Core.Lua
{
    /// <summary>注入 Lua 页的 self.ui：按 key 找控件，缺件/类型不符只告警一次并按 no-op 降级。
    /// 控件解析由宿主注入委托，本类不依赖 UI 层；复合控件见部分类。</summary>
    public sealed partial class LuaPageProxy
    {
        private readonly Func<string, Component> _resolve;
        private readonly Func<string, string> _textOf;
        private readonly string _page;
        private readonly HashSet<string> _warned = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<Effect> _effects = new List<Effect>();

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

        /// <summary>闪一下：要求节点上有 CanvasGroup（没有则告警一次）。</summary>
        public void Flash(string id)
        {
            var component = Resolve(id);
            var group = component == null ? null : component as CanvasGroup ?? component.GetComponent<CanvasGroup>();
            if (group == null)
            {
                Warn(id, "Flash 缺 CanvasGroup");
                return;
            }

            _effects.RemoveAll(effect => effect.Group == group);
            _effects.Add(Effect.ForFlash(group, 0.18f));
        }

        public void Pulse(string id)
        {
            var component = Resolve(id);
            if (component == null)
            {
                return;
            }

            var target = component.transform;
            _effects.RemoveAll(effect => effect.Transform == target);
            _effects.Add(Effect.ForPulse(target, 0.2f));
        }

        /// <summary>由宿主每帧推进（Lua 页 OnUpdate 之前）。</summary>
        public void Tick(float deltaSeconds)
        {
            for (var i = _effects.Count - 1; i >= 0; i--)
            {
                var effect = _effects[i];
                effect.Elapsed += deltaSeconds;
                var progress = Mathf.Clamp01(effect.Elapsed / effect.Duration);
                var wave = Mathf.Sin(Mathf.PI * progress);
                if (effect.Group != null)
                {
                    effect.Group.alpha = 1f - 0.6f * wave;
                }

                if (effect.Transform != null)
                {
                    effect.Transform.localScale = Vector3.one * (effect.BaseScale * (1f + 0.12f * wave));
                }

                if (progress >= 1f)
                {
                    if (effect.Group != null)
                    {
                        effect.Group.alpha = 1f;
                    }

                    if (effect.Transform != null)
                    {
                        effect.Transform.localScale = Vector3.one * effect.BaseScale;
                    }

                    _effects.RemoveAt(i);
                    continue;
                }

                _effects[i] = effect;
            }
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

        private struct Effect
        {
            private CanvasGroup _group;
            private Transform _transform;
            private float _duration;
            private float _elapsed;
            private float _baseScale;

            public CanvasGroup Group => _group;

            public Transform Transform => _transform;

            public float Duration => _duration;

            public float Elapsed
            {
                get => _elapsed;
                set => _elapsed = value;
            }

            public float BaseScale => _baseScale;

            public static Effect ForFlash(CanvasGroup group, float duration)
            {
                return new Effect { _group = group, _duration = duration, _baseScale = 1f };
            }

            public static Effect ForPulse(Transform target, float duration)
            {
                return new Effect { _transform = target, _duration = duration, _baseScale = target.localScale.x };
            }
        }
    }
}
