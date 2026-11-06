using UnityEngine;

namespace LiteGame.Features.Battle
{
    /// <summary>角色表现件：从状态推导动画参数，变值才写；不参与判定（01 §9.2 五条纪律）。</summary>
    public sealed class CharacterView : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        private CharacterAnimator _animation;
        private float _speed = float.NaN;
        private float _upperBodyWeight = float.NaN;
        private int _grounded = -1;
        private int _aiming = -1;

        private void Awake()
        {
            var animator = _animator != null ? _animator : GetComponent<Animator>();
            if (animator == null)
            {
                throw new System.InvalidOperationException("CharacterView 缺少 Animator：" + name);
            }

            _animation = new CharacterAnimator(animator);
        }

        public void SetSpeed(float metersPerSecond)
        {
            if (_speed.Equals(metersPerSecond))
            {
                return;
            }

            _speed = metersPerSecond;
            _animation.SetFloat(AnimationIds.Speed, metersPerSecond);
        }

        public void SetGrounded(bool grounded)
        {
            var value = grounded ? 1 : 0;
            if (_grounded == value)
            {
                return;
            }

            _grounded = value;
            _animation.SetBool(AnimationIds.Grounded, grounded);
        }

        public void SetAiming(bool aiming)
        {
            var value = aiming ? 1 : 0;
            if (_aiming == value)
            {
                return;
            }

            _aiming = value;
            _animation.SetBool(AnimationIds.Aiming, aiming);
        }

        public void Fire()
        {
            _animation.SetTrigger(AnimationIds.Fire);
        }

        public void Reload()
        {
            _animation.SetTrigger(AnimationIds.Reload);
        }

        public void Die()
        {
            _animation.SetTrigger(AnimationIds.Death);
        }

        public void SetUpperBodyWeight(float weight)
        {
            if (_upperBodyWeight.Equals(weight))
            {
                return;
            }

            _upperBodyWeight = weight;
            _animation.SetLayerWeight(AnimationIds.UpperBodyLayer, weight);
        }
    }
}
