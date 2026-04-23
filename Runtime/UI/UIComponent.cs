using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CFramework.Runtime.UI
{
    /// <summary>
    ///     UI 组件绑定数据
    ///     <para>记录一个需要绑定到 IUI 实例的子物体组件信息</para>
    /// </summary>
    [Serializable]
    public class UIComponent
    {
        /// <summary>
        ///     组件类型名称（程序集限定名，用于序列化）
        /// </summary>
        [SerializeField] [HideInInspector] private string _typeName;

        /// <summary>
        ///     缓存的组件类型，避免每次 getter 调用 Type.GetType 反射
        /// </summary>
        private Type _cachedType;

        [Required] [LabelText("目标物体")] public GameObject gameObject;

        /// <summary>
        ///     名称（自动取目标物体名称）
        /// </summary>
        [ShowInInspector]
        [LabelText("名称")]
        [ReadOnly]
        public string Name => gameObject != null ? gameObject.name : string.Empty;

        /// <summary>
        ///     组件类型（运行时从 _typeName 解析，结果已缓存）
        /// </summary>
        [ShowInInspector]
        [LabelText("组件类型")]
        [ValueDropdown(nameof(AvailableComponentTypes))]
        public Type ComponentType
        {
            get
            {
                if (string.IsNullOrEmpty(_typeName)) return null;
                if (_cachedType == null)
                    _cachedType = Type.GetType(_typeName);
                return _cachedType;
            }
            set
            {
                _typeName = value?.AssemblyQualifiedName;
                _cachedType = value;
            }
        }

        /// <summary>
        ///     获取目标物体身上所有可绑定的组件类型
        /// </summary>
        private IEnumerable<Type> AvailableComponentTypes
        {
            get
            {
                if (gameObject == null) return Type.EmptyTypes;

                return gameObject.GetComponents<Component>()
                    .Select(c => c.GetType())
                    .Distinct();
            }
        }
    }
}