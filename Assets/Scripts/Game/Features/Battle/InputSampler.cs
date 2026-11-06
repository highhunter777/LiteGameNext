using LiteSim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LiteGame.Features.Battle
{
    /// <summary>战斗输入采集：Input System 动作 → InputSnapshot → SimInputFrame；瞄准点取准心射线命中点。</summary>
    public sealed class InputSampler : MonoBehaviour
    {
        private const float AimDistance = 200f;

        [SerializeField] private Camera _aimCamera;
        [SerializeField] private LayerMask _aimMask = ~0;

        private InputActionMap _map;
        private InputAction _move;
        private InputAction _fire;
        private InputAction _aim;
        private InputAction _reload;
        private InputAction _slot1;
        private InputAction _slot2;
        private uint _actionSeq;
        private int _weaponSlot;

        /// <summary>采样一帧；Awake 前或无设备时返回默认帧。</summary>
        public SimInputFrame Sample(long entityId)
        {
            if (_map == null)
            {
                return default;
            }

            var reload = _reload.WasPressedThisFrame();
            var toSlot1 = _slot1.WasPressedThisFrame();
            var toSlot2 = _slot2.WasPressedThisFrame();
            var switchWeapon = toSlot1 || toSlot2;
            if (switchWeapon)
            {
                _weaponSlot = toSlot2 ? 1 : 0;
            }

            if (reload || switchWeapon)
            {
                _actionSeq++;
            }

            var snapshot = new InputSnapshot(
                _move.ReadValue<Vector2>(),
                ResolveAimPoint(),
                _fire.IsPressed(),
                _aim.IsPressed(),
                reload,
                switchWeapon,
                _weaponSlot);
            return InputFrameBuilder.Build(snapshot, entityId, _actionSeq);
        }

        private void Awake()
        {
            _map = new InputActionMap("Battle");
            _move = _map.AddAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _fire = _map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            _aim = _map.AddAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
            _reload = _map.AddAction("Reload", InputActionType.Button, "<Keyboard>/r");
            _slot1 = _map.AddAction("Slot1", InputActionType.Button, "<Keyboard>/1");
            _slot2 = _map.AddAction("Slot2", InputActionType.Button, "<Keyboard>/2");
            _map.Enable();
        }

        private void OnDestroy()
        {
            _map?.Dispose();
            _map = null;
        }

        /// <summary>准心（鼠标位置）射线命中点；未命中取射线远端。</summary>
        private Vector3 ResolveAimPoint()
        {
            if (_aimCamera == null)
            {
                return Vector3.zero;
            }

            var screen = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var ray = _aimCamera.ScreenPointToRay(screen);
            return Physics.Raycast(ray, out var hit, AimDistance, _aimMask) ? hit.point : ray.GetPoint(AimDistance);
        }
    }
}
