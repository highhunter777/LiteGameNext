using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LiteGame.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LiteGame.Tests.EditMode
{
    /// <summary>ScreenManager 的栈契约：压/替/退、栈深上限、缺页与重页 fail-fast。</summary>
    public sealed class ScreenManagerTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("UiTestRoot");
            FakeScreen.Hook = null;
            FakeScreen.Shows.Clear();
            FakeScreen.Reveals.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void Show_PushesAndShowsTop()
        {
            var prefab = MakeScreen("A");
            var manager = MakeManager(ScreenId.Loading, prefab);

            manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult();

            Assert.AreEqual(1, manager.Depth);
            Assert.IsTrue(manager.Current.gameObject.activeSelf);
        }

        [Test]
        public void Show_HidesPrevious()
        {
            var a = MakeScreen("A");
            var b = MakeScreen("B");
            var manager = MakeManager(
                new ScreenCatalog.Entry(ScreenId.Loading, a),
                new ScreenCatalog.Entry(ScreenId.Error, b));

            manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult();
            var first = manager.Current;
            manager.ShowAsync(ScreenId.Error).GetAwaiter().GetResult();

            Assert.AreEqual(2, manager.Depth);
            Assert.IsFalse(first.gameObject.activeSelf);
            Assert.AreNotSame(first, manager.Current);
        }

        [Test]
        public void Back_PopsAndShowsPrevious()
        {
            var a = MakeScreen("A");
            var b = MakeScreen("B");
            var manager = MakeManager(
                new ScreenCatalog.Entry(ScreenId.Loading, a),
                new ScreenCatalog.Entry(ScreenId.Error, b));

            manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult();
            var first = manager.Current;
            manager.ShowAsync(ScreenId.Error).GetAwaiter().GetResult();
            var second = manager.Current;

            manager.BackAsync().GetAwaiter().GetResult();

            Assert.AreEqual(1, manager.Depth);
            Assert.AreSame(first, manager.Current);
            Assert.IsTrue(first.gameObject.activeSelf);
            Assert.IsTrue(second == null);
        }

        [Test]
        public void Replace_SwapsTop()
        {
            var a = MakeScreen("A");
            var b = MakeScreen("B");
            var manager = MakeManager(
                new ScreenCatalog.Entry(ScreenId.Loading, a),
                new ScreenCatalog.Entry(ScreenId.Error, b));

            manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult();
            manager.ReplaceAsync(ScreenId.Error).GetAwaiter().GetResult();

            Assert.AreEqual(1, manager.Depth);
            Assert.AreEqual("B", manager.Current.name);
        }

        [Test]
        public void Show_BeyondMaxDepth_Throws()
        {
            var manager = MakeManager(
                new ScreenCatalog.Entry(ScreenId.Loading, MakeScreen("A")),
                new ScreenCatalog.Entry(ScreenId.Error, MakeScreen("B")));
            for (var i = 0; i < ScreenManager.MaxDepth; i++)
            {
                manager.ShowAsync(i % 2 == 0 ? ScreenId.Loading : ScreenId.Error).GetAwaiter().GetResult();
            }

            Assert.AreEqual(ScreenManager.MaxDepth, manager.Depth);
            Assert.Throws<InvalidOperationException>(() => manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult());
        }

        [Test]
        public void Show_SameIdOnTop_DoesNotPush()
        {
            var manager = MakeManager(ScreenId.Loading, MakeScreen("A"));

            manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult();
            manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult();

            Assert.AreEqual(1, manager.Depth);
        }

        [Test]
        public void IsOpen_ReflectsStack()
        {
            var manager = MakeManager(
                new ScreenCatalog.Entry(ScreenId.Loading, MakeScreen("A")),
                new ScreenCatalog.Entry(ScreenId.Error, MakeScreen("B")));

            manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult();

            Assert.IsTrue(manager.IsOpen(ScreenId.Loading));
            Assert.IsFalse(manager.IsOpen(ScreenId.Error));
            Assert.IsFalse(manager.IsBusy);
        }

        [Test]
        public void Show_InsideShow_IsDropped()
        {
            var loading = MakeScreen("A");
            var manager = MakeManager(
                new ScreenCatalog.Entry(ScreenId.Loading, loading),
                new ScreenCatalog.Entry(ScreenId.Error, MakeScreen("B")));
            FakeScreen.Hook = () => manager.ShowAsync(ScreenId.Error).GetAwaiter().GetResult();
            LogAssert.Expect(LogType.Warning, new Regex("重入"));

            manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult();

            Assert.AreEqual(1, manager.Depth);
            Assert.AreEqual(ScreenId.Loading, manager.CurrentId);
        }

        [Test]
        public void Back_RevealsPreviousInsteadOfRerunningShow()
        {
            var manager = MakeManager(
                new ScreenCatalog.Entry(ScreenId.Loading, MakeScreen("A")),
                new ScreenCatalog.Entry(ScreenId.Error, MakeScreen("B")));

            manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult();
            manager.ShowAsync(ScreenId.Error).GetAwaiter().GetResult();
            manager.BackAsync().GetAwaiter().GetResult();

            Assert.AreEqual(1, FakeScreen.Shows["A"]);
            Assert.AreEqual(1, FakeScreen.Reveals["A"]);
            Assert.AreEqual(ScreenId.Loading, manager.CurrentId);
        }

        [Test]
        public void Catalog_PageLookup_RoundTrips()
        {
            var catalog = new ScreenCatalog(new[]
            {
                new ScreenCatalog.Entry(ScreenId.Lobby, MakeScreen("A"), "ui.Page_Lobby"),
            });

            Assert.AreEqual(ScreenId.Lobby, catalog.IdOfPage("ui.Page_Lobby"));
            Assert.AreEqual("ui.Page_Lobby", catalog.PageOf(ScreenId.Lobby));
            Assert.IsFalse(catalog.TryIdOfPage("ui.Missing", out _));
        }

        [Test]
        public void Catalog_DuplicatePage_Throws()
        {
            var prefab = MakeScreen("A");

            Assert.Throws<ArgumentException>(() => new ScreenCatalog(new[]
            {
                new ScreenCatalog.Entry(ScreenId.Loading, prefab, "ui.Page_Lobby"),
                new ScreenCatalog.Entry(ScreenId.Error, prefab, "ui.Page_Lobby"),
            }));
        }

        [Test]
        public void Back_OnEmptyStack_NoOp()
        {
            var manager = MakeManager(ScreenId.Loading, MakeScreen("A"));

            manager.BackAsync().GetAwaiter().GetResult();

            Assert.AreEqual(0, manager.Depth);
            Assert.IsFalse(manager.IsBusy);
        }

        [Test]
        public void CloseAll_ClearsStack()
        {
            var prefab = MakeScreen("A");
            var manager = MakeManager(ScreenId.Loading, prefab);
            manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult();
            manager.ShowAsync(ScreenId.Loading).GetAwaiter().GetResult();

            manager.CloseAllAsync().GetAwaiter().GetResult();

            Assert.AreEqual(0, manager.Depth);
            Assert.IsNull(manager.Current);
        }

        [Test]
        public void Show_UnregisteredScreen_Throws()
        {
            var manager = MakeManager(ScreenId.Loading, MakeScreen("A"));

            Assert.Throws<ArgumentOutOfRangeException>(() => manager.ShowAsync(ScreenId.Error).GetAwaiter().GetResult());
        }

        [Test]
        public void Catalog_DuplicateId_Throws()
        {
            var prefab = MakeScreen("A");

            Assert.Throws<ArgumentException>(() => new ScreenCatalog(new[]
            {
                new ScreenCatalog.Entry(ScreenId.Loading, prefab),
                new ScreenCatalog.Entry(ScreenId.Loading, prefab),
            }));
        }

        private ScreenManager MakeManager(params ScreenCatalog.Entry[] entries)
        {
            return new ScreenManager(new ScreenCatalog(entries), _root.transform);
        }

        private ScreenManager MakeManager(ScreenId id, GameObject prefab)
        {
            return MakeManager(new ScreenCatalog.Entry(id, prefab));
        }

        private GameObject MakeScreen(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<FakeScreen>();
            go.transform.SetParent(_root.transform, false);
            go.SetActive(false);
            return go;
        }

        private sealed class FakeScreen : ScreenBase
        {
            // 静态钩子/计数：Instantiate 只复制序列化字段，实例上挂的委托传不到克隆体。
            public static readonly Dictionary<string, int> Shows = new Dictionary<string, int>();

            public static readonly Dictionary<string, int> Reveals = new Dictionary<string, int>();

            public static Action Hook { get; set; }

            protected override void OnShow()
            {
                Bump(Shows);
                Hook?.Invoke();
            }

            protected override void OnReveal()
            {
                Bump(Reveals);
            }

            private void Bump(Dictionary<string, int> map)
            {
                map.TryGetValue(name, out var count);
                map[name] = count + 1;
            }
        }
    }
}
