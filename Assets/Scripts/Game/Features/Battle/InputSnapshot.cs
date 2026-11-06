using UnityEngine;

namespace LiteGame.Features.Battle
{
    /// <summary>一帧原始输入：移动、瞄准点与按钮意图，采集层产物，不依赖具体设备。</summary>
    public readonly struct InputSnapshot
    {
        public InputSnapshot(Vector2 move, Vector3 aimPoint, bool fire, bool aim, bool reload, bool switchWeapon,
            int weaponSlot)
        {
            Move = move;
            AimPoint = aimPoint;
            Fire = fire;
            Aim = aim;
            Reload = reload;
            SwitchWeapon = switchWeapon;
            WeaponSlot = weaponSlot;
        }

        public Vector2 Move { get; }

        public Vector3 AimPoint { get; }

        public bool Fire { get; }

        public bool Aim { get; }

        public bool Reload { get; }

        public bool SwitchWeapon { get; }

        public int WeaponSlot { get; }
    }
}
