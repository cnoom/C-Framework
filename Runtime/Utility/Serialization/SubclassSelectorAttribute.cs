using UnityEngine;

namespace CFramework.Utility.Serialization
{
    /// <summary>
    /// 标记在 [SerializeReference] 字段上，在 Inspector 中提供子类型下拉选择器。
    /// <para>
    /// 仅用于接口或抽象类型字段，自动搜索所有实现了该类型的 [Serializable] 具体类。
    /// </para>
    /// <example>
    /// [SerializeReference, SubclassSelector]
    /// private IWeapon _weapon;
    /// </example>
    /// </summary>
    public class SubclassSelectorAttribute : PropertyAttribute
    {
        /// <summary>
        /// 是否在弹出的下拉菜单中显示类型的完整命名空间。
        /// 默认为 false，仅显示类名。
        /// </summary>
        public bool ShowFullNamespace { get; set; }
    }
}
