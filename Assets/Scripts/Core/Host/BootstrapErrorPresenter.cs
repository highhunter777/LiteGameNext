using System;
using UnityEngine;

namespace LiteGame.Core.Host
{
    /// <summary>AOT 错误屏：引导失败时的最小可见面，不依赖 UI 栈。</summary>
    public sealed class BootstrapErrorPresenter : MonoBehaviour
    {
        private const int MaxLength = 2000;

        private string _text = string.Empty;

        /// <summary>复制异常文本并挂一个常驻错误屏；不抛异常。</summary>
        public static void Show(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            var host = new GameObject("BootstrapError");
            DontDestroyOnLoad(host);
            var presenter = host.AddComponent<BootstrapErrorPresenter>();
            var text = exception.ToString();
            presenter._text = text.Length > MaxLength ? text.Substring(0, MaxLength) : text;
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(20, 20, Screen.width - 40, Screen.height - 40), "启动失败\n\n" + _text);
        }
    }
}
