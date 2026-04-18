#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Inspectors
{
    [CustomEditor(typeof(ConfigTableBase), true)]
    public class ConfigTableEditor : OdinEditor
    {
        private VisualElement _rootElement;

        public override VisualElement CreateInspectorGUI()
        {
            _rootElement = new VisualElement();

            var headerContainer = new VisualElement();
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.alignItems = Align.Center;
            headerContainer.style.paddingTop = 4;
            headerContainer.style.paddingBottom = 4;

            var config = (ConfigTableBase)target;
            var typeName = config.GetType().Name;

            var nameLabel = new Label(typeName);
            nameLabel.style.fontSize = 13;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.color = new Color(0.78f, 0.78f, 0.78f);
            nameLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            headerContainer.Add(nameLabel);

            headerContainer.Add(new VisualElement { style = { flexGrow = 1 } });

            var infoLabel = new Label($"{config.Count} 条记录");
            infoLabel.style.fontSize = 11;
            infoLabel.style.color = new Color(0.55f, 0.55f, 0.55f);
            headerContainer.Add(infoLabel);

            _rootElement.Add(headerContainer);

            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.19f, 0.19f, 0.19f);
            divider.style.marginTop = 4;
            divider.style.marginBottom = 4;
            _rootElement.Add(divider);

            return _rootElement;
        }
    }
}
#else
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Inspectors
{
    /// <summary>
    ///     配置表自定义编辑器（UIToolkit 默认实现）
    /// </summary>
    [CustomEditor(typeof(ConfigTableBase), true)]
    public class ConfigTableEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // 信息头区域
            var headerContainer = new VisualElement();
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.alignItems = Align.Center;
            headerContainer.style.paddingTop = 4;
            headerContainer.style.paddingBottom = 4;
            headerContainer.style.marginBottom = 2;

            var config = (ConfigTableBase)target;
            var typeName = config.GetType().Name;

            var nameLabel = new Label(typeName);
            nameLabel.style.fontSize = 13;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.color = new Color(0.78f, 0.78f, 0.78f);
            nameLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            headerContainer.Add(nameLabel);

            headerContainer.Add(new VisualElement { style = { flexGrow = 1 } });

            var countLabel = new Label($"{config.Count} 条记录");
            countLabel.style.fontSize = 11;
            countLabel.style.color = new Color(0.55f, 0.55f, 0.55f);
            headerContainer.Add(countLabel);

            root.Add(headerContainer);

            // 分割线
            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.19f, 0.19f, 0.19f);
            divider.style.marginTop = 4;
            divider.style.marginBottom = 4;
            root.Add(divider);

            // 使用默认属性绘制器显示序列化属性（排除 m_Script）
            var defaultInspector = new VisualElement();
            InspectorElement.DrawPropertiesExcluding(defaultInspector, serializedObject, this, "m_Script");
            root.Add(defaultInspector);

            return root;
        }
    }
}
#endif
