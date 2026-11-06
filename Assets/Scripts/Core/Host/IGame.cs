using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteGame.Core.Host
{
    /// <summary>AOT 侧认识的唯一业务抽象；实现方是业务程序集（未来热更）。</summary>
    public interface IGame : IDisposable
    {
        UniTask StartAsync(CancellationToken token);

        void Tick(float deltaSeconds);

        /// <summary>返回键：宿主读输入后转发，页面栈自行决定退到哪。</summary>
        void HandleBack();
    }
}
