using System;
using LiteGame.Core.Host;
using LiteGame.Ui;
using LiteGame.Ui.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>P0-5 一次性资产构建：UIRoot / LoadingScreen / ErrorScreen 预制体 + Boot 场景接线。</summary>
public static class UiPrefabBuilder
{
    private const string ContentDir = "Assets/Content";
    private const string SharedDir = "Assets/Content/Shared";
    private const string ScreensDir = "Assets/Content/Shared/UiScreens";
    private const string UiRootPath = SharedDir + "/UIRoot.prefab";
    private const string LoadingPath = ScreensDir + "/LoadingScreen.prefab";
    private const string ErrorPath = ScreensDir + "/ErrorScreen.prefab";

    public static string Build()
    {
        EnsureFolder(ContentDir);
        EnsureFolder(SharedDir);
        EnsureFolder(ScreensDir);

        var loading = BuildScreenPrefab<LoadingScreen>(LoadingPath, "Loading...", false);
        var error = BuildScreenPrefab<ErrorScreen>(ErrorPath, "启动失败，详见日志", true);
        var uiRoot = BuildUiRootPrefab(loading, error);

        AssignToBootScene(uiRoot);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return "uiRoot=" + AssetDatabase.GetAssetPath(uiRoot)
            + " loading=" + AssetDatabase.GetAssetPath(loading)
            + " error=" + AssetDatabase.GetAssetPath(error);
    }

    private static GameObject BuildScreenPrefab<T>(string path, string label, bool withBackButton) where T : ScreenBase
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        var root = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(Image), typeof(T));
        var rect = (RectTransform)root.transform;
        Stretch(rect);
        root.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 1f);

        var text = CreateText(root.transform, "Label", label, 40, TextAnchor.MiddleCenter);
        text.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        text.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        text.rectTransform.sizeDelta = new Vector2(0f, 120f);

        if (withBackButton)
        {
            CreateButton(root.transform);
        }

        var asset = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return asset;
    }

    private static GameObject BuildUiRootPrefab(GameObject loading, GameObject error)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(UiRootPath) != null)
        {
            AssetDatabase.DeleteAsset(UiRootPath);
        }

        var root = new GameObject("UIRoot", typeof(RectTransform), typeof(UiHost));

        var screenCanvas = CreateCanvas(root.transform, "Canvas_Screen", 0);
        CreateCanvas(root.transform, "Canvas_Popup", 10);
        CreateCanvas(root.transform, "Canvas_Overlay", 100);

        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        events.transform.SetParent(root.transform, false);

        var host = root.GetComponent<UiHost>();
        var so = new SerializedObject(host);
        so.FindProperty("_screenRoot").objectReferenceValue = screenCanvas.transform;
        var screens = so.FindProperty("_screens");
        screens.arraySize = 2;
        SetEntry(screens.GetArrayElementAtIndex(0), ScreenId.Loading, loading);
        SetEntry(screens.GetArrayElementAtIndex(1), ScreenId.Error, error);
        so.ApplyModifiedPropertiesWithoutUndo();

        var asset = PrefabUtility.SaveAsPrefabAsset(root, UiRootPath);
        UnityEngine.Object.DestroyImmediate(root);
        return asset;
    }

    private static void AssignToBootScene(GameObject uiRoot)
    {
        var entry = UnityEngine.Object.FindObjectOfType<GameEntry>();
        if (entry == null)
        {
            throw new InvalidOperationException("当前场景里找不到 GameEntry（先在编辑器打开 Boot.unity）");
        }

        var so = new SerializedObject(entry);
        var property = so.FindProperty("_uiRootPrefab");
        if (property == null)
        {
            throw new InvalidOperationException("GameEntry 缺少 _uiRootPrefab 字段");
        }

        property.objectReferenceValue = uiRoot;
        so.ApplyModifiedPropertiesWithoutUndo();

        var scene = entry.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetEntry(SerializedProperty element, ScreenId id, GameObject prefab)
    {
        element.FindPropertyRelative("_id").enumValueIndex = (int)id;
        element.FindPropertyRelative("_prefab").objectReferenceValue = prefab;
    }

    private static RectTransform CreateCanvas(Transform parent, string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return (RectTransform)go.transform;
    }

    private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.text = content;
        text.font = BuiltinFont();
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }

    private static Button CreateButton(Transform parent)
    {
        var go = new GameObject("Back", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.25f);
        rect.anchorMax = new Vector2(0.5f, 0.25f);
        rect.sizeDelta = new Vector2(240f, 72f);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.45f, 0.85f, 1f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        CreateText(go.transform, "Caption", "返回", 32, TextAnchor.MiddleCenter);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Font BuiltinFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font != null ? font : Font.CreateDynamicFontFromOSFont("Arial", 16);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }
}
