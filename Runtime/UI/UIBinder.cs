using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace CFramework.Runtime.UI
{
    /// <summary>
    ///     UI 绑定器，挂载在 UI Prefab 根节点上
    ///     <para>作为组件引用的容器，在 InjectUI 时将组件注入到 IUI 实例中，之后不再使用</para>
    /// </summary>
    public class UIBinder : MonoBehaviour
    {
        [SerializeField] [LabelText("组件列表")] private UIComponent[] _components;

        /// <summary>
        ///     组件列表
        /// </summary>
        public UIComponent[] Components => _components;

        /// <summary>
        ///     按索引获取指定类型的组件
        ///     <para>用于代码生成器生成的 InjectUI 方法中</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="index">组件索引</param>
        /// <returns>组件实例，失败返回 null</returns>
        public T Get<T>(int index) where T : Component
        {
            if (index < 0 || index >= _components.Length) return null;

            return _components[index].gameObject?.GetComponent<T>();
        }

        /// <summary>
        ///     按名称获取指定类型的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="name">组件名称</param>
        /// <returns>组件实例，失败返回 null</returns>
        public T Get<T>(string name) where T : Component
        {
            for (var i = 0; i < _components.Length; i++)
                if (_components[i].Name == name)
                    return _components[i].gameObject?.GetComponent<T>();

            return null;
        }

        /// <summary>
        ///     按索引获取目标 GameObject
        /// </summary>
        /// <param name="index">组件索引</param>
        /// <returns>目标 GameObject，失败返回 null</returns>
        public GameObject GetGameObject(int index)
        {
            if (index < 0 || index >= _components.Length) return null;

            return _components[index].gameObject;
        }

        /// <summary>
        ///     设置组件列表（由编辑器生成器调用）
        /// </summary>
        public void SetComponents(UIComponent[] components)
        {
            _components = components;
        }

#if UNITY_EDITOR
        [Button("生成绑定代码", ButtonSizes.Large)]
        [PropertyOrder(100)]
        [ShowIf(nameof(HasValidComponents))]
        private void GenerateBindingCode()
        {
            // 获取 Prefab 名称
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            var prefabName = prefabStage != null
                ? prefabStage.prefabContentsRoot.name
                : gameObject.name;

            // 通过反射调用 Editor 程序集中的 UIPanelGenerator.Generate
            // Runtime 程序集无法直接引用 Editor 程序集，必须用反射
            var type = Type.GetType("CFramework.Editor.Generators.UIPanelGenerator, CFramework.Editor");
            if (type == null)
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType("CFramework.Editor.Generators.UIPanelGenerator");
                    if (type != null) break;
                }

            if (type != null)
            {
                var method = type.GetMethod("Generate",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(UIBinder), typeof(string) },
                    null);

                method?.Invoke(null, new object[] { this, prefabName });
            }
            else
            {
                Debug.LogError("[UIBinder] 未找到 UIPanelGenerator 类型，请确认 Editor 程序集已编译");
            }
        }

        /// <summary>
        ///     是否有有效组件（用于控制按钮显示）
        /// </summary>
        private bool HasValidComponents()
        {
            if (_components == null || _components.Length == 0) return false;

            foreach (var comp in _components)
                if (comp.ComponentType != null && comp.gameObject != null)
                    return true;

            return false;
        }
#endif
    }
}