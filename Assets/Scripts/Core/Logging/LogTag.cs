namespace LiteGame.Core.Logging
{
    /// <summary>日志分类标签：一个模块一个值，不按类名逐个扩张。
    /// 页面名、信号类型等自由文本进消息详情，不进标签。</summary>
    public enum LogTag
    {
        /// <summary>未分类：仅占 default 值，正式日志不得使用。</summary>
        None = 0,

        /// <summary>AOT 宿主壳：GameEntry / BootEntry。</summary>
        Host,

        /// <summary>业务组合根 Game。</summary>
        Game,

        /// <summary>应用状态机 AppFlow 及其状态实现。</summary>
        Flow,

        /// <summary>Lua 宿主与回调。</summary>
        Lua,

        /// <summary>强类型事件总线。</summary>
        EventBus,

        /// <summary>UI 层：页面栈与页面宿主。</summary>
        Ui,
    }
}
