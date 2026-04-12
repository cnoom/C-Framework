using System;
using Sirenix.OdinInspector;

namespace CFramework
{
    /// <summary>
    ///     配置表基类
    /// </summary>
    public abstract class ConfigTableBase : SerializedScriptableObject
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