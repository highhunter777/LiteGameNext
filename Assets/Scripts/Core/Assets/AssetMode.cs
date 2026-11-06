namespace LiteGame.Core.Assets
{
    /// <summary>资源运行模式；P0 只用编辑器模拟，离线/联机在 P1 接内容管线后生效。</summary>
    public enum AssetMode
    {
        EditorSimulator = 0,
        Offline,
        Host,
    }
}
