using System;
using LiteGame.Ui;

namespace LiteGame.Ui.Screens
{
    /// <summary>错误页：只展示与发请求；返回请求由持有者决定怎么处理（页面不切状态）。</summary>
    public sealed partial class ErrorScreen : ScreenBase
    {
        public Action BackRequested { get; set; }

        protected override void OnShow()
        {
            Label.text = "启动失败，详见日志";
            Back.onClick.RemoveListener(OnBackClicked);
            Back.onClick.AddListener(OnBackClicked);
        }

        protected override void OnHide()
        {
            Back.onClick.RemoveListener(OnBackClicked);
        }

        private void OnBackClicked()
        {
            BackRequested?.Invoke();
        }
    }
}
