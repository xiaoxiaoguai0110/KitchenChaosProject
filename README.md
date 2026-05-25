# KitchenChaos - 本地双人合作厨房模拟游戏

基于 Unity 开发的本地双人合作厨房模拟游戏，灵感来源于《胡闹厨房》。该项目从 Unity 教程项目起步，经过持续重构与扩展，已演变为一个包含 AI 队友系统、复杂决策逻辑和完整游戏循环的独立项目。

---

## 项目亮点

### 1. AI 队友系统（核心复杂度）
单人模式下 Player 2 由 AI 控制，实现了完整的"观察-决策-执行"循环：

**目标选择系统：**
- 根据手上物品 + 当前订单智能决策下一步目标
- 实时扫描场景中所有柜台的食材状态，避免重复拿取
- 支持"原料追溯"——当订单需要加工后的成品（如 CookedMeat）时，自动反向查找配方链，追溯到对应的生原料（RawMeat）并完成加工流程
- 使用 `list.Contains()` 做差集运算实现"订单需要的 - 台子上已有的 = 真正缺的"
- **按订单锁定装盘**——盘子里已有食材是某个订单的子集时才继续装该订单的食材，杜绝混装

**显式状态机架构：**
- 使用 `enum AIState` + `switch` 实现 4 个独立状态：Idle / MovingToTarget / Cutting / Waiting
- 每个状态对应一个 `Update*()` 方法，逻辑完全隔离，互不干扰
- 通过 `ChangeState()` 统一管理状态切换，状态流转清晰可追溯
- 移除了旧版散落在 Update 中的 if-else 冷却判断，全部收归各状态内部管理

**碰撞检测与主动避让：**
- `IsTargetCounterBlocked()` — 每帧检查目标柜台是否被玩家占用
- 通过 `CanAddKitchenObjectSO()` 预测交互是否成功，食材不合法/已存在时立即换目标
- 从根本上解决了 AI 与玩家选同一柜台时卡死不动的死循环问题

**技术挑战与解决方案：**
- 首次交互后 0.8s 冷却导致 AI 频繁发呆 → 优化为 0.15s，连贯性大幅提升
- `missingIngredients` 只包含成品食材，找不到对应的 ContainerCounter 导致死锁 → 新增配方反向查询
- 订单生成前 AI 乱拿原料 → 改为事件驱动，监听到第一个订单后再行动
- AI 与玩家选同一柜台后卡死不动 → 每帧检测柜台是否被占，提前换目标而非事后补救
- 代码膨胀（单文件 500+ 行）→ 状态模式拆分 + 10 个语义化子方法 + 显式状态机管理
- 游戏结束倒计时只到 1 不到 0 → 延迟一帧切状态 + UI 钳制 `Mathf.Max(0, timer)`
- AI 把不同订单的食材混装到一盘 → 按订单锁定装盘，盘子里食材必须是某个订单的子集才继续装

### 2. OOP 架构设计
- **多态继承体系**：`BaseCounter` 作为抽象基类，7 个子类各自重写 `Interact()` 实现不同柜台行为
- **ScriptableObject 数据驱动**：食材属性、切菜配方、烹饪配方、订单模板全部配置化，无需修改代码即可扩展新菜品
- **组件式设计**：AI 作为独立的 `MonoBehaviour` 组件挂载到 Player 上，通过 `Player.enabled = false` 禁用真人输入控制

### 3. 事件驱动架构
- 使用 C# `event` 关键字实现模块间解耦
  - `OrderManager.OnRecipeSpawned` → OrderListUI 刷新 + AI 启动决策
  - `CuttingCounter.OnCut` → SoundManager 播放音效
  - `KitchenObjectHolder.OnDrop / OnPickup` → 全局物品拾取/放下音效
- 避免直接依赖，新增 AI 等模块时不需要修改已有代码

### 4. 双人输入系统
- Player 1 使用 Unity New Input System（支持自定义键位）
- Player 2 直接键盘轮询读取（`Input.GetKeyDown`），不受输入系统存档影响
- 解决了"一个人改了键位，两个人的按键都受影响"的 bug

### 5. 版本控制与工程实践
- 语义化版本号（v1.1 ~ v1.8），每次更新有明确版本记录
- 每次 commit 聚焦单一变更，commit message 规范化
- 从单文件逐步重构为清晰的方法分拆，保证可维护性
- SSH 部署，自动化推送流程

---

## 游戏玩法

1. **拿食材** → ContainerCounter 获取原料
2. **加工处理** → CuttingCounter 切菜 / StoveCounter 烹饪
3. **组合装盘** → ClearCounter 将食材组合到盘子
4. **完成订单** → DeliveryCounter 送餐

---

## 版本记录

### v1.8 - AI 智能装盘：按订单锁定，避免混装
- **按订单锁定装盘**：盘子里已有食材必须是某个订单的子集才继续装该订单
- 不再混装多个订单的食材到同一盘子，杜绝"永远凑不齐"的死锁
- **禁止随机放食材**：AI 不再把食材随手放到非目标 ClearCounter 上
- 回归默认 Unity 编译，移除测试程序集依赖

### v1.7 - AI 碰撞检测 + 倒计时修复 + GameOver 逻辑修正
- **`IsTargetCounterBlocked()`** — 每帧检测目标柜台是否被占，提前换目标
- **`CanAddKitchenObjectSO()`** — 无副作用预测食材能否装盘
- AI 启动时机：新增 `hasReceivedFirstOrder`，订单未加载前不行动
- `TurnToGameOver()` 修正：`EnablePlayer()` → `DisablePlayer()`
- 计时器归零延迟：保留一帧显示 0，UI 添加 `Mathf.Max` 钳制

### v1.6 - AIPlayer 状态机重构
- 显式状态机：`enum AIState` + `switch(data:Idle/MovingToTarget/Cutting/Waiting)`
- 删除旧 `AIActionType`，用统一 `ChangeState()` 管理状态切换
- `HandleActionAtCounter` 拆分为 `OnReachedTarget` + `UpdateCutting` + `UpdateWaiting`
- 每个状态逻辑完全隔离，降低维护成本和 Bug 风险

### v1.5 - AI 原料追溯与死锁修复
- 新增配方反向查询：AI 能自动将"成品需求"追溯为"原料需求"
- 实时场景扫描：统计 ClearCounter 上已有食材，只拿真正缺的
- 修复空目标死锁：`availableCounters` 为空时保底去 ContainerCounter
- 冷却时间优化：0.8s → 0.15s，AI 行动更连贯

### v1.4 - AI 代码重构与智能装盘
- 全面重构：`PickNewTarget` 拆分为 7 个语义化方法
- `IsPlateMatchingAnyOrder` 检查：只送完整匹配订单的菜品
- 按订单需求拿原料，不再随机拿食材
- Build 修复：删除 `UnityEditor.Rendering.CameraUI` 引用 + 8 处 `Destory` → `Destroy`

### v1.3 - AI 行为修复
- 新增 Waiting 状态，支持 StoveCounter 等待烹饪
- 碰撞修复：`Physics.IgnoreCollision` 防止 AI 与玩家碰撞旋转
- 修复 OrderManager 订单生成上限 log 错误

### v1.2 - AI 决策系统
- 根据手上物品智能选目标（状态机初步实现）
- 完整切菜流程 + RotateTowards 转向修复

### v1.1 - AI 玩家系统
- AIPlayer 组件首次引入，单人模式可用

---

## 项目结构

```
Assets/Script/
├── AIPlayer.cs                 # AI 决策系统（状态机 + 10 个语义化子方法 + 碰撞检测）
├── Player.cs                   # 玩家控制器
├── KitchenObjectHolder.cs      # 物品持有基类
├── KitchenObject.cs            # 物品基类
├── PlateKitchenObject.cs       # 盘子（物品容器）
├── Manager/
│   ├── GameManager.cs          # 全局状态机
│   ├── OrderManager.cs         # 订单系统
│   ├── SoundManager.cs         # 音效管理
│   └── MusicManager.cs         # 音乐管理
├── Counter/
│   ├── BaseCounter.cs          # 抽象基类
│   ├── ContainerCounter.cs     # 原料箱
│   ├── CuttingCounter.cs       # 切菜台
│   ├── StoveCounter.cs         # 灶台（状态机：Idle/Frying/Burning）
│   ├── ClearCounter.cs         # 操作台（食材组合逻辑）
│   ├── PlatesCounter.cs        # 盘子生成器
│   ├── DeliveryCounter.cs      # 送餐口
│   └── TrashCounter.cs         # 垃圾桶
├── ScriptObjects/              # 数据配置（ScriptableObject）
└── UI/
    ├── OrderListUI.cs          # 订单列表 UI
    └── RecipeUI.cs             # 订单模板 UI
```

---

## 技术栈

| 技术 | 用途 |
|------|------|
| Unity 2022.3 | 游戏引擎 |
| C# | 全部游戏逻辑 |
| ScriptableObject | 数据驱动架构 |
| Unity New Input System | Player 1 输入 |
| Direct Input Polling | Player 2 输入 |
| SSH / Git | 版本控制 |
| C# Events | 模块通信 |
