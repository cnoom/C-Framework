using System;

namespace CFramework
{
    /// <summary>
    ///     存档槽信息
    /// </summary>
    public sealed class SaveSlotInfo
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public DateTime LastModified { get; set; }
        public bool HasData { get; set; }
    }
}