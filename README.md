# KitchenChaos - 本地双人合作厨房模拟游戏

基于 Unity 开发的本地双人合作厨房模拟游戏，灵感来源于《胡闹厨房》。玩家需要在限定时间内完成订单，通过配合协作来获得高分。

## 游戏玩法

1. 从 **ContainerCounter** 拿取食材
2. 在 **CuttingCounter** 切菜
3. 在 **StoveCounter** 烹饪
4. 在 **ClearCounter** 将食材组合到盘子上
5. 在 **DeliveryCounter** 送餐完成订单

## 双人操作

| 操作 | Player 1 | Player 2 |
|------|----------|----------|
| 移动 | W / A / S / D | 8 / 4 / 5 / 6 |
| 交互 | E | 7 |
| 操作 | F | 9 |
| 暂停 | Escape | Escape |

Player 2 的输入采用直接键盘轮询方式，不受 Input System 重绑定存档的影响，确保双人按键始终有效。

## 单人模式（AI 队友）

支持单人游玩，Player 2 将由 AI 控制。

AI 的行为模式：
- 根据手上物品智能选择目标柜台（空手→ContainerCounter/PlatesCounter、食材→CuttingCounter/StoveCounter/ClearCounter、装盘→DeliveryCounter）
- 自动走向目标并与之交互
- 支持完整切菜流程：走到 CuttingCounter → E 放食材 → F 多次切菜 → E 取回
- 支持完整烹饪流程：走到 StoveCounter → E 放食材 → 等待煮熟 → E 取回
- 支持完整装盘流程：拿到盘子 → 找 ClearCounter 上的食材装盘 → 凑齐后送餐
- 只送完整匹配订单的菜品，不会拿不完整的盘子去送餐
- 按订单需求拿取原料，不再随机拿食材
- 与真人玩家同步：仅在 GamePlaying 状态下行动
- 交互后有冷却时间，防止过度触发

**使用方法：** 在 Player (1) 对象上挂载 `AIPlayer` 组件即可。

## 版本记录

### 1. 双人输入系统重构
- Player 2 的移动、交互、操作全部改为直接键盘读取，绕过了 Unity New Input System 的绑定覆盖问题
- 同时支持主键盘数字行和小键盘

### 2. Bug 修复
- **OrderManager.IsCorrect**: 修复了订单验证逻辑中的 `list1.Contains(item)` 应为 `list2.Contains(item)` 的 bug，原代码导致所有送餐（数量匹配时）都会成功
- **CuttingCounter**: 禁止将盘子放到切菜台上

### 3. 音效系统
- 为 CuttingCounter 增加了切菜音效（通过静态事件 `OnCut` 驱动）

### v1.1 - AI 玩家系统
- 新增 **AIPlayer** 组件，支持单人模式下 Player 2 由 AI 控制
- AI 自动选择柜台为目标，移动并交互
- AI 与 GameManager 状态同步，仅在 GamePlaying 时行动
- 为 Player 增加 `SetIsWalking()` 公开方法供 AI 控制动画

### v1.5 - AI 智能原料追溯与死锁修复
- **AIPlayer**：新增 `OnFirstOrderSpawned` 事件监听，AI 等第一个订单生成后才开始行动
- **AIPlayer**：`PickIngredientsOrPlates` 新增原料追溯功能，当订单需要的成品食材（如 CookedMeat、SlicedTomato）没有对应的 ContainerCounter 时，自动查配方找到生原料（RawMeat、Tomato）
- **AIPlayer**：`PickIngredientsOrPlates` 扫描 ClearCounter 上已有的食材，只拿真正缺的
- **AIPlayer**：修复 AI 因 missingIngredients 找不到 ContainerCounter 而卡死的 bug
- **AIPlayer**：优化冷却时间，普通交互从 0.8s 降至 0.15s

### v1.4 - AI 代码重构与智能装盘、按订单需求做菜
- **AIPlayer**：全面重构代码结构，`PickNewTarget` 拆分为 7 个独立子方法
- **AIPlayer**：新增 `FindCountersWithPlatedFood`、`PickTargetWhenEmptyHanded`、`PickTargetWhenHoldingObject` 等方法
- **AIPlayer**：新增 `HandleActionAtCounter` 独立处理到达目标后的交互逻辑
- **AIPlayer**：新增 `IsPlateMatchingAnyOrder` 检查，只有完整匹配订单的盘子才送去上菜
- **AIPlayer**：手上盘子不完整时不再送餐，改为继续去 ClearCounter 装食材
- **AIPlayer**：按订单需求拿取原料，优先拿当前订单需要的食材
- **ContainerCounter**：新增 `KitchenObjectSO` 公开属性供 AI 读取
- **多个脚本**：修复 `Destory` → `Destroy` 拼写错误（8 处）
- **CuttingCounter**：删除 `UnityEditor.Rendering.CameraUI` 引用，修复 Build 错误

### v1.3 - AI 行为修复与订单系统 Bug 修复
- **AIPlayer**：新增 `Waiting` 状态，AI 现在可以在 StoveCounter 等待食物烹饪完成
- **AIPlayer**：新增 `Physics.IgnoreCollision`，解决 AI 与玩家碰撞导致的旋转/穿模问题
- **AIPlayer**：修复 AI 无法从 StoveCounter 拿起煮熟食物的 bug
- **OrderManager**：修复交付订单后 `orderCount` 累计导致无法继续生成新订单的 bug
- **OrderListUI**：修复遍历时直接销毁子物体导致的 `InvalidOperationException`
- **GameManager**：游戏时长从 20 秒调整为 60 秒

### v1.2 - AI 智能决策与完整切菜流程
- AI 根据手上物品智能选择目标：空手→ContainerCounter、食材→CuttingCounter/StoveCounter、装盘→DeliveryCounter
- AI 支持完整切菜流程（E 放食材 → F 多次切菜 → E 取回），引入 `AIActionType` 任务状态机
- 转向算法从 `Slerp` 改为 `RotateTowards`，修复角落抽搐问题
- 修复 `PickNewTarget` 空列表崩溃 bug

## 项目架构

### 核心模块
- **GameManager** - 全局状态机（WaitingToStart → CountDownToStart → GamePlaying → GameOver）
- **GameInput** - 输入管理（Player 1 走 Input System，Player 2 走键盘轮询）
- **OrderManager** - 订单生成与验证系统
- **SoundManager** / **MusicManager** - 音效管理

### 柜台系统（继承 BaseCounter）
| 柜台 | 功能 |
|------|------|
| ContainerCounter | 生成食材 |
| CuttingCounter | 切菜 |
| StoveCounter | 烹饪（状态机：Idle/Frying/Burning） |
| ClearCounter | 放置/组合食材 |
| PlatesCounter | 定时生成盘子 |
| DeliveryCounter | 送餐 |
| TrashCounter | 丢弃食材 |

### 技术要点
- **ScriptableObject** 数据驱动架构（食材/配方/烹饪流程配置化）
- **OOP 多态**（BaseCounter virtual/override）
- **C# 事件驱动** 实现 UI 与逻辑层解耦
- **状态机模式** 管理游戏流程与烹饪状态
