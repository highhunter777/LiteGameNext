using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteGame.Ui
{
    /// <summary>页面绑定器：key = 子节点名 → 组件；Inspector 数组可由 UiBindingGenerator 生成。</summary>
    public sealed class UiBinder : MonoBehaviour
    {
        /// <summary>一行绑定；空 key / 空目标 / 重 key 在建表时 fail-fast。</summary>
        [Serializable]
        public struct Entry
        {
            [SerializeField] private string _key;
            [SerializeField] private Component _target;

            public Entry(string key, Component target)
            {
                _key = key;
                _target = target;
            }

            public string Key => _key;

            public Component Target => _target;
        }

        [SerializeField] private Entry[] _entries;

        private Dictionary<string, Component> _map;

        private Dictionary<string, Component> Map
        {
            get
            {
                if (_map == null)
                {
                    _map = BuildMap();
                }

                return _map;
            }
        }

        /// <summary>按 key 取组件；未绑定或类型不符抛 ArgumentException。</summary>
        public T Get<T>(string key) where T : Component
        {
            if (!TryGet(key, out T value))
            {
                throw new ArgumentException("未绑定或类型不符：" + key, nameof(key));
            }

            return value;
        }

        public bool TryGet<T>(string key, out T value) where T : Component
        {
            value = null;
            if (string.IsNullOrEmpty(key) || !Map.TryGetValue(key, out var component))
            {
                return false;
            }

            value = component as T;
            return value != null;
        }

        private Dictionary<string, Component> BuildMap()
        {
            var map = new Dictionary<string, Component>(_entries == null ? 0 : _entries.Length, StringComparer.Ordinal);
            if (_entries == null)
            {
                return map;
            }

            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    throw new InvalidOperationException(nameof(UiBinder) + " 存在空 key");
                }

                if (entry.Target == null)
                {
                    throw new InvalidOperationException("绑定目标为空：" + entry.Key);
                }

                if (map.ContainsKey(entry.Key))
                {
                    throw new InvalidOperationException("重复绑定：" + entry.Key);
                }

                map.Add(entry.Key, entry.Target);
            }

            return map;
        }
    }
}
