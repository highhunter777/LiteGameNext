using UnityEngine;

namespace LiteGame.Features.Battle
{
    /// <summary>动画契约（01 §9.3.1）：参数、层、状态名只在这里出现；调用点用 hash，禁字符串字面量。</summary>
    public static class AnimationIds
    {
        public static readonly int Speed = Animator.StringToHash("Speed");

        public static readonly int Grounded = Animator.StringToHash("Grounded");

        public static readonly int Aiming = Animator.StringToHash("Aiming");

        public static readonly int Fire = Animator.StringToHash("Fire");

        public static readonly int Reload = Animator.StringToHash("Reload");

        public static readonly int Death = Animator.StringToHash("Death");

        public static readonly int UpperBodyLayer = 1;

        public static readonly int IdleState = Animator.StringToHash("Idle");

        public static readonly int RunState = Animator.StringToHash("Run");

        public static readonly int AimIdleState = Animator.StringToHash("AimIdle");
    }
}
