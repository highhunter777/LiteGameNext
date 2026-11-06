using System;
using System.Reflection;
using LiteGame.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LiteGame.Tests.EditMode
{
    /// <summary>UiBinder 与绑定钩子的契约：取件、失败面、空表、重复键、OnBind 只调一次。</summary>
    public sealed class UiBinderTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("BinderTestRoot");
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
        public void Get_ReturnsBoundComponent()
        {
            var label = AddChild<Text>("Label");
            var binder = MakeBinder(_root, new UiBinder.Entry("Label", label));

            Assert.AreSame(label, binder.Get<Text>("Label"));
        }

        [Test]
        public void Get_UnknownKey_Throws()
        {
            var binder = MakeBinder(_root, new UiBinder.Entry("Label", AddChild<Text>("Label")));

            Assert.Throws<ArgumentException>(() => binder.Get<Text>("Missing"));
        }

        [Test]
        public void Get_WrongType_Throws()
        {
            var binder = MakeBinder(_root, new UiBinder.Entry("Label", AddChild<Text>("Label")));

            Assert.Throws<ArgumentException>(() => binder.Get<Image>("Label"));
        }

        [Test]
        public void Build_EmptyKey_Throws()
        {
            var binder = MakeBinder(_root, new UiBinder.Entry(string.Empty, AddChild<Text>("Label")));

            Assert.Throws<InvalidOperationException>(() => binder.Get<Text>("Label"));
        }

        [Test]
        public void Build_DuplicateKey_Throws()
        {
            var label = AddChild<Text>("Label");
            var binder = MakeBinder(_root,
                new UiBinder.Entry("Label", label),
                new UiBinder.Entry("Label", label));

            Assert.Throws<InvalidOperationException>(() => binder.Get<Text>("Label"));
        }

        [Test]
        public void Build_NullTarget_Throws()
        {
            var binder = MakeBinder(_root, new UiBinder.Entry("Label", null));

            Assert.Throws<InvalidOperationException>(() => binder.Get<Text>("Label"));
        }

        [Test]
        public void Screen_OnBindCalledOncePerInstance()
        {
            var screen = _root.AddComponent<FakeScreen>();
            MakeBinder(_root);

            screen.Show();
            screen.Hide();
            screen.Show();

            Assert.AreEqual(1, screen.Binds);
        }

        private T AddChild<T>(string name) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            return go.AddComponent<T>();
        }

        private static UiBinder MakeBinder(GameObject host, params UiBinder.Entry[] entries)
        {
            var binder = host.AddComponent<UiBinder>();
            typeof(UiBinder)
                .GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(binder, entries);
            return binder;
        }

        private sealed class FakeScreen : ScreenBase
        {
            public int Binds { get; private set; }

            protected override void OnBind(UiBinder binder)
            {
                Binds++;
            }
        }
    }
}
