using System.IO;
using UnityEngine;

namespace CFramework.Editor
{
    /// <summary>
    ///     编辑器路径约定
    ///     统一管理项目中所有路径常量
    /// </summary>
    public static class EditorPaths
    {
        #region 根路径

        /// <summary>
        ///     Assets 根路径
        /// </summary>
        public const string AssetsRoot = "Assets";

        /// <summary>
        ///     CFramework 库根路径
        /// </summary>
        public const string CFrameworkRoot = "Assets/CFramework";

        /// <summary>
        ///     CFramework 编辑器路径
        /// </summary>
        public const string CFrameworkEditor = "Assets/CFramework/Editor";

        #endregion

        #region 编辑器资源路径

        /// <summary>
        ///     编辑器资源根目录
        /// </summary>
        public const string EditorResRoot = "Assets/EditorRes";

        /// <summary>
        ///     编辑器配置目录
        ///     用于存储编辑器配置资产（ScriptableObject）
        /// </summary>
        public const string EditorConfigs = "Assets/EditorRes/Configs";

        /// <summary>
        ///     编辑器资源目录
        ///     用于存储编辑器专用资源（图标、模板等）
        /// </summary>
        public const string EditorAssets = "Assets/EditorRes/Assets";

        /// <summary>
        ///     Addressable 配置路径
        /// </summary>
        public const string AddressableConfig = "Assets/EditorRes/Configs/AddressableConfig.asset";

        #endregion

        #region 代码生成路径

        /// <summary>
        ///     代码生成根目录
        /// </summary>
        public const string GeneratedRoot = "Assets/Scripts/Generated";

        /// <summary>
        ///     Addressables 常量生成路径
        /// </summary>
        public const string AddressableConstantsOutput = "Assets/Scripts/Generated";

        /// <summary>
        ///     UI 绑定代码生成路径
        /// </summary>
        public const string UIBindingsOutput = "Assets/Scripts/UI";

        #endregion

        #region 运行时代码路径

        /// <summary>
        ///     脚本根目录
        /// </summary>
        public const string ScriptsRoot = "Assets/Scripts";

        /// <summary>
        ///     配置表代码目录
        /// </summary>
        public const string ConfigScripts = "Assets/Scripts/Config";

        /// <summary>
        ///     服务代码目录
        /// </summary>
        public const string ServiceScripts = "Assets/Scripts/Services";

        /// <summary>
        ///     UI 代码目录
        /// </summary>
        public const string UIScripts = "Assets/Scripts/UI";

        #endregion

        #region 资源路径

        /// <summary>
        ///     Resources 目录（运行时加载根）
        /// </summary>
        public const string Resources = "Assets/Resources";

        /// <summary>
        ///     框架 Settings 默认输出目录
        ///     <para>FrameworkSettings 及各模块 Settings 默认存放在此</para>
        /// </summary>
        public const string FrameworkSettings = "Assets/Resources";

        /// <summary>
        ///     预制体目录
        /// </summary>
        public const string Prefabs = "Assets/Prefabs";

        /// <summary>
        ///     场景目录
        /// </summary>
        public const string Scenes = "Assets/Scenes";

        /// <summary>
        ///     音频目录
        /// </summary>
        public const string Audio = "Assets/Audio";

        /// <summary>
        ///     纹理目录
        /// </summary>
        public const string Textures = "Assets/Textures";

        /// <summary>
        ///     精灵图目录
        /// </summary>
        public const string Sprites = "Assets/Sprites";

        /// <summary>
        ///     材质目录
        /// </summary>
        public const string Materials = "Assets/Materials";

        /// <summary>
        ///     动画目录
        /// </summary>
        public const string Animations = "Assets/Animations";

        #endregion

        #region 工具方法

        /// <summary>
        ///     确保目录存在
        /// </summary>
        /// <param name="path">目录路径</param>
        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        /// <summary>
        ///     转换为绝对路径
        /// </summary>
        /// <param name="assetPath">Assets 相对路径</param>
        /// <returns>绝对路径</returns>
        public static string ToAbsolutePath(string assetPath)
        {
            return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
        }

        /// <summary>
        ///     转换为 Assets 相对路径
        /// </summary>
        /// <param name="absolutePath">绝对路径</param>
        /// <returns>Assets 相对路径</returns>
        public static string ToAssetPath(string absolutePath)
        {
            var dataPath = Application.dataPath.Replace('\\', '/');
            absolutePath = absolutePath.Replace('\\', '/');

            if (absolutePath.StartsWith(dataPath)) return "Assets" + absolutePath.Substring(dataPath.Length);

            return absolutePath;
        }

        #endregion
    }
}