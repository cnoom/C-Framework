using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     GameObject 对象池实现
    ///     <para>通过 IAssetService 加载 Prefab 并实例化</para>
    ///     <para>对象归还时 SetActive(false)，获取时 SetActive(true)</para>
    ///     <para>支持同步获取（空闲栈有对象时）和异步获取（需要实例化时）</para>
    /// </summary>
    public sealed class GameObjectPool : IPool<GameObject>
    {
        private const int DefaultMaxSize = 1000;

        private readonly Stack<GameObject> _pool;
        private readonly HashSet<GameObject> _activeSet;
        private readonly IAssetService _assetService;
        private readonly object _prefabKey;
        private readonly Transform _parent;
        private readonly int _maxSize;
        private readonly object _lock = new();
        private bool _disposed;

        // Prefab 缓存（首次加载后复用，持有 AssetHandle 防止资源被回收）
        private GameObject _prefab;
        private AssetHandle _prefabHandle;
        private bool _prefabLoaded;

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string ItemTypeName => "GameObject";

        /// <inheritdoc />
        public int CountInactive
        {
            get
            {
                lock (_lock) { return _pool.Count; }
            }
        }

        /// <inheritdoc />
        public int CountActive
        {
            get
            {
                lock (_lock) { return _activeSet.Count; }
            }
        }

        /// <inheritdoc />
        public int CountAll
        {
            get
            {
                lock (_lock) { return _pool.Count + _activeSet.Count; }
            }
        }

        /// <summary>
        ///     Prefab 是否已加载
        /// </summary>
        public bool IsPrefabLoaded
        {
            get
            {
                lock (_lock) { return _prefabLoaded; }
            }
        }

        /// <summary>
        ///     构造 GameObject 对象池
        /// </summary>
        /// <param name="name">池名称</param>
        /// <param name="assetService">资源服务</param>
        /// <param name="prefabKey">Prefab Addressable Key</param>
        /// <param name="parent">默认父节点</param>
        /// <param name="defaultCapacity">默认容量</param>
        /// <param name="maxSize">最大容量（0=不限）</param>
        public GameObjectPool(
            string name,
            IAssetService assetService,
            object prefabKey,
            Transform parent = null,
            int defaultCapacity = 5,
            int maxSize = 0)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
            _prefabKey = prefabKey ?? throw new ArgumentNullException(nameof(prefabKey));
            _parent = parent;
            _maxSize = maxSize > 0 ? maxSize : DefaultMaxSize;
            _pool = new Stack<GameObject>(defaultCapacity);
            _activeSet = new HashSet<GameObject>();
        }

        /// <inheritdoc />
        public GameObject Get()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    var obj = _pool.Pop();
                    _activeSet.Add(obj);
                    Activate(obj);
                    return obj;
                }

                // 无空闲对象且未加载 prefab，无法同步创建
                if (!_prefabLoaded || _prefab == null) return null;

                // 同步实例化
                var instance = Object.Instantiate(_prefab, _parent);
                instance.name = $"{_prefab.name}(Pooled)";
                _activeSet.Add(instance);
                Activate(instance);
                return instance;
            }
        }

        /// <inheritdoc />
        public PoolHandle<GameObject> Get(out GameObject item)
        {
            item = Get();
            return new PoolHandle<GameObject>(item, this);
        }

        /// <summary>
        ///     尝试同步获取（池中有空闲对象时）
        /// </summary>
        public bool TryGet(out GameObject item)
        {
            item = Get();
            return item != null;
        }

        /// <summary>
        ///     异步获取（需要实例化时会 await）
        /// </summary>
        public async UniTask<GameObject> GetAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    var obj = _pool.Pop();
                    _activeSet.Add(obj);
                    Activate(obj);
                    return obj;
                }
            }

            // 需要实例化新对象，先确保 Prefab 已加载
            await EnsurePrefabLoaded(ct);

            lock (_lock)
            {
                // 双检：等待期间可能已有对象归还
                if (_pool.Count > 0)
                {
                    var obj = _pool.Pop();
                    _activeSet.Add(obj);
                    Activate(obj);
                    return obj;
                }

                var instance = Object.Instantiate(_prefab, _parent);
                instance.name = $"{_prefab.name}(Pooled)";
                _activeSet.Add(instance);
                Activate(instance);
                return instance;
            }
        }

        /// <inheritdoc />
        public void Return(GameObject item)
        {
            if (item == null) return;

            lock (_lock)
            {
                if (!_activeSet.Remove(item)) return;

                NotifyReturn(item);
                item.SetActive(false);

                if (item.transform.parent != _parent && _parent != null)
                {
                    item.transform.SetParent(_parent);
                }

                if (_pool.Count < _maxSize)
                {
                    _pool.Push(item);
                }
                else
                {
                    Object.Destroy(item);
                }
            }
        }

        /// <inheritdoc />
        public void ReturnAll()
        {
            lock (_lock)
            {
                foreach (var obj in _activeSet)
                {
                    NotifyReturn(obj);
                    obj.SetActive(false);

                    if (obj.transform.parent != _parent && _parent != null)
                    {
                        obj.transform.SetParent(_parent);
                    }

                    if (_pool.Count < _maxSize)
                    {
                        _pool.Push(obj);
                    }
                    else
                    {
                        Object.Destroy(obj);
                    }
                }

                _activeSet.Clear();
            }
        }

        /// <inheritdoc />
        public void Prewarm(int count)
        {
            lock (_lock)
            {
                if (!_prefabLoaded || _prefab == null)
                {
                    LogUtility.Warning(nameof(GameObjectPool),
                        $"Prefab 未加载，无法同步预分配: {Name}，请使用 PrewarmAsync");
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    if (_pool.Count >= _maxSize) break;

                    var instance = Object.Instantiate(_prefab, _parent);
                    instance.name = $"{_prefab.name}(Pooled)";
                    instance.SetActive(false);
                    _pool.Push(instance);
                }
            }
        }

        /// <summary>
        ///     异步预分配
        /// </summary>
        public async UniTask PrewarmAsync(int count, CancellationToken ct = default)
        {
            await EnsurePrefabLoaded(ct);

            lock (_lock)
            {
                for (int i = 0; i < count; i++)
                {
                    if (_pool.Count >= _maxSize) break;

                    var instance = Object.Instantiate(_prefab, _parent);
                    instance.name = $"{_prefab.name}(Pooled)";
                    instance.SetActive(false);
                    _pool.Push(instance);
                }
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            lock (_lock)
            {
                while (_pool.Count > 0)
                {
                    var obj = _pool.Pop();
                    if (obj != null) Object.Destroy(obj);
                }
            }
        }

        /// <inheritdoc />
        public void ShrinkTo(int capacity)
        {
            lock (_lock)
            {
                while (_pool.Count > capacity)
                {
                    var obj = _pool.Pop();
                    if (obj != null) Object.Destroy(obj);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;

                foreach (var obj in _activeSet)
                {
                    if (obj != null) Object.Destroy(obj);
                }

                while (_pool.Count > 0)
                {
                    var obj = _pool.Pop();
                    if (obj != null) Object.Destroy(obj);
                }

                _activeSet.Clear();
                _pool.Clear();

                // 释放 Prefab 资源句柄
                _prefabHandle.Dispose();
                _prefabHandle = default;
                _prefab = null;
                _prefabLoaded = false;
            }
        }

        /// <summary>
        ///     确保 Prefab 已加载（持有 AssetHandle 防止资源被 GC）
        /// </summary>
        private async UniTask EnsurePrefabLoaded(CancellationToken ct)
        {
            if (_prefabLoaded) return;

            var handle = await _assetService.LoadAsync<GameObject>(_prefabKey, ct);

            lock (_lock)
            {
                if (_prefabLoaded) return;

                _prefab = handle.As<GameObject>();
                _prefabHandle = handle;
                _prefabLoaded = true;
            }
        }

        /// <summary>
        ///     激活对象并触发 IPoolable 回调
        /// </summary>
        private void Activate(GameObject obj)
        {
            // 触发所有 IPoolable 组件
            var poolables = obj.GetComponents<IPoolable>();
            if (poolables != null)
            {
                foreach (var p in poolables)
                {
                    p.OnGet();
                }
            }

            obj.SetActive(true);
        }

        /// <summary>
        ///     通知对象归还
        /// </summary>
        private void NotifyReturn(GameObject obj)
        {
            var poolables = obj.GetComponents<IPoolable>();
            if (poolables != null)
            {
                foreach (var p in poolables)
                {
                    p.OnReturn();
                }
            }
        }
    }
}
