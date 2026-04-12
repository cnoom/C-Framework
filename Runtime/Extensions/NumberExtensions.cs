using UnityEngine;

namespace CFramework.Runtime.Extensions
{
    public static class NumberExtensions
    {
        public static int RoundInt(this float value) => Mathf.RoundToInt(value);
        public static int FloorInt(this float value) => Mathf.FloorToInt(value);
        public static int CeilInt(this float value) => Mathf.CeilToInt(value);
    }
}