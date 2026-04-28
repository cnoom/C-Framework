using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     UI 服务接口
    ///     <para>负责 UI 面板的加载、缓存、生命周期管理和导航栈</para>
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        ///     导航栈数量
        /// </summary>
        int StackCount { get; }

        /// <summary>
        ///     导航栈最大容量
        /// </summary>
        int MaxStackCapacity { get; set; }

        /// <summary>
        ///     面板打开事件
        /// </summary>
        Observable<string> OnPanelOpened { get; }

        /// <summary>
        ///     面板关闭事件
        /// </summary>
        Observable<string> OnPanelClosed { get; }

        /// <summary>
        ///     UIRoot 上的 Canvas 组件
        /// </summary>
        Canvas Canvas { get; }

        /// <summary>
        ///     通过类型打开面板
        ///     <para>Prefab 名称必须与类名一致（即 Addressable Key = typeof(T).Name）</para>
        ///     <para>首次打开会加载 Prefab 并创建 IUI 实例，后续从缓存中复用</para>
        /// </summary>
        /// <typeparam name="T">IUI 实现类型，必须有无参构造函数</typeparam>
        UniTask<T> OpenAsync<T>() where T : IUI, new();

        /// <summary>
        ///     通过类型关闭面板（隐藏并缓存）
        /// </summary>
        /// <typeparam name="T">IUI 实现类型</typeparam>
        void Close<T>() where T : IUI;

        /// <summary>
        ///     关闭所有面板
        /// </summary>
        void CloseAll();

        /// <summary>
        ///     返回上一层面板
        /// </summary>
        void GoBack();
    }
}