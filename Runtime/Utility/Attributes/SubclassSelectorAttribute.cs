#if !CNOOM_UNITY_TOOL
using UnityEngine;

namespace CNoom.UnityTool
{
    /// <summary>
    ///     本地存根：当 CNoom.UnityTool 包未安装时，提供空的 SubclassSelectorAttribute。
    ///     <para>功能：在 Inspector 中为 [SerializeReference] 字段提供子类选择下拉框。</para>
    ///     <para>若安装了 CNoom.UnityTool 包，请在项目 Player Settings 中添加定义符号 CNOOM_UNITY_TOOL 以使用完整版本。</para>
    /// </summary>
    public class SubclassSelectorAttribute : PropertyAttribute
    {
    }
}
#endif
