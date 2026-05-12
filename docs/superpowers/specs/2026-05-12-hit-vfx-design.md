# 受击与治疗特效架构设计 (Hit/Heal VFX Architecture Design)

## 1. 目标 (Objective)
在前后端分离的网游架构下，为客户端实现安全、精准的技能卡牌受击/治疗特效播放机制。同时清理客户端因为架构迁移而遗留的废弃战斗逻辑代码。

## 2. 问题分析与现状 (Context)
- 当前项目已转为“服务端状态权威”模式（基于 `PlayEventSequence.cs` 播放 `BattleEvent`）。
- 客户端的 `fight/Core` 目录下存在大量不再使用的计算与执行逻辑（如 `CardSkillExecutor.cs`），引发了理解上的混乱。
- 需要实现受击特效，而 `CardDataSO.cs` 中已经预留了 `CardEffectPrefab`，需要将该特效在发生 `DamageTaken` 或 `HealTaken` 时正确实例化。

## 3. 架构设计 (Architecture)

### 3.1 协议层 (Protobuf)
为了让客户端准确知道某个战斗事件是由哪张卡牌引起的，将统一在 `BattleEvent` 基类中新增来源卡牌信息：
```protobuf
message BattleEvent
{
    BattleEventType eventType = 1;
    int32 sourceId = 2; 
    int32 targetId = 3; 
    int32 sourceCardConfigId = 4; // <--- 新增字段：导致该事件的卡牌配置ID
    // ...
}
```

### 3.2 服务端职责 (Server)
在服务端生成 `DamageTaken`、`HealTaken` 等结果事件时，需要顺带注入当前正在计算的 `Skill` 或 `Card` 的 `ConfigId`（即 `sourceCardConfigId`）。

### 3.3 客户端职责 (Client)
客户端的 `PlayEventSequence.cs` 在处理对应的动画事件时：
- 识别 `evt.sourceCardConfigId`。
- 如果其 `> 0`，调用数据中心（如 `ResKit` 结合本地 SO，或直接找 `CardManager` 提供的查表方式）获取对应的 `CardDataSO`。
- 将 `CardDataSO.CardEffectPrefab` 实例化在 `targetId` 所代表的 `CombatInstance` 上。
- 定时/监听销毁该特效。

### 3.4 冗余代码清理 (Cleanup)
- 删除客户端不再使用的逻辑类：
  - `CardSkillExecutor.cs`
  - 明确评估不被服务端驱动影响的 `fight/Core/Commands` 和 `Systems`。
- 保留客户端用于数据同步的核心数据类（如 `CardEntity.cs`, `CombatEntity.cs`, `CombatContext.cs`）。

## 4. 实施顺序 (Execution Order)
1. **协议更新**：修改 `GamesProtocol.proto` 并重新生成 C# 代码（客户端和服务端）。
2. **清理冗余**：删除客户端已废弃的执行器代码，确保没有遗留引用错误。
3. **表现层对接**：在 `PlayEventSequence.cs` 中加入特效读取和播放逻辑。

## 5. 边界与约束 (Constraints)
- **严格遵循后端驱动**：不自行在客户端推导“当前是哪张卡”，必须由 `sourceCardConfigId` 指定，防止事件堆积时的时序混乱。
- 特效如果自带 Destroy 脚本更好，如果没有则添加延迟销毁。