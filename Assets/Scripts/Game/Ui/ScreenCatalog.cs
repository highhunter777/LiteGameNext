using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteGame.Ui
{
    /// <summary>页面目录：id → 预制体，Lua 页额外记页面名（用于 Bridge.ui 反查）；未登记/重复/空件 fail-fast。</summary>
    public sealed class ScreenCatalog
    {
        /// <summary>Inspector 里的一行；私有字段 + 属性，避免 public 可变字段。</summary>
        [Serializable]
        public struct Entry
        {
            [SerializeField] private ScreenId _id;
            [SerializeField] private GameObject _prefab;
            [SerializeField] private string _page;

            public Entry(ScreenId id, GameObject prefab, string page = null)
            {
                _id = id;
                _prefab = prefab;
                _page = page;
            }

            public ScreenId Id => _id;

            public GameObject Prefab => _prefab;

            /// <summary>Lua 页面名（如 ui.Page_Lobby）；C# 页为 null/空。</summary>
            public string Page => _page;
        }

        private readonly Dictionary<ScreenId, GameObject> _prefabs;
        private readonly Dictionary<ScreenId, string> _pages;
        private readonly Dictionary<string, ScreenId> _ids;

        public ScreenCatalog(IReadOnlyCollection<Entry> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            _prefabs = new Dictionary<ScreenId, GameObject>(entries.Count);
            _pages = new Dictionary<ScreenId, string>(entries.Count);
            _ids = new Dictionary<string, ScreenId>(entries.Count, StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                Validate(entry);
                _prefabs.Add(entry.Id, entry.Prefab);
                if (!string.IsNullOrEmpty(entry.Page))
                {
                    _pages.Add(entry.Id, entry.Page);
                    _ids.Add(entry.Page, entry.Id);
                }
            }
        }

        public GameObject PrefabOf(ScreenId id)
        {
            if (!_prefabs.TryGetValue(id, out var prefab))
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "页面未登记");
            }

            return prefab;
        }

        /// <summary>C# 页返回 null；Lua 页返回页面名。</summary>
        public string PageOf(ScreenId id)
        {
            return _pages.TryGetValue(id, out var page) ? page : null;
        }

        /// <summary>页面名反查 id；未登记抛（Bridge.ui 的入口校验）。</summary>
        public ScreenId IdOfPage(string page)
        {
            if (!TryIdOfPage(page, out var id))
            {
                throw new ArgumentOutOfRangeException(nameof(page), page, "页面未登记");
            }

            return id;
        }

        public bool TryIdOfPage(string page, out ScreenId id)
        {
            id = ScreenId.None;
            return !string.IsNullOrEmpty(page) && _ids.TryGetValue(page, out id);
        }

        private void Validate(Entry entry)
        {
            if (entry.Id == ScreenId.None)
            {
                throw new ArgumentException("页面 id 不能是 None", nameof(entry));
            }

            if (entry.Prefab == null)
            {
                throw new ArgumentException("页面预制体为空：" + entry.Id, nameof(entry));
            }

            if (_prefabs.ContainsKey(entry.Id))
            {
                throw new ArgumentException("重复登记的页面：" + entry.Id, nameof(entry));
            }

            if (!string.IsNullOrEmpty(entry.Page) && _ids.ContainsKey(entry.Page))
            {
                throw new ArgumentException("重复登记的页面名：" + entry.Page, nameof(entry));
            }
        }
    }
}
