using System.Text;
using UnityEngine;

namespace CFramework.Utility.String
{
    public static class StringFormatterUtility
    {
        // 数字带千分位，支持自定义小数点后位数
        public static string FormatNumber(float number, int decimalPlaces = 2, bool useThousandsSeparator = true)
        {
            var format = useThousandsSeparator ? "N" + decimalPlaces : "F" + decimalPlaces;
            return number.ToString(format);
        }

        // 时间格式化（秒 -> 00:00 / 00:00:00）
        public static string FormatTime(float seconds, bool showHours = false)
        {
            var totalSeconds = Mathf.FloorToInt(Mathf.Abs(seconds));
            var hours = totalSeconds / 3600;
            var minutes = totalSeconds % 3600 / 60;
            var secs = totalSeconds % 60;

            if (showHours || hours > 0)
                return $"{hours:D2}:{minutes:D2}:{secs:D2}";
            return $"{minutes:D2}:{secs:D2}";
        }

        // 文件大小格式化（B, KB, MB, GB）
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            var order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                len /= 1024;
                order++;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        // 首字母大写（每个单词）
        public static string ToTitleCase(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            var sb = new StringBuilder(str.Length);
            var capitalizeNext = true;
            foreach (var c in str)
            {
                sb.Append(capitalizeNext ? char.ToUpper(c) : char.ToLower(c));
                capitalizeNext = char.IsWhiteSpace(c);
            }

            return sb.ToString();
        }
    }
}