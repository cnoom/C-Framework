using UnityEngine;

namespace CFramework.Utility.String
{
    public static class StringRichTextUtility
    {
        public static string Color(string text, Color color)
        {
            var hex = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hex}>{text}</color>";
        }

        public static string Size(string text, int size)
        {
            return $"<size={size}>{text}</size>";
        }

        public static string Bold(string text)
        {
            return $"<b>{text}</b>";
        }

        public static string Italic(string text)
        {
            return $"<i>{text}</i>";
        }
    }
}