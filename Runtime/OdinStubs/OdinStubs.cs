#if !ODIN_INSPECTOR
// 当 Odin Inspector 未安装时，提供空类型替代，确保代码可编译
// 这些类型没有实际功能，仅用于编译兼容

using System;
using UnityEngine;

namespace Sirenix.OdinInspector
{
    /// <summary>
    ///     Odin 序列化 ScriptableObject 基类桩
    /// </summary>
    public class SerializedScriptableObject : ScriptableObject
    {
    }

    /// <summary>
    ///     按钮大小枚举桩
    /// </summary>
    public enum ButtonSizes
    {
        Small,
        Medium,
        Large,
        Gigantic
    }



    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class LabelTextAttribute : Attribute
    {
        public LabelTextAttribute(string label)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class ButtonAttribute : Attribute
    {
        public ButtonAttribute(string name = "", ButtonSizes sizes = ButtonSizes.Medium)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class PropertyOrderAttribute : Attribute
    {
        public PropertyOrderAttribute(float order)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class ShowIfAttribute : Attribute
    {
        public ShowIfAttribute(string condition)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class EnableIfAttribute : Attribute
    {
        public EnableIfAttribute(string condition)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class RequiredAttribute : Attribute
    {
        public RequiredAttribute(string errorMessage = "")
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class ShowInInspectorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class ReadOnlyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class ValueDropdownAttribute : Attribute
    {
        public bool IsUniqueList { get; set; }

        public ValueDropdownAttribute(string memberName)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class TableListAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class SearchableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class DisplayAsStringAttribute : Attribute
    {
        public bool Overflow { get; set; } = true;

        public DisplayAsStringAttribute()
        {
        }

        public DisplayAsStringAttribute(TextAlignment alignment)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class HideLabelAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class HorizontalGroupAttribute : Attribute
    {
        public HorizontalGroupAttribute(string group, float width = 0)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class BoxGroupAttribute : Attribute
    {
        public bool ShowLabel { get; set; } = true;

        public BoxGroupAttribute(string group)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class ListDrawerSettingsAttribute : Attribute
    {
        public bool ShowPaging { get; set; }
        public int NumberOfItemsPerPage { get; set; }
        public bool IsReadOnly { get; set; }
        public string OnTitleBarGUI { get; set; }
        public bool ShowIndexLabels { get; set; }
        public bool DraggableItems { get; set; } = true;
        public string CustomAddFunction { get; set; }
        public bool ShowItemCount { get; set; }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class OnValueChangedAttribute : Attribute
    {
        public OnValueChangedAttribute(string methodName)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class OnCollectionChangedAttribute : Attribute
    {
        public OnCollectionChangedAttribute(string methodName)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class TitleGroupAttribute : Attribute
    {
        public string Subtitle { get; set; } = "";

        public TitleGroupAttribute(string title)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class FolderPathAttribute : Attribute
    {
        public bool RequireExistingPath { get; set; }
        public bool UseBackslashes { get; set; }
        public bool AbsolutePath { get; set; }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class FoldoutGroupAttribute : Attribute
    {
        public FoldoutGroupAttribute(string group)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class GUIColorAttribute : Attribute
    {
        public GUIColorAttribute(float r, float g, float b, float a = 1f)
        {
        }
    }
}

namespace Sirenix.Serialization
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class OdinSerializeAttribute : Attribute
    {
    }
}
#endif
