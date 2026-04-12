using System;
using R3;

namespace CFramework
{
    /// <summary>
    ///     黑板键变化信息
    /// </summary>
    public readonly struct BlackboardChange
    {
        /// <summary>
        ///     键名称
        /// </summary>
        public string KeyName { get; }

        /// <summary>
        ///     值类型
        /// </summary>
        public Type ValueType { get; }

        public BlackboardChange(string keyName, Type valueType)
        {
            KeyName = keyName;
            ValueType = valueType;
        }
    }

    /// <summary>
    ///     黑板接口，支持响应式数据存储
    /// </summary>
    public interface IBlackboard : IDisposable
    {
        /// <summary>
        ///     任意键变化事件
        /// </summary>
        Observable<BlackboardChange> OnKeyChanged { get; }

        /// <summary>
        ///     设置值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        void Set<T>(BlackboardKey<T> key, T value);

        /// <summary>
        ///     尝试获取值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">获取到的值</param>
        /// <returns>是否获取成功</returns>
        bool TryGet<T>(BlackboardKey<T> key, out T value);

        /// <summary>
        ///     获取值，如果不存在则返回默认值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>获取到的值或默认值</returns>
        T Get<T>(BlackboardKey<T> key, T defaultValue = default);

        /// <summary>
        ///     检查键是否存在
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <returns>是否存在</returns>
        bool Has<T>(BlackboardKey<T> key);

        /// <summary>
        ///     移除键
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <returns>是否移除成功</returns>
        bool Remove<T>(BlackboardKey<T> key);

        /// <summary>
        ///     清空所有数据
        /// </summary>
        void Clear();

        /// <summary>
        ///     观察指定键的值变化
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <returns>可观察的值流</returns>
        Observable<T> Observe<T>(BlackboardKey<T> key);
    }
}