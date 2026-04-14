#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace CFramework.Editor.Inspectors
{
    /// <summary>
    ///     配置表自定义编辑器（Odin 版本）
    /// </summary>
    [CustomEditor(typeof(ConfigTableBase), true)]
    public class ConfigTableEditor : OdinEditor
    {
        // 使用 Odin 默认的 Inspector 显示
        // 如需自定义功能，可在此扩展
    }
}
#else
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Inspectors
{
    /// <summary>
    ///     配置表自定义编辑器（默认实现）
    ///     <para>为 ConfigTableBase 提供增强的 Inspector 显示</para>
    /// </summary>
    [CustomEditor(typeof(ConfigTableBase), true)]
    public class ConfigTableEditor : UnityEditor.Editor
    {
        private GUIContent _dataListLabel;

        private void OnEnable()
        {
            _dataListLabel = new GUIContent("数据列表");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(4);

            // 显示配置表信息头
            var config = (ConfigTableBase)target;
            var typeName = config.GetType().Name;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"{config.Count} 条记录", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(2);

            // 分割线
            var lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 1f));
            EditorGUILayout.Space(4);

            // 显示默认属性（包含 dataList）
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
