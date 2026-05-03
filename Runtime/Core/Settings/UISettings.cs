using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     UI 模块配置
    /// </summary>
    [CreateAssetMenu(fileName = "UISettings", menuName = "CFramework/UI Settings")]
    public sealed class UISettings : ScriptableObject
    {
        private const string DefaultPath = "UISettings";

        [Tooltip("导航栈最大容量")]
        public int MaxNavigationStack = 10;

        [Tooltip("UIRoot Prefab 的 Addressable Key")]
        public string UIRootAddress = "UIRoot";

        /// <summary>
        ///     加载默认配置
        /// </summary>
        public static UISettings LoadDefault()
        {
            var settings = Resources.Load<UISettings>(DefaultPath);
            if (settings == null)
            {
                settings = CreateInstance<UISettings>();
                LogUtility.Debug("CFramework",
                    $"{nameof(UISettings)} 未在 Resources/{DefaultPath} 找到，使用默认值");
            }

            return settings;
        }
    }
}
