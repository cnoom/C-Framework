using System;
using System.Collections;
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
    /// 支持单个字段、数组/List（含 ManagedReference 模式）、以及 SerializableDictionary 中的值类型。
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        private static readonly Dictionary<Type, List<Type>> _typeCache = new();
        private static readonly Dictionary<Type, string[]> _displayNameCache = new();

        // SerializedProperty.isArray == true 时的 ReorderableList 缓存
        private static readonly Dictionary<string, ReorderableList> _listCache = new();

        // ManagedReference 列表的折叠状态缓存
        private static readonly Dictionary<string, bool> _foldoutCache = new();

        #region 入口

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var (isList, elementType) = CheckIfListField(property);

            if (isList && elementType != null)
            {
                if (property.isArray && property.propertyType != SerializedPropertyType.String)
                    DrawNativeListField(position, property, label, elementType);
                else
                    DrawManagedReferenceListField(position, property, label, elementType);
                return;
            }

            DrawSingleField(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var (isList, elementType) = CheckIfListField(property);

            if (isList && elementType != null)
            {
                if (property.isArray && property.propertyType != SerializedPropertyType.String)
                    return GetNativeListHeight(property, label, elementType);
                else
                    return GetManagedReferenceListHeight(property, label, elementType);
            }

            return GetSingleFieldHeight(property);
        }

        #endregion

        #region 原生列表渲染（property.isArray == true）

        private void DrawNativeListField(Rect position, SerializedProperty property,
            GUIContent label, Type elementType)
        {
            if (!elementType.IsInterface && !elementType.IsAbstract)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var list = GetOrCreateReorderableList(property, label, elementType);
            list.DoList(position);
        }

        private float GetNativeListHeight(SerializedProperty property, GUIContent label, Type elementType)
        {
            if (!elementType.IsInterface && !elementType.IsAbstract)
                return EditorGUI.GetPropertyHeight(property, label, true);

            var list = GetOrCreateReorderableList(property, label, elementType);
            return list.GetHeight();
        }

        private ReorderableList GetOrCreateReorderableList(SerializedProperty property,
            GUIContent label, Type elementType)
        {
            var path = property.propertyPath;

            if (_listCache.TryGetValue(path, out var list))
            {
                list.serializedProperty = property;
                return list;
            }

            var concreteTypes = GetConcreteTypes(elementType);
            var displayNames = GetDisplayNames(elementType);

            list = new ReorderableList(property.serializedObject, property, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, label);
                },

                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    DrawElementWithSelector(rect, element, index, concreteTypes, displayNames);
                },

                elementHeightCallback = index =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    return GetElementHeight(element);
                },

                onAddDropdownCallback = (buttonRect, _) =>
                {
                    ShowAddTypeMenu(buttonRect, concreteTypes, displayNames, type =>
                    {
                        property.arraySize++;
                        var newElement = property.GetArrayElementAtIndex(property.arraySize - 1);
                        newElement.managedReferenceValue = Activator.CreateInstance(type);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
            };

            _listCache[path] = list;
            return list;
        }

        #endregion

        #region ManagedReference 列表渲染（[SerializeReference] List<T>）

        private void DrawManagedReferenceListField(Rect position, SerializedProperty property,
            GUIContent label, Type elementType)
        {
            if (!elementType.IsInterface && !elementType.IsAbstract)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var concreteTypes = GetConcreteTypes(elementType);
            var displayNames = GetDisplayNames(elementType);
            if (concreteTypes.Count <= 1)
            {
                EditorGUI.LabelField(position, label.text,
                    $"未找到 {elementType.Name} 的 [Serializable] 实现类");
                return;
            }

            // 获取 IList 引用
            var list = property.managedReferenceValue as IList;
            var path = property.propertyPath;
            var foldout = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                _foldoutCache.GetValueOrDefault(path, false),
                $"{label.text} ({list?.Count ?? 0})", true);

            _foldoutCache[path] = foldout;

            // 添加按钮行
            var buttonRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight,
                position.width, EditorGUIUtility.singleLineHeight);

            if (GUI.Button(buttonRect, "+ 添加元素"))
            {
                ShowAddTypeMenu(buttonRect, concreteTypes, displayNames, type =>
                {
                    if (list == null)
                    {
                        // 创建列表实例
                        var listType = typeof(List<>).MakeGenericType(elementType);
                        list = (IList)Activator.CreateInstance(listType);
                        property.managedReferenceValue = list;
                    }

                    list.Add(Activator.CreateInstance(type));
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            if (!foldout || list == null) return;

            // 绘制每个元素
            EditorGUI.indentLevel++;
            var yOffset = buttonRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < list.Count; i++)
            {
                // 查找对应元素的 SerializedProperty
                // ManagedReference 列表的元素通过 "Array.data[i]" 访问
                var elementProp = property.FindPropertyRelative($"Array.data[{i}]");
                if (elementProp == null) continue;

                var elementHeight = GetElementHeight(elementProp);
                var elementRect = new Rect(position.x, yOffset, position.width, elementHeight);

                // 删除按钮区域
                var deleteRect = new Rect(elementRect.xMax - 20, elementRect.y, 20,
                    EditorGUIUtility.singleLineHeight);

                DrawElementWithSelector(elementRect, elementProp, i, concreteTypes, displayNames);

                if (GUI.Button(deleteRect, "×"))
                {
                    list.RemoveAt(i);
                    property.serializedObject.ApplyModifiedProperties();
                    break;
                }

                yOffset += elementHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.indentLevel--;
        }

        private float GetManagedReferenceListHeight(SerializedProperty property,
            GUIContent label, Type elementType)
        {
            if (!elementType.IsInterface && !elementType.IsAbstract)
                return EditorGUI.GetPropertyHeight(property, label, true);

            var concreteTypes = GetConcreteTypes(elementType);
            if (concreteTypes.Count <= 1)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight; // Foldout
            height += EditorGUIUtility.singleLineHeight;      // 添加按钮

            var path = property.propertyPath;
            if (!_foldoutCache.GetValueOrDefault(path, false))
                return height;

            var list = property.managedReferenceValue as IList;
            if (list == null) return height;

            height += EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < list.Count; i++)
            {
                var elementProp = property.FindPropertyRelative($"Array.data[{i}]");
                if (elementProp != null)
                    height += GetElementHeight(elementProp) + EditorGUIUtility.standardVerticalSpacing;
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
                var yOffset = popupRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                DrawChildProperties(position, property, ref yOffset);
                EditorGUI.indentLevel--;
            }
        }

        private float GetSingleFieldHeight(SerializedProperty property)
        {
            var state = PrepareState(property);
            if (state.FieldType == null)
                return EditorGUI.GetPropertyHeight(property, GUIContent.none, true);

            if (state.ConcreteTypes == null || state.ConcreteTypes.Count == 0)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue != null)
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += GetChildPropertiesHeight(property);
            }

            return height;
        }

        #endregion

        #region 元素绘制（共用）

        /// <summary>
        /// 绘制带子类选择器的列表元素
        /// </summary>
        private void DrawElementWithSelector(Rect rect, SerializedProperty element, int index,
            List<Type> concreteTypes, string[] displayNames)
        {
            rect.y += 2;

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

            if (element.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                var yOffset = popupRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                DrawChildProperties(rect, element, ref yOffset);
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// 计算带子类选择器的元素高度
        /// </summary>
        private float GetElementHeight(SerializedProperty element)
        {
            float height = EditorGUIUtility.singleLineHeight + 4;

            if (element.managedReferenceValue != null)
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += GetChildPropertiesHeight(element);
            }

            return height;
        }

        /// <summary>
        /// 绘制 managed reference 的子属性
        /// </summary>
        private static void DrawChildProperties(Rect container, SerializedProperty property,
            ref float yOffset)
        {
            var childProperty = property.Copy();
            var childEnd = property.GetEndProperty();
            var enterChildren = true;

            while (childProperty.NextVisible(enterChildren))
            {
                if (SerializedProperty.EqualContents(childProperty, childEnd))
                    break;

                var childHeight = EditorGUI.GetPropertyHeight(childProperty, true);
                var childRect = new Rect(container.x, yOffset, container.width, childHeight);
                EditorGUI.PropertyField(childRect, childProperty, true);
                yOffset += childHeight + EditorGUIUtility.standardVerticalSpacing;
                enterChildren = false;
            }
        }

        /// <summary>
        /// 计算 managed reference 子属性的总高度
        /// </summary>
        private static float GetChildPropertiesHeight(SerializedProperty property)
        {
            float height = 0;
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

            return height;
        }

        /// <summary>
        /// 显示添加类型的选择菜单
        /// </summary>
        private static void ShowAddTypeMenu(Rect buttonRect, List<Type> concreteTypes,
            string[] displayNames, Action<Type> onSelected)
        {
            var menu = new GenericMenu();
            for (int i = 1; i < concreteTypes.Count; i++)
            {
                var type = concreteTypes[i];
                menu.AddItem(new GUIContent(displayNames[i]), false, () => onSelected(type));
            }

            menu.DropDown(buttonRect);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 通过反射判断字段是否为数组/列表类型，并获取元素类型。
        /// 不依赖 property.isArray，以支持 [SerializeReference] List&lt;T&gt; 场景。
        /// </summary>
        private (bool isList, Type elementType) CheckIfListField(SerializedProperty property)
        {
            var fi = GetFieldInfo(property, out var fieldType);
            if (fi == null) return (false, null);

            if (fieldType.IsArray)
                return (true, fieldType.GetElementType());

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                return (true, fieldType.GetGenericArguments()[0]);

            return (false, null);
        }

        private static List<Type> GetConcreteTypes(Type baseType)
        {
            if (!_typeCache.TryGetValue(baseType, out var types))
            {
                types = FindConcreteTypes(baseType);
                _typeCache[baseType] = types;
            }

            return types;
        }

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

        private static int GetSelectedIndex(SerializedProperty property, List<Type> concreteTypes)
        {
            var value = property.managedReferenceValue;
            if (value == null) return 0;

            var index = concreteTypes.IndexOf(value.GetType());
            return index < 0 ? 0 : index;
        }

        private DrawerState PrepareState(SerializedProperty property)
        {
            var state = new DrawerState();

            var fi = GetFieldInfo(property, out _);
            if (fi == null) return state;

            state.FieldType = fi.FieldType;

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

        private static List<Type> FindConcreteTypes(Type baseType)
        {
            var result = new List<Type> { null }; // 第 0 项为 null（清除选择）

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
                if (part.StartsWith("["))
                    continue;

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

                if (type.IsArray)
                {
                    type = type.GetElementType();
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    type = type.GetGenericArguments()[0];
                }
            }

            if (fieldInfo != null)
                fieldType = fieldInfo.FieldType;

            return fieldInfo;
        }

        [InitializeOnLoadMethod]
        private static void ClearCacheOnLoad()
        {
            _typeCache.Clear();
            _displayNameCache.Clear();
            _listCache.Clear();
            _foldoutCache.Clear();
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
