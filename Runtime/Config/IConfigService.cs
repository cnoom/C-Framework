using System.Threading;
using Cysharp.Threading.Tasks;

namespace CFramework
{
    /// <summary>
    ///     配置服务接口
    /// </summary>
    public interface IConfigService
    {
        UniTask LoadAsync<TConfigTable>(CancellationToken ct = default);

        T GetTable<T>() where T : ConfigTableBase;
        bool TryGetTable<T>(out T table) where T : ConfigTableBase;
        TValue Get<TKey, TValue>(TKey key);

        UniTask ReloadAsync<TConfigTable>(CancellationToken ct = default);
    }
}
