# KitchenChaos 任务交接

更新时间：2026-08-19

## 当前进度

[ROADMAP.md](ROADMAP.md) 的 **P0 第 2 项：AI 寻路与脱困** 与 **P0 第 3 项：事件生命周期** 已完成代码改造，下一步是 Unity 场景实机验收。

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
