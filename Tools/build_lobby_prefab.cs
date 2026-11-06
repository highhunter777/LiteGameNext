using System;
using LiteGame.Ui;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>P0-6 增量构建：Page_Lobby 预制体 + UIRoot 目录追加（不重建 UIRoot，保住 GUID）。</summary>
public static class LobbyUiBuilder
{
    private const string PagePath = "Assets/Content/Shared/UiScreens/Page_Lobby.prefab";
    private const string UiRootPath = "Assets/Content/Shared/UIRoot.prefab";
    private const string PageName = "ui.Page_Lobby";

    public static string Build()
    {
        var page = BuildPage();
        AppendCatalog(page);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "page=" + AssetDatabase.GetAssetPath(page) + " catalog=" + UiRootPath;
    }

    private static GameObject BuildPage()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PagePath) != null)
        {
            AssetDatabase.DeleteAsset(PagePath);
        }

        var root = new GameObject("Page_Lobby", typeof(RectTransform), typeof(Image), typeof(LuaScreen), typeof(UiBinder));
        Stretch((RectTransform)root.transform);
        root.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 1f);

        var label = CreateText(root.transform, "Label", "hello", 48, TextAnchor.MiddleCenter);
        label.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        label.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        label.rectTransform.sizeDelta = new Vector2(0f, 140f);

        var lua = root.GetComponent<LuaScreen>();
        var luaSo = new SerializedObject(lua);
        luaSo.FindProperty("_script").stringValue = PageName;
        luaSo.ApplyModifiedPropertiesWithoutUndo();

        var binder = root.GetComponent<UiBinder>();
        var binderSo = new SerializedObject(binder);
        var entries = binderSo.FindProperty("_entries");
        entries.arraySize = 1;
        var entry = entries.GetArrayElementAtIndex(0);
        entry.FindPropertyRelative("_key").stringValue = "Label";
        entry.FindPropertyRelative("_target").objectReferenceValue = label;
        binderSo.ApplyModifiedPropertiesWithoutUndo();

        var asset = PrefabUtility.SaveAsPrefabAsset(root, PagePath);
        UnityEngine.Object.DestroyImmediate(root);
        return asset;
    }

    private static void AppendCatalog(GameObject page)
    {
        var contents = PrefabUtility.LoadPrefabContents(UiRootPath);
        try
        {
            var host = contents.GetComponent<UiHost>();
            if (host == null)
            {
                throw new InvalidOperationException("UIRoot 缺少 UiHost");
            }

            var so = new SerializedObject(host);
            var screens = so.FindProperty("_screens");
            var index = -1;
            for (var i = 0; i < screens.arraySize; i++)
            {
                if (screens.GetArrayElementAtIndex(i).FindPropertyRelative("_id").enumValueIndex == (int)ScreenId.Lobby)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                index = screens.arraySize;
                screens.arraySize = index + 1;
            }

            var entry = screens.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("_id").enumValueIndex = (int)ScreenId.Lobby;
            entry.FindPropertyRelative("_prefab").objectReferenceValue = page;
            entry.FindPropertyRelative("_page").stringValue = PageName;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents, UiRootPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
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
        Stretch(text.rectTransform);
        return text;
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
}
