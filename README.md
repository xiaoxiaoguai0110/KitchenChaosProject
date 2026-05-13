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

## 本次修改内容

### 1. 双人输入系统重构
- Player 2 的移动、交互、操作全部改为直接键盘读取，绕过了 Unity New Input System 的绑定覆盖问题
- 同时支持主键盘数字行和小键盘

### 2. Bug 修复
- **OrderManager.IsCorrect**: 修复了订单验证逻辑中的 `list1.Contains(item)` 应为 `list2.Contains(item)` 的 bug，原代码导致所有送餐（数量匹配时）都会成功
- **CuttingCounter**: 禁止将盘子放到切菜台上

### 3. 音效系统
- 为 CuttingCounter 增加了切菜音效（通过静态事件 `OnCut` 驱动）

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
