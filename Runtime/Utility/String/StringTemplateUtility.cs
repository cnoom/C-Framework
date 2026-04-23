using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CFramework.Utility.String
{
    /// <summary>
    ///     字符串模板工具类
    ///     <para>支持 {key} 占位符的模板替换</para>
    /// </summary>
    public static class StringTemplateUtility
    {
        private static readonly Regex PlaceholderRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

        /// <summary>
        ///     替换模板中的 {key} 占位符
        /// </summary>
        /// <param name="template">模板字符串，如 "Hello, {name}!"</param>
        /// <param name="parameters">键值对参数</param>
        /// <returns>替换后的字符串</returns>
        public static string Replace(string template, Dictionary<string, object> parameters)
        {
            return PlaceholderRegex.Replace(template, match =>
            {
                var key = match.Groups[1].Value;
                return parameters.TryGetValue(key, out var value) ? value.ToString() : match.Value;
            });
        }

        /// <summary>
        ///     替换模板中的 {key} 占位符（匿名对象版本）
        ///     <para>使用反射，性能较低，适合编辑器或低频场景</para>
        /// </summary>
        /// <param name="template">模板字符串</param>
        /// <param name="parameters">匿名对象参数，如 new { name = "World" }</param>
        /// <returns>替换后的字符串</returns>
        public static string Replace(string template, object parameters)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in parameters.GetType().GetProperties())
                dict[prop.Name] = prop.GetValue(parameters);
            return Replace(template, dict);
        }
    }
}
