using UnityEngine;

namespace CFramework.Utility.String
{
    /// <summary>
    ///     富文本工具类
    ///     <para>提供 Unity Rich Text 标签的快捷生成方法</para>
    /// </summary>
    public static class StringRichTextUtility
    {
        /// <summary>
        ///     为文本添加颜色标签
        /// </summary>
        /// <param name="text">源文本</param>
        /// <param name="color">目标颜色</param>
        /// <returns>带颜色标签的富文本</returns>
        public static string Color(string text, Color color)
        {
            var hex = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hex}>{text}</color>";
        }

        /// <summary>
        ///     为文本添加字号标签
        /// </summary>
        /// <param name="text">源文本</param>
        /// <param name="size">字号大小</param>
        /// <returns>带字号标签的富文本</returns>
        public static string Size(string text, int size)
        {
            return $"<size={size}>{text}</size>";
        }

        /// <summary>
        ///     为文本添加加粗标签
        /// </summary>
        /// <param name="text">源文本</param>
        /// <returns>加粗的富文本</returns>
        public static string Bold(string text)
        {
            return $"<b>{text}</b>";
        }

        /// <summary>
        ///     为文本添加斜体标签
        /// </summary>
        /// <param name="text">源文本</param>
        /// <returns>斜体的富文本</returns>
        public static string Italic(string text)
        {
            return $"<i>{text}</i>";
        }
    }
}
