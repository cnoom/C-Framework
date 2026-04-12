CFramework 的资源管理服务围绕 **"谁持有，谁释放"** 这一核心原则，在 Unity Addressables 系统之上构建了一套完整的资源生命周期管理方案。该服务由两层抽象组成——`IAssetProvider` 负责封装底层加载细节，`IAssetService` 负责引用计数、并发去重、内存预算与生命周期绑定。通过依赖注入注册后，任何模块都可以安全地获取和共享资源，无需关心底层句柄管理的复杂性。

Sources: [IAssetService.cs](Runtime/Asset/IAssetService.cs#L1-L84), [AssetService.cs](Runtime/Asset/AssetService.cs#L1-L29)

## 架构总览

资源管理服务的整体设计遵循 **Provider-Service 分层模式**。底层 `IAssetProvider` 将 Addressables 的 `AsyncOperationHandle` 封装为统一接口，上层 `AssetService` 在此基础上叠加引用计数、并发加载去重、内存预算监控和生命周期绑定等横切关注点。这种分层使得 `AssetService` 的核心逻辑与具体的资源加载实现完全解耦——测试时只需替换为 `MockAssetProvider`，生产环境中默认使用 `AddressableAssetProvider`。

```
┌─────────────────────────────────────────────────────────┐
│                    调用层（业务代码）                       │
│  LoadAsync<T>() / InstantiateAsync() / LinkToScope()     │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│                  AssetService (IAssetService)             │
│  ┌───────────┐ ┌──────────┐ ┌──────────────────────────┐│
│  │ 引用计数   │ │ 并发去重  │ │ 内存预算 (AssetMemoryBudget)││
│  │ _refCounts│ │ _loading  │ │ BudgetBytes / UsedBytes   ││
│  └───────────┘ │ Tasks     │ └──────────────────────────┘│
│  ┌──────────────────────────────────────────────────────┐│
│  │ 生命周期绑定                                         ││
│  │ AssetLifetimeTracker / ScopeLink / DisposeOnDestroy  ││
│  └──────────────────────────────────────────────────────┘│
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│            IAssetProvider (可替换抽象层)                    │
│  ┌──────────────────────┐  ┌─────────────────────────┐  │
│  │ AddressableAsset     │  │ MockAssetProvider        │  │
│  │ Provider (生产环境)   │  │ (单元测试环境)            │  │
│  └──────────────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

在依赖注入体系中，`IAssetProvider` 与 `IAssetService` 分别在不同的安装器中注册。`CoreServiceInstaller` 负责注册 `AddressableAssetProvider` 作为底层提供者，`FrameworkModuleInstaller` 则在此基础上注册 `AssetService`。这意味着你可以通过自定义安装器替换 `IAssetProvider` 的实现，而无需修改 `AssetService` 的任何代码。

Sources: [CoreServiceInstaller.cs](Runtime/Core/DI/CoreServiceInstaller.cs#L1-L22), [FrameworkModuleInstaller.cs](Runtime/Core/DI/FrameworkModuleInstaller.cs#L1-L25)

## 接口设计：两层抽象的职责划分

资源管理系统通过两个核心接口将"如何加载资源"与"如何管理资源生命周期"彻底分离。

### IAssetProvider — 底层资源加载抽象

`IAssetProvider` 是对底层资源加载引擎（Addressables、Resources 或自定义方案）的最薄封装。它只关心四件事：异步加载资源、异步实例化预制体、释放句柄、查询内存占用。该接口不维护任何引用计数逻辑，仅负责原始句柄的持有与释放。

| 方法 | 返回值 | 说明 |
|---|---|---|
| `LoadAssetAsync<T>(key, ct)` | `UniTask<Object>` | 根据 key 异步加载资源 |
| `InstantiateAsync(key, parent, ct)` | `UniTask<GameObject>` | 根据 key 异步实例化预制体 |
| `ReleaseHandle(key, isInstance)` | `void` | 释放底层句柄 |
| `GetAssetMemorySize(key)` | `long` | 返回资源占用的内存字节数 |

Sources: [IAssetService.cs](Runtime/Asset/IAssetService.cs#L14-L35)

### IAssetService — 资源生命周期管理服务

`IAssetService` 是面向业务代码的上层接口，提供了完整的资源生命周期管理能力。调用者通过该接口获取 `AssetHandle`（一个值类型句柄），并通过句柄的 `Dispose()` 方法或 `LinkToScope` 机制实现自动释放。

| 方法 / 属性 | 说明 |
|---|---|
| `MemoryBudget` | 获取当前内存预算监控器 |
| `LoadAsync<T>(key, ct)` | 异步加载资源，返回带引用计数的 `AssetHandle` |
| `InstantiateAsync(key, parent, ct)` | 异步实例化预制体，返回 `GameObject` |
| `LinkToScope(key, scope)` | 将资源绑定到 `GameObject` 或 `IDisposable` 的生命周期 |
| `Release(key)` | 手动释放一次引用 |
| `ReleaseAll()` | 强制释放所有已加载资源（通常在场景切换时调用） |
| `PreloadAsync(keys, progress, maxLoadPerFrame, ct)` | 批量预加载资源，支持分帧和进度回调 |

Sources: [IAssetService.cs](Runtime/Asset/IAssetService.cs#L40-L83)

## AddressableAssetProvider：默认的 Addressables 封装

`AddressableAssetProvider` 是 `IAssetProvider` 的默认实现，内部维护了一个 `Dictionary<object, AsyncOperationHandle>` 来追踪所有活跃的 Addressables 句柄。它在加载成功后将句柄存入字典，在释放时通过 `Addressables.Release(handle)` 归还资源。

值得注意的是实例化操作使用 `$inst_` 前缀对 key 进行了区分，这是为了在内部字典中避免与普通 `LoadAssetAsync` 的句柄产生 key 冲突——同一段代码路径中，同一 key 可能既有加载请求又有实例化请求。

目前 `GetAssetMemorySize` 返回固定值 `1024L`，这是一个占位实现。在实际项目中，你可以通过实现自定义 `IAssetProvider` 来提供精确的内存统计，例如通过 `handle.Result` 获取 `Texture` 的像素大小或 `Mesh` 的顶点缓冲区大小。

Sources: [AddressableAssetProvider.cs](Runtime/Asset/AddressableAssetProvider.cs#L1-L72)

## 引用计数机制

引用计数是 `AssetService` 的核心机制。它通过 `_refCounts` 字典为每个资源 key 维护一个整数计数器，确保资源仅在所有持有者释放后才被真正卸载。

### 单次加载与释放

最基础的场景：调用 `LoadAsync` 时引用计数从 0 变为 1（首次加载触发实际资源加载），调用 `AssetHandle.Dispose()` 时计数减 1，归零后调用 `IAssetProvider.ReleaseHandle` 释放底层句柄。

```csharp
// 引用计数: 0 → 1（触发实际加载）
var handle = await assetService.LoadAsync<GameObject>("MyPrefab");

// 使用资源...
var prefab = handle.As<GameObject>();

// 引用计数: 1 → 0（触发实际释放）
handle.Dispose();
```

### 多次加载同一资源

对同一 key 的多次 `LoadAsync` 调用不会重复加载资源。`AssetService` 检测到 key 已存在于 `_refCounts` 中时，仅递增计数并返回指向同一资源的 `AssetHandle`。这意味着无论多少个模块请求同一资源，底层只加载一次，而所有句柄共享同一份内存。

Sources: [AssetService.cs](Runtime/Asset/AssetService.cs#L33-L92)

### 并发加载去重

当多个异步任务同时请求尚未加载完成的同一资源时，`AssetService` 通过 `_loadingTasks` 字典实现**加载去重**。第一个请求成为"加载发起者"，后续请求成为"等待者"。发起者执行实际加载，完成后通过 `UniTaskCompletionSource` 一次性通知所有等待者。这种设计避免了并发场景下的重复加载问题。

具体流程如下：

1. **首个请求**到达 → `_refCounts[key] = 1`，创建 `UniTaskCompletionSource`，执行实际加载
2. **后续并发请求**到达 → 发现 `_loadingTasks` 中已有记录，直接 `await` 同一个 `Task`
3. 加载完成 → 发起者将结果写入 `_loadedAssets`，通过 `TrySetResult` 通知所有等待者
4. 每个等待者各自递增 `_refCounts[key]`

```
并发请求 A ──┐                            ┌──→ 返回 AssetHandle (ref+1)
             │   首个请求执行实际加载         │
并发请求 B ──┼──→ _loadingTasks[key] ──────┼──→ 返回 AssetHandle (ref+1)
             │   UniTaskCompletionSource    │
并发请求 C ──┘   (只加载一次)               └──→ 返回 AssetHandle (ref+1)
```

Sources: [AssetService.cs](Runtime/Asset/AssetService.cs#L33-L92)

## 生命周期绑定

引用计数解决了"何时释放资源"的问题，但要求每个调用者手动调用 `Dispose()` 仍然容易出错。CFramework 提供了两种生命周期绑定机制，让资源的释放时机自动与持有者的生命状态同步。

### 方式一：AssetHandle.AddTo — 绑定到 GameObject 销毁

`AssetHandleExtensions.AddTo` 是最常用的绑定方式。它将 `AssetHandle` 注册到目标 `GameObject` 上的 `DisposeOnDestroy` 组件中。当该 `GameObject` 被销毁时（例如 UI 面板关闭、敌人死亡），`OnDestroy` 回调自动触发所有注册句柄的 `Dispose()`，从而递减引用计数。

```csharp
// 加载资源并绑定到 this.gameObject 的生命周期
var handle = await assetService.LoadAsync<Sprite>("Icon_Health");
handle.AddTo(gameObject);  // GameObject 销毁时自动释放

// 也可以绑定到 MonoBehaviour
handle.AddTo(this);
```

`DisposeOnDestroy` 内部维护了一个线程安全的句柄列表，通过 `_destroyed` 标志位防止在销毁后误添加新句柄。

Sources: [AssetHandleExtensions.cs](Runtime/Asset/AssetHandleExtensions.cs#L1-L68)

### 方式二：LinkToScope — 灵活的作用域绑定

`IAssetService.LinkToScope` 提供了更通用的绑定方案，支持两种作用域类型：

- **GameObject 作用域**：在目标 GameObject 上挂载 `AssetLifetimeTracker` 组件，当 GameObject 销毁时自动调用 `Release`
- **IDisposable 作用域**：创建 `ScopeLink` 组合 Disposable，当 `ScopeLink.Dispose()` 被调用时同时释放资源和作用域对象

```csharp
// 绑定到 GameObject 生命周期
var binding = assetService.LinkToScope("MyPrefab", someGameObject);
// someGameObject 销毁时自动 Release

// 绑定到 IDisposable 作用域
var binding = assetService.LinkToScope("MyTexture", cancellationTokenSource);
// 调用 binding.Dispose() 时同时 Release 资源和 CancellationTokenSource
```

三种生命周期绑定方式的适用场景对比如下：

| 绑定方式 | 适用场景 | 释放触发时机 | 线程安全 |
|---|---|---|---|
| `AssetHandle.AddTo(GameObject)` | 资源与 UI/实体生命周期一致 | GameObject 的 `OnDestroy` | ✅ |
| `AssetHandle.AddTo(MonoBehaviour)` | 同上，语法糖 | MonoBehaviour 所在 GameObject 销毁 | ✅ |
| `LinkToScope(key, GameObject)` | 需要提前手动解绑 | GameObject 销毁或手动 `IDisposable.Dispose()` | ✅ |
| `LinkToScope(key, IDisposable)` | 资源与某个作用域对象绑定 | 手动 `Dispose()` | ✅ |

Sources: [AssetService.cs](Runtime/Asset/AssetService.cs#L111-L134), [AssetHandleExtensions.cs](Runtime/Asset/AssetHandleExtensions.cs#L1-L39)

## 实例化与资源加载的 Key 隔离

`AssetService` 将**加载资源**与**实例化预制体**视为两种不同的操作，通过 `$inst_` 前缀在内部字典中进行 key 隔离。这意味着加载一个预制体（`LoadAsync`）和实例化它（`InstantiateAsync`）使用不同的内部 key，它们的引用计数互不干扰。

这种设计的关键考量是：**加载**操作持有的是原始资产（如预制体本身），而**实例化**操作持有的是场景中的运行时实例。当实例被销毁时，不应影响预制体资产本身；当预制体资产被卸载时，也不应影响已经在场景中运行的实例。

```csharp
// 内部 key: "MyPrefab" → 引用计数独立管理
var assetHandle = await assetService.LoadAsync<GameObject>("MyPrefab");

// 内部 key: "$inst_MyPrefab" → 引用计数独立管理
var instance = await assetService.InstantiateAsync("MyPrefab", parent);

// 释放实例（不影响已加载的预制体资产）
assetService.Release("$inst_MyPrefab");
```

Sources: [AssetService.cs](Runtime/Asset/AssetService.cs#L94-L109), [AddressableAssetProvider.cs](Runtime/Asset/AddressableAssetProvider.cs#L37-L53)

## 内存预算监控

`AssetMemoryBudget` 是一个轻量级的内存监控组件，通过 `BudgetBytes`（预算上限）和 `UsedBytes`（已使用量）两个属性实时追踪资源内存占用。每当资源加载完成时，`AssetService` 将 `Provider` 返回的内存大小累加到 `UsedBytes`；每当资源释放时相应扣减。

当 `UsedBytes` 超过 `BudgetBytes` 时，`CheckBudget()` 触发 `OnBudgetExceeded` 事件，传入当前使用率（`UsageRatio`）。调用者可以订阅此事件来执行自定义策略，例如按 LRU 顺序卸载旧资源、显示内存警告 UI 或记录日志。

预算上限来自 `FrameworkSettings.MemoryBudgetMB` 配置，在 `AssetService` 构造时转换为字节数：

```csharp
// FrameworkSettings 中配置（默认 512 MB）
settings.MemoryBudgetMB = 512;

// AssetService 构造时初始化预算
MemoryBudget = new AssetMemoryBudget
{
    BudgetBytes = settings.MemoryBudgetMB * 1024L * 1024L  // 512 * 1024 * 1024 = 536,870,912 bytes
};
```

Sources: [AssetMemoryBudget.cs](Runtime/Asset/AssetMemoryBudget.cs#L1-L22), [AssetService.cs](Runtime/Asset/AssetService.cs#L22-L29), [FrameworkSettings.cs](Runtime/Core/FrameworkSettings.cs#L13-L16)

## 分帧预加载

`PreloadAsync` 方法提供了批量资源预加载能力，通过 `maxLoadPerFrame` 参数控制每帧最大加载数量，避免在一次性加载大量资源时造成帧率卡顿。当某帧内加载数达到上限时，调用 `UniTask.Yield()` 让出当前帧，在下一帧继续加载。

该方法的第三个参数 `IProgress<float>` 支持进度回调（0.0 ~ 1.0），适合在加载界面中显示进度条。加载失败的资源不会中断整个预加载流程，而是通过 `Debug.LogWarning` 记录警告后继续处理后续资源。

```csharp
var keys = new object[] { "UIAtlas_1", "UIAtlas_2", "UIAtlas_3" };
var progress = new Progress<float>(p => loadingBar.value = p);

// 每帧最多加载 3 个资源，带进度回调
await assetService.PreloadAsync(keys, progress, maxLoadPerFrame: 3);
```

Sources: [AssetService.cs](Runtime/Asset/AssetService.cs#L182-L217)

## 线程安全设计

`AssetService` 内部所有的状态修改操作都通过 `lock (_lock)` 进行保护，确保在多线程或异步并发场景下的数据一致性。这包括 `_refCounts`（引用计数）、`_loadedAssets`（已加载资源）、`_loadingTasks`（正在加载的任务）和 `_instanceFlags`（实例化标记）四个核心字典的读写操作。

`ReleaseAll` 方法同样在锁内完成所有清理工作，确保在场景切换等需要批量释放的时刻不会出现竞态条件。

Sources: [AssetService.cs](Runtime/Asset/AssetService.cs#L136-L179)

## 依赖注入注册

资源管理服务通过两个安装器完成注册。在 `CoreServiceInstaller` 中，`AddressableAssetProvider` 作为 `IAssetProvider` 的单例实现被注册到容器中；在 `FrameworkModuleInstaller` 中，`AssetService` 作为 `IAssetService` 的实现被注册。由于 `AssetService` 的构造函数同时依赖 `FrameworkSettings` 和 `IAssetProvider`，VContainer 会自动注入这两个依赖。

```csharp
// CoreServiceInstaller.cs — 注册底层提供者
builder.Register<IAssetProvider, AddressableAssetProvider>(Lifetime.Singleton);

// FrameworkModuleInstaller.cs — 注册资源服务
builder.InstallModule<IAssetService, AssetService>();
```

如果你需要使用自定义的资源加载方案（例如基于 AssetBundle 的加载器），只需创建一个实现 `IAssetProvider` 的新类，并通过自定义安装器替换注册即可，无需修改 `AssetService` 的任何代码。详细的自定义扩展方法请参考 [框架扩展指南：自定义 IInstaller、IAssetProvider 与 ISceneTransition](23-kuang-ji-kuo-zhan-zhi-nan-zi-ding-yi-iinstaller-iassetprovider-yu-iscenetransition)。

Sources: [CoreServiceInstaller.cs](Runtime/Core/DI/CoreServiceInstaller.cs#L16-L21), [FrameworkModuleInstaller.cs](Runtime/Core/DI/FrameworkModuleInstaller.cs#L16-L24)

## 完整使用示例

以下示例展示了资源管理服务在实际业务场景中的典型用法：

```csharp
public class EnemySpawner : MonoBehaviour
{
    [Inject] private IAssetService _assetService;
    
    private readonly List<AssetHandle> _handles = new();
    
    private async UniTaskVoid SpawnEnemy(string enemyAddress)
    {
        // 1. 加载预制体资源，引用计数 +1
        var handle = await _assetService.LoadAsync<GameObject>(enemyAddress);
        
        // 2. 绑定到自身 GameObject 的生命周期
        //    当 EnemySpawner 被销毁时，handle 自动 Dispose
        handle.AddTo(gameObject);
        
        // 3. 实例化敌人到场景中
        var enemy = await _assetService.InstantiateAsync(enemyAddress, transform);
    }
    
    private void OnDestroy()
    {
        // LinkToScope 或 AddTo 已自动管理释放
        // 无需手动调用 Release
    }
}
```

Sources: [AssetHandleExtensions.cs](Runtime/Asset/AssetHandleExtensions.cs#L14-L28), [AssetService.cs](Runtime/Asset/AssetService.cs#L33-L109)

## 延伸阅读

- **资源句柄的详细 API 与内存预算机制**：[AssetHandle 资源句柄：加载、实例化、内存预算与分帧预加载](11-assethandle-zi-yuan-ju-bing-jia-zai-shi-li-hua-nei-cun-yu-suan-yu-fen-zheng-yu-jia-zai)
- **资源常量自动生成与后处理**：[Addressable 常量代码生成器与资源后处理器](20-addressable-chang-liang-dai-ma-sheng-cheng-qi-yu-zi-yuan-hou-chu-li-qi)
- **自定义资源加载方案**：[框架扩展指南：自定义 IInstaller、IAssetProvider 与 ISceneTransition](23-kuang-ji-kuo-zhan-zhi-nan-zi-ding-yi-iinstaller-iassetprovider-yu-iscenetransition)
- **测试中的 Mock 替换模式**：[单元测试指南：测试覆盖策略与 Mock 替换模式](22-dan-yuan-ce-shi-zhi-nan-ce-shi-fu-gai-ce-lue-yu-mock-ti-huan-mo-shi)