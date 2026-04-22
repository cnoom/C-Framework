using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CFramework
{
    /// <summary>
    ///     配置服务接口
    ///     <para>提供配置数据的加载和卸载功能</para>
    ///     <para>配合 Luban 配置库使用，游戏项目需实现 <see cref="LubanConfigService" /> 子类</para>
    /// </summary>
    public interface IConfigService : IDisposable
    {
        /// <summary>
        ///     配置数据是否已加载
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        ///     加载所有配置数据
        /// </summary>
        /// <param name="ct">取消令牌</param>
        UniTask LoadAllAsync(CancellationToken ct = default);

        /// <summary>
        ///     卸载所有配置数据
        /// </summary>
        void UnloadAll();
    }
}
