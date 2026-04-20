using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CFramework.Utility.Serialization;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CFramework.Editor.Utilities
{
    /// <summary>
    /// SubclassSelector 的 PropertyDrawer 实现。
    /// 为 [SerializeReference] + [SubclassSelector] 字段提供子类型下拉选择器。
    /// 支持单个字段、数组/List、以及 SerializableDictionary 中的值类型。
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        // 缓存：字段类型 → 可选的具体类型列表
        private static readonly Dictionary<Type, List<Type>> _typeCache = new();

        // 缓存：字段类型 → 类型显示名列表
        private static readonly Dictionary<Type, string[]> _displayNameCache = new();

        // 缓存：属性路径 → ReorderableList（列表/数组场景）
        private static readonly Dictionary<string, ReorderableList> _listCache = new();

        #region 入口

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 检测列表/数组：需要用 ReorderableList 为每个元素单独提供子类选择器
            if (IsArrayOrList(property))
            {
                DrawListField(position, property, label);
                return;
            }

            DrawSingleField(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (IsArrayOrList(property))
            {
                return GetListFieldHeight(property, label);
            }

            return GetSingleFieldHeight(property);
        }

        #endregion

        #region 列表/数组渲染

        private void DrawListField(Rect position, SerializedProperty property, GUIContent label)
        {
            var elementType = GetElementTypeInfo(property);
            if (elementType == null || (!elementType.IsInterface && !elementType.IsAbstract))
            {
                // 元素类型不是接口/抽象，回退默认绘制
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var list = GetOrCreateList(property, label, elementType);
            list.DoList(position);
        }

        private float GetListFieldHeight(SerializedProperty property, GUIContent label)
        {
            var elementType = GetElementTypeInfo(property);
            if (elementType == null || (!elementType.IsInterface && !elementType.IsAbstract))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            var list = GetOrCreateList(property, label, elementType);
            return list.GetHeight();
        }

        /// <summary>
        /// 获取或创建列表对应的 ReorderableList，缓存以避免每帧重建
        /// </summary>
        private ReorderableList GetOrCreateList(SerializedProperty property, GUIContent label, Type elementType)
        {
            var path = property.propertyPath;

            if (_listCache.TryGetValue(path, out var list))
            {
                // 更新 serializedProperty 引用（可能因撤销/重做而变化）
                list.serializedProperty = property;
                return list;
            }

            var concreteTypes = GetConcreteTypes(elementType);
            var displayNames = GetDisplayNames(elementType);
            var attr = (SubclassSelectorAttribute)attribute;

            list = new ReorderableList(property.serializedObject, property, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    var sizeRect = new Rect(rect.x + rect.width - 100, rect.y, 100, rect.height);
                    EditorGUI.LabelField(rect, label);
                    EditorGUI.LabelField(sizeRect, $"Count: {property.arraySize}",
                        new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight });
                },

                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    DrawListElement(rect, element, index, elementType, concreteTypes, displayNames);
                },

                elementHeightCallback = index =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    return GetListElementHeight(element, elementType, concreteTypes);
                },

                onAddDropdownCallback = (buttonRect, reorderableList) =>
                {
                    // 添加新元素时显示类型选择菜单
                    var menu = new GenericMenu();
                    for (int i = 1; i < concreteTypes.Count; i++)
                    {
                        var type = concreteTypes[i];
                        menu.AddItem(new GUIContent(displayNames[i]), false, () =>
                        {
                            property.arraySize++;
                            var newElement = property.GetArrayElementAtIndex(property.arraySize - 1);
                            newElement.managedReferenceValue = Activator.CreateInstance(type);
                            property.serializedObject.ApplyModifiedProperties();
                        });
                    }

                    // 也支持添加 null 元素
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("None (Null)"), false, () =>
                    {
                        property.arraySize++;
                        property.serializedObject.ApplyModifiedProperties();
                    });

                    menu.DropDown(buttonRect);
                }
            };

            _listCache[path] = list;
            return list;
        }

        /// <summary>
        /// 绘制列表中的单个元素：类型选择 Popup + 子属性
        /// </summary>
        private void DrawListElement(Rect rect, SerializedProperty element, int index,
            Type elementType, List<Type> concreteTypes, string[] displayNames)
        {
            // ReorderableList 的元素区域需要微调
            rect.y += 2;

            // 类型选择 Popup
            var selectedIndex = GetSelectedIndex(element, concreteTypes);
            var popupRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(rect, new GUIContent($"Element {index}"), element);

            var newIndex = EditorGUI.Popup(popupRect, $"Element {index}", selectedIndex, displayNames);
            if (newIndex != selectedIndex)
            {
                var selectedType = concreteTypes[newIndex];
                element.managedReferenceValue = selectedType == null
                    ? null
                    : Activator.CreateInstance(selectedType);
                element.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();

            // 子属性
            if (element.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                var yOffset = popupRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                var childProperty = element.Copy();
                var childEnd = element.GetEndProperty();
                var enterChildren = true;

                while (childProperty.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(childProperty, childEnd))
                        break;

                    var childHeight = EditorGUI.GetPropertyHeight(childProperty, true);
                    var childRect = new Rect(rect.x, yOffset, rect.width, childHeight);
                    EditorGUI.PropertyField(childRect, childProperty, true);
                    yOffset += childHeight + EditorGUIUtility.standardVerticalSpacing;
                    enterChildren = false;
                }

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// 计算列表中单个元素的总高度
        /// </summary>
        private float GetListElementHeight(SerializedProperty element, Type elementType,
            List<Type> concreteTypes)
        {
            float height = EditorGUIUtility.singleLineHeight + 4; // 4 = 上下 padding

            if (element.managedReferenceValue != null)
            {
                height += EditorGUIUtility.standardVerticalSpacing;

                var childProperty = element.Copy();
                var childEnd = element.GetEndProperty();
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

        #endregion

        #region 单字段渲染

        private void DrawSingleField(Rect position, SerializedProperty property, GUIContent label)
        {
            var state = PrepareState(property);
            if (state.FieldType == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (state.ConcreteTypes == null || state.ConcreteTypes.Count == 0)
            {
                EditorGUI.LabelField(position, label.text,
                    $"未找到 {state.FieldType.Name} 的 [Serializable] 实现类");
                return;
            }

            // 类型选择 Popup
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

            // 子属性
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

        private float GetSingleFieldHeight(SerializedProperty property)
        {
            var state = PrepareState(property);
            if (state.FieldType == null)
            {
                return EditorGUI.GetPropertyHeight(property, GUIContent.none, true);
            }

            if (state.ConcreteTypes == null || state.ConcreteTypes.Count == 0)
            {
                return EditorGUIUtility.singleLineHeight;
            }

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

        #endregion

        #region 工具方法

        /// <summary>
        /// 判断属性是否为数组或列表
        /// </summary>
        private static bool IsArrayOrList(SerializedProperty property)
        {
            return property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        /// <summary>
        /// 获取列表/数组字段的元素类型信息（提取泛型参数或数组元素类型）
        /// </summary>
        private Type GetElementTypeInfo(SerializedProperty property)
        {
            var fieldInfo = GetFieldInfo(property, out var fieldType);
            if (fieldInfo == null) return null;

            if (fieldType.IsArray)
                return fieldType.GetElementType();

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                return fieldType.GetGenericArguments()[0];

            return null;
        }

        /// <summary>
        /// 获取或缓存具体类型列表
        /// </summary>
        private static List<Type> GetConcreteTypes(Type baseType)
        {
            if (!_typeCache.TryGetValue(baseType, out var types))
            {
                types = FindConcreteTypes(baseType);
                _typeCache[baseType] = types;
            }

            return types;
        }

        /// <summary>
        /// 获取或缓存显示名称
        /// </summary>
        private string[] GetDisplayNames(Type baseType)
        {
            if (!_displayNameCache.TryGetValue(baseType, out var names))
            {
                var concreteTypes = GetConcreteTypes(baseType);
                var attr = (SubclassSelectorAttribute)attribute;
                names = BuildDisplayNames(concreteTypes, attr.ShowFullNamespace);
                _displayNameCache[baseType] = names;
            }

            return names;
        }

        /// <summary>
        /// 获取当前 managedReferenceValue 在类型列表中的索引
        /// </summary>
        private static int GetSelectedIndex(SerializedProperty property, List<Type> concreteTypes)
        {
            var value = property.managedReferenceValue;
            if (value == null) return 0;

            var index = concreteTypes.IndexOf(value.GetType());
            return index < 0 ? 0 : index;
        }

        /// <summary>
        /// 准备单字段的绘制状态
        /// </summary>
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

            state.ConcreteTypes = GetConcreteTypes(state.FieldType);
            state.DisplayNames = GetDisplayNames(state.FieldType);
            state.SelectedIndex = GetSelectedIndex(property, state.ConcreteTypes);

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
        /// 当程序集重新加载时清除所有缓存
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearCacheOnLoad()
        {
            _typeCache.Clear();
            _displayNameCache.Clear();
            _listCache.Clear();
        }

        private struct DrawerState
        {
            public int SelectedIndex;
            public Type FieldType;
            public List<Type> ConcreteTypes;
            public string[] DisplayNames;
        }

        #endregion
    }
}
