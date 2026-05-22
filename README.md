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

**行为状态机：**
- 通过枚举 + 状态分支，实现了 Cutting（切菜循环）、Waiting（等待烹饪）、None（自由决策）三种行为模式
- 到达目标柜台后根据柜台类型自动切换行为状态
- 冷却系统防止多帧重复触发的交互冲突

**技术挑战与解决方案：**
- 首次交互后 0.8s 冷却导致 AI 频繁发呆 → 优化为 0.15s，连贯性大幅提升
- `missingIngredients` 只包含成品食材，找不到对应的 ContainerCounter 导致死锁 → 新增配方反向查询
- 订单生成前 AI 乱拿原料 → 改为事件驱动，监听到第一个订单后再行动
- 代码膨胀（单文件 300+ 行）→ 重构拆分为 7 个语义化子方法

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
- 语义化版本号（v1.1 ~ v1.5），每次更新有明确版本记录
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
├── AIPlayer.cs                 # AI 决策系统（10 个语义化方法）
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
