# PVP 匹配编队入战设计

## 背景

当前客户端已有 `LevelView` 匹配按钮、`MatchmakingView` 匹配界面、`PrepareFightView` 编队界面和服务端 PvP 战斗基础逻辑。现有服务端 `JoinPvP` 在匹配到两名玩家后会立即创建 `BattleInstance` 并推送战斗数据，但目标流程需要在匹配成功后先进入编队界面，等双方都确认阵容后再创建并进入 PvP 战斗。

## 目标

实现第一阶段 PvP 最小闭环：

1. 玩家在关卡界面点击匹配按钮
2. 客户端打开匹配界面并向服务端发送匹配请求
3. 服务端匹配到两名玩家后给双方分配 `PlayerId`，但不立即创建战斗
4. 双方进入编队界面选择角色
5. 双方提交阵容后，服务端创建 PvP 战斗实例
6. 服务端向双方推送战斗初始快照和事件
7. 客户端进入战斗界面，后续出牌、移牌、撤销、结束回合都携带本地 `PlayerId`

## 非目标

第一阶段不做以下内容：

- 完整的对手资料展示
- 复杂的 PvP 等待 UI
- 断线重连完整体验
- 观战、房间邀请、自定义匹配规则
- 独立的 PvP 战斗美术表现

## 客户端页面流程

### LevelView

`LevelView` 的匹配按钮作为 PvP 入口。点击后打开 `MatchmakingView`，不直接进入战斗或编队。

```text
LevelView.Btn_matchMaking
  -> Open MatchmakingView
```

### MatchmakingView

`MatchmakingView` 负责匹配阶段：

- `Open()` 时发送 `JoinPvP`
- 文本显示“匹配中”
- 点击取消时发送 `LeavePvP` 并返回上一级
- 收到匹配成功后保存 `PlayerId`，打开 `PrepareFightView` 的 PvP 模式
- 收到失败时显示服务端 `StrMsg`

```text
MatchmakingView.Open
  -> SendJoinPvP
  -> 等待 JoinPvP 响应
     -> 未匹配：显示匹配中
     -> 匹配成功：保存 PlayerId，打开 PrepareFightView(PvP)
```

### PrepareFightView

`PrepareFightView` 复用现有编队界面，但需要区分 PvE / PvP 打开模式。

PvE 模式保持现有行为：

```text
PrepareFightView.Open(levelId)
  -> 点击开始
  -> 组装 LevelModel
  -> 加载 Fight 场景
  -> FightController.SendEnterPve(levelId)
```

PvP 模式新增行为：

```text
PrepareFightView.Open(PvpPrepareData)
  -> 点击开始
  -> 收集 formationCards 中的 CharacterDataSO.Id
  -> SendSubmitPvPTeam(heroIds)
  -> 显示“等待对方选择完成”
  -> 收到双方准备完成的战斗数据
  -> 加载 Fight 场景
  -> FightController 使用 PvP 战斗数据初始化
```

建议新增客户端数据结构：

```csharp
public class PvpPrepareData
{
    public int PlayerId;
    public string MatchId;
}
```

如果服务端第一阶段不需要 `MatchId`，客户端仍可以保留字段，实际逻辑通过用户名在服务端定位准备房间。

## 客户端会话状态

建议新增一个轻量 PvP 会话状态，用来避免把 PvP 状态散落在多个 View 和 Controller 中。

```csharp
public class PvpSession
{
    public bool IsPvp;
    public int PlayerId;
    public string MatchId;
    public bool IsMatched;
    public bool IsTeamSubmitted;
}
```

职责：

- 保存服务端分配的 `PlayerId`
- 保存当前是否处于 PvP 流程
- 提供当前战斗发包使用的玩家 ID
- 在取消匹配、离开准备界面、战斗结束时清理状态

战斗发包使用规则：

```text
CurrentPlayerId = PvpSession.IsPvp ? PvpSession.PlayerId : 1
```

## 客户端网络控制

第一阶段建议扩展现有 `BattleNetworkController`，因为 PvE/PvP 战斗协议都属于 `RequestCode.Battle`。

新增发送接口：

- `SendJoinPvP()`
- `SendLeavePvP()`
- `SendSubmitPvPTeam(List<int> heroIds)`

新增响应处理：

- `ActionCode.JoinPvP`
- `ActionCode.LeavePvP`
- `ActionCode.SubmitPvPTeam`

现有战斗操作需要携带 `PlayerId`：

- `SendPlayCard()`
- `SendMoveCard()`
- `SendEndTurn()`
- `SendUndo()`

## 服务端协议设计

允许修改 proto。建议新增或调整字段与 ActionCode。

### ActionCode

保留：

- `JoinPvP`
- `LeavePvP`

新增：

- `SubmitPvPTeam`

语义：

```text
JoinPvP       加入 PvP 匹配队列
LeavePvP      离开匹配队列或准备房间
SubmitPvPTeam 提交 PvP 阵容并进入准备完成状态
```

### BattlePack 字段

建议给 `BattlePack` 增加：

```proto
repeated int32 hero_ids = 9;
string match_id = 10;
bool is_match_success = 11;
bool is_team_ready = 12;
```

字段用途：

- `hero_ids`：客户端提交的阵容角色配置 ID
- `match_id`：服务端准备房间 ID
- `is_match_success`：区分 `JoinPvP` 返回“等待中”还是“匹配成功”
- `is_team_ready`：区分 `SubmitPvPTeam` 返回“等待对方”还是“双方完成”

字段号基于当前 `BattlePack` 已使用 1-8 的前提；实施前以 `.proto` 当前内容为准，如果已有字段占用 9-12，则顺延使用未占用字段号。

## 服务端状态流

### 匹配阶段

现有 `BattleManager.TryMatch()` 会直接调用 `CreatePvPBattle()`，需要改成创建准备房间。

```text
JoinPvP
  -> 加入 MatchmakingQueue
  -> TryMatch
     -> 未匹配：返回 Succeed + “匹配中，等待对手...”，is_match_success=false
     -> 匹配成功：创建 PvpPrepareRoom，分配 PlayerId，推送双方 is_match_success=true
```

### 准备阶段

新增服务端准备房间结构，保存双方用户名、PlayerId、选择的英雄列表和准备状态。

```csharp
internal class PvpPrepareRoom
{
    public string MatchId;
    public string Player1;
    public string Player2;
    public List<int> HeroIdsP1;
    public List<int> HeroIdsP2;
}
```

`SubmitPvPTeam` 流程：

```text
SubmitPvPTeam
  -> 根据用户名找到 PvpPrepareRoom
  -> 保存 hero_ids
  -> 如果另一方未提交：返回/推送 is_team_ready=false
  -> 如果双方都提交：CreatePvPBattle(player1, player2, heroIdsP1, heroIdsP2)
  -> 向双方推送 BattlePack(PlayerId + MatchId + StateSnapshot + Events + is_team_ready=true)
```

### 取消阶段

`LeavePvP` 需要覆盖两个场景：

- 玩家还在匹配队列：从队列移除
- 玩家已在准备房间：移除准备房间，通知另一方匹配/准备已取消

第一阶段最低要求是主动离开的客户端能返回界面并清理状态；通知另一方可以显示服务端 `StrMsg` 并返回匹配或关卡界面。

## 入战与实体表现

第一阶段为了复用现有战斗表现：

- 本地玩家阵容仍生成到 `AliveHeroes`
- 对手阵容可以先复用敌方站位和 `AliveEnemies` 侧表现
- 服务端 `StateSnapshot.Entities` 用于同步 `CombatInstanceId`、血量和行动点
- 后续再单独优化“对手也是玩家角色”的模型、动画和 UI

`FightController` 需要支持两种入战数据：

```text
PvE: LevelModel
PvP: PvpBattleStartData
```

`PvpBattleStartData` 建议包含：

```csharp
public class PvpBattleStartData
{
    public int PlayerId;
    public BattlePack BattlePack;
    public List<CharacterDataSO> LocalCharacters;
    public List<CharacterDataSO> OpponentCharacters;
}
```

如果第一阶段服务端不回传对手阵容的完整客户端数据，客户端可以根据 `StateSnapshot.Entities.ConfigId` 映射本地 `CharacterDataSO`。

## 错误处理

客户端只做第一阶段必要错误处理：

- `JoinPvP` 失败：`MatchmakingView` 显示失败文本
- `LeavePvP` 成功：返回上一级并清理 `PvpSession`
- `SubmitPvPTeam` 失败：`PrepareFightView` 显示失败文本，开始按钮恢复可点
- 已提交阵容后：开始按钮置灰，显示等待对方
- 任意服务端失败响应：不自动进入战斗

服务端错误文案通过 `MainPack.StrMsg` 返回。

## 测试方案

### 客户端手动验证

1. 启动服务端
2. 启动两个客户端并使用两个账号登录
3. 客户端 A 点击匹配，进入匹配中
4. 客户端 B 点击匹配
5. 双方进入 `PrepareFightView`
6. 双方选择角色并点击开始
7. 双方进入战斗界面
8. 当前行动方可以出牌、移牌、撤销、结束回合
9. 服务端收到的战斗操作包含正确 `PlayerId`

### 服务端验证

验证以下路径：

- 单人 JoinPvP 后队列等待
- 第二人 JoinPvP 后创建准备房间，不创建战斗实例
- 一方 SubmitPvPTeam 后等待另一方
- 双方 SubmitPvPTeam 后创建 PvP BattleInstance
- LeavePvP 能从队列或准备房间移除玩家

### 回归验证

必须确认 PvE 不受影响：

- 关卡按钮仍进入 `PrepareFightView`
- PvE 编队后仍进入战斗
- `EnterPve`、出牌、移牌、撤销、结束回合仍正常

## 实施顺序建议

1. 修改 proto，新增 `SubmitPvPTeam` 和 PvP 准备字段
2. 同步生成客户端和服务端协议代码
3. 服务端新增 PvP 准备房间，并调整 `JoinPvP` / `LeavePvP` / `SubmitPvPTeam`
4. 客户端新增 PvP 会话状态
5. 客户端扩展 `BattleNetworkController`
6. `LevelView` 匹配按钮打开 `MatchmakingView`
7. `MatchmakingView` 接入匹配请求、取消和匹配成功跳转
8. `PrepareFightView` 支持 PvE/PvP 双模式和提交 PvP 阵容
9. `FightController` 支持 PvP 入战数据
10. 所有战斗操作发包携带当前 `PlayerId`
11. 双客户端手动跑通最小闭环

## 设计结论

采用“匹配成功后进入准备房间，双方提交阵容后再创建战斗”的方案。该方案需要修改客户端、服务端和 proto，但能保证 PvP 的角色选择真实生效，也避免服务端提前创建使用默认阵容的战斗实例。第一阶段优先跑通最小闭环，UI 体验和对手表现后续再增强。
