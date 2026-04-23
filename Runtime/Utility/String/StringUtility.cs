using System.Text;
using UnityEngine;

namespace CFramework.Utility.String
{
    /// <summary>
    ///     字符串工具类
    ///     <para>提供常用的字符串操作扩展方法</para>
    /// </summary>
    public static class StringUtility
    {
        /// <summary>
        ///     反转字符串
        /// </summary>
        /// <param name="str">要反转的字符串</param>
        /// <returns>反转后的字符串</returns>
        public static string Reverse(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            var sb = new StringBuilder(str.Length);
            for (var i = str.Length - 1; i >= 0; i--)
                sb.Append(str[i]);
            return sb.ToString();
        }

        /// <summary>
        ///     移除所有空白字符（包括换行、制表）
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <returns>移除空白后的字符串</returns>
        public static string RemoveWhitespace(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            var sb = new StringBuilder(str.Length);
            foreach (var c in str)
                if (!char.IsWhiteSpace(c))
                    sb.Append(c);
            return sb.ToString();
        }

        /// <summary>
        ///     截取字符串并添加省略号（支持中英文混合）
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <param name="maxLength">最大长度</param>
        /// <param name="ellipsis">省略号字符串，默认 "..."</param>
        /// <returns>截取后的字符串</returns>
        public static string TruncateWithEllipsis(string str, int maxLength, string ellipsis = "...")
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength) return str;
            return str.Substring(0, maxLength - ellipsis.Length) + ellipsis;
        }

        /// <summary>
        ///     安全 Substring（避免越界异常）
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <param name="startIndex">起始索引</param>
        /// <param name="length">截取长度</param>
        /// <returns>截取的子字符串</returns>
        public static string SafeSubstring(string str, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            startIndex = Mathf.Clamp(startIndex, 0, str.Length);
            length = Mathf.Clamp(length, 0, str.Length - startIndex);
            return str.Substring(startIndex, length);
        }
    }
}
