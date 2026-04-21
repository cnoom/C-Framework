using System.IO;
using CFramework.Editor.Configs;
using CFramework.Editor.Utilities;
using CFramework.Editor.Windows;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor
{
    /// <summary>
    ///     Luban 菜单项
    /// </summary>
    public static class LubanMenuItems
    {
        private const string MenuBase = "CFramework/Luban/";

        [MenuItem(MenuBase + "生成器", priority = 100)]
        public static void OpenGenerator()
        {
            LubanWindow.OpenWindow();
        }

        [MenuItem(MenuBase + "设置", priority = 200)]
        public static void OpenSettings()
        {
            // 打开窗口并切换到设置 Tab
            var window = EditorWindow.GetWindow<LubanWindow>("Luban 生成器");
            window.minSize = new Vector2(480, 520);
            // 通过反射设置 _currentTab 字段
            var field = typeof(LubanWindow).GetField("_currentTab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(window, 1); // TabType.Settings = 1
            }
        }

        [MenuItem(MenuBase + "一键生成", priority = 300)]
        public static void QuickGenerate()
        {
            EditorApplication.delayCall += () =>
            {
                var result = LubanGenerator.Generate();
                if (result.Success)
                {
                    Debug.Log($"[Luban] 一键生成成功！耗时: {result.Duration.TotalSeconds:F1}s");
                }
                else
                {
                    Debug.LogError($"[Luban] 一键生成失败: {result.Error}");
                }
            };
        }

        [MenuItem(MenuBase + "创建默认 luban.conf", priority = 400)]
        public static void CreateDefaultLubanConf()
        {
            var path = EditorUtility.SaveFilePanel(
                "保存 luban.conf",
                "Assets",
                "luban.conf",
                "conf");

            if (string.IsNullOrEmpty(path)) return;

            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var relativePath = path.StartsWith(projectRoot)
                ? path.Substring(projectRoot.Length + 1).Replace('\\', '/')
                : path;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var conf = @"{
    ""groups"":
    [
        {""names"":[""c""], ""default"":true},
        {""names"":[""s""], ""default"":true}
    ],
    ""schemaFiles"":
    [
        {""fileName"":""Defines"", ""type"":""""},
        {""fileName"":""Datas/__tables__.xlsx"", ""type"":""table""},
        {""fileName"":""Datas/__beans__.xlsx"", ""type"":""bean""},
        {""fileName"":""Datas/__enums__.xlsx"", ""type"":""enum""}
    ],
    ""dataDir"": ""Datas"",
    ""targets"":
    [
        {""name"":""client"", ""manager"":""Tables"", ""groups"":[""c""], ""topModule"":""cfg""},
        {""name"":""server"", ""manager"":""Tables"", ""groups"":[""s""], ""topModule"":""cfg""},
        {""name"":""all"", ""manager"":""Tables"", ""groups"":[""c"",""s""], ""topModule"":""cfg""}
    ],
    ""xargs"":
    [
    ]
}";
            File.WriteAllText(path, conf);
            AssetDatabase.Refresh();

            Debug.Log($"[Luban] 已创建 luban.conf: {relativePath}");
        }
    }
}
