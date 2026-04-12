using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace CFramework.Editor.Inspectors
{
    /// <summary>
    ///     配置表自定义编辑器
    /// </summary>
    [CustomEditor(typeof(ConfigTableBase), true)]
    public class ConfigTableEditor : OdinEditor
    {
        // 使用 Odin 默认的 Inspector 显示
        // 如需自定义功能，可在此扩展
    }
}