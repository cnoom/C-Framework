using CFramework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Inspectors
{
    /// <summary>
    ///     ConfigTableAsset 自定义编辑器
    ///     <para>显示配置表类型名 + 记录数标题栏 + 序列化属性编辑</para>
    /// </summary>
    [CustomEditor(typeof(ConfigTableAsset), true)]
    public class ConfigTableAssetEditor : UnityEditor.Editor
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

            var config = (ConfigTableAsset)target;
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

            // 使用 PropertyField 显示序列化属性（排除 m_Script）
            var defaultInspector = new VisualElement();
            var prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.propertyPath == "m_Script") continue;
                    var field = new PropertyField(prop);
                    field.Bind(serializedObject);
                    defaultInspector.Add(field);
                } while (prop.NextVisible(false));
            }

            root.Add(defaultInspector);

            return root;
        }
    }
}
