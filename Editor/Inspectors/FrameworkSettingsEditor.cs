using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Inspectors
{
    /// <summary>
    ///     FrameworkSettings 自定义 Inspector
    /// </summary>
    [CustomEditor(typeof(FrameworkSettings))]
    internal sealed class FrameworkSettingsEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // 使用 InspectorElement 绘制默认 Inspector 内容
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            return root;
        }
    }
}
