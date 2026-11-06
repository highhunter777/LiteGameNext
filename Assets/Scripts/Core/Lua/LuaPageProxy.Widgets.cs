using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LiteGame.Core.Lua
{
    /// <summary>LuaPageProxy 的控件部分：Toggle / Slider / Dropdown / Tab / List。
    /// Tab 约定：容器节点的 Toggle 子节点按顺序即索引；List 约定：content 首个子节点是行模板。</summary>
    public sealed partial class LuaPageProxy
    {
        private readonly Dictionary<string, Action<int>> _listHandlers =
            new Dictionary<string, Action<int>>(StringComparer.Ordinal);

        public void SetToggle(string id, bool value)
        {
            var toggle = Find<Toggle>(id);
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(value);
            }
        }

        public void OnToggleChanged(string id, Action<bool> callback)
        {
            var toggle = Find<Toggle>(id);
            if (toggle != null && callback != null)
            {
                toggle.onValueChanged.AddListener(value => callback(value));
            }
        }

        public void SetSlider(string id, float value)
        {
            var slider = Find<Slider>(id);
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        public void OnSliderChanged(string id, Action<float> callback)
        {
            var slider = Find<Slider>(id);
            if (slider != null && callback != null)
            {
                slider.onValueChanged.AddListener(value => callback(value));
            }
        }

        public void SetDropdownOptions(string id, string[] options)
        {
            var dropdown = Find<Dropdown>(id);
            if (dropdown == null)
            {
                return;
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options ?? Array.Empty<string>()));
        }

        public void SetDropdown(string id, int index)
        {
            var dropdown = Find<Dropdown>(id);
            if (dropdown != null && index >= 0 && index < dropdown.options.Count)
            {
                dropdown.SetValueWithoutNotify(index);
            }
        }

        public int GetDropdown(string id)
        {
            var dropdown = Find<Dropdown>(id);
            return dropdown == null ? 0 : dropdown.value;
        }

        public void OnDropdownChanged(string id, Action<int> callback)
        {
            var dropdown = Find<Dropdown>(id);
            if (dropdown != null && callback != null)
            {
                dropdown.onValueChanged.AddListener(index => callback(index));
            }
        }

        public void SetTab(string id, int index)
        {
            var toggles = TabToggles(id);
            if (toggles == null)
            {
                return;
            }

            if (index < 0 || index >= toggles.Count)
            {
                Warn(id, "Tab 索引越界");
                return;
            }

            for (var i = 0; i < toggles.Count; i++)
            {
                toggles[i].SetIsOnWithoutNotify(i == index);
            }
        }

        public void OnTabChanged(string id, Action<int> callback)
        {
            var toggles = TabToggles(id);
            if (toggles == null || callback == null)
            {
                return;
            }

            for (var i = 0; i < toggles.Count; i++)
            {
                var index = i;
                toggles[i].onValueChanged.AddListener(on =>
                {
                    if (on)
                    {
                        callback(index);
                    }
                });
            }
        }

        /// <summary>rows 为字符串行；行内首个 Text 显示文本，首个 Button 触发 OnListClick。</summary>
        public void SetList(string id, string[] rows)
        {
            var scroll = Find<ScrollRect>(id);
            var content = scroll == null ? null : scroll.content;
            if (content == null || content.childCount == 0)
            {
                Warn(id, "List 缺模板（content 首个子节点）");
                return;
            }

            var template = content.GetChild(0).gameObject;
            for (var i = content.childCount - 1; i >= 1; i--)
            {
                UnityEngine.Object.Destroy(content.GetChild(i).gameObject);
            }

            var count = rows == null ? 0 : rows.Length;
            _listHandlers.TryGetValue(id, out var handler);
            for (var i = 0; i < count; i++)
            {
                var row = UnityEngine.Object.Instantiate(template, content);
                row.SetActive(true);
                var text = row.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = rows[i] ?? string.Empty;
                }

                var button = row.GetComponentInChildren<Button>();
                if (button != null && handler != null)
                {
                    var index = i;
                    button.onClick.AddListener(() => handler(index));
                }
            }

            template.SetActive(false);
        }

        public void OnListClick(string id, Action<int> callback)
        {
            if (callback == null)
            {
                return;
            }

            _listHandlers[id] = callback;
        }

        /// <summary>弹窗层随 P1 的 PopupQueue 落地；P0 只保留方法面并告警一次。</summary>
        public void ShowToast(string text)
        {
            Warn("toast", "Toast 未接（P1）");
        }

        public void ShowConfirm(string text, Action onOk)
        {
            Warn("confirm", "Confirm 未接（P1）");
        }

        private List<Toggle> TabToggles(string id)
        {
            var component = Resolve(id);
            if (component == null)
            {
                return null;
            }

            var toggles = new List<Toggle>();
            foreach (Transform child in component.transform)
            {
                var toggle = child.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggles.Add(toggle);
                }
            }

            if (toggles.Count == 0)
            {
                Warn(id, "Tab 缺 Toggle 子节点");
                return null;
            }

            return toggles;
        }
    }
}
