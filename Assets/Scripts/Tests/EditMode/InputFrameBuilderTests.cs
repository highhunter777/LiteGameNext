using LiteGame.Features.Battle;
using LiteSim;
using NUnit.Framework;
using UnityEngine;

namespace LiteGame.Tests.EditMode
{
    /// <summary>InputFrameBuilder：移动钳制、按钮位映射、离散意图序号与瞄准点透传。</summary>
    public sealed class InputFrameBuilderTests
    {
        [Test]
        public void Build_MapsMoveAndAimPoint()
        {
            var frame = InputFrameBuilder.Build(
                new InputSnapshot(new Vector2(0.3f, -0.4f), new Vector3(1f, 2f, 3f), false, false, false, false, 0),
                42L,
                0u);

            Assert.AreEqual(0.3f, frame.MoveX, 1e-4f);
            Assert.AreEqual(-0.4f, frame.MoveZ, 1e-4f);
            Assert.AreEqual(42L, frame.EntityId);
            Assert.AreEqual(1f, frame.AimPointX, 1e-4f);
            Assert.AreEqual(2f, frame.AimPointY, 1e-4f);
            Assert.AreEqual(3f, frame.AimPointZ, 1e-4f);
        }

        [Test]
        public void Build_ClampsMoveLength()
        {
            var frame = InputFrameBuilder.Build(
                new InputSnapshot(new Vector2(3f, 4f), Vector3.zero, false, false, false, false, 0), 1L, 0u);

            Assert.AreEqual(1f, Mathf.Sqrt(frame.MoveX * frame.MoveX + frame.MoveZ * frame.MoveZ), 1e-4f);
        }

        [Test]
        public void Build_MapsButtonsAndDiscreteIntent()
        {
            var frame = InputFrameBuilder.Build(
                new InputSnapshot(Vector2.zero, Vector3.zero, true, true, true, true, 1), 1L, 7u);

            var expected = SimInputFrame.ButtonFire | SimInputFrame.ButtonAim | SimInputFrame.ButtonReload |
                           SimInputFrame.ButtonSwitchWeapon;
            Assert.AreEqual(expected, frame.Buttons);
            Assert.AreEqual(7u, frame.ActionSeq);
            Assert.AreEqual(1, frame.SelectedWeaponSlot);
        }

        [Test]
        public void Build_EmptySnapshot_LeavesSeqAsGiven()
        {
            var frame = InputFrameBuilder.Build(default, 0L, 3u);

            Assert.AreEqual(0u, frame.Buttons);
            Assert.AreEqual(3u, frame.ActionSeq);
        }
    }
}
