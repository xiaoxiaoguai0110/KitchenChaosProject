# KitchenChaos 任务交接

更新时间：2026-08-20

## 当前进度

[ROADMAP.md](ROADMAP.md) 的 **P0 第 4、5 项** 已完成人工验收。**P0 第 6 项：CharacterController 实机回归** 已开始，配置检查与贴地修正完成，等待 Unity 场景手感验收；P0 第 2、3 项的长时间回归验收仍按下方清单进行。

**P1 第 7 项：移动手感** 已完成并通过实机验收。真人与 AI 现在共用加减速后的实际水平速度；动画播放速度、脚步间隔与音量会跟随实际速度变化。下一项进入 **P1 第 8 项：交互反馈**。

**P1 第 8 项：交互反馈** 已完成示例效果代码，等待实机验收。运行时反馈中心会自动接入物品弹跳、切菜粒子与镜头震动、炉灶三阶段提示、送餐跳字及错误 UI 抖动，不需要修改场景或手动挂组件。

**P1 第 12、13 项** 已完成代码与场景实现，等待完整人工验收：暂停页新增重新开始，结算页新增成功/失败订单和最终分数；双玩家已迁移到 Input System Action Map，支持方向键 P2、分别改键及键盘/手柄提示。

### 暂停、结算与输入

- 暂停页：继续、重新开始、设置、返回菜单。
- 结算页：成功订单、失败订单、最终分数；成功 `+100`，失败 `-25`，最低 `0`。
- P1 键盘：`WASD`、`E` 交互、`F` 操作、`Esc` 暂停。
- P2 键盘：方向键、右 `Ctrl` 交互、右 `Shift` 操作、`Backspace` 暂停；数字小键盘仍是备用。
- 单手柄默认给 P2；两个手柄分别给 P1/P2；设置页“当前：玩家 1/2”按钮用于选择要改键的玩家。
- `K` / `G` 分别表示当前提示来自键盘或手柄。
- Unity 运行时与 Editor 程序集均编译成功；UI 构建器已把新增按钮写入 `2-GameScene.unity`。

### 编码清理

- 全部 C# 脚本已通过严格 UTF-8 扫描，乱码和替换字符扫描结果为 `0`。
- 根目录 `.editorconfig` 强制脚本和文档使用 UTF-8，并已清理临时 `print("11")`。

### CharacterController 回归

- Player 与 AIPlayer 均使用半径 `0.65`、高度 `2`、Step Offset `0.3`、Skin Width `0.08`。
- AI 的 NavMeshAgent 半径/高度与 CharacterController 一致。
- 本地双人规则确定为互相阻挡；AI 协作仍忽略真人与 AI 的物理胶囊碰撞。
- `Player.Move()` 统一加入贴地重力，避免走下低碰撞体后悬空。
- 新增控制器配置、碰撞层、NavMesh 尺寸及 30/60/120 FPS 位移测试；Play Mode 下可从 `Tools > Kitchen Chaos > Movement Test` 快速切换目标帧率。
- 运行时代码命令行编译：`0 errors`。
- 顺带修复交互候选对非凸 MeshCollider 调用 `ClosestPoint()` 导致的 Console 刷屏，改用兼容所有 Collider 的 Bounds 最近点。

### 交互检测

- 新增 `PlayerInteractionTargetSelector`，从附近柜台中按朝向、距离和当前选择稳定加分选出目标。
- 柜台层射线会排除被其他柜台遮挡的后方目标。
- `BaseCounter` 会记录所有选择它的玩家，一名玩家移开不会误关另一名玩家的高亮。
- 原 `BaseCounter.cs` 已从 GBK 转换为 UTF-8，中文日志保持不变。
- 命令行 C# 编译：`0 errors`；新增 2 项交互评分 EditMode 测试，柜台选择手感与双人高亮人工验收通过。
- `InteractionPromptUI` 分别显示 Player 1 与 Player 2 的交互/操作按键；无目标时隐藏。
- 各柜台通过 `CanInteract` / `CanOperate` 提供明确失败原因，错误提示会在 `1.35s` 内渐隐。
- `GameSceneUIBuilder` 已生成橙色 Player 1 与青色 Player 2 提示面板，交互提示和错误反馈视觉验收通过。

### 游戏状态统一控制模拟

- `GameManager.IsGameSimulationRunning()` 统一判断“GamePlaying 且未暂停”。
- 订单生成与交付、真人输入、AI、炉灶、盘子生成和炉灶警告均已接入模拟开关。
- 倒计时和结算期间的暂停输入会被忽略；暂停状态仍可用同一按键恢复。
- GameManager 销毁、进入结算和场景加载前都会恢复 `Time.timeScale = 1`。
- 新增 2 项 EditMode 状态测试；倒计时、暂停、结算和重新进入场景的人工验收已通过。

### 事件生命周期

- `KitchenObjectHolder`、`CuttingCounter`、`TrashCounter` 的静态事件改为 `SubsystemRegistration` 自动重置。
- `ClearStaticData.Start()` 已移除，不再依赖菜单场景对象的执行顺序。
- GameInput、Player、AIPlayer、GameManager、OrderManager、SoundManager 及相关 UI 已使用 `OnEnable`/`OnDisable` 对称订阅和解绑。
- 单例在新的 Play Session 前自动清空，并在自身销毁时释放 `Instance`。
- `DeliveryResultUI.cs` 已由 GBK 转换为 UTF-8，中文注释不再乱码。
- EditMode 测试：`3 passed / 0 failed`；Unity 最终编译通过。

已完成：

- `AIPlayerMovement` 使用 NavMesh 规划完整路径，`CharacterController.Move()` 仍是唯一实际位移入口。
- 柜台周围会采样多个可交互站位，并选择完整且较短的路径。
- 连续 `2s` 没有至少 `0.08m` 的有效位移时重新规划，最多重试 `2` 次。
- 同一目标总到达时间超过 `12s` 时放弃，重规划不会重置该总计时。
- 寻路失败或柜台被真人占用后，目标会被短暂排除，避免立刻重复选择。
- Player 1 运行时会获得 `NavMeshObstacle`：移动时参与局部避障，停止 `0.5s` 后启用 Carving。
- AI 的 Agent 半径和 KitchenAgent 烘焙半径已统一为 `0.65`，场景速度统一为 `5`。

Unity 冒烟测试结果：

- 20 秒 AI 移动距离：`54.25m`
- 完整路径：通过
- 路径包含拐点：通过
- AI 保持在 NavMesh 上：通过
- 真人动态障碍已创建：通过
- C# 编译：通过

## 下一步人工验收

CharacterController 优先验收：贴着厨房内外墙持续斜向移动，确认能沿墙滑动而不抖动；本地双人迎面移动，确认两人互相阻挡但不会卡死或弹飞；让角色贴近所有墙角和柜台边缘，确认不能爬上柜台、离开碰撞边缘后不会悬空。最后分别以 30、60、120 FPS 持续直线移动相同时长，观察速度和转向是否有明显差异。

事件生命周期优先验收：连续执行至少 20 次“进入游戏 → 返回菜单 → 再次进入”以及“游戏结束 → 重新开始”，确认一次交互只播放一次音效、订单 UI 只刷新一次、暂停按钮只切换一次，Console 没有 MissingReference 或重复回调异常。

AI 验收：

1. AI 从厨房一侧绕柜台到另一侧并完成交互。
2. 玩家堵住窄路超过 `2s`，确认 AI 会改站位、重规划或更换目标。
3. 玩家站在目标柜台前，确认 AI 不抢同一个柜台；玩家离开后目标可以恢复。
4. 封死目标，确认 AI 不会永久等待，最迟 `12s` 后放弃。
5. 暂停超过 `12s` 再恢复，确认暂停时间不会触发目标超时。
6. 游戏结束和重新开始后确认没有旧路径残留。
7. 分别在 30、60、120 FPS 下检查碰撞、转向和移动一致性。
8. 连续运行至少 10 局，Console 不出现异常，AI 不永久卡住。

完成上述验收后，将 [ROADMAP.md](ROADMAP.md) 中 P0 第 2 项剩余的 `[~]` 改为 `[x]`。

## Git 注意事项

- 当前分支：`codex/main-menu-character-controller`
- 工作区包含用户的其他未提交修改，不要使用 `git add -A`，不要覆盖或还原无关文件。
- 本轮相关文件：
  - `Assets/Script/AIPlayer.cs`
  - `Assets/Script/AI/AIPlayerMovement.cs`
  - `Assets/Script/AI/AIPlayerTargetSelector.cs`
  - `Assets/Scenes/2-GameScene.unity`
  - `Assets/Script/ClearStaticData.cs`
  - `Assets/Script/KitchenObjectHolder.cs`
  - `Assets/Script/GameInput.cs`
  - `Assets/Script/Player.cs`
  - `Assets/Script/Manager/*.cs`
  - `Assets/Script/UI/*.cs`
  - `Assets/Script/Counter/CuttingCounter.cs`
  - `Assets/Script/Counter/TrashCounter.cs`
  - `Assets/Tests/EditMode/EventLifecycleTests.cs`
  - `ROADMAP.md`
  - `HANDOFF.md`
