using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteGame.Core.Logging;
using UnityEngine;

namespace LiteGame.Core.Host
{
    /// <summary>AOT 宿主壳：唯一 MonoBehaviour 入口，只认 IGame；启动失败进错误屏。</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameEntry : MonoBehaviour
    {
        private static bool _booted;

        private IGame _game;
        private CancellationTokenSource _lifetime;
        private bool _active;
        private bool _tickFailed;

        [SerializeField] private GameObject _uiRootPrefab;

        /// <summary>UIRoot 预制体；由 Boot 场景的 GameEntry 序列化引用。</summary>
        public GameObject UiRootPrefab => _uiRootPrefab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForEditorReload()
        {
            _booted = false;
        }

        private void Awake()
        {
            if (_booted)
            {
                Log.Warning("检测到重复 GameEntry，已销毁", LogTag.Host);
                Destroy(gameObject);
                return;
            }

            _booted = true;
            _lifetime = new CancellationTokenSource();
            try
            {
                var config = LaunchConfig.Load(Environment.GetCommandLineArgs(), DefaultLuaRoot());
                Log.Configure(config.LogLevel);
                _game = BootEntry.Create(this, config);
                DontDestroyOnLoad(gameObject);
                _active = true;
            }
            catch (Exception exception)
            {
                Log.Error(exception, LogTag.Host);
                BootstrapErrorPresenter.Show(exception);
                _lifetime.Cancel();
                _lifetime.Dispose();
                _lifetime = null;
                _booted = false;
            }
        }

        private void Start()
        {
            if (!_active)
            {
                return;
            }

            _game.StartAsync(_lifetime.Token).Forget(exception => Log.Error(exception, LogTag.Host));
        }

        private void Update()
        {
            if (!_active || _tickFailed)
            {
                return;
            }

            try
            {
                _game.Tick(Time.unscaledDeltaTime);
                // Android back 在 legacy 输入里映射为 Escape；activeInputHandler=Both 时仍然有效。
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _game.HandleBack();
                }
            }
            catch (Exception exception)
            {
                _tickFailed = true;
                Log.Error(exception, LogTag.Host);
            }
        }

        private void OnDestroy()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
            _game?.Dispose();
            _game = null;
            _booted = false;
        }

        private static string DefaultLuaRoot()
        {
            return System.IO.Path.Combine(Application.dataPath, "Scripts", "Lua");
        }
    }
}
