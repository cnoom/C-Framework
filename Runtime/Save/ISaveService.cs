using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CFramework
{
    /// <summary>
    ///     存档服务接口
    /// </summary>
    public interface ISaveService
    {
        #region 存档槽管理

        int CurrentSlot { get; }
        string[] GetSlotNames();
        SaveSlotInfo[] GetSlotInfos();
        void SetSlot(int slotIndex);

        #endregion

        #region 保存/加载

        UniTask SaveAsync(CancellationToken ct = default);
        UniTask<T> LoadAsync<T>(string key, T defaultValue = default);
        UniTask SaveAsync<T>(string key, T value, CancellationToken ct = default);
        bool HasKey(string key);
        UniTask<bool> DeleteAsync(string key, CancellationToken ct = default);
        UniTask DeleteAllAsync(CancellationToken ct = default);

        #endregion

        #region 脏状态管理

        bool IsDirty { get; }
        void MarkDirty();
        void ClearDirty();
        Observable<bool> OnDirtyChanged { get; }

        #endregion

        #region 自动保存

        void EnableAutoSave(float intervalSeconds = 60f);
        void DisableAutoSave();

        #endregion
    }
}