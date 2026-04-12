# 更新日志

框架所有重要变更均记录于此。

## [1.4.2] - 2026-03-31

### 修复

- **安全修复**：SaveService AES 加密使用随机 IV 替代确定性 IV，密文格式变更为 `[IV][密文]`
- **内存泄漏修复**：GameScope 添加 `[RuntimeInitializeOnLoadMethod]` 自动清理静态 `_additionalInstallers`，防止 Domain Reload 关闭时残留
- **引用计数修复**：AssetService.InstantiateAsync 使用独立 key 前缀 `"$inst_"` 避免与 LoadAsync 冲突
- **命名空间修复**：修正 Utility、String、State/FSM 接口中 `CFramework.CFramework.Runtime.*` → 正确命名空间
- **命名空间修复**：同步修正测试文件中的旧命名空间引用
- StateMachine/StateMachineStack 的 `TryChangeState` 异常不再静默吞掉，添加 Warning 日志
- AudioService SFX 延迟回收考虑 pitch 的影响（`clip.length / pitch`）
- ConfigService.LoadAllAsync 改为抛出 `NotImplementedException`，避免静默失败
- UnityLogger 日志级别直接读取 settings，支持运行时热更新

### 变更

- EventBus 同步/异步 handler 改为订阅时维护排序（插入排序），避免每次 Publish 重新排序
- ConfigService.Get 缓存反射委托，避免每次调用 `MakeGenericType`

## [1.4.1] - 2026-03-26

### 修复

- 补充测试项目的程序集引用（Sirenix、Unity.ResourceManager）
- 为所有 UnityTest 添加超时保护，修复运行测试时 Unity 退出的问题
- AssetServiceTests 添加资源存在性检查，缺少 Addressables 资源时自动跳过

### 新增

- 新增 AudioServiceTests、SceneServiceTests、ConfigServiceTests 单元测试
- 测试覆盖率从 57% 提升到 100%

### 变更

- **破坏性变更**：复合文件拆分为单一职责文件
  - Config 模块：`IConfigService.cs` → `ConfigDataSource.cs`、`ConfigTableBase.cs`、`ConfigTable.cs`、`IConfigService.cs`
  - Asset 模块：`IAssetService.cs` → `AssetHandle.cs`、`AssetMemoryBudget.cs`、`IAssetService.cs`
  - Audio 模块：`IAudioService.cs` → `AudioGroup.cs`、`IAudioService.cs`
  - Save 模块：`ISaveService.cs` → `SaveDataBase.cs`、`SaveSlotInfo.cs`、`ISaveService.cs`
  - Scene 模块：拆分为 `ISceneTransition.cs`、`ISceneService.cs`、`FadeTransition.cs`、`SceneService.cs`

## [1.4.0] - 2026-03-26

### 新增

- UI 代码生成器：根据预制体自动生成组件绑定代码
  - `UIPanelGenerator`：从预制体生成绑定代码
  - `UIPanelGeneratorWindow`：可视化配置窗口
  - 支持批量生成、智能命名约定（btn_、txt_、img_ 等）

### 变更

- UIService 打开面板时自动调用 `UIAutoBinder.Bind()`

### 修复

- 修正 `GameScope.cs` 私有字段命名（`settings` → `_settings`）
- 修正 `RandomUtility.cs` 重复命名空间

## [1.3.0] - 2026-03-24

### 新增

- Core 模块：生命周期管理、事件系统（支持优先级）、全局异常处理、GameScope / SceneScope
- Asset 模块：Addressables 封装，引用计数与生命周期绑定，分帧预加载
- UI 模块：面板管理、自动绑定、R3 响应式数据绑定、导航栈
- Audio 模块：双音轨 BGM，分组音量控制，交叉淡入淡出
- Scene 模块：场景加载管理，过渡动画，叠加场景
- Config 模块：ScriptableObject 配置表，多数据源，热重载
- Save 模块：原子写入，脏状态管理，自动保存，多存档槽
- 编辑器工具：FrameworkSettings、异常查看器、配置资产编辑器

## [1.0.0] - 2026-03-23

- 初始发布
