using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CFramework.Tests
{
    /// <summary>
    ///     内存模拟资源加载提供者（用于单元测试，无需真实 Addressable 资源）
    ///     <para>在构造时预注册模拟资源，加载时直接返回内存中的对象</para>
    /// </summary>
    public sealed class MockAssetProvider : IAssetProvider
    {
        private readonly Dictionary<object, Object> _assets = new();
        private readonly HashSet<object> _instantiated = new();
        private readonly Dictionary<object, long> _memorySizes = new();
        private int _loadDelayMs;

        /// <summary>
        ///     记录所有释放操作（用于断言验证）
        /// </summary>
        public List<(object key, bool isInstance)> ReleaseLog { get; } = new();

        /// <summary>
        ///     创建模拟资源提供者
        /// </summary>
        /// <param name="loadDelayMs">模拟加载延迟（毫秒），默认 0</param>
        public MockAssetProvider(int loadDelayMs = 0)
        {
            _loadDelayMs = loadDelayMs;
        }

        /// <summary>
        ///     注册模拟资源
        /// </summary>
        /// <param name="key">资源 key</param>
        /// <param name="asset">模拟资源对象</param>
        /// <param name="memorySize">模拟内存占用（字节），默认 1024</param>
        public void RegisterAsset(object key, Object asset, long memorySize = 1024L)
        {
            _assets[key] = asset;
            _memorySizes[key] = memorySize;
        }

        /// <summary>
        ///     注册一个自动创建的 GameObject 模拟资源
        /// </summary>
        public void RegisterGameObject(object key, string name = null, long memorySize = 1024L)
        {
            var go = new GameObject(name ?? key.ToString());
            // 隐藏以避免干扰测试场景
            go.SetActive(false);
            _assets[key] = go;
            _memorySizes[key] = memorySize;
        }

        public async UniTask<Object> LoadAssetAsync<T>(object key, CancellationToken ct = default) where T : Object
        {
            if (_loadDelayMs > 0) await UniTask.Delay(_loadDelayMs, cancellationToken: ct);

            if (!_assets.TryGetValue(key, out var asset))
                throw new System.Exception($"Mock asset not found: {key}");

            return asset;
        }

        public async UniTask<GameObject> InstantiateAsync(object key, Transform parent,
            CancellationToken ct = default)
        {
            if (_loadDelayMs > 0) await UniTask.Delay(_loadDelayMs, cancellationToken: ct);

            if (!_assets.TryGetValue(key, out var sourceAsset))
                throw new System.Exception($"Mock asset not found: {key}");

            var sourceGo = sourceAsset as GameObject;
            if (sourceGo == null)
                throw new System.Exception($"Mock asset is not a GameObject: {key}");

            var instance = Object.Instantiate(sourceGo, parent);
            instance.name = sourceGo.name + "(Clone)";
            instance.SetActive(true);

            var instKey = "$inst_" + key;
            lock (_instantiated)
            {
                _instantiated.Add(instKey);
            }

            return instance;
        }

        public void ReleaseHandle(object key, bool isInstance)
        {
            ReleaseLog.Add((key, isInstance));

            if (isInstance)
            {
                lock (_instantiated)
                {
                    _instantiated.Remove(key);
                }
            }
        }

        public long GetAssetMemorySize(object key)
        {
            return _memorySizes.TryGetValue(key, out var size) ? size : 1024L;
        }

        /// <summary>
        ///     清理所有模拟资源
        /// </summary>
        public void Cleanup()
        {
            foreach (var asset in _assets.Values)
            {
                if (asset != null && asset is GameObject go && go != null)
                    Object.DestroyImmediate(go);
            }

            _assets.Clear();
            _memorySizes.Clear();
            _instantiated.Clear();
            ReleaseLog.Clear();
        }
    }
}
