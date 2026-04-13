#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Utilities
{
    /// <summary>
    ///     Odin Inspector 自动检测器
    ///     <para>自动检测项目中是否安装了 Odin Inspector，并管理 ODIN_INSPECTOR 脚本定义符号</para>
    /// </summary>
    [InitializeOnLoad]
    public static class OdinDetector
    {
        private const string DEFINE_SYMBOL = "ODIN_INSPECTOR";
        private const string ODIN_ASSEMBLY_NAME = "Sirenix.OdinInspector.Attributes";

        static OdinDetector()
        {
            // 延迟执行，避免在编译过程中修改定义符号
            EditorApplication.delayCall += DetectAndSetDefine;
        }

        /// <summary>
        ///     检测 Odin Inspector 是否已安装，并自动设置 ODIN_INSPECTOR 定义符号
        /// </summary>
        private static void DetectAndSetDefine()
        {
            var hasOdin = false;
            try
            {
                hasOdin = AppDomain.CurrentDomain.GetAssemblies()
                    .Any(a => a.GetName().Name == ODIN_ASSEMBLY_NAME);
            }
            catch
            {
                return;
            }

            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var symbols = defines.Split(';').ToList();

            var changed = false;
            if (hasOdin && !symbols.Contains(DEFINE_SYMBOL))
            {
                symbols.Add(DEFINE_SYMBOL);
                changed = true;
            }
            else if (!hasOdin && symbols.Contains(DEFINE_SYMBOL))
            {
                symbols.Remove(DEFINE_SYMBOL);
                changed = true;
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", symbols));
                Debug.Log($"[OdinDetector] Odin Inspector {(hasOdin ? "已检测到，添加" : "未检测到，移除")} {DEFINE_SYMBOL} 定义符号");
            }
        }
    }
}
#endif
