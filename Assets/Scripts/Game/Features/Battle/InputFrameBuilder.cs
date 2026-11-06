using LiteSim;
using UnityEngine;

namespace LiteGame.Features.Battle
{
    /// <summary>InputSnapshot → SimInputFrame：移动长度钳到 1，按钮位按共享包常量映射，离散意图带 actionSeq。</summary>
    public static class InputFrameBuilder
    {
        public static SimInputFrame Build(in InputSnapshot snapshot, long entityId, uint actionSeq)
        {
            var move = snapshot.Move;
            if (move.sqrMagnitude > 1f)
            {
                move = move.normalized;
            }

            uint buttons = 0u;
            if (snapshot.Fire)
            {
                buttons |= SimInputFrame.ButtonFire;
            }

            if (snapshot.Aim)
            {
                buttons |= SimInputFrame.ButtonAim;
            }

            if (snapshot.Reload)
            {
                buttons |= SimInputFrame.ButtonReload;
            }

            if (snapshot.SwitchWeapon)
            {
                buttons |= SimInputFrame.ButtonSwitchWeapon;
            }

            return new SimInputFrame
            {
                EntityId = entityId,
                MoveX = move.x,
                MoveZ = move.y,
                AimPointX = snapshot.AimPoint.x,
                AimPointY = snapshot.AimPoint.y,
                AimPointZ = snapshot.AimPoint.z,
                Buttons = buttons,
                SelectedWeaponSlot = snapshot.WeaponSlot,
                ActionSeq = actionSeq,
            };
        }
    }
}
