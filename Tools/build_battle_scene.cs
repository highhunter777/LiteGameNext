using Cinemachine;
using LiteGame.Features.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>P1 战斗场景起步：导入环境 + Player(Rifle) + Cinemachine 跟随 + InputSampler。</summary>
public static class BattleSceneBuilder
{
    private const string EnvScene = "Assets/Imported/Scenes/TrainingGround.unity";
    private const string PlayerPrefab = "Assets/Imported/LiteGame/Modules/Battle/Content/Characters/Player(Rifle).prefab";
    private const string TargetScene = "Assets/Scenes/Battle.unity";

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(EnvScene, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        var spawn = GameObject.Find("MapCollision/spawn_00");
        player.transform.position = spawn != null ? spawn.transform.position : Vector3.zero;
        player.transform.rotation = Quaternion.LookRotation(Vector3.zero - player.transform.position);

        var camera = Camera.main;
        if (camera == null)
        {
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            camera = cameraGo.GetComponent<Camera>();
        }

        if (camera.GetComponent<CinemachineBrain>() == null)
        {
            camera.gameObject.AddComponent<CinemachineBrain>();
        }

        var vcamGo = new GameObject("CM_Battle", typeof(CinemachineVirtualCamera));
        var vcam = vcamGo.GetComponent<CinemachineVirtualCamera>();
        vcam.Priority = 20;
        vcam.Follow = player.transform;
        vcam.LookAt = player.transform;
        var transposer = vcam.AddCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = new Vector3(0f, 2.4f, -4.2f);
        var composer = vcam.AddCinemachineComponent<CinemachineComposer>();
        composer.m_TrackedObjectOffset = new Vector3(0f, 1.4f, 0f);

        var animator = player.GetComponent<Animator>();
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Content/Animators/RifleGirl.controller");
        if (animator != null && controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        if (player.GetComponent<CharacterView>() == null)
        {
            player.AddComponent<CharacterView>();
        }

        var sampler = player.GetComponent<InputSampler>();
        if (sampler == null)
        {
            sampler = player.AddComponent<InputSampler>();
        }

        var so = new SerializedObject(sampler);
        so.FindProperty("_aimCamera").objectReferenceValue = camera;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScene);
        AssetDatabase.SaveAssets();
        return "scene=" + TargetScene + " player=" + player.name + " camera=" + camera.name;
    }
}
