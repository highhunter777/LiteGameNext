using Cysharp.Threading.Tasks;

namespace LiteGame.Boot
{
    /// <summary>应用状态契约：进入/退出/每帧；实现不得自行切换状态。</summary>
    public interface IAppState
    {
        UniTask EnterAsync();

        UniTask ExitAsync();

        void Tick(float deltaSeconds);
    }
}
