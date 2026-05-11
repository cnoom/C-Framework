using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     对象池模块配置
    /// </summary>
    [CreateAssetMenu(fileName = "PoolSettings", menuName = "CFramework/Pool Settings")]
    public sealed class PoolSettings : ScriptableObject
    {
        private const string DefaultPath = "PoolSettings";

        [Tooltip("对象池默认初始容量")]
        public int PoolDefaultCapacity = 10;

        [Tooltip("对象池默认最大容量（0=不限）")]
        public int PoolMaxSize = 100;

        /// <summary>
        ///     加载默认配置
        /// </summary>
        public static PoolSettings LoadDefault()
        {
            var settings = Resources.Load<PoolSettings>(DefaultPath);
            if (settings == null)
            {
                settings = CreateInstance<PoolSettings>();
                LogUtility.Debug("CFramework",
                    $"{nameof(PoolSettings)} 未在 Resources/{DefaultPath} 找到，使用默认值");
            }

            return settings;
        }
    }
}
