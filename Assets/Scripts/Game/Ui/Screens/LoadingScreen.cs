namespace LiteGame.Ui.Screens
{
    /// <summary>加载页：P0 只做占位与显隐，进度/取消在 P1 接内容管线时补。</summary>
    public sealed partial class LoadingScreen : ScreenBase
    {
        protected override void OnShow()
        {
            Label.text = "Loading...";
        }
    }
}
