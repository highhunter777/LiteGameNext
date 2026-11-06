using System;
using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;
using LiteGame.Ui;
using XLua;

namespace LiteGame.Facade
{
    /// <summary>Bridge.ui：页面导航（唯一写者仍是 ScreenManager）；等方法只转发，不缓存。</summary>
    public sealed partial class Bridge
    {
        private void InstallUi(LuaTable root)
        {
            var ui = _env.NewTable();
            ui.Set("Show", new Action<string>(Show));
            ui.Set("Replace", new Action<string>(Replace));
            ui.Set("Back", new Action(Back));
            ui.Set("Close", new Action<string>(Close));
            ui.Set("IsOpen", new Func<string, bool>(IsOpen));
            ui.Set("ShowToast", new Action<string>(ShowToast));
            ui.Set("ShowConfirm", new Action<string, Action>(ShowConfirm));
            root.Set("ui", ui);
        }

        private void Show(string page)
        {
            if (!TryPage(page, out var id))
            {
                return;
            }

            Run(_screens.ShowAsync(id), "ui.Show " + page);
        }

        private void Replace(string page)
        {
            if (!TryPage(page, out var id))
            {
                return;
            }

            Run(_screens.ReplaceAsync(id), "ui.Replace " + page);
        }

        private void Back()
        {
            Run(_screens.BackAsync(), "ui.Back");
        }

        private void Close(string page)
        {
            if (!TryPage(page, out var id))
            {
                return;
            }

            if (_screens.CurrentId != id)
            {
                Log.Warning("只支持关闭栈顶页：" + page, LogTag.Lua);
                return;
            }

            Back();
        }

        private bool IsOpen(string page)
        {
            return _screens.IsPageOpen(page);
        }

        private void ShowToast(string text)
        {
            Log.Warning("Toast 未接（P1）：" + text, LogTag.Lua);
        }

        private void ShowConfirm(string text, Action onOk)
        {
            Log.Warning("Confirm 未接（P1）：" + text, LogTag.Lua);
        }

        private bool TryPage(string page, out ScreenId id)
        {
            if (_screens.TryPageId(page, out id))
            {
                return true;
            }

            Log.Warning("页面未登记：" + page, LogTag.Lua);
            return false;
        }

        private static void Run(UniTask task, string what)
        {
            task.Forget(exception => Log.Error(exception, LogTag.Lua, what));
        }
    }
}
