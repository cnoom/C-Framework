using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     组合配置数据加载提供者
    ///     <para>支持同时注册多个 Provider，通过地址前缀自动路由到对应 Provider</para>
    ///     <para>未匹配前缀的地址使用默认 Provider 处理</para>
    ///     <para>使用示例：</para>
    ///     <code>
    ///     var composite = new CompositeConfigProvider(soProvider);
    ///     composite.AddProvider("json", jsonProvider);
    ///     composite.AddProvider("memory", memoryProvider);
    ///     
    ///     // 地址 "json/Items" → jsonProvider，实际传入 "Items"
    ///     // 地址 "Config/Table" → soProvider（默认）
    ///     </code>
    /// </summary>
    public class CompositeConfigProvider : IConfigProvider
    {
        private readonly IConfigProvider _defaultProvider;
        private readonly List<KeyValuePair<string, IConfigProvider>> _providers = new();
        private bool _disposed;

        public CompositeConfigProvider(IConfigProvider defaultProvider)
        {
            _defaultProvider = defaultProvider;
        }

        /// <summary>
        ///     添加 Provider 及其地址前缀
        ///     <para>前缀匹配规则：加载地址以 "{prefix}/" 开头时，路由到此 Provider，并移除前缀后传递</para>
        ///     <para>添加顺序影响优先级：先添加的前缀优先匹配</para>
        /// </summary>
        /// <param name="prefix">地址前缀（不含 "/"，系统自动拼接）</param>
        /// <param name="provider">对应的 Provider 实例</param>
        public void AddProvider(string prefix, IConfigProvider provider)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                Debug.LogWarning("[CompositeConfigProvider] 前缀为空，已忽略");
                return;
            }

            if (provider == null)
            {
                Debug.LogWarning("[CompositeConfigProvider] provider 为空，已忽略");
                return;
            }

            _providers.Add(new KeyValuePair<string, IConfigProvider>(prefix, provider));
        }

        public async UniTask<ConfigTable<TKey, TValue>> LoadAsync<TKey, TValue>(string address,
            CancellationToken ct = default)
            where TValue : IConfigItem<TKey>
        {
            var (provider, resolvedAddress) = ResolveProvider(address);
            return await provider.LoadAsync<TKey, TValue>(resolvedAddress, ct);
        }

        public void Release(string address)
        {
            var (provider, resolvedAddress) = ResolveProvider(address);
            provider.Release(resolvedAddress);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _defaultProvider?.Dispose();
            foreach (var kvp in _providers)
                kvp.Value?.Dispose();

            _providers.Clear();
        }

        /// <summary>
        ///     根据地址前缀解析应该使用的 Provider
        ///     <para>匹配规则：地址以 "{prefix}/" 开头时命中，前缀被移除后作为实际地址传递</para>
        ///     <para>未匹配任何前缀时使用默认 Provider</para>
        /// </summary>
        private (IConfigProvider provider, string address) ResolveProvider(string address)
        {
            foreach (var kvp in _providers)
            {
                var prefix = kvp.Key + "/";
                if (address.StartsWith(prefix))
                {
                    return (kvp.Value, address.Substring(prefix.Length));
                }
            }

            return (_defaultProvider, address);
        }
    }
}
