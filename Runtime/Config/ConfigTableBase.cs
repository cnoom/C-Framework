using System;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CFramework
{
    /// <summary>
    ///     配置表基类
    /// </summary>
#if ODIN_INSPECTOR
    public abstract class ConfigTableBase : SerializedScriptableObject
#else
    public abstract class ConfigTableBase : ScriptableObject
#endif
    {
        public bool IsLoaded { get; set; }
        public abstract int Count { get; }
        public ConfigDataSource Source { get; set; } = ConfigDataSource.ScriptableObject;

        /// <summary>
        ///     数据加载完成事件
        /// </summary>
        public event Action OnDataLoaded;

        protected void NotifyDataLoaded()
        {
            OnDataLoaded?.Invoke();
        }
    }
}
