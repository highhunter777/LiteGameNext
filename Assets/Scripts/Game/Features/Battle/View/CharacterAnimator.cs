using UnityEngine;

namespace LiteGame.Features.Battle
{
    /// <summary>Animator 薄封装：只转发，不判断该不该切（01 §9.2）。</summary>
    public sealed class CharacterAnimator
    {
        private readonly Animator _animator;

        public CharacterAnimator(Animator animator)
        {
            _animator = animator != null ? animator : throw new System.ArgumentNullException(nameof(animator));
        }

        public void SetFloat(int hash, float value)
        {
            _animator.SetFloat(hash, value);
        }

        public void SetBool(int hash, bool value)
        {
            _animator.SetBool(hash, value);
        }

        public void SetTrigger(int hash)
        {
            _animator.SetTrigger(hash);
        }

        /// <summary>直接切状态（固定时长淡入）；状态机仍以 Animator 那份为准。</summary>
        public void Play(int stateHash, float fadeSeconds = 0.1f)
        {
            _animator.CrossFadeInFixedTime(stateHash, fadeSeconds);
        }

        public void SetLayerWeight(int layerIndex, float weight)
        {
            _animator.SetLayerWeight(layerIndex, weight);
        }
    }
}
