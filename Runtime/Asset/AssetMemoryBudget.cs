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
        ///     原子增加内存使用量（不允许负数下溢）
        /// </summary>
        internal void AddUsedBytes(long delta)
        {
            if (delta >= 0)
            {
                Interlocked.Add(ref _usedBytes, delta);
            }
            else
            {
                // 释放时防止下溢到负数
                long current, target;
                do
                {
                    current = Volatile.Read(ref _usedBytes);
                    target = Math.Max(0, current + delta);
                } while (Interlocked.CompareExchange(ref _usedBytes, target, current) != current);
            }
        }

        public float UsageRatio => BudgetBytes > 0 ? (float)UsedBytes / BudgetBytes : 0f;

        public event Action<float> OnBudgetExceeded;

        internal void CheckBudget()
        {
            if (UsedBytes > BudgetBytes)
            {
                var handler = OnBudgetExceeded;
                if (handler != null)
                {
                    try { handler.Invoke(UsageRatio); }
                    catch { /* 防止外部监听器异常中断正常流程 */ }
                }
            }
        }
    }
}
