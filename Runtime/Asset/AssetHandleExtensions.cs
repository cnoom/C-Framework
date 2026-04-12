using System.Collections.Generic;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     AssetHandle 扩展方法
    /// </summary>
    public static class AssetHandleExtensions
    {
        /// <summary>
        ///     绑定到GameObject生命周期
        /// </summary>
        public static void AddTo(this AssetHandle handle, GameObject gameObject)
        {
            if (gameObject == null || !handle.Asset) return;

            var disposer = GetOrAddComponent<DisposeOnDestroy>(gameObject);
            disposer.Add(handle);
        }

        /// <summary>
        ///     绑定到MonoBehaviour生命周期
        /// </summary>
        public static void AddTo(this AssetHandle handle, MonoBehaviour behaviour)
        {
            if (behaviour != null) handle.AddTo(behaviour.gameObject);
        }

        /// <summary>
        ///     获取或添加组件
        /// </summary>
        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null) component = go.AddComponent<T>();
            return component;
        }
    }

    /// <summary>
    ///     销毁时释放资源
    /// </summary>
    internal sealed class DisposeOnDestroy : MonoBehaviour
    {
        private readonly List<AssetHandle> _handles = new();

        private void OnDestroy()
        {
            foreach (var handle in _handles) handle.Dispose();
            _handles.Clear();
        }

        public void Add(AssetHandle handle)
        {
            _handles.Add(handle);
        }
    }
}