using System;
using Cysharp.Threading.Tasks;

namespace CFramework
{
    /// <summary>
    ///     配置数据加载提供者接口
    ///     <para>抽象数据加载方式（SO、JSON、网络等），实现此接口以自定义数据来源</para>
    /// </summary>
    public interface IConfigProvider : IDisposable
    {
        /// <summary>
        ///     加载指定类型的配置表
        /// </summary>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <typeparam name="TValue">数据行类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <returns>填充好数据的 ConfigTable 实例</returns>
        UniTask<ConfigTable<TKey, TValue>> LoadAsync<TKey, TValue>(string address)
            where TValue : class, IConfigItem<TKey>;

        /// <summary>
        ///     释放指定地址的资源
        /// </summary>
        void Release(string address);
    }
}
