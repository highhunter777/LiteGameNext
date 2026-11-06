namespace LiteGame.Boot
{
    /// <summary>应用状态；None 仅表示尚未进入任何状态。</summary>
    public enum AppState
    {
        None = 0,
        Boot,
        Patch,
        Login,
        Lobby,
        Room,
        Battle,
        Result,
        Error,
    }
}
