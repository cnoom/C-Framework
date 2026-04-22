# CFramework

基于 VContainer、UniTask、R3 和 Addressables 的 Unity 游戏开发框架。Odin Inspector 为可选依赖。

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity 2021.3+](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity3d.com/get-unity/download)

## 模块总览

| 模块 | 说明 |
|------|------|
| Core | 生命周期管理、事件系统、依赖注入、全局异常处理、黑板系统、日志 |
| Asset | Addressables 封装，引用计数与生命周期绑定 |
| UI | 面板管理、自动绑定、响应式数据绑定、代码生成 |
| Audio | 双音轨 BGM 系统，分组音量控制 |
| Scene | 场景加载管理，支持过渡动画和叠加场景 |
| Config | 基于 ScriptableObject 的配置表系统 |
| Save | 原子写入存档系统，脏状态管理 |
| State | 有限状态机（FSM），支持普通状态与栈状态 |
| Utility | 字符串、随机数、日志等通用工具 |

## 可选模块定义符号

| 符号 | 说明 |
|------|------|
| `CFRAMEWORK_AUDIO` | 启用音频模块（AudioService）。未定义时整个音频模块不会编译，需在 Player Settings → Scripting Define Symbols 中手动添加 |

## 环境要求

- Unity 2021.3+
- VContainer 1.17.0+
- UniTask 2.5.0+
- R3 1.3.0+
- Addressables 1.21+

### 可选依赖

- **Odin Inspector 3.0+**：安装后将自动检测并启用增强的 Inspector 和编辑器功能。未安装时框架提供默认的可视化编辑实现。

## 安装

在 `Packages/manifest.json` 中先添加依赖(不包含Odin Inspector)：

```json
{
  "dependencies": {
    "jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer",
    "cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask",
    "com.glitchenzo.nugetforunity": "https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity",
  }
}
```
然后在NuGetForUnity中安装[R3](https://github.com/Cysharp/R3?tab=readme-ov-file#unity)

最后在 `Packages/manifest.json` 中添加CFramework依赖：
```
{
  "dependencies": {
    "com.cframework": "https://github.com/cnoom/C-Framework.git"
  }
}
```

## 快速开始
### 1. 创建框架配置

```
Assets > Create > CFramework > Framework Settings
```

### 2. 游戏入口

```csharp
using CFramework;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class GameEntry : MonoBehaviour
{
    [SerializeField] private FrameworkSettings settings;

    private async UniTaskVoid Start()
    {
        // 创建全局作用域
        var scope = GameScope.Create(settings);
        var container = scope.Container;

        // 注册全局异常处理器
        var exceptionDispatcher = container.Resolve<IExceptionDispatcher>();
        exceptionDispatcher.RegisterHandler(ex =>
        {
            Debug.LogError($"[全局异常] {ex.Message}");
        });

        // 初始化服务
        await UniTask.WhenAll(
            container.Resolve<IConfigService>().InitializeAsync(),
            container.Resolve<IAssetService>().InitializeAsync()
        );

        // 加载初始场景
        var sceneService = container.Resolve<ISceneService>();
        await sceneService.LoadAsync("MainMenu");
    }
}
```

### 3. 事件系统

```csharp
// 定义事件（使用 struct 避免 GC）
public readonly struct PlayerDiedEvent : IEvent
{
    public readonly int PlayerId;
    public PlayerDiedEvent(int id) => PlayerId = id;
}

// 订阅（支持优先级，值越大越先执行）
eventBus.Subscribe<PlayerDiedEvent>(e =>
{
    Debug.Log($"玩家 {e.PlayerId} 死亡");
}, priority: 10);

// 发布
eventBus.Publish(new PlayerDiedEvent(1));

// 响应式订阅（R3）
eventBus.Receive<PlayerDiedEvent>()
    .Subscribe(e => Debug.Log($"响应式收到: {e.PlayerId}"))
    .AddTo(disposables);
```

### 4. 资源加载

```csharp
public class PlayerView : MonoBehaviour
{
    private AssetHandle _modelHandle;

    public async UniTask LoadModel(IAssetService assetService)
    {
        // 加载资源（自动引用计数）
        _modelHandle = await assetService.LoadAsync<GameObject>("Assets/Models/Hero.prefab");

        // 绑定到 GameObject 生命周期（销毁时自动释放）
        _modelHandle.AddTo(gameObject);

        // 实例化
        Instantiate(_modelHandle.Asset, transform);
    }
}
```

### 5. UI 面板

#### 代码生成方式（推荐）

**步骤一：** 在预制体中按命名规范命名组件（`btn_`、`txt_`、`img_`、`slider_`、`toggle_` 前缀）。

**步骤二：** 选择预制体，通过菜单生成绑定代码：
- 右键菜单：`CFramework → 生成UI绑定代码`
- 菜单栏：`CFramework → UI → UI面板生成器`

**步骤三：** 编写业务逻辑：

```csharp
// PlayerPanel.cs - 业务逻辑
public sealed partial class PlayerPanel : UIPanel
{
    // 组件字段已自动生成在 PlayerPanel.Bindings.cs 中
    // 例如：private Text _coinText; public Text CoinText => _coinText;

    private PlayerViewModel _viewModel;

    public override UniTask OnOpen(CancellationToken ct)
    {
        _viewModel = new PlayerViewModel();

        _viewModel.Gold
            .SubscribeToText(CoinText, "Gold: {0}")
            .AddTo(Disposables);

        CloseButton.BindTo(Close)
            .AddTo(Disposables);

        return UniTask.CompletedTask;
    }
}
```

生成的文件结构：

```
UI/Panels/
├── PlayerPanel.prefab          # UI 预制体
├── PlayerPanel.cs              # 业务逻辑（手动编写）
└── PlayerPanel.Bindings.cs     # 组件绑定（自动生成）
```

#### 手动绑定方式

```csharp
public sealed class PlayerPanel : UIPanel
{
    [UIAutoBind] private Text coinText;
    [UIAutoBind] private Button closeButton;

    public override UniTask OnOpen(CancellationToken ct)
    {
        _viewModel.Gold
            .SubscribeToText(coinText)
            .AddTo(Disposables);

        closeButton.OnClickAsObservable()
            .Subscribe(_ => Close())
            .AddTo(Disposables);

        return UniTask.CompletedTask;
    }
}
```

### 6. 状态机

```csharp
// 定义状态
public class IdleState : IStateEnter, IStateUpdate
{
    public void Enter() { /* 进入状态 */ }
    public void Update() { /* 每帧更新 */ }
}

// 创建并使用状态机
var fsm = new StateMachine();
fsm.AddState<IdleState>();
fsm.AddState<RunState>();
fsm.ChangeState<IdleState>();

// 栈状态机（支持推入/弹出）
var stackFsm = new StateMachineStack();
stackFsm.Push<MenuState>();
stackFsm.Push<PauseState>(); // 暂停覆盖菜单
stackFsm.Pop();              // 回到菜单
```

### 7. 黑板系统

```csharp
var blackboard = new Blackboard();
blackboard.Set("Health", 100);
blackboard.Set("Name", "Player1");

if (blackboard.TryGet<int>("Health", out var hp))
{
    Debug.Log($"生命值: {hp}");
}
```

### 8. 配置表

框架提供基于 ScriptableObject 的配置表系统，支持多数据源（SO、JSON、Resources、内存）和热重载。

#### 定义配置数据项

```csharp
// ItemData.cs - 配置数据行
public class ItemData : IConfigItem<int>
{
    public int Id { get; set; }          // 主键
    public string Name { get; set; }
    public int Price { get; set; }
}
```

#### 加载配置表

```csharp
public class GameEntry : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var configService = container.Resolve<IConfigService>();

        // 加载单个配置表
        await configService.LoadAsync<ItemData>("Assets/Config/ItemConfig.asset");

        // 查询数据
        var item = configService.Get<int, ItemData>(1001);
        Debug.Log($"物品名称: {item.Name}");
    }
}
```

#### 使用不同的数据源

框架提供四种 `IConfigProvider` 实现：

| Provider | 数据源 | 适用场景 |
|----------|--------|----------|
| `SOConfigProvider` | Addressables + ScriptableObject | 生产环境，支持热更新 |
| `JsonConfigProvider` | Addressables + JSON 文件 | 外部工具导出的配置 |
| `ResourcesConfigProvider` | Resources 目录 | 小型项目，无需 Addressables |
| `MemoryConfigProvider` | 内存注入 | 单元测试、快速原型 |

```csharp
// 使用 JSON 数据源
var jsonProvider = new JsonConfigProvider(assetService);
var configService = new ConfigService(jsonProvider, settings);
```

## 模块详情

### Core 模块

Core 模块是框架的基础，提供以下能力：

- **ScopeServiceBase**：服务基类，提供统一的初始化和释放生命周期管理
- **GameScope / SceneScope**：VContainer 作用域集成，管理全局和场景级别的依赖注入
- **IEventBus**：事件总线，支持同步/异步发布订阅，支持优先级和 R3 响应式订阅
- **IExceptionDispatcher**：全局异常分发器，统一捕获 UniTask 和 R3 中的未处理异常
- **Blackboard**：黑板系统，键值对数据共享，支持泛型存取
- **ILogger / UnityLogger**：日志系统封装，支持日志级别控制

### Asset 模块

- **AssetHandle**：资源句柄结构体，封装引用计数，支持 `using` 自动释放
- **AssetMemoryBudget**：内存预算管理，监控资源内存使用
- **AssetHandleExtensions**：`AddTo(gameObject)` 生命周期绑定，对象销毁时自动释放资源
- 分帧预加载，避免大量资源同时加载导致卡顿

### UI 模块

- **UIPanel**：UI 面板基类，提供 `OnOpen` / `OnClose` 生命周期
- **UIBindingExtensions**：R3 响应式数据绑定扩展（Text、Image、Slider、Toggle 等）
- **UIAutoBindAttribute / UIAutoBinder**：运行时反射自动绑定
- **UIPanelGenerator**：编辑器代码生成器，根据预制体自动生成绑定代码
- 导航栈：支持容量限制的 UI 导航历史管理

### Audio 模块

- 指定 AudioMixer：通过 FrameworkSettings 指定 AudioMixer，框架自动解析分组和初始化
- 双音轨 BGM：无缝切换背景音乐，支持交叉淡入淡出
- 分组音量控制：基于 AudioMixer Group 层级自动生成分组，独立音量控制
- 空间音效：支持在指定位置播放 3D 音效

### Scene 模块

- 进度回调：场景加载过程中实时回调加载进度
- **ISceneTransition**：场景过渡动画接口，内置 `FadeTransition` 淡入淡出实现
- 叠加场景：支持叠加加载多个场景

### Config 模块

- **IConfigItem<TKey>**：配置数据项接口，定义主键
- **ConfigTable<TKey, TValue>**：纯 C# 数据容器，支持 `Get()` / `TryGet()` / `All` / `Contains()` 查询
- **IConfigService**：配置服务接口，提供加载、查询、重载、卸载功能
- **ConfigService**：配置服务实现，通过反射推断 TKey，管理 ConfigTable 生命周期
- **IConfigProvider**：数据加载策略接口，抽象数据来源
- 四种 Provider 实现：`SOConfigProvider`、`JsonConfigProvider`、`ResourcesConfigProvider`、`MemoryConfigProvider`
- 热重载支持：`ReloadAsync()` 重新加载配置表
- 地址自动解析：未指定地址时按 `{ConfigAddressPrefix}/{TypeName}` 自动构建

### Save 模块

- 原子写入：临时文件 + 重命名机制，确保存档完整性
- **IsDirty**：脏状态追踪，支持脏状态变化事件
- 自动保存：可配置间隔的自动保存功能
- 多存档槽：支持多个独立存档位

### State 模块

- **IState / IStateEnter / IStateExit / IStateUpdate / IStateFixedUpdate**：细粒度状态接口，按需实现
- **StateMachine**：标准有限状态机
- **IStackState / StateMachineStack**：栈状态机，支持 Push/Pop，适用于菜单叠加、暂停等场景
- **IStateMachineHolder**：状态机持有者接口，便于组合使用

### Utility 模块

- **StringUtility**：字符串通用工具
- **StringFormatterUtility**：字符串格式化工具
- **StringRichTextUtility**：富文本工具
- **StringTemplateUtility**：字符串模板工具
- **RandomUtility**：随机数工具
- **LogUtility**：日志工具

## 编辑器工具

| 工具 | 菜单路径 | 说明 |
|------|----------|------|
| FrameworkSettings | `Create > CFramework > Framework Settings` | 框架全局配置资产 |
| FrameworkSettingsEditor | — | 配置资产自定义 Inspector |
| UIPanelGenerator | `CFramework → 生成UI绑定代码` | UI 面板绑定代码生成器 |
| UIPanelGeneratorWindow | `CFramework → UI → UI面板生成器` | 代码生成器配置窗口 |
| AddressableConstantsGenerator | — | Addressable 资源常量代码生成器 |
| ConfigEditorWindow | `CFramework → Config → 配置表编辑器` | 配置表可视化编辑窗口 |
| ConfigCreatorWindow | `CFramework → Config → 配置表创建器` | 配置表创建和代码生成窗口 |
| ConfigTableAssetEditor | — | 配置表资产自定义 Inspector |
| ExceptionViewerWindow | `CFramework → Tools → 异常查看器` | 运行时异常查看窗口 |

## FrameworkSettings 配置项

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| MemoryBudgetMB | int | 512 | 资源内存预算上限（MB） |
| MaxLoadPerFrame | int | 5 | 每帧最大资源加载数量 |
| MaxNavigationStack | int | 10 | UI 导航栈最大深度 |
| AudioMixerRef | AudioMixer | null | 音频混合器引用（未设置时自动加载框架内置 AudioMixer） |
| GroupSlotConfig | string | "Master_Music:2,Master_Effect:5" | 各分组预分配 Slot 数量 |
| MaxSlotsPerGroup | int | 20 | 分组 Slot 自动扩容上限 |
| VolumePrefsPrefix | string | "Audio_Volume_" | 音量持久化存储键前缀 |
| AutoSaveInterval | int | 60 | 自动保存间隔（秒） |
| EncryptionKey | string | "CFramework_DefaultKey" | 存档加密密钥（AES-128 需要 16 字符） |
| LogLevel | LogLevel | Debug | 日志级别 |

## 测试

打开 Test Runner（`Window > General > Test Runner`），选择 EditMode 或 PlayMode 标签页运行测试。

测试覆盖模块：Core（EventBus、ExceptionDispatcher）、Log、Asset、UI、Audio、Scene、Config、Save、State/FSM。

## 项目结构

```
Assets/CFramework/
├── Runtime/                    # 运行时代码
│   ├── Core/                   # 核心模块
│   │   ├── Blackboard/         # 黑板系统
│   │   ├── DI/                 # 依赖注入（Scope、Installer）
│   │   ├── Event/              # 事件系统
│   │   ├── Exception/          # 异常处理
│   │   └── Log/                # 日志系统
│   ├── Asset/                  # 资源管理模块
│   ├── Audio/                  # 音频模块
│   ├── Config/                 # 配置表模块
│   ├── Save/                   # 存档模块
│   ├── Scene/                  # 场景模块
│   ├── State/                  # 状态机模块
│   │   └── FSM/                # 有限状态机
│   ├── UI/                     # UI 模块
│   └── Utility/                # 通用工具
├── Editor/                     # 编辑器代码
│   ├── Configs/                # 编辑器配置
│   ├── Generators/             # 代码生成器
│   ├── Inspectors/             # 自定义 Inspector
│   ├── Utilities/              # 编辑器工具
│   └── Windows/                # 编辑器窗口
├── Tests/                      # 单元测试
│   └── Runtime/
├── Docs/                       # 文档
├── package.json
├── README.md
└── CHANGELOG.md
```

## 开源协议

MIT License

## 致谢

- [VContainer](https://vcontainer.hadashikick.jp/) — 高性能依赖注入容器
- [UniTask](https://github.com/Cysharp/UniTask) — Unity 高效异步方案
- [R3](https://github.com/Cysharp/R3) — Unity 响应式扩展库
- [Odin Inspector](https://odininspector.com/) — 序列化与 Inspector 增强
- [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest) — Unity 资源管理系统


---

**作者**：CNoom
**主页**：https://github.com/cnoom
