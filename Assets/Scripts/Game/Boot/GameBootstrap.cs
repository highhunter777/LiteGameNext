using LiteGame.Core.Host;
using UnityEngine;

namespace LiteGame.Boot
{
    /// <summary>业务侧自注册：AOT 只认 BootEntry 缝，业务在这里把 Game 装进去。</summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {
            BootEntry.Register(static (host, config) => Game.Build(host, config));
        }
    }
}
