using System;

namespace CFramework
{
    /// <summary>
    ///     存档数据基类
    /// </summary>
    [Serializable]
    public abstract class SaveDataBase
    {
        public int Version { get; set; }
        public DateTime LastSaveTime { get; set; }
    }
}