# PvP 自动匹配 —— 设计规格

**日期**: 2026-05-14
**状态**: 待审批

---

## 目标

实现两个玩家自动排队匹配进入 PvP 战斗。PvE 路径零影响。

## 数据流

```
Player A                     Server                      Player B
  │                           │                            │
  ├─ JoinPvP ────────────────►│                            │
  │                           ├─ 放入队列 (1人)            │
  │◄── "匹配中..." ─────────┤                            │
  │                           │◄─── JoinPvP ──────────────┤
  │                           ├─ 队列凑够 2 人              │
  │                           ├─ CreatePvPBattle(A,B)      │
  │◄── BattleStart(P1) ──────┤                            │
  │                           ├─ BattleStart(P2) ─────────►│
  │=== P1 回合 ===            │                            │
  │─ PlayCard(1, ...) ───────►├─ 同步事件 ────────────────►│
  │─ EndTurn(1) ─────────────►├─ 切换→P2 ─────────────────►│
  │                           │            ══ P2 回合 ══   │
  │◄── 同步事件 ────────────┤◄─ PlayCard(2, ...) ────────┤
  │◄── 同步事件 ────────────┤◄─ EndTurn(2) ──────────────┤
  │                           ├─ 双方结算 → startNextRound │
```

## 服务端改动清单

### 1. Proto 定义

`GamesProtocol.proto` 修改两处：

**ActionCode 追加**:
```proto
JoinPvP = 15;   // 加入 PvP 匹配队列
LeavePvP = 16;  // 离开匹配队列
```

**BattlePack 新增字段**:
```proto
int32 PlayerId = 10;  // 操作者 ID（PvE 填 1，PvP 填服务端分配值）
```

客户端自行重新生成 `.cs`。

### 2. `Network/Matchmaking/MatchmakingQueue.cs`

简单队列，不依赖任何外部库：

| 成员 | 说明 |
|------|------|
| `void Enqueue(string username)` | 加入队尾，重复加入忽略 |
| `void Dequeue(string username)` | 从队列除名 |
| `bool CanMatch` | `Count >= 2` |
| `(string, string) PopMatch()` | 取出前 2 人并移除 |

### 3. 修改 `BattleManager.cs`

```csharp
private readonly MatchmakingQueue _matchQueue = new();
// ... 使用 lock(_matchQueueLock) 保护并发 ...

public void JoinQueue(string username)
public void LeaveQueue(string username)
public (string p1, string p2, BattleInstance)? TryMatch()
  // 如果 CanMatch → PopMatch → CreatePvPBattle → 双 key 挂入 _battles
```

### 4. 修改 `BattleHandler.cs`

- 新增 `handleJoinPvP(Client client, MainPack pack)`:
  1. 检查登录
  2. `JoinQueue(client.UserName)`
  3. `TryMatch()`，没结果则回复"等待中"
  4. 有结果则给 P1/P2 分别回复 BattleStart 事件

- 新增 `handleLeavePvP(Client client, MainPack pack)`:
  1. `LeaveQueue(client.UserName)`
  2. 回复"已离开"

- 现有 `handlePlayCard` / `handleMoveCard` / `handleEndTurn` / `handleUndoAction`：
  `battle.XXX(1, ...)` 改为 `battle.XXX(bp.PlayerId, ...)`

### 5. 注册协议

```csharp
ProtocolRouter.Register(ActionCode.JoinPvP, battleHandler);
ProtocolRouter.Register(ActionCode.LeavePvP, battleHandler);
```

---

## 不做的事（第二阶段）

- 客户端 PvP UI（匹配按钮、等待界面、FightingView 锁输入）
- 房间创建/加入
- 匹配超时自动清理
- 断线重连 PvP

---

## 自检

- [x] 无 TODO/占位符
- [x] GameMode.PvE 路径不受影响
- [x] Proto 只加不删，向后兼容
- [x] PvE 时 BattlePack.PlayerId 默认值 0 或客户端填 1 均兼容
