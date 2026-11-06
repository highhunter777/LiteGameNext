using UnityEngine;

namespace LiteGame.Ui
{
    /// <summary>C# 页基类：显隐与每帧回调只由 ScreenManager 调；页面不得自行开关。
    /// 首次 Show 走 OnShow，被覆盖后回来走 OnReveal；覆盖期间不派发 OnUpdate。</summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        [SerializeField] private UiBinder _binder;

        private bool _bound;
        private bool _shown;

        protected UiBinder Binder { get { return _binder; } }

        public void Show()
        {
            EnsureBound();
            gameObject.SetActive(true);
            if (_shown)
            {
                OnReveal();
                return;
            }

            _shown = true;
            OnShow();
        }

        /// <summary>被新页覆盖：停更但不销毁，回来走 OnReveal。</summary>
        public void Cover()
        {
            if (!_shown)
            {
                return;
            }

            gameObject.SetActive(false);
            OnCovered();
        }

        public void Reveal()
        {
            if (!_shown)
            {
                return;
            }

            gameObject.SetActive(true);
            OnReveal();
        }

        public void Hide()
        {
            if (_shown)
            {
                _shown = false;
                OnHide();
            }

            gameObject.SetActive(false);
        }

        public void Tick(float deltaSeconds)
        {
            OnUpdate(deltaSeconds);
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnCovered()
        {
        }

        protected virtual void OnReveal()
        {
        }

        protected virtual void OnHide()
        {
        }

        protected virtual void OnUpdate(float deltaSeconds)
        {
        }

        /// <summary>绑定钩子：仅在首次 Show 调用一次；生成代码在这里取控件。</summary>
        protected virtual void OnBind(UiBinder binder)
        {
        }

        private void EnsureBound()
        {
            if (_bound)
            {
                return;
            }

            _bound = true;
            if (_binder == null)
            {
                _binder = GetComponent<UiBinder>();
            }

            if (_binder != null)
            {
                OnBind(_binder);
            }
        }
    }
}
