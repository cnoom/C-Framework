using System.Threading;
using Cysharp.Threading.Tasks;

namespace CFramework
{
    /// <summary>
    ///     Luban 配置服务抽象基类
    ///     <para>游戏项目需继承此类，实现 Luban Tables 的创建和数据加载逻辑</para>
    ///     <para>
    ///         使用示例：
    ///         <code>
    /// public class GameConfigService : LubanConfigService
    /// {
    ///     private cfg.Tables _tables;
    ///     private readonly ILubanDataLoader _loader;
    ///
    ///     public GameConfigService(ILubanDataLoader loader)
    ///     {
    ///         _loader = loader;
    ///     }
    ///
    ///     public cfg.Tables Tables => _tables;
    ///
    ///     protected override async UniTask OnLoadConfigAsync(CancellationToken ct)
    ///     {
    ///         await _loader.PreloadAsync(new[] { "TbItem", "TbSkill", "TbGlobal" }, ct);
    ///         _tables = new cfg.Tables(file => new ByteBuf(_loader.GetData(file)));
    ///     }
    ///
    ///     public override void UnloadAll()
    ///     {
    ///         _tables = null;
    ///         _loader.UnloadAll();
    ///     }
    /// }
    /// </code>
    ///     </para>
    /// </summary>
    public abstract class LubanConfigService : IConfigService
    {
        private bool _disposed;

        /// <summary>
        ///     配置数据是否已加载
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        ///     加载所有配置数据
        ///     <para>预加载数据后调用 <see cref="OnLoadConfigAsync" /> 创建 Luban Tables</para>
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async UniTask LoadAllAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await OnLoadConfigAsync(ct);
            IsLoaded = true;
        }

        /// <summary>
        ///     子类实现：执行具体的配置加载逻辑
        ///     <para>典型实现：预加载 bytes 数据 → 创建 Luban Tables 实例</para>
        /// </summary>
        /// <param name="ct">取消令牌</param>
        protected abstract UniTask OnLoadConfigAsync(CancellationToken ct);

        /// <summary>
        ///     子类实现：卸载所有配置数据
        /// </summary>
        public abstract void UnloadAll();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnloadAll();
        }
    }
}
