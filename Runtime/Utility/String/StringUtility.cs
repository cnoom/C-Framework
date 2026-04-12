using System.Text;
using UnityEngine;

namespace CFramework.Utility.String
{
    public static class StringUtility
    {
        // 反转字符串（使用 StringBuilder）
        public static string Reverse(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            var sb = new StringBuilder(str.Length);
            for (var i = str.Length - 1; i >= 0; i--)
                sb.Append(str[i]);
            return sb.ToString();
        }

        // 移除所有空白字符（包括换行、制表）
        public static string RemoveWhitespace(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            var sb = new StringBuilder(str.Length);
            foreach (var c in str)
                if (!char.IsWhiteSpace(c))
                    sb.Append(c);
            return sb.ToString();
        }

        // 截取并添加省略号（支持中英文混合）
        public static string TruncateWithEllipsis(string str, int maxLength, string ellipsis = "...")
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength) return str;
            return str.Substring(0, maxLength - ellipsis.Length) + ellipsis;
        }

        // 安全 Substring（避免越界）
        public static string SafeSubstring(string str, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            startIndex = Mathf.Clamp(startIndex, 0, str.Length);
            length = Mathf.Clamp(length, 0, str.Length - startIndex);
            return str.Substring(startIndex, length);
        }
    }
}