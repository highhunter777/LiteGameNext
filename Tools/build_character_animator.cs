using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>按 01 §9.3.1 参数契约生成 RifleGirl 控制器（Idle/Run/AimIdle/Reload/Death）。</summary>
public static class CharacterAnimatorBuilder
{
    private const string Dir = "Assets/Content/Animators";
    private const string Output = Dir + "/RifleGirl.controller";
    private const string Clips = "Assets/Imported/ThirdParty/CombatGirlsCharacterPack/RifleGirl/Animations";

    public static string Build()
    {
        if (!AssetDatabase.IsValidFolder(Dir))
        {
            AssetDatabase.CreateFolder("Assets/Content", "Animators");
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(Output);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Aiming", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        var machine = controller.layers[0].stateMachine;
        var idle = State(machine, "Idle", Clip("Normal/Idle.fbx"));
        var run = State(machine, "Run", Clip("Normal/Run.fbx"));
        var aimIdle = State(machine, "AimIdle", Clip("Aiming/AimIdle.fbx"));
        var reload = State(machine, "Reload", Clip("Aiming/Reload.fbx"));
        var death = State(machine, "Death", Clip("Normal/Die2.fbx"));
        machine.defaultState = idle;

        Transition(idle, run, AnimatorConditionMode.Greater, 0.1f, "Speed");
        Transition(run, idle, AnimatorConditionMode.Less, 0.1f, "Speed");
        Transition(idle, aimIdle, AnimatorConditionMode.If, 0f, "Aiming");
        Transition(aimIdle, idle, AnimatorConditionMode.IfNot, 0f, "Aiming");
        Transition(run, aimIdle, AnimatorConditionMode.If, 0f, "Aiming");

        var reloadBack = reload.AddTransition(idle);
        reloadBack.hasExitTime = true;
        reloadBack.exitTime = 0.9f;
        reloadBack.duration = 0.1f;

        Any(machine, reload, "Reload");
        Any(machine, death, "Death");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return "controller=" + Output + " states=" + machine.states.Length;
    }

    private static AnimatorState State(AnimatorStateMachine machine, string name, AnimationClip clip)
    {
        var state = machine.AddState(name);
        state.motion = clip;
        return state;
    }

    private static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold,
        string parameter)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.15f;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void Any(AnimatorStateMachine machine, AnimatorState to, string parameter)
    {
        var transition = machine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.1f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static AnimationClip Clip(string relative)
    {
        var path = Clips + "/" + relative;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
            {
                return clip;
            }
        }

        throw new InvalidOperationException("找不到动画片段：" + path);
    }
}
