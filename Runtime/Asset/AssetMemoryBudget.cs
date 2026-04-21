using System;
using System.Threading;

namespace CFramework
{
    /// <summary>
    ///     内存预算（线程安全）
    /// </summary>
    public sealed class AssetMemoryBudget
    {
        private long _budgetBytes;
        private long _usedBytes;

        public long BudgetBytes
        {
            get => Volatile.Read(ref _budgetBytes);
            set => Volatile.Write(ref _budgetBytes, value);
        }

        public long UsedBytes
        {
            get => Volatile.Read(ref _usedBytes);
            internal set => Interlocked.Exchange(ref _usedBytes, value);
        }

        /// <summary>
        ///     原子增加内存使用量
        /// </summary>
        internal void AddUsedBytes(long delta)
        {
            Interlocked.Add(ref _usedBytes, delta);
        }

        public float UsageRatio => BudgetBytes > 0 ? (float)UsedBytes / BudgetBytes : 0f;

        public event Action<float> OnBudgetExceeded;

        internal void CheckBudget()
        {
            if (UsedBytes > BudgetBytes) OnBudgetExceeded?.Invoke(UsageRatio);
        }
    }
}
