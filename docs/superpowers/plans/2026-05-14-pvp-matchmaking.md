# PvP 自动匹配 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为现有服务端添加 PvP 自动匹配功能——两个玩家排队 → 配对 → 进入 PvP 战斗。

**Architecture:** 新增 `MatchmakingQueue` 管理排队队列；`BattleManager` 内嵌队列并在凑够 2 人时调用已有的 `CreatePvPBattle()`；`BattleHandler` 新增 `JoinPvP`/`LeavePvP` 协议；`BattlePack` 扩展 `PlayerId` 字段使 PvP 下客户端可传身份。

**Tech Stack:** C# / .NET 8 / Protobuf / 现有 GameServer 架构

---

## File Structure

| 文件 | 操作 | 职责 |
|------|------|------|
| `Network/Matchmaking/MatchmakingQueue.cs` | 新增 | 排队队列 |
| `Battle/BattleManager.cs` | 修改 | 内嵌队列 + TryMatch() |
| `Network/Protobuf/ProtocolHandlers/BattleHandler.cs` | 修改 | 新增 JoinPvP/LeavePvP，现有方法用 bp.PlayerId |
| `Network/Protobuf/ProtocolHandlers/ProtocolRouter.cs` | 修改 | 注册新 ActionCode |
| `Program.cs` | 修改 | 注册 JoinPvP/LeavePvP 到 router |
| `[useful]/GamesProtocol.proto` | 修改 | 新增 JoinPvP=15, LeavePvP=16, BattlePack.PlayerId=10 |

---

### Task 1: Proto 定义

**Files:**
- Modify: `Assets/Scripts/[useful]/GamesProtocol.proto`

- [ ] **Step 1: 在 ActionCode 枚举末尾加两个值**

在 `RequestBattleState = 14;` 之后：

```proto
    //进入PVP匹配队列
    JoinPvP = 15;
    //离开PVP匹配队列
    LeavePvP = 16;
```

- [ ] **Step 2: 在 BattlePack 消息中加 PlayerId 字段**

在 `BattlePack` message 的最后一个字段后追加：

```proto
    //操作者ID（PvE=1，PvP=1或2）
    int32 PlayerId = 10;
```

- [ ] **Step 3: 自行重新生成 .cs 文件，放到服务端和客户端的 Protobuf 目录**

---

### Task 2: MatchmakingQueue.cs

**Files:**
- Create: `AAAGameServer/ConsoleMock_Server/GameServer/Network/Matchmaking/MatchmakingQueue.cs`

- [ ] **Step 1: 创建目录并写入文件**

```bash
New-Item -ItemType Directory -Path "D:\CCodes\unity\Remastered_Reverse-1999\AAAGameServer\ConsoleMock_Server\GameServer\Network\Matchmaking" -Force
```

- [ ] **Step 2: 写入 MatchmakingQueue.cs**

```csharp
/*
* ┌──────────────────────────────────┐
* │  描    述: PvP 匹配队列
* │  类    名: MatchmakingQueue.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace Network.Matchmaking
{
    internal class MatchmakingQueue
    {
        private readonly List<string> _queue = new();

        public bool CanMatch => _queue.Count >= 2;

        public void Enqueue(string username)
        {
            if (string.IsNullOrEmpty(username)) return;
            if (_queue.Contains(username)) return;
            _queue.Add(username);
        }

        public void Dequeue(string username)
        {
            _queue.Remove(username);
        }

        public (string p1, string p2) PopMatch()
        {
            var p1 = _queue[0];
            var p2 = _queue[1];
            _queue.RemoveRange(0, 2);
            return (p1, p2);
        }

        public int Count => _queue.Count;
    }
}
```

- [ ] **Step 3: `dotnet build` 验证编译通过**

---

### Task 3: BattleManager 加队列支持

**Files:**
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Battle/BattleManager.cs`

- [ ] **Step 1: 读取当前文件，添加 using 和字段**

在 `using System.Linq;` 后追加：
```csharp
using Network.Matchmaking;
```

在 `private readonly Dictionary<string, BattleInstance> _battles;` 后追加：
```csharp
private readonly MatchmakingQueue _matchQueue = new();
```

- [ ] **Step 2: 追加三个公开方法**

在 `RemoveBattle` 方法之后追加：

```csharp
public void JoinQueue(string username)
{
    _matchQueue.Enqueue(username);
}

public void LeaveQueue(string username)
{
    _matchQueue.Dequeue(username);
}

public (string p1, string p2, BattleInstance)? TryMatch()
{
    if (!_matchQueue.CanMatch) return null;

    var (p1, p2) = _matchQueue.PopMatch();
    var battle = CreatePvPBattle(p1, p2);
    _battles[p1] = battle;
    _battles[p2] = battle;
    return (p1, p2, battle);
}
```

- [ ] **Step 3: `dotnet build` 验证编译通过**

---

### Task 4: BattleHandler 新增 / 修改

**Files:**
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/ProtocolHandlers/BattleHandler.cs`

- [ ] **Step 1: 在 switch 块中注册新 ActionCode**

在 `case ActionCode.RequestBattleState:` 分支之后追加：

```csharp
case ActionCode.JoinPvP:
    handleJoinPvP(client, pack);
    break;
case ActionCode.LeavePvP:
    handleLeavePvP(client, pack);
    break;
```

- [ ] **Step 2: 在 #region 处理消息 内新增两个方法**

```csharp
private void handleJoinPvP(Client client, MainPack pack)
{
    if (!checkLogin(client, ActionCode.JoinPvP)) return;

    client.Server.BattleManager.JoinQueue(client.UserName);

    var matchResult = client.Server.BattleManager.TryMatch();
    if (matchResult == null)
    {
        var waitPack = new MainPack
        {
            RequestCode = RequestCode.Battle,
            ActionCode = ActionCode.JoinPvP,
            ReturnCode = ReturnCode.Succeed,
            StrMsg = "匹配中，等待对手..."
        };
        client.Send(waitPack.ToByteArray());
        return;
    }

    var (p1, p2, battle) = matchResult.Value;
    var events = battle.CollectEvents();

    // 通知 P1
    var clientP1 = client.Server.GetClientByUsername(p1);
    if (clientP1 != null)
    {
        var res1 = new BattlePack { PlayerId = 1 };
        res1.Events.AddRange(events);
        res1.StateSnapshot = battle.GetStateSnapshot();
        sendBattleResponse(clientP1, ActionCode.JoinPvP, res1);
    }

    // 通知 P2
    var clientP2 = client.Server.GetClientByUsername(p2);
    if (clientP2 != null)
    {
        var res2 = new BattlePack { PlayerId = 2 };
        res2.Events.AddRange(events);
        res2.StateSnapshot = battle.GetStateSnapshot();
        sendBattleResponse(clientP2, ActionCode.JoinPvP, res2);
    }
}

private void handleLeavePvP(Client client, MainPack pack)
{
    if (!checkLogin(client, ActionCode.LeavePvP)) return;

    client.Server.BattleManager.LeaveQueue(client.UserName);

    var resPack = new MainPack
    {
        RequestCode = RequestCode.Battle,
        ActionCode = ActionCode.LeavePvP,
        ReturnCode = ReturnCode.Succeed,
        StrMsg = "已离开匹配队列"
    };
    client.Send(resPack.ToByteArray());
}
```

- [ ] **Step 3: 现有 4 个方法改用 bp.PlayerId**

```csharp
// handlePlayCard: battle.PlayCard(1, ...) → battle.PlayCard(bp.PlayerId, ...)
bool success = battle.PlayCard(bp.PlayerId, bp.CardInstanceId, bp.TargetEntityId, bp.SourceSlotIndex);

// handleEndTurn: battle.EndTurn(1) → battle.EndTurn(bp.PlayerId)
battle.EndTurn(bp.PlayerId);

// handleMoveCard: battle.MoveCard(1, ...) → battle.MoveCard(bp.PlayerId, ...)
bool success = battle.MoveCard(bp.PlayerId, bp.SourceSlotIndex, bp.TargetSlotIndex);

// handleUndoAction: battle.Undo(1) → battle.Undo(bp.PlayerId)
bool success = battle.Undo(bp.PlayerId);
```

- [ ] **Step 4: `dotnet build` 验证编译通过**

---

### Task 5: 注册协议 + 最终验证

**Files:**
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Program.cs`
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/ProtocolHandlers/ProtocolRouter.cs`

- [ ] **Step 1: Program.cs 中注册新 ActionCode**

在 `RegisterProtocolHandlers()` 方法末尾追加：

```csharp
ProtocolRouter.Register(ActionCode.JoinPvP, new BattleHandler());
ProtocolRouter.Register(ActionCode.LeavePvP, new BattleHandler());
```

- [ ] **Step 2: ProtocolRouter.cs 不需要改**

现有的 `Route` 方法用 `TryGetValue` 查找，新注册自动生效。

- [ ] **Step 3: 构建 + 测试**

```bash
dotnet test
```

预期：6 pass / 0 fail

---

## 自检

- [x] 每个 Task 对应 spec 的一项改动
- [x] 无 TODO/占位符
- [x] 类型签名与现有代码一致（`BattlePack.PlayerId` → `bp.PlayerId`）
- [x] PvE 路径：`bp.PlayerId` 默认 0，`isCurrentPlayer(0)` 在 PvE 下 `0 == PLAYER1_ID(1)` → false → 需要兼容处理

**⚠ 发现一个兼容性问题：** PvE 时客户端发的 BattlePack 没有 PlayerId 字段，protobuf 反序列化后 `bp.PlayerId` 默认为 0。但 `isCurrentPlayer(0)` 在 PvE 模式下 `0 != PLAYER1_ID(1)` → 操作会被拒绝。

**修复：** 在 `isCurrentPlayer` 中加 PvE 兼容逻辑——PvE 模式下 playerId 为 0 或 1 都视为当前玩家。

在 Task 4 Step 3 中追加此修复（改 `BattleInstance.cs`）：

```csharp
private bool isCurrentPlayer(int playerId)
{
    if (_env.Mode == GameMode.PvE)
        return playerId == 0 || playerId == PLAYER1_ID;
    return playerId == _env.CurrentPlayerId;
}
```
