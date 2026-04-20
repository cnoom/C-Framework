using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     存档服务实现
    /// </summary>
    public sealed class SaveService : ISaveService, IDisposable
    {
        private readonly ConcurrentDictionary<string, object> _cache = new();
        private readonly Subject<bool> _dirtyChanged = new();
        private readonly FrameworkSettings _settings;
        private CancellationTokenSource _autoSaveCts;

        public SaveService(FrameworkSettings settings)
        {
            _settings = settings;
        }

        private string SavePath => Path.Combine(Application.persistentDataPath, "Save", $"slot_{CurrentSlot}");

        public void Dispose()
        {
            DisableAutoSave();
            _cache.Clear();
            _dirtyChanged.Dispose();
        }

        public int CurrentSlot { get; private set; }

        public bool IsDirty { get; private set; }

        public Observable<bool> OnDirtyChanged => _dirtyChanged;

        #region 存档槽管理

        public string[] GetSlotNames()
        {
            var slots = new List<string>();
            var saveDir = Path.Combine(Application.persistentDataPath, "Save");

            if (Directory.Exists(saveDir))
                foreach (var dir in Directory.GetDirectories(saveDir))
                    slots.Add(Path.GetFileName(dir));

            return slots.ToArray();
        }

        public SaveSlotInfo[] GetSlotInfos()
        {
            var slotNames = GetSlotNames();
            var infos = new List<SaveSlotInfo>();

            foreach (var slotName in slotNames)
                // 解析槽位索引：slot_0 -> 0
                if (slotName.StartsWith("slot_") && int.TryParse(slotName[5..], out var index))
                {
                    var slotPath = Path.Combine(Application.persistentDataPath, "Save", slotName);
                    var hasData = Directory.Exists(slotPath) && Directory.GetFiles(slotPath, "*.sav").Length > 0;

                    var lastModified = DateTime.MinValue;
                    if (hasData)
                        try
                        {
                            lastModified = Directory.GetLastWriteTime(slotPath);
                        }
                        catch
                        {
                            // 忽略访问错误
                        }

                    infos.Add(new SaveSlotInfo
                    {
                        Index = index,
                        Name = slotName,
                        LastModified = lastModified,
                        HasData = hasData
                    });
                }

            return infos.ToArray();
        }

        public void SetSlot(int slotIndex)
        {
            if (slotIndex < 0) slotIndex = 0;
            CurrentSlot = slotIndex;
            _cache.Clear();
        }

        #endregion

        #region 保存/加载

        public async UniTask SaveAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            Directory.CreateDirectory(SavePath);

            // 复制缓存键值对，避免遍历时修改集合
            var cacheCopy = new List<KeyValuePair<string, object>>(_cache);

            foreach (var kvp in cacheCopy) await SaveToFileAsync(kvp.Key, kvp.Value, ct);

            ClearDirty();
        }

        public async UniTask<T> LoadAsync<T>(string key, T defaultValue = default)
        {
            if (_cache.TryGetValue(key, out var cached)) return (T)cached;

            var filePath = GetFilePath(key);

            if (!File.Exists(filePath)) return defaultValue;

            try
            {
                var bytes = await File.ReadAllBytesAsync(filePath);
                var json = Decrypt(bytes);
                var data = JsonUtility.FromJson<T>(json);

                _cache[key] = data;
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveService] Failed to load: {key}, Error: {ex.Message}");
                return defaultValue;
            }
        }

        public async UniTask SaveAsync<T>(string key, T value, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            _cache[key] = value;

            await SaveToFileAsync(key, value, ct);

            MarkDirty();
        }

        /// <summary>
        ///     内部方法：仅写入文件，不修改缓存和脏状态
        /// </summary>
        private async UniTask SaveToFileAsync<T>(string key, T value, CancellationToken ct)
        {
            var filePath = GetFilePath(key);
            var tempPath = filePath + ".tmp";

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                var json = JsonUtility.ToJson(value, true);
                var bytes = Encrypt(json);

                // 原子写入：先写临时文件，再重命名
                await File.WriteAllBytesAsync(tempPath, bytes, ct);

                // 删除目标文件（如果存在）
                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(tempPath, filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Failed to save: {key}, Error: {ex.Message}");

                // 清理临时文件
                if (File.Exists(tempPath)) File.Delete(tempPath);

                throw;
            }
        }

        public bool HasKey(string key)
        {
            // 先检查缓存
            if (_cache.ContainsKey(key)) return true;

            // 检查文件
            return File.Exists(GetFilePath(key));
        }

        public async UniTask<bool> DeleteAsync(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            // 从缓存中移除
            _cache.TryRemove(key, out _);

            // 在主线程获取路径
            var filePath = GetFilePath(key);

            // 删除文件
            if (File.Exists(filePath))
                try
                {
                    await UniTask.RunOnThreadPool(() => File.Delete(filePath), cancellationToken: ct);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveService] Failed to delete: {key}, Error: {ex.Message}");
                    return false;
                }

            return false;
        }

        public async UniTask DeleteAllAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            _cache.Clear();

            // 在主线程获取路径
            var savePath = SavePath;

            if (Directory.Exists(savePath))
                try
                {
                    await UniTask.RunOnThreadPool(() => Directory.Delete(savePath, true), cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveService] Failed to delete all: {ex.Message}");
                }

            ClearDirty();
        }

        private string GetFilePath(string key)
        {
            return Path.Combine(SavePath, $"{key}.sav");
        }

        #endregion

        #region 脏状态管理

        public void MarkDirty()
        {
            if (!IsDirty)
            {
                IsDirty = true;
                _dirtyChanged.OnNext(true);
            }
        }

        public void ClearDirty()
        {
            if (IsDirty)
            {
                IsDirty = false;
                _dirtyChanged.OnNext(false);
            }
        }

        #endregion

        #region 自动保存

        public void EnableAutoSave(float intervalSeconds = 60f)
        {
            DisableAutoSave();

            _autoSaveCts = new CancellationTokenSource();
            AutoSaveLoop(intervalSeconds, _autoSaveCts.Token).Forget();
        }

        public void DisableAutoSave()
        {
            _autoSaveCts?.Cancel();
            _autoSaveCts?.Dispose();
            _autoSaveCts = null;
        }

        private async UniTaskVoid AutoSaveLoop(float interval, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct);

                if (IsDirty)
                    try
                    {
                        await SaveAsync(ct);
                        Debug.Log("[SaveService] Auto save completed");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SaveService] Auto save failed: {ex.Message}");
                    }
            }
        }

        #endregion

        #region 加密/解密

        private byte[] GetEncryptionKeyBytes()
        {
            var keyStr = _settings.EncryptionKey;
            if (string.IsNullOrEmpty(keyStr))
                keyStr = "CFramework_DefaultKey";

            // 按 UTF-8 编码，截取或填充到恰好 16 字节（AES-128）
            var bytes = Encoding.UTF8.GetBytes(keyStr);
            var key = new byte[16];
            var copyLen = Math.Min(bytes.Length, 16);
            Buffer.BlockCopy(bytes, 0, key, 0, copyLen);
            return key;
        }

        private byte[] Encrypt(string data)
        {
            var key = GetEncryptionKeyBytes();

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(data);
            var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

            // 将 IV 拼接到密文头部：[IV 16字节][密文]
            var result = new byte[aes.IV.Length + encrypted.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);
            return result;
        }

        private string Decrypt(byte[] data)
        {
            var key = GetEncryptionKeyBytes();

            using var aes = Aes.Create();
            aes.Key = key;

            // 从密文头部提取 IV（前 16 字节）
            var iv = new byte[aes.BlockSize / 8];
            var encrypted = new byte[data.Length - iv.Length];
            Buffer.BlockCopy(data, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(data, iv.Length, encrypted, 0, encrypted.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var bytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
            return Encoding.UTF8.GetString(bytes);
        }

        #endregion
    }
}