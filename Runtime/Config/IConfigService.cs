using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CFramework
{
    /// <summary>
    ///     配置服务接口
    ///     <para>提供配置表的加载、查询、卸载功能</para>
    /// </summary>
    public interface IConfigService : IDisposable
    {
        /// <summary>
        ///     加载指定数据类型的配置表
        /// </summary>
        /// <typeparam name="TValue">数据行类型（如 ItemData）</typeparam>
        /// <param name="address">资源地址（为空时自动从映射表查找）</param>
        /// <param name="ct">取消令牌</param>
        UniTask LoadAsync<TValue>(string address = null, CancellationToken ct = default)
            where TValue : class;

        /// <summary>
        ///     加载所有已注册的配置表
        /// </summary>
        UniTask LoadAllAsync(CancellationToken ct = default);

        /// <summary>
        ///     获取指定数据类型的配置表（通过反射推断 TKey）
        /// </summary>
        /// <typeparam name="TValue">数据行类型</typeparam>
        ConfigTable<TKey, TValue> GetTable<TKey, TValue>()
            where TValue : class, IConfigItem<TKey>;

        /// <summary>
        ///     尝试获取配置表
        /// </summary>
        bool TryGetTable<TKey, TValue>(out ConfigTable<TKey, TValue> table)
            where TValue : class, IConfigItem<TKey>;

        /// <summary>
        ///     通过主键快速获取配置数据
        /// </summary>
        TValue Get<TKey, TValue>(TKey key) where TValue : class, IConfigItem<TKey>;

        /// <summary>
        ///     重新加载指定配置表
        /// </summary>
        UniTask ReloadAsync<TValue>(string address = null, CancellationToken ct = default)
            where TValue : class;

        /// <summary>
        ///     卸载指定配置表
        /// </summary>
        void Unload<TValue>() where TValue : class;

        /// <summary>
        ///     卸载所有配置表
        /// </summary>
        void UnloadAll();

        /// <summary>
        ///     注册数据类型到地址的映射
        /// </summary>
        void RegisterAddress<TValue>(string address) where TValue : class;

        /// <summary>
        ///     为指定数据类型注册专用 Provider（覆盖默认 Provider）
        ///     <para>适用于不同配置表需要从不同数据源加载的场景（如部分 SO、部分 JSON）</para>
        /// </summary>
        /// <typeparam name="TValue">数据行类型</typeparam>
        /// <param name="provider">专用 Provider 实例</param>
        void RegisterProvider<TValue>(IConfigProvider provider) where TValue : class;
    }
}
