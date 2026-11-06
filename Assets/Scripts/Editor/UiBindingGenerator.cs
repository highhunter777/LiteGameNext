using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LiteGame.Ui;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LiteGame.Editor
{
    /// <summary>UI 绑定生成：扫描页面 prefab → 写 UiBinder 条目 + 生成 partial 部分类。
    /// 只做生成，不做契约校验/预览/工作台；页面类必须是 partial。</summary>
    public static class UiBindingGenerator
    {
        private static readonly Type[] Supported =
        {
            typeof(Button), typeof(Text), typeof(Image), typeof(CanvasGroup),
            typeof(Toggle), typeof(Slider), typeof(InputField), typeof(ScrollRect),
        };

        private static readonly Regex _identifier = new Regex("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

        [MenuItem("LiteGame/Ui/生成页面绑定（选中 prefab）")]
        public static void GenerateFromSelection()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("请在 Project 窗口选中一个 .prefab");
            }

            Debug.Log(Generate(path));
        }

        /// <summary>生成入口（run_script 可调）：返回摘要字符串。</summary>
        public static string Generate(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
            {
                throw new ArgumentException("prefabPath 不能为空", nameof(prefabPath));
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("找不到 prefab：" + prefabPath);
            }

            var screen = prefab.GetComponent<ScreenBase>();
            if (screen == null)
            {
                throw new InvalidOperationException("prefab 根节点缺少 ScreenBase 派生组件：" + prefabPath);
            }

            var screenType = screen.GetType();
            var scriptPath = FindScriptPath(screenType);
            EnsurePartial(scriptPath, screenType);

            var bindings = Collect(prefab.transform, screenType);
            WriteBinder(prefabPath, bindings);

            var outputPath = scriptPath.Substring(0, scriptPath.Length - 3) + ".Bindings.g.cs";
            var changed = WriteIfChanged(outputPath, BuildSource(screenType, prefabPath, bindings));
            AssetDatabase.SaveAssets();

            return "bindings=" + bindings.Count + (changed ? " generated=" : " unchanged=") + outputPath;
        }

        /// <summary>只认类声明上的 partial 修饰符；注释里出现该词不算数。</summary>
        private static void EnsurePartial(string scriptPath, Type screenType)
        {
            var pattern = @"\bpartial\s+(?:sealed\s+|abstract\s+)?class\s+" + Regex.Escape(screenType.Name) + @"\b";
            if (!Regex.IsMatch(File.ReadAllText(scriptPath), pattern))
            {
                throw new InvalidOperationException("页面类需要声明为 partial：" + scriptPath);
            }
        }

        /// <summary>内容相同就不写，避免无谓的导入与重编译。</summary>
        private static bool WriteIfChanged(string path, string source)
        {
            if (File.Exists(path) && File.ReadAllText(path) == source)
            {
                return false;
            }

            File.WriteAllText(path, source, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            return true;
        }

        private static void WriteBinder(string prefabPath, IReadOnlyList<Binding> bindings)
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;
                var binder = root.GetComponent<UiBinder>();
                if (binder == null)
                {
                    binder = root.AddComponent<UiBinder>();
                }

                var serialized = new SerializedObject(binder);
                var entries = serialized.FindProperty("_entries");
                entries.arraySize = bindings.Count;
                for (var i = 0; i < bindings.Count; i++)
                {
                    var element = entries.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("_key").stringValue = bindings[i].Name;
                    element.FindPropertyRelative("_target").objectReferenceValue = bindings[i].Target;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static List<Binding> Collect(Transform root, Type screenType)
        {
            var bindings = new List<Binding>();
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);
            Walk(root, root, screenType, bindings, seen);
            return bindings;
        }

        private static void Walk(Transform root, Transform node, Type screenType, List<Binding> bindings,
            Dictionary<string, string> seen)
        {
            foreach (Transform child in node)
            {
                if (child == root)
                {
                    continue;
                }

                var name = child.name;
                if (!_identifier.IsMatch(name) || name.StartsWith("_", StringComparison.Ordinal))
                {
                    Walk(root, child, screenType, bindings, seen);
                    continue;
                }

                var component = PickSupported(child.gameObject);
                if (component == null)
                {
                    Walk(root, child, screenType, bindings, seen);
                    continue;
                }

                var members = screenType.GetMember(name, System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.FlattenHierarchy);
                if (members.Any(member => member.DeclaringType != screenType))
                {
                    throw new InvalidOperationException("节点名与页面成员冲突：" + name);
                }

                if (seen.TryGetValue(name, out var previous))
                {
                    throw new InvalidOperationException("同名节点无法生成绑定：" + name + "（" + previous + " 与 " + child.name + "）");
                }

                seen.Add(name, child.name);
                bindings.Add(new Binding(name, component));

                // 已绑定的节点视为控件，其内部节点不再生成绑定（避免 Back/Label 这类内部件重名）。
                continue;
            }
        }

        private static Component PickSupported(GameObject target)
        {
            foreach (var type in Supported)
            {
                var component = target.GetComponent(type);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static string BuildSource(Type screenType, string prefabPath, IReadOnlyList<Binding> bindings)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("// 由 LiteGame.Editor.UiBindingGenerator 生成（源：" + prefabPath + "），勿手改。");
            builder.AppendLine("using LiteGame.Ui;");
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine("using UnityEngine.UI;");
            builder.AppendLine();
            builder.Append("namespace ").AppendLine(screenType.Namespace);
            builder.AppendLine("{");

            var modifiers = "public " + (screenType.IsAbstract ? "abstract " : string.Empty)
                + (screenType.IsSealed ? "sealed " : string.Empty) + "partial class ";
            builder.Append("    ").Append(modifiers).AppendLine(screenType.Name);
            builder.AppendLine("    {");

            foreach (var binding in bindings)
            {
                builder.Append("        public ").Append(binding.Target.GetType().Name).Append(' ')
                    .Append(binding.Name).AppendLine(" { get; private set; }");
            }

            builder.AppendLine();
            builder.AppendLine("        protected override void OnBind(UiBinder binder)");
            builder.AppendLine("        {");
            foreach (var binding in bindings)
            {
                builder.Append("            ").Append(binding.Name).Append(" = binder.Get<")
                    .Append(binding.Target.GetType().Name).Append(">(\"").Append(binding.Name).AppendLine("\");");
            }

            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string FindScriptPath(Type screenType)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript " + screenType.Name))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == screenType)
                {
                    return path;
                }
            }

            throw new InvalidOperationException("找不到页面脚本：" + screenType.Name);
        }

        private sealed class Binding
        {
            public Binding(string name, Component target)
            {
                Name = name;
                Target = target;
            }

            public string Name { get; }

            public Component Target { get; }
        }
    }
}
