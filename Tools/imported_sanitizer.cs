using UnityEditor;
using UnityEngine;

/// <summary>导入后清理：移除预制体里指向旧仓脚本的 Missing MonoBehaviour（脚本不导入）。</summary>
public static class ImportedSanitizer
{
    private static readonly string[] Targets =
    {
        "Assets/Imported/LiteGame/Modules/Battle/Content/Characters/Player(Rifle).prefab",
        "Assets/Imported/LiteGame/Modules/Battle/Content/Characters/Player(Kazuko).prefab",
        "Assets/Imported/ThirdParty/CombatGirlsCharacterPack/UI_&_environmental/Environment/Figure_Base/Prefab/Figure_Base_EditNormal.prefab",
    };

    public static string StripAll()
    {
        var report = string.Empty;
        foreach (var path in Targets)
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var removed = 0;
                foreach (var t in scope.prefabContentsRoot.GetComponentsInChildren<Transform>(true))
                {
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                }

                report += path.Substring(path.LastIndexOf('/') + 1) + " removed=" + removed + " | ";
            }
        }

        AssetDatabase.SaveAssets();
        return report;
    }
}
