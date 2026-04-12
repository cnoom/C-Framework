using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CFramework.Utility.String
{
    public static class StringTemplateUtility
    {
        private static readonly Regex PlaceholderRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

        // 替换模板中的 {key}
        public static string Replace(string template, Dictionary<string, object> parameters)
        {
            return PlaceholderRegex.Replace(template, match =>
            {
                var key = match.Groups[1].Value;
                return parameters.TryGetValue(key, out var value) ? value.ToString() : match.Value;
            });
        }

        // 简化用法：传入匿名对象（使用反射，性能较低，适合编辑器或低频）
        public static string Replace(string template, object parameters)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in parameters.GetType().GetProperties())
                dict[prop.Name] = prop.GetValue(parameters);
            return Replace(template, dict);
        }
    }
}