using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CFramework
{
    /// <summary>
    ///     基于内存的配置数据加载提供者
    ///     <para>预先注册数据，用于单元测试或快速原型</para>
    ///     <para>支持直接注入 List&lt;TValue&gt;，无需任何资源加载</para>
    /// </summary>
    public class MemoryConfigProvider : IConfigProvider
    {
        private readonly Dictionary<string, object> _registeredTables = new();
        private bool _disposed;

        /// <summary>
        ///     注册配置数据
        /// </summary>
        /// <param name="address">资源地址（需与 ConfigService.LoadAsync 传入的地址一致）</param>
        /// <param name="data">配置数据列表</param>
        public void Register<TKey, TValue>(string address, List<TValue> data)
            where TValue : IConfigItem<TKey>
        {
            var table = new ConfigTable<TKey, TValue>();
            table.Load(data);
            _registeredTables[address] = table;
        }

        /// <summary>
        ///     注册已构建好的 ConfigTable
        /// </summary>
        public void Register<TKey, TValue>(string address, ConfigTable<TKey, TValue> table)
            where TValue : IConfigItem<TKey>
        {
            _registeredTables[address] = table;
        }

        public UniTask<ConfigTable<TKey, TValue>> LoadAsync<TKey, TValue>(string address,
            CancellationToken ct = default)
            where TValue : IConfigItem<TKey>
        {
            if (_registeredTables.TryGetValue(address, out var table)
                && table is ConfigTable<TKey, TValue> typedTable)
            {
                return UniTask.FromResult(typedTable);
            }

            LogUtility.Error("MemoryConfigProvider",
                $"未注册的配置地址: {address}，" +
                $"请先调用 Register<{typeof(TKey).Name}, {typeof(TValue).Name}>(\"{address}\", data)");
            return UniTask.FromResult<ConfigTable<TKey, TValue>>(null);
        }

        public void Release(string address)
        {
            // 内存数据无需释放资源
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _registeredTables.Clear();
        }
    }
}
