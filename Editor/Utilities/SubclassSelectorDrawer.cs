using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CFramework.Utility.Serialization;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Utilities
{
    /// <summary>
    /// SubclassSelector 的 DecorateDrawer 实现。
    /// 为 [SerializeReference] + [SubclassSelector] 字段提供子类型下拉选择器。
    /// 使用 DecoratorDrawer 模式在属性上方插入类型选择 Popup，
    /// 子属性绘制完全交给 Unity 默认处理，确保列表/字典等容器内也能正常工作。
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        // 缓存：字段类型 → 可选的具体类型列表
        private static readonly Dictionary<Type, List<Type>> _typeCache = new();

        // 缓存：字段类型 → 类型显示名列表
        private static readonly Dictionary<Type, string[]> _displayNameCache = new();

        // 每个属性的临时状态
        private struct DrawerState
        {
            public int SelectedIndex;
            public Type FieldType;
            public List<Type> ConcreteTypes;
            public string[] DisplayNames;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var state = PrepareState(property);
            if (state.FieldType == null)
            {
                // 非 接口/抽象类型 时静默回退到默认绘制
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (state.ConcreteTypes == null || state.ConcreteTypes.Count == 0)
            {
                EditorGUI.LabelField(position, label.text,
                    $"未找到 {state.FieldType.Name} 的 [Serializable] 实现类");
                return;
            }

            // 第一行：类型选择弹出菜单
            var popupRect = new Rect(position.x, position.y, position.width,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(position, label, property);

            var newIndex = EditorGUI.Popup(popupRect, label.text,
                state.SelectedIndex, state.DisplayNames);

            if (newIndex != state.SelectedIndex)
            {
                var selectedType = state.ConcreteTypes[newIndex];
                property.managedReferenceValue = selectedType == null
                    ? null
                    : Activator.CreateInstance(selectedType);
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();

            // 子属性：使用 Unity 默认绘制，确保列表/字典容器中正常工作
            if (property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                var childProperty = property.Copy();
                var childEnd = property.GetEndProperty();
                var enterChildren = true;
                var yOffset = popupRect.yMax + EditorGUIUtility.standardVerticalSpacing;

                while (childProperty.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(childProperty, childEnd))
                        break;

                    var childHeight = EditorGUI.GetPropertyHeight(childProperty, true);
                    var childRect = new Rect(position.x, yOffset, position.width, childHeight);

                    EditorGUI.PropertyField(childRect, childProperty, true);
                    yOffset += childHeight + EditorGUIUtility.standardVerticalSpacing;
                    enterChildren = false;
                }

                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var state = PrepareState(property);
            if (state.FieldType == null)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            if (state.ConcreteTypes == null || state.ConcreteTypes.Count == 0)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            // 基础高度 = 类型选择 Popup 的一行
            float height = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue != null)
            {
                height += EditorGUIUtility.standardVerticalSpacing;

                var childProperty = property.Copy();
                var childEnd = property.GetEndProperty();
                var enterChildren = true;

                while (childProperty.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(childProperty, childEnd))
                        break;

                    height += EditorGUI.GetPropertyHeight(childProperty, true)
                              + EditorGUIUtility.standardVerticalSpacing;
                    enterChildren = false;
                }
            }

            return height;
        }

        private DrawerState PrepareState(SerializedProperty property)
        {
            var state = new DrawerState();

            var fieldInfo = GetFieldInfo(property, out _);
            if (fieldInfo == null) return state;

            state.FieldType = fieldInfo.FieldType;

            // 接口或抽象类才启用选择器
            if (!state.FieldType.IsInterface && !state.FieldType.IsAbstract)
            {
                state.FieldType = null;
                return state;
            }

            // 获取或缓存具体类型列表
            if (!_typeCache.TryGetValue(state.FieldType, out var concreteTypes))
            {
                concreteTypes = FindConcreteTypes(state.FieldType);
                _typeCache[state.FieldType] = concreteTypes;
            }

            state.ConcreteTypes = concreteTypes;

            // 获取或缓存显示名
            if (!_displayNameCache.TryGetValue(state.FieldType, out var displayNames))
            {
                var attr = (SubclassSelectorAttribute)attribute;
                displayNames = BuildDisplayNames(concreteTypes, attr.ShowFullNamespace);
                _displayNameCache[state.FieldType] = displayNames;
            }

            state.DisplayNames = displayNames;

            // 确定当前选中索引
            var currentValue = property.managedReferenceValue;
            if (currentValue == null)
            {
                state.SelectedIndex = 0; // "None (Null)"
            }
            else
            {
                var currentType = currentValue.GetType();
                state.SelectedIndex = concreteTypes.IndexOf(currentType);
                if (state.SelectedIndex < 0) state.SelectedIndex = 0;
            }

            return state;
        }

        /// <summary>
        /// 查找所有实现了目标接口/抽象类的 [Serializable] 具体类型
        /// </summary>
        private static List<Type> FindConcreteTypes(Type baseType)
        {
            var result = new List<Type>();

            // 第 0 项始终是 null（清除选择）
            result.Add(null);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface) continue;
                    if (!baseType.IsAssignableFrom(type)) continue;
                    if (!type.IsSerializable && type.GetCustomAttribute<SerializableAttribute>(false) == null)
                        continue;
                    // 排除 Unity 内置类型
                    if (typeof(UnityEngine.Object).IsAssignableFrom(type)) continue;

                    result.Add(type);
                }
            }

            return result;
        }

        /// <summary>
        /// 构建下拉菜单的显示名称数组
        /// </summary>
        private static string[] BuildDisplayNames(List<Type> types, bool showFullNamespace)
        {
            var names = new string[types.Count];
            for (int i = 0; i < types.Count; i++)
            {
                names[i] = types[i] == null
                    ? "None (Null)"
                    : showFullNamespace
                        ? types[i].FullName
                        : types[i].Name;
            }

            return names;
        }

        /// <summary>
        /// 获取 SerializedProperty 对应的 FieldInfo。
        /// 支持嵌套路径、数组/列表元素、SerializableDictionary 的 _pairs[i].Value 路径。
        /// </summary>
        private static FieldInfo GetFieldInfo(SerializedProperty property, out Type fieldType)
        {
            fieldType = null;
            var targetObject = property.serializedObject.targetObject;
            if (targetObject == null) return null;

            var type = targetObject.GetType();
            var path = property.propertyPath.Replace(".Array.data[", "[");

            FieldInfo fieldInfo = null;
            var parts = path.Split('.');

            foreach (var part in parts)
            {
                // 处理数组/列表索引，如 [0]
                if (part.StartsWith("["))
                {
                    continue;
                }

                fieldInfo = null;
                var searchType = type;

                while (searchType != null && searchType != typeof(object))
                {
                    fieldInfo = searchType.GetField(part,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo != null) break;
                    searchType = searchType.BaseType;
                }

                if (fieldInfo == null) return null;

                type = fieldInfo.FieldType;

                // 如果是数组，取元素类型
                if (type.IsArray)
                {
                    type = type.GetElementType();
                }
                // 如果是 List<T>，取元素类型 T
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    type = type.GetGenericArguments()[0];
                }
            }

            if (fieldInfo != null)
            {
                fieldType = fieldInfo.FieldType;
            }

            return fieldInfo;
        }

        /// <summary>
        /// 当程序集重新加载时清除缓存
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearCacheOnLoad()
        {
            _typeCache.Clear();
            _displayNameCache.Clear();
        }
    }
}
