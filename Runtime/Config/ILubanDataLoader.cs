using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CFramework
{
    /// <summary>
    ///     Luban 数据加载器接口
    ///     <para>抽象 Luban bytes 数据的加载方式，支持 Addressables、Resources、文件系统等</para>
    /// </summary>
    public interface ILubanDataLoader : IDisposable
    {
        /// <summary>
        ///     异步预加载指定配置表的字节数据
        /// </summary>
        /// <param name="tableFiles">配置表文件名列表（不含扩展名，如 "TbItem"）</param>
        /// <param name="ct">取消令牌</param>
        UniTask PreloadAsync(IEnumerable<string> tableFiles, CancellationToken ct = default);

        /// <summary>
        ///     同步获取已加载的字节数据
        ///     <para>必须在 <see cref="PreloadAsync" /> 完成后调用</para>
        /// </summary>
        /// <param name="tableFile">配置表文件名</param>
        /// <returns>字节数据，未找到时返回 null</returns>
        byte[] GetData(string tableFile);

        /// <summary>
        ///     释放所有已加载的数据
        /// </summary>
        void UnloadAll();
    }
}
