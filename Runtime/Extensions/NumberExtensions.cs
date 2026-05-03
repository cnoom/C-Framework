using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     数值类型扩展方法
    ///     <para>提供 float 到 int 的常用取整转换</para>
    /// </summary>
    public static class NumberExtensions
    {
        /// <summary>
        ///     四舍五入取整
        /// </summary>
        /// <param name="value">浮点数值</param>
        /// <returns>四舍五入后的整数</returns>
        public static int RoundInt(this float value) => Mathf.RoundToInt(value);

        /// <summary>
        ///     向下取整
        /// </summary>
        /// <param name="value">浮点数值</param>
        /// <returns>向下取整后的整数</returns>
        public static int FloorInt(this float value) => Mathf.FloorToInt(value);

        /// <summary>
        ///     向上取整
        /// </summary>
        /// <param name="value">浮点数值</param>
        /// <returns>向上取整后的整数</returns>
        public static int CeilInt(this float value) => Mathf.CeilToInt(value);
    }
}
