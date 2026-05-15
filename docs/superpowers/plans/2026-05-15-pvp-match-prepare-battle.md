# PVP 匹配编队入战 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现 PvP 最小闭环：关卡页匹配、匹配成功进入编队、双方提交阵容后由服务端创建 PvP 战斗并进入战斗界面。

**Architecture:** Proto 增加 `SubmitPvPTeam` 和 PvP 准备字段，服务端把 `JoinPvP` 从“立即开战”改为“创建准备房间”，双方提交阵容后再创建 `BattleInstance`。客户端新增 PvP 会话状态，复用 `MatchmakingView`、`PrepareFightView`、`FightController` 和 `BattleNetworkController`，用 `PlayerId` 区分 PvP 操作者。

**Tech Stack:** Unity C#、.NET 8、Google.Protobuf、xUnit、现有 MVC/ViewManager/MessageCenter/GameServer 架构。

---

## File Structure

| 文件 | 操作 | 职责 |
|------|------|------|
| `Assets/Scripts/[useful]/GamesProtocol.proto` | 修改 | 新增 `SubmitPvPTeam` 与 `BattlePack` PvP 准备字段 |
| `Assets/Scripts/Network/Protocol/GamesProtocol.cs` | 生成 | 客户端 protobuf C# 代码 |
| `AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/GamesProtocol.cs` | 生成 | 服务端 protobuf C# 代码 |
| `AAAGameServer/ConsoleMock_Server/GameServer/Network/Matchmaking/PvpPrepareRoom.cs` | 新建 | 服务端 PvP 准备房间数据与提交状态 |
| `AAAGameServer/ConsoleMock_Server/GameServer.Tests/PvpPrepareRoomTests.cs` | 新建 | 验证准备房间双方提交与取消判断 |
| `AAAGameServer/ConsoleMock_Server/GameServer/Battle/BattleManager.cs` | 修改 | 匹配成功创建准备房间，双方提交后创建 PvP 战斗 |
| `AAAGameServer/ConsoleMock_Server/GameServer.Tests/BattleManagerPvpPrepareTests.cs` | 新建 | 验证匹配不立即开战、双方提交后开战 |
| `AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/ProtocolHandlers/BattleHandler.cs` | 修改 | 处理 `JoinPvP`、`LeavePvP`、`SubmitPvPTeam` 响应 |
| `AAAGameServer/ConsoleMock_Server/GameServer/Program.cs` | 修改 | 注册 `SubmitPvPTeam` 路由 |
| `Assets/Scripts/Module/fight/Network/PvpSession.cs` | 新建 | 客户端 PvP 会话状态 |
| `Assets/Scripts/GameApp.cs` | 修改 | 挂载全局 `PvpSession` |
| `Assets/Scripts/Common/Defines/EventDefines.cs` | 修改 | 新增 PvP 匹配/准备/开战事件名 |
| `Assets/Scripts/Module/fight/Network/PvpPrepareData.cs` | 新建 | 匹配成功后传给编队界面的数据 |
| `Assets/Scripts/Module/fight/Network/PvpBattleStartData.cs` | 新建 | 双方准备完成后传给战斗控制器的数据 |
| `Assets/Scripts/Module/fight/Network/BattleNetworkController.cs` | 修改 | 增加 PvP 匹配/准备协议，战斗操作携带 `PlayerId` |
| `Assets/Scripts/Module/View/LevelView.cs` | 修改 | 匹配按钮打开匹配界面 |
| `Assets/Scripts/Module/View/MatchmakingView.cs` | 修改 | 打开即匹配、取消匹配、匹配成功跳转编队 |
| `Assets/Scripts/Module/View/PrepareFightView.cs` | 修改 | 支持 PvE/PvP 双模式，PvP 提交阵容并等待开战 |
| `Assets/Scripts/Module/Character/Entity/EntityManager.cs` | 修改 | 增加 PvP 实体生成入口，使用英雄预制体和敌方站位生成对手 |
| `Assets/Scripts/Module/fight/FightController.cs` | 修改 | 支持 `PvpBattleStartData` 入战，不再发送 `EnterPve` |

---

### Task 1: Proto 增加 PvP 准备协议

**Files:**
- Modify: `Assets/Scripts/[useful]/GamesProtocol.proto`
- Generate: `Assets/Scripts/Network/Protocol/GamesProtocol.cs`
- Generate: `AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/GamesProtocol.cs`

- [ ] **Step 1: 修改 ActionCode**

在 `Assets/Scripts/[useful]/GamesProtocol.proto` 的 `ActionCode` 中，把当前末尾：

```proto
    //进入PVP匹配队列
    JoinPvP = 15;
    //离开PVP匹配队列
    LeavePvP = 16;
```

改为：

```proto
    //进入PVP匹配队列
    JoinPvP = 15;
    //离开PVP匹配队列或准备房间
    LeavePvP = 16;
    //提交PVP阵容
    SubmitPvPTeam = 17;
```

- [ ] **Step 2: 修改 BattlePack 字段**

在 `message BattlePack` 中，当前字段为：

```proto
    int32 playerId = 6; // 操作者ID（PvE=1, PvP=1或2）

    repeated BattleEvent events = 10; // 战斗事件列表
    BattleStateSnapshot stateSnapshot = 11; // 完整状态快照
```

改为：

```proto
    int32 playerId = 6; // 操作者ID（PvE=1, PvP=1或2）
    repeated int32 heroIds = 7; // PVP提交阵容的角色配置ID
    string matchId = 8; // PVP准备房间ID
    bool isMatchSuccess = 9; // 是否已经匹配到对手

    repeated BattleEvent events = 10; // 战斗事件列表
    BattleStateSnapshot stateSnapshot = 11; // 完整状态快照
    bool isTeamReady = 12; // PVP双方阵容是否都已提交
```

- [ ] **Step 3: 生成 protobuf C# 文件**

如果本机已有 `protoc`，在仓库根目录运行：

```bash
protoc --csharp_out="Assets/Scripts/Network/Protocol" "Assets/Scripts/[useful]/GamesProtocol.proto" && cp "Assets/Scripts/Network/Protocol/GamesProtocol.cs" "AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/GamesProtocol.cs"
```

Expected: 两个 `GamesProtocol.cs` 都生成成功，且包含 `SubmitPvPTeam`、`HeroIds`、`MatchId`、`IsMatchSuccess`、`IsTeamReady`。

- [ ] **Step 4: 验证生成结果**

Run:

```bash
grep -n "SubmitPvPTeam\|HeroIds\|MatchId\|IsMatchSuccess\|IsTeamReady" "Assets/Scripts/Network/Protocol/GamesProtocol.cs" "AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/GamesProtocol.cs"
```

Expected: 两个生成文件都能搜索到上述符号。

- [ ] **Step 5: 服务端编译验证**

Run:

```bash
dotnet build "AAAGameServer/ConsoleMock_Server/GameServer/GameServer.csproj"
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/[useful]/GamesProtocol.proto" "Assets/Scripts/Network/Protocol/GamesProtocol.cs" "AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/GamesProtocol.cs"
git commit -m "feat: extend battle protocol for pvp preparation"
```

---

### Task 2: 服务端新增 PvpPrepareRoom

**Files:**
- Create: `AAAGameServer/ConsoleMock_Server/GameServer/Network/Matchmaking/PvpPrepareRoom.cs`
- Create: `AAAGameServer/ConsoleMock_Server/GameServer.Tests/PvpPrepareRoomTests.cs`

- [ ] **Step 1: 写 failing tests**

创建 `AAAGameServer/ConsoleMock_Server/GameServer.Tests/PvpPrepareRoomTests.cs`：

```csharp
using Network.Matchmaking;

namespace GameServer.Tests;

public class PvpPrepareRoomTests
{
    [Fact]
    public void SubmitTeam_StoresTeamForCorrectPlayer()
    {
        var room = new PvpPrepareRoom("match-1", "alice", "bob");

        Assert.True(room.SubmitTeam("alice", new List<int> { 1001, 1002 }));

        Assert.False(room.IsBothReady);
        Assert.Equal(new List<int> { 1001, 1002 }, room.HeroIdsP1);
        Assert.Empty(room.HeroIdsP2);
    }

    [Fact]
    public void SubmitTeam_ReturnsFalseForUserOutsideRoom()
    {
        var room = new PvpPrepareRoom("match-1", "alice", "bob");

        Assert.False(room.SubmitTeam("charlie", new List<int> { 1001 }));

        Assert.False(room.IsBothReady);
        Assert.Empty(room.HeroIdsP1);
        Assert.Empty(room.HeroIdsP2);
    }

    [Fact]
    public void IsBothReady_ReturnsTrueAfterBothPlayersSubmit()
    {
        var room = new PvpPrepareRoom("match-1", "alice", "bob");

        room.SubmitTeam("alice", new List<int> { 1001, 1002 });
        room.SubmitTeam("bob", new List<int> { 1003, 1004 });

        Assert.True(room.IsBothReady);
    }

    [Fact]
    public void Contains_ReturnsTrueForRoomPlayersOnly()
    {
        var room = new PvpPrepareRoom("match-1", "alice", "bob");

        Assert.True(room.Contains("alice"));
        Assert.True(room.Contains("bob"));
        Assert.False(room.Contains("charlie"));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test "AAAGameServer/ConsoleMock_Server/GameServer.Tests/GameServer.Tests.csproj" --filter PvpPrepareRoomTests
```

Expected: FAIL because `PvpPrepareRoom` does not exist.

- [ ] **Step 3: Create PvpPrepareRoom implementation**

创建 `AAAGameServer/ConsoleMock_Server/GameServer/Network/Matchmaking/PvpPrepareRoom.cs`：

```csharp
/*
* ┌──────────────────────────────────┐
* │  描    述: PvP 准备房间，记录双方玩家与阵容提交状态
* │  类    名: PvpPrepareRoom.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace Network.Matchmaking
{
    internal class PvpPrepareRoom
    {
        public string MatchId { get; }
        public string Player1 { get; }
        public string Player2 { get; }
        public List<int> HeroIdsP1 { get; private set; }
        public List<int> HeroIdsP2 { get; private set; }
        public bool IsBothReady => HeroIdsP1.Count > 0 && HeroIdsP2.Count > 0;

        public PvpPrepareRoom(string matchId, string player1, string player2)
        {
            MatchId = matchId;
            Player1 = player1;
            Player2 = player2;
            HeroIdsP1 = new List<int>();
            HeroIdsP2 = new List<int>();
        }

        // 判断用户是否属于当前准备房间
        public bool Contains(string username)
        {
            return username == Player1 || username == Player2;
        }

        // 获取用户在当前房间内的玩家ID
        public int GetPlayerId(string username)
        {
            if (username == Player1) return 1;
            if (username == Player2) return 2;
            return 0;
        }

        // 提交玩家阵容
        public bool SubmitTeam(string username, List<int> heroIds)
        {
            if (username == Player1)
            {
                HeroIdsP1 = new List<int>(heroIds);
                return true;
            }

            if (username == Player2)
            {
                HeroIdsP2 = new List<int>(heroIds);
                return true;
            }

            return false;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run:

```bash
dotnet test "AAAGameServer/ConsoleMock_Server/GameServer.Tests/GameServer.Tests.csproj" --filter PvpPrepareRoomTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add "AAAGameServer/ConsoleMock_Server/GameServer/Network/Matchmaking/PvpPrepareRoom.cs" "AAAGameServer/ConsoleMock_Server/GameServer.Tests/PvpPrepareRoomTests.cs"
git commit -m "feat: add pvp prepare room state"
```

---

### Task 3: BattleManager 使用准备房间替代立即开战

**Files:**
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Battle/BattleManager.cs`
- Create: `AAAGameServer/ConsoleMock_Server/GameServer.Tests/BattleManagerPvpPrepareTests.cs`

- [ ] **Step 1: 写 failing tests**

创建 `AAAGameServer/ConsoleMock_Server/GameServer.Tests/BattleManagerPvpPrepareTests.cs`：

```csharp
using GameServer.Battle;

namespace GameServer.Tests;

public class BattleManagerPvpPrepareTests
{
    [Fact]
    public void TryMatch_CreatesPrepareRoomWithoutBattleInstance()
    {
        var manager = new BattleManager(TestConfigFactory.CreateConfigManager());

        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        var room = manager.TryMatch();

        Assert.NotNull(room);
        Assert.Equal("alice", room.Player1);
        Assert.Equal("bob", room.Player2);
        Assert.Null(manager.GetBattle("alice"));
        Assert.Null(manager.GetBattle("bob"));
    }

    [Fact]
    public void SubmitPvpTeam_CreatesBattleAfterBothPlayersSubmit()
    {
        var manager = new BattleManager(TestConfigFactory.CreateConfigManager());
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();

        var first = manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002, 1003, 1004 });
        var second = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002, 1003, 1004 });

        Assert.Null(first);
        Assert.NotNull(second);
        Assert.Same(second, manager.GetBattle("alice"));
        Assert.Same(second, manager.GetBattle("bob"));
    }
}
```

创建 `AAAGameServer/ConsoleMock_Server/GameServer.Tests/TestConfigFactory.cs`：

```csharp
using GameServer.Battle.Data;

namespace GameServer.Tests;

internal static class TestConfigFactory
{
    public static ConfigManager CreateConfigManager()
    {
        var configManager = new ConfigManager();
        var configDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "GameServer", "Battle", "Data", "json"));
        configManager.LoadAll(configDir);
        return configManager;
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test "AAAGameServer/ConsoleMock_Server/GameServer.Tests/GameServer.Tests.csproj" --filter BattleManagerPvpPrepareTests
```

Expected: FAIL because `TryMatch()` still returns a tuple with `BattleInstance` and `SubmitPvpTeam()` does not exist.

- [ ] **Step 3: 修改 BattleManager 字段**

在 `AAAGameServer/ConsoleMock_Server/GameServer/Battle/BattleManager.cs` 中，将字段区改成：

```csharp
private readonly Dictionary<string, BattleInstance> _battles;
private readonly Dictionary<string, PvpPrepareRoom> _prepareRooms;
private readonly ConfigManager _configManager;
private readonly MatchmakingQueue _matchQueue = new();
private int _matchSequence;
```

构造函数改成：

```csharp
public BattleManager(ConfigManager configManager)
{
    _configManager = configManager;
    _battles = new Dictionary<string, BattleInstance>();
    _prepareRooms = new Dictionary<string, PvpPrepareRoom>();
}
```

- [ ] **Step 4: 修改匹配和提交方法**

将 `TryMatch()` 改为返回 `PvpPrepareRoom`，并新增 `GetPrepareRoom()`、`SubmitPvpTeam()`：

```csharp
public PvpPrepareRoom GetPrepareRoom(string username)
{
    _prepareRooms.TryGetValue(username, out var room);
    return room;
}

public PvpPrepareRoom TryMatch()
{
    if (!_matchQueue.CanMatch) return null;

    var (p1, p2) = _matchQueue.PopMatch();
    var matchId = $"pvp-{++_matchSequence}";
    var room = new PvpPrepareRoom(matchId, p1, p2);
    _prepareRooms[p1] = room;
    _prepareRooms[p2] = room;
    return room;
}

// 提交PvP阵容，双方提交后创建战斗
public BattleInstance SubmitPvpTeam(string username, List<int> heroIds)
{
    if (!_prepareRooms.TryGetValue(username, out var room)) return null;
    if (!room.SubmitTeam(username, heroIds)) return null;
    if (!room.IsBothReady) return null;

    var battle = CreatePvPBattle(room.Player1, room.Player2, room.HeroIdsP1, room.HeroIdsP2);
    _prepareRooms.Remove(room.Player1);
    _prepareRooms.Remove(room.Player2);
    return battle;
}
```

- [ ] **Step 5: 修改 LeaveQueue**

将 `LeaveQueue` 改为同时清理匹配队列和准备房间：

```csharp
public PvpPrepareRoom LeaveQueue(string username)
{
    _matchQueue.Dequeue(username);

    if (!_prepareRooms.TryGetValue(username, out var room)) return null;

    _prepareRooms.Remove(room.Player1);
    _prepareRooms.Remove(room.Player2);
    return room;
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run:

```bash
dotnet test "AAAGameServer/ConsoleMock_Server/GameServer.Tests/GameServer.Tests.csproj" --filter "PvpPrepareRoomTests|BattleManagerPvpPrepareTests"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add "AAAGameServer/ConsoleMock_Server/GameServer/Battle/BattleManager.cs" "AAAGameServer/ConsoleMock_Server/GameServer.Tests/BattleManagerPvpPrepareTests.cs" "AAAGameServer/ConsoleMock_Server/GameServer.Tests/TestConfigFactory.cs"
git commit -m "feat: create pvp battles after team submission"
```

---

### Task 4: BattleHandler 接入 Join/Leave/SubmitPvPTeam

**Files:**
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/ProtocolHandlers/BattleHandler.cs`
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Program.cs`

- [ ] **Step 1: 注册 SubmitPvPTeam switch 分支**

在 `BattleHandler.Handle()` 的 switch 里，在 `LeavePvP` 分支后添加：

```csharp
case ActionCode.SubmitPvPTeam:
    handleSubmitPvPTeam(client, pack);
    break;
```

- [ ] **Step 2: 替换 handleJoinPvP**

将 `handleJoinPvP` 替换为：

```csharp
private void handleJoinPvP(Client client, MainPack pack)
{
    if (!checkLogin(client, ActionCode.JoinPvP)) return;

    client.Server.BattleManager.JoinQueue(client.UserName);

    var room = client.Server.BattleManager.TryMatch();
    if (room == null)
    {
        var waitPack = new BattlePack { IsMatchSuccess = false };
        sendBattleResponse(client, ActionCode.JoinPvP, waitPack);
        return;
    }

    sendMatchSuccess(room.Player1, room);
    sendMatchSuccess(room.Player2, room);
}
```

- [ ] **Step 3: 添加 sendMatchSuccess**

在发送消息区域前添加：

```csharp
// 推送匹配成功结果
private void sendMatchSuccess(Client sourceClient, string username, Network.Matchmaking.PvpPrepareRoom room)
{
    var targetClient = sourceClient.Server.GetClientByUsername(username);
    if (targetClient == null) return;

    var battlePack = new BattlePack
    {
        PlayerId = room.GetPlayerId(username),
        MatchId = room.MatchId,
        IsMatchSuccess = true
    };
    sendBattleResponse(targetClient, ActionCode.JoinPvP, battlePack);
}
```

然后把 `handleJoinPvP` 末尾两行改成：

```csharp
sendMatchSuccess(client, room.Player1, room);
sendMatchSuccess(client, room.Player2, room);
```

- [ ] **Step 4: 替换 handleLeavePvP**

将 `handleLeavePvP` 替换为：

```csharp
private void handleLeavePvP(Client client, MainPack pack)
{
    if (!checkLogin(client, ActionCode.LeavePvP)) return;

    var room = client.Server.BattleManager.LeaveQueue(client.UserName);

    var resPack = new MainPack
    {
        RequestCode = RequestCode.Battle,
        ActionCode = ActionCode.LeavePvP,
        ReturnCode = ReturnCode.Succeed,
        StrMsg = "已离开匹配队列"
    };
    client.Send(resPack.ToByteArray());

    if (room == null) return;

    var other = room.Player1 == client.UserName ? room.Player2 : room.Player1;
    var otherClient = client.Server.GetClientByUsername(other);
    if (otherClient == null) return;

    var cancelPack = new MainPack
    {
        RequestCode = RequestCode.Battle,
        ActionCode = ActionCode.LeavePvP,
        ReturnCode = ReturnCode.Failed,
        StrMsg = "对手已离开匹配"
    };
    otherClient.Send(cancelPack.ToByteArray());
}
```

- [ ] **Step 5: 新增 handleSubmitPvPTeam**

在 `handleLeavePvP` 后添加：

```csharp
private void handleSubmitPvPTeam(Client client, MainPack pack)
{
    if (!checkLogin(client, ActionCode.SubmitPvPTeam)) return;

    var battlePack = pack.BattlePack;
    if (battlePack == null || battlePack.HeroIds.Count == 0)
    {
        sendBattleError(client, ActionCode.SubmitPvPTeam, "阵容不能为空");
        return;
    }

    var room = client.Server.BattleManager.GetPrepareRoom(client.UserName);
    if (room == null)
    {
        sendBattleError(client, ActionCode.SubmitPvPTeam, "当前不在PvP准备房间");
        return;
    }

    var battle = client.Server.BattleManager.SubmitPvpTeam(client.UserName, new List<int>(battlePack.HeroIds));
    if (battle == null)
    {
        var waitPack = new BattlePack
        {
            PlayerId = room.GetPlayerId(client.UserName),
            MatchId = room.MatchId,
            IsTeamReady = false
        };
        sendBattleResponse(client, ActionCode.SubmitPvPTeam, waitPack);
        return;
    }

    var events = battle.CollectEvents();
    sendPvpBattleStart(client, room.Player1, room, battle, events);
    sendPvpBattleStart(client, room.Player2, room, battle, events);
}
```

- [ ] **Step 6: 新增 sendPvpBattleStart**

在 `handleSubmitPvPTeam` 后添加：

```csharp
// 推送PvP战斗开始数据
private void sendPvpBattleStart(Client sourceClient, string username, Network.Matchmaking.PvpPrepareRoom room, BattleInstance battle, List<BattleEvent> events)
{
    var targetClient = sourceClient.Server.GetClientByUsername(username);
    if (targetClient == null) return;

    var response = new BattlePack
    {
        PlayerId = room.GetPlayerId(username),
        MatchId = room.MatchId,
        IsTeamReady = true,
        StateSnapshot = battle.GetStateSnapshot()
    };
    response.Events.AddRange(events);
    sendBattleResponse(targetClient, ActionCode.SubmitPvPTeam, response);
}
```

- [ ] **Step 7: Program 注册 SubmitPvPTeam**

在 `AAAGameServer/ConsoleMock_Server/GameServer/Program.cs` 的路由注册处，在 `LeavePvP` 后添加：

```csharp
ProtocolRouter.Register(ActionCode.SubmitPvPTeam, new BattleHandler());
```

- [ ] **Step 8: 编译验证**

Run:

```bash
dotnet build "AAAGameServer/ConsoleMock_Server/GameServer/GameServer.csproj"
```

Expected: `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
git add "AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/ProtocolHandlers/BattleHandler.cs" "AAAGameServer/ConsoleMock_Server/GameServer/Program.cs"
git commit -m "feat: handle pvp team submission protocol"
```

---

### Task 5: 客户端新增 PvP 会话和事件数据

**Files:**
- Create: `Assets/Scripts/Module/fight/Network/PvpSession.cs`
- Create: `Assets/Scripts/Module/fight/Network/PvpPrepareData.cs`
- Create: `Assets/Scripts/Module/fight/Network/PvpBattleStartData.cs`
- Modify: `Assets/Scripts/GameApp.cs`
- Modify: `Assets/Scripts/Common/Defines/EventDefines.cs`

- [ ] **Step 1: 创建 PvpSession**

创建 `Assets/Scripts/Module/fight/Network/PvpSession.cs`：

```csharp
/*
* ┌──────────────────────────────────┐
* │  描    述: PvP会话状态，记录匹配、准备与本地玩家ID
* │  类    名: PvpSession.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.fight.Network
{
    public class PvpSession
    {
        public bool IsPvp { get; private set; }
        public int PlayerId { get; private set; } = 1;
        public string MatchId { get; private set; } = string.Empty;
        public bool IsMatched { get; private set; }
        public bool IsTeamSubmitted { get; private set; }

        public int CurrentPlayerId => IsPvp ? PlayerId : 1;

        // 记录匹配成功数据
        public void SetMatched(int playerId, string matchId)
        {
            IsPvp = true;
            IsMatched = true;
            IsTeamSubmitted = false;
            PlayerId = playerId;
            MatchId = matchId ?? string.Empty;
        }

        // 标记本地阵容已提交
        public void MarkTeamSubmitted()
        {
            IsTeamSubmitted = true;
        }

        // 清理PvP状态
        public void Clear()
        {
            IsPvp = false;
            PlayerId = 1;
            MatchId = string.Empty;
            IsMatched = false;
            IsTeamSubmitted = false;
        }
    }
}
```

- [ ] **Step 2: 创建 PvpPrepareData**

创建 `Assets/Scripts/Module/fight/Network/PvpPrepareData.cs`：

```csharp
/*
* ┌──────────────────────────────────┐
* │  描    述: PvP匹配成功后传入编队界面的数据
* │  类    名: PvpPrepareData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.fight.Network
{
    public class PvpPrepareData
    {
        public int PlayerId { get; }
        public string MatchId { get; }

        public PvpPrepareData(int playerId, string matchId)
        {
            PlayerId = playerId;
            MatchId = matchId ?? string.Empty;
        }
    }
}
```

- [ ] **Step 3: 创建 PvpBattleStartData**

创建 `Assets/Scripts/Module/fight/Network/PvpBattleStartData.cs`：

```csharp
/*
* ┌──────────────────────────────────┐
* │  描    述: PvP双方准备完成后进入战斗的数据
* │  类    名: PvpBattleStartData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Data.card;
using GameProtocol;

namespace Module.fight.Network
{
    public class PvpBattleStartData
    {
        public int PlayerId { get; }
        public BattlePack BattlePack { get; }
        public List<CharacterDataSO> LocalCharacters { get; }
        public List<CharacterDataSO> OpponentCharacters { get; }

        public PvpBattleStartData(int playerId, BattlePack battlePack, List<CharacterDataSO> localCharacters, List<CharacterDataSO> opponentCharacters)
        {
            PlayerId = playerId;
            BattlePack = battlePack;
            LocalCharacters = localCharacters ?? new List<CharacterDataSO>();
            OpponentCharacters = opponentCharacters ?? new List<CharacterDataSO>();
        }
    }
}
```

- [ ] **Step 4: GameApp 挂载 PvpSession**

修改 `Assets/Scripts/GameApp.cs`：

在 using 区添加：

```csharp
using Module.fight.Network;
```

在静态字段区添加：

```csharp
public static PvpSession PvpSession;
```

在 `Init()` 里 `CardManager = new CardManager();` 后添加：

```csharp
PvpSession = new PvpSession();
```

- [ ] **Step 5: EventDefines 增加事件**

修改 `Assets/Scripts/Common/Defines/EventDefines.cs`，在网络战斗事件附近添加：

```csharp
public const string OnPvpMatchSuccess = "OnPvpMatchSuccess";
public const string OnPvpMatchFailed = "OnPvpMatchFailed";
public const string OnPvpTeamWaiting = "OnPvpTeamWaiting";
public const string OnPvpBattleStart = "OnPvpBattleStart";
```

- [ ] **Step 6: Unity 编译检查**

在 Unity Editor 中等待脚本编译完成。

Expected: Console 没有 C# 编译错误。

- [ ] **Step 7: Commit**

```bash
git add "Assets/Scripts/Module/fight/Network/PvpSession.cs" "Assets/Scripts/Module/fight/Network/PvpPrepareData.cs" "Assets/Scripts/Module/fight/Network/PvpBattleStartData.cs" "Assets/Scripts/GameApp.cs" "Assets/Scripts/Common/Defines/EventDefines.cs"
git commit -m "feat: add client pvp session state"
```

---

### Task 6: BattleNetworkController 接入 PvP 协议并携带 PlayerId

**Files:**
- Modify: `Assets/Scripts/Module/fight/Network/BattleNetworkController.cs`

- [ ] **Step 1: 添加 using**

在文件顶部添加：

```csharp
using System.Collections.Generic;
```

- [ ] **Step 2: Init/UnInit 注册 PvP action**

在 `Init()` 中 `RequestBattleState` 注册后添加：

```csharp
GameApp.NetworkManager.AddMessageHandler(ActionCode.JoinPvP, onJoinPvpResponse);
GameApp.NetworkManager.AddMessageHandler(ActionCode.LeavePvP, onLeavePvpResponse);
GameApp.NetworkManager.AddMessageHandler(ActionCode.SubmitPvPTeam, onSubmitPvpTeamResponse);
```

在 `UnInit()` 中对应添加：

```csharp
GameApp.NetworkManager.RemoveMessageHandler(ActionCode.JoinPvP, onJoinPvpResponse);
GameApp.NetworkManager.RemoveMessageHandler(ActionCode.LeavePvP, onLeavePvpResponse);
GameApp.NetworkManager.RemoveMessageHandler(ActionCode.SubmitPvPTeam, onSubmitPvpTeamResponse);
```

- [ ] **Step 3: 战斗发包携带 PlayerId**

修改 `SendPlayCard` 的 `BattlePack`：

```csharp
BattlePack = new BattlePack
{
    CardInstanceId = cardInstanceId,
    TargetEntityId = targetInstanceId,
    SourceSlotIndex = handIndex,
    PlayerId = GameApp.PvpSession.CurrentPlayerId
}
```

修改 `SendEndTurn`：

```csharp
BattlePack = new BattlePack
{
    PlayerId = GameApp.PvpSession.CurrentPlayerId
}
```

修改 `SendMoveCard` 的 `BattlePack`：

```csharp
BattlePack = new BattlePack
{
    SourceSlotIndex = fromIndex,
    TargetSlotIndex = toIndex,
    PlayerId = GameApp.PvpSession.CurrentPlayerId
}
```

修改 `SendUndo`：

```csharp
BattlePack = new BattlePack
{
    PlayerId = GameApp.PvpSession.CurrentPlayerId
}
```

- [ ] **Step 4: 新增 PvP 发送方法**

在发送请求区域添加：

```csharp
// 发送加入PvP匹配请求
public void SendJoinPvP()
{
    var pack = new MainPack
    {
        RequestCode = RequestCode.Battle,
        ActionCode = ActionCode.JoinPvP
    };
    GameApp.NetworkManager.Send(pack);
}

// 发送离开PvP匹配请求
public void SendLeavePvP()
{
    var pack = new MainPack
    {
        RequestCode = RequestCode.Battle,
        ActionCode = ActionCode.LeavePvP
    };
    GameApp.NetworkManager.Send(pack);
}

// 发送提交PvP阵容请求
public void SendSubmitPvPTeam(List<int> heroIds)
{
    var battlePack = new BattlePack();
    battlePack.HeroIds.AddRange(heroIds);

    var pack = new MainPack
    {
        RequestCode = RequestCode.Battle,
        ActionCode = ActionCode.SubmitPvPTeam,
        BattlePack = battlePack
    };
    GameApp.NetworkManager.Send(pack);
}
```

- [ ] **Step 5: 新增 PvP 响应处理**

在响应处理区域添加：

```csharp
// 加入PvP响应
private void onJoinPvpResponse(MainPack pack)
{
    if (!checkResponse(pack, ActionCode.JoinPvP))
    {
        GameApp.MessageCenter.PostEvent(EventDefines.OnPvpMatchFailed, pack.StrMsg);
        return;
    }

    if (pack.BattlePack == null || !pack.BattlePack.IsMatchSuccess) return;

    GameApp.PvpSession.SetMatched(pack.BattlePack.PlayerId, pack.BattlePack.MatchId);
    GameApp.MessageCenter.PostEvent(EventDefines.OnPvpMatchSuccess,
        new PvpPrepareData(pack.BattlePack.PlayerId, pack.BattlePack.MatchId));
}

// 离开PvP响应
private void onLeavePvpResponse(MainPack pack)
{
    GameApp.PvpSession.Clear();

    if (pack.ReturnCode == ReturnCode.Succeed) return;

    GameApp.MessageCenter.PostEvent(EventDefines.OnPvpMatchFailed, pack.StrMsg);
}

// 提交PvP阵容响应
private void onSubmitPvpTeamResponse(MainPack pack)
{
    if (!checkResponse(pack, ActionCode.SubmitPvPTeam))
    {
        GameApp.MessageCenter.PostEvent(EventDefines.OnPvpMatchFailed, pack.StrMsg);
        return;
    }

    if (pack.BattlePack == null) return;

    if (!pack.BattlePack.IsTeamReady)
    {
        GameApp.PvpSession.MarkTeamSubmitted();
        GameApp.MessageCenter.PostEvent(EventDefines.OnPvpTeamWaiting, pack.StrMsg);
        return;
    }

    GameApp.MessageCenter.PostEvent(EventDefines.OnPvpBattleStart, pack.BattlePack);
}
```

- [ ] **Step 6: Unity 编译检查**

在 Unity Editor 中等待脚本编译完成。

Expected: Console 没有 C# 编译错误。

- [ ] **Step 7: Commit**

```bash
git add "Assets/Scripts/Module/fight/Network/BattleNetworkController.cs"
git commit -m "feat: add pvp network requests"
```

---

### Task 7: LevelView 和 MatchmakingView 接入匹配流程

**Files:**
- Modify: `Assets/Scripts/Module/View/LevelView.cs`
- Modify: `Assets/Scripts/Module/View/MatchmakingView.cs`

- [ ] **Step 1: 修改 LevelView 匹配按钮**

在 `Assets/Scripts/Module/View/LevelView.cs` 的 `onMatchmakingBtn()` 中替换 TODO：

```csharp
private void onMatchmakingBtn()
{
    GameApp.ViewManager.Open(ViewType.MatchmakingView);
}
```

- [ ] **Step 2: 修改 MatchmakingView 字段和 using**

在 `MatchmakingView.cs` 顶部添加：

```csharp
using Common.Defines;
using Module.fight.Network;
using MVC;
```

将字段替换为：

```csharp
private TextMeshProUGUI m_txt;
private readonly BattleNetworkController m_battleNetwork = new BattleNetworkController();
```

- [ ] **Step 3: 修改 OnAwake**

替换 `OnAwake()`：

```csharp
protected override void OnAwake()
{
    m_txt = Find<TextMeshProUGUI>("txt");
    Find<Button>("Btn_cancel").onClick.AddListener(onCancelBtn);
}
```

- [ ] **Step 4: 新增 Open/Close 事件生命周期**

在 `OnAwake()` 后添加：

```csharp
public override void Open(params object[] args)
{
    m_txt.text = "匹配中...";
    m_battleNetwork.Init();
    GameApp.MessageCenter.AddEvent(EventDefines.OnPvpMatchSuccess, onPvpMatchSuccess);
    GameApp.MessageCenter.AddEvent(EventDefines.OnPvpMatchFailed, onPvpMatchFailed);
    m_battleNetwork.SendJoinPvP();
}

public override void Close(params object[] args)
{
    GameApp.MessageCenter.RemoveEvent(EventDefines.OnPvpMatchSuccess, onPvpMatchSuccess);
    GameApp.MessageCenter.RemoveEvent(EventDefines.OnPvpMatchFailed, onPvpMatchFailed);
    m_battleNetwork.UnInit();
    base.Close(args);
}
```

`BaseView.Close(params object[] args)` 是 virtual；这里使用 `public override void Close(params object[] args)`，并在方法末尾调用 `base.Close(args)`。

- [ ] **Step 5: 新增匹配回调**

添加：

```csharp
// 处理PvP匹配成功
private void onPvpMatchSuccess(object args)
{
    var data = args as PvpPrepareData;
    if (data == null) return;

    m_txt.text = "匹配成功";
    GameApp.ViewManager.Open(ViewType.PrepareFightView, data);
}

// 处理PvP匹配失败
private void onPvpMatchFailed(object args)
{
    string msg = args as string;
    m_txt.text = string.IsNullOrEmpty(msg) ? "匹配失败" : msg;
}
```

- [ ] **Step 6: 修改取消按钮**

替换 `onCancelBtn()`：

```csharp
private void onCancelBtn()
{
    m_battleNetwork.SendLeavePvP();
    GameApp.PvpSession.Clear();
    GameApp.ViewManager.NavigateBack();
}
```

- [ ] **Step 7: Unity 编译检查**

在 Unity Editor 中等待脚本编译完成。

Expected: Console 没有 C# 编译错误。

- [ ] **Step 8: Commit**

```bash
git add "Assets/Scripts/Module/View/LevelView.cs" "Assets/Scripts/Module/View/MatchmakingView.cs"
git commit -m "feat: connect matchmaking view to pvp queue"
```

---

### Task 8: PrepareFightView 支持 PvP 编队提交

**Files:**
- Modify: `Assets/Scripts/Module/View/PrepareFightView.cs`

- [ ] **Step 1: 添加 using**

添加：

```csharp
using GameProtocol;
using Module.fight.Network;
```

- [ ] **Step 2: 添加字段**

在 `_currentLevelId` 后添加：

```csharp
private bool m_isPvpMode;
private PvpPrepareData m_pvpPrepareData;
private readonly BattleNetworkController m_battleNetwork = new BattleNetworkController();
```

- [ ] **Step 3: 修改 Open**

将 `Open(params object[] args)` 替换为：

```csharp
public override void Open(params object[] args)
{
    m_isPvpMode = args.Length > 0 && args[0] is PvpPrepareData;

    if (m_isPvpMode)
    {
        m_pvpPrepareData = args[0] as PvpPrepareData;
        levelTargetText1.text = "选择你的PvP阵容";
        levelTargetText2.text = "双方确认后开始战斗";
        m_battleNetwork.Init();
        GameApp.MessageCenter.AddEvent(EventDefines.OnPvpTeamWaiting, onPvpTeamWaiting);
        GameApp.MessageCenter.AddEvent(EventDefines.OnPvpBattleStart, onPvpBattleStart);
        return;
    }

    _currentLevelId = (int)args[0];
    LevelDataSO dataSo = GameApp.ConfigManager.Level.Get(_currentLevelId);
    if (dataSo == null)
    {
        QLog.Error("未找到关卡数据, id: " + _currentLevelId);
        return;
    }

    string[] desParts = dataSo.Description.Split('-');
    if (desParts.Length >= 2)
    {
        levelTargetText1.text = desParts[0];
        levelTargetText2.text = desParts[1];
    }
}
```

- [ ] **Step 4: 添加 Close 清理**

添加：

```csharp
public override void Close(params object[] args)
{
    GameApp.MessageCenter.RemoveEvent(EventDefines.OnPvpTeamWaiting, onPvpTeamWaiting);
    GameApp.MessageCenter.RemoveEvent(EventDefines.OnPvpBattleStart, onPvpBattleStart);
    m_battleNetwork.UnInit();
    base.Close(args);
}
```

- [ ] **Step 5: 修改返回按钮**

替换 `onReturnBtn()`：

```csharp
private void onReturnBtn()
{
    if (m_isPvpMode)
    {
        m_battleNetwork.SendLeavePvP();
        GameApp.PvpSession.Clear();
    }

    GameApp.ViewManager.NavigateBack();
}
```

- [ ] **Step 6: 修改开始按钮**

替换 `onActionBtn()`：

```csharp
private void onActionBtn()
{
    if (m_isPvpMode)
    {
        submitPvpTeam();
        return;
    }

    ViewExtensions.LoadScene(this, SceneDefines.Fight,() =>
    {
        ApplyControllerFunc(ControllerType.Fight, EventDefines.OpenFightingView, GetLevelInitData());
    });
}
```

- [ ] **Step 7: 新增 submitPvpTeam**

添加：

```csharp
// 提交PvP阵容
private void submitPvpTeam()
{
    var heroIds = new List<int>();
    for (int i = 0; i < formationCards.Length; i++)
    {
        string cardName = formationCards[i].GetCardName();
        CharacterDataSO data = GameApp.ConfigManager.Character.GetByName(cardName);
        if (data == null) continue;
        heroIds.Add(data.Id);
    }

    if (heroIds.Count == 0)
    {
        levelTargetText1.text = "请至少选择一个角色";
        return;
    }

    GameApp.PvpSession.MarkTeamSubmitted();
    levelTargetText1.text = "已提交阵容";
    levelTargetText2.text = "等待对方选择完成";
    m_battleNetwork.SendSubmitPvPTeam(heroIds);
}
```

- [ ] **Step 8: 新增等待和开战回调**

添加：

```csharp
// 等待对方提交PvP阵容
private void onPvpTeamWaiting(object args)
{
    levelTargetText1.text = "已提交阵容";
    levelTargetText2.text = "等待对方选择完成";
}

// 双方准备完成后进入PvP战斗
private void onPvpBattleStart(object args)
{
    var battlePack = args as BattlePack;
    if (battlePack == null) return;

    var localCharacters = collectSelectedCharacters();
    var opponentCharacters = collectOpponentCharacters(battlePack);
    var startData = new PvpBattleStartData(GameApp.PvpSession.PlayerId, battlePack, localCharacters, opponentCharacters);

    ViewExtensions.LoadScene(this, SceneDefines.Fight, () =>
    {
        ApplyControllerFunc(ControllerType.Fight, EventDefines.OpenFightingView, startData);
    });
}
```

- [ ] **Step 9: 新增角色收集方法**

添加：

```csharp
// 收集本地已选择角色
private List<CharacterDataSO> collectSelectedCharacters()
{
    var characters = new List<CharacterDataSO>();
    for (int i = 0; i < formationCards.Length; i++)
    {
        string cardName = formationCards[i].GetCardName();
        CharacterDataSO data = GameApp.ConfigManager.Character.GetByName(cardName);
        if (data != null) characters.Add(data);
    }
    return characters;
}

// 从服务端快照收集对手角色
private List<CharacterDataSO> collectOpponentCharacters(BattlePack battlePack)
{
    var characters = new List<CharacterDataSO>();
    if (battlePack.StateSnapshot == null) return characters;

    for (int i = 0; i < battlePack.StateSnapshot.Entities.Count; i++)
    {
        var entity = battlePack.StateSnapshot.Entities[i];
        bool isLocal = GameApp.PvpSession.PlayerId == 1 ? entity.IsPlayerSide : !entity.IsPlayerSide;
        if (isLocal) continue;

        CharacterDataSO data = GameApp.ConfigManager.Character.Get(entity.ConfigId);
        if (data != null) characters.Add(data);
    }

    return characters;
}
```

- [ ] **Step 10: Unity 编译检查**

在 Unity Editor 中等待脚本编译完成。

Expected: Console 没有 C# 编译错误。

- [ ] **Step 11: Commit**

```bash
git add "Assets/Scripts/Module/View/PrepareFightView.cs"
git commit -m "feat: submit pvp team from prepare view"
```

---

### Task 9: EntityManager 和 FightController 支持 PvP 入战

**Files:**
- Modify: `Assets/Scripts/Module/Character/Entity/EntityManager.cs`
- Modify: `Assets/Scripts/Module/fight/FightController.cs`

- [ ] **Step 1: EntityManager 增加 PvP 生成方法**

在 `EntityManager.cs` 的 `SpawnBattleEntities` 方法后添加：

```csharp
// 生成PvP双方角色实体
public void SpawnPvpBattleEntities(List<CharacterDataSO> localCharacters, List<CharacterDataSO> opponentCharacters, Action onComplete)
{
    ClearBattleEntities();

    int localCount = localCharacters == null ? 0 : localCharacters.Count;
    int opponentCount = opponentCharacters == null ? 0 : opponentCharacters.Count;
    if (localCount == 0 && opponentCount == 0)
    {
        onComplete?.Invoke();
        return;
    }

    for (int i = 0; i < localCount; i++)
    {
        int index = i;
        CharacterDataSO data = localCharacters[index];
        if (data == null || string.IsNullOrEmpty(data.En_Name)) continue;

        string heroRes = AddressDefines.Character_Hero + data.En_Name;
        ResManager.InstantiateAsync(heroRes, (go) =>
        {
            setHeroEntityData(go, data);
            if (AliveHeroes.Count == localCount && AliveEnemies.Count == opponentCount)
                onComplete?.Invoke();
        });
    }

    for (int i = 0; i < opponentCount; i++)
    {
        int index = i;
        CharacterDataSO data = opponentCharacters[index];
        if (data == null || string.IsNullOrEmpty(data.En_Name)) continue;

        string enemyRes = AddressDefines.Character_Enemy + data.En_Name;
        ResManager.InstantiateAsync(enemyRes, (go) =>
        {
            setEnemyEntityData(go, data);
            if (AliveHeroes.Count == localCount && AliveEnemies.Count == opponentCount)
                onComplete?.Invoke();
        });
    }
}
```

- [ ] **Step 2: FightController 添加 PvP 字段**

在字段区添加：

```csharp
private PvpBattleStartData m_pvpBattleStartData;
```

- [ ] **Step 3: 修改 onOpenFightView**

将 `onOpenFightView` 替换为：

```csharp
private void onOpenFightView(System.Object[] args)
{
    if (args[0] is PvpBattleStartData pvpData)
    {
        m_pvpBattleStartData = pvpData;
        GameApp.EntityManager.SpawnPvpBattleEntities(
            pvpData.LocalCharacters,
            pvpData.OpponentCharacters,
            onSpawnPvpBattleCallback);
        return;
    }

    _currentModel = args[0] as LevelModel;
    GameApp.EntityManager.SpawnBattleEntities(_currentModel, onSpawnBattleCallback);
}
```

- [ ] **Step 4: 新增 onSpawnPvpBattleCallback**

在 `onSpawnBattleCallback` 后添加：

```csharp
// PvP实体生成完成后进入战斗
private void onSpawnPvpBattleCallback()
{
    _isBattleActive = true;

    var localModel = new LevelModel(m_pvpBattleStartData.LocalCharacters, new List<MonsterSpawnData>(), 0);
    GameApp.CardManager.InitCards(localModel);

    GameApp.ViewManager.CloseAll();
    GameApp.ViewManager.Open(ViewType.FightingView);

    if (m_pvpBattleStartData.BattlePack.StateSnapshot != null)
        restoreFromStateSnapshot(m_pvpBattleStartData.BattlePack.StateSnapshot, false);

    PlayEventSequence.Play(m_pvpBattleStartData.BattlePack.Events);
}
```

- [ ] **Step 5: 添加 using**

确认 `FightController.cs` 顶部包含以下 using：

```csharp
using System.Collections.Generic;
using Module.fight.Network;
```

- [ ] **Step 6: Unity 编译检查**

在 Unity Editor 中等待脚本编译完成。

Expected: Console 没有 C# 编译错误。

- [ ] **Step 7: Commit**

```bash
git add "Assets/Scripts/Module/Character/Entity/EntityManager.cs" "Assets/Scripts/Module/fight/FightController.cs"
git commit -m "feat: enter fight from pvp battle start"
```

---

### Task 10: 端到端验证与修正

**Files:**
- Verify: files touched in Tasks 1-9.
- Modify only the exact file that fails verification, then re-run the failed verification step.

- [ ] **Step 1: 服务端全量测试**

Run:

```bash
dotnet test "AAAGameServer/ConsoleMock_Server/GameServer.Tests/GameServer.Tests.csproj"
```

Expected: all tests pass.

- [ ] **Step 2: 服务端编译**

Run:

```bash
dotnet build "AAAGameServer/ConsoleMock_Server/GameServer/GameServer.csproj"
```

Expected: `Build succeeded.`

- [ ] **Step 3: Unity 编译**

打开 Unity Editor，等待脚本编译完成。

Expected: Console 无 C# 编译错误。

- [ ] **Step 4: PvE 回归手测**

手动验证：

```text
打开关卡界面 -> 点击普通关卡 -> 进入 PrepareFightView -> 选择角色 -> 点击开始 -> 进入 FightingView -> 能出牌/结束回合
```

Expected: 原 PvE 流程可用。

- [ ] **Step 5: PvP 双客户端手测**

手动验证：

```text
启动服务端
客户端A登录账号A
客户端B登录账号B
客户端A点击匹配 -> MatchmakingView显示匹配中
客户端B点击匹配 -> 双方进入PrepareFightView
双方选择角色并点击开始
第一位提交者显示等待对方选择完成
第二位提交后双方进入FightingView
当前行动方出牌/移牌/撤销/结束回合请求带正确PlayerId
```

Expected: 双方进入战斗，服务端不再在匹配成功时立即创建战斗，而是在双方提交阵容后创建战斗。

- [ ] **Step 6: 取消流程手测**

手动验证：

```text
客户端A点击匹配 -> 点击取消 -> 返回关卡界面
客户端A和B匹配成功进入PrepareFightView -> A点击返回 -> A清理PvP状态，B收到对手离开提示
```

Expected: 主动离开的客户端能返回，另一方不会进入战斗。

- [ ] **Step 7: Final commit if fixes were needed**

如果验证中修改了具体文件，按实际修改文件精确添加，例如修复了 `FightController.cs` 和 `BattleHandler.cs` 时运行：

```bash
git add "Assets/Scripts/Module/fight/FightController.cs" "AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/ProtocolHandlers/BattleHandler.cs"
git commit -m "fix: stabilize pvp match prepare flow"
```

如果没有修复，不创建空提交。
