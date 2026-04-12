using System;

namespace CFramework
{
    /// <summary>
    ///     内存预算
    /// </summary>
    public sealed class AssetMemoryBudget
    {
        public long BudgetBytes { get; set; }
        public long UsedBytes { get; internal set; }

        public float UsageRatio => BudgetBytes > 0 ? (float)UsedBytes / BudgetBytes : 0f;

        public event Action<float> OnBudgetExceeded;

        internal void CheckBudget()
        {
            if (UsedBytes > BudgetBytes) OnBudgetExceeded?.Invoke(UsageRatio);
        }
    }
}