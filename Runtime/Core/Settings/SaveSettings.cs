using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     存档模块配置
    /// </summary>
    [CreateAssetMenu(fileName = "SaveSettings", menuName = "CFramework/Save Settings")]
    public sealed class SaveSettings : ScriptableObject
    {
        private const string DefaultPath = "SaveSettings";

        [Tooltip("自动保存间隔(秒)")]
        public int AutoSaveInterval = 60;

        [Tooltip("存档加密密钥（AES-128 需要 16 字符，AES-256 需要 32 字符）\n留空则不加密，以明文存储")]
        public string EncryptionKey = "";

        /// <summary>
        ///     加载默认配置
        /// </summary>
        public static SaveSettings LoadDefault()
        {
            var settings = Resources.Load<SaveSettings>(DefaultPath);
            if (settings == null)
            {
                settings = CreateInstance<SaveSettings>();
                LogUtility.Debug("CFramework",
                    $"{nameof(SaveSettings)} 未在 Resources/{DefaultPath} 找到，使用默认值");
            }

            return settings;
        }
    }
}
