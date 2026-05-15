/*
* ┌──────────────────────────────────┐
* │  描    述: BattleManager PvP准备房间单元测试
* │  类    名: BattleManagerPvpPrepareTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameServer.Battle;
using GameServer.Battle.Data;
using Xunit;

namespace GameServer.Tests;

public class BattleManagerPvpPrepareTests
{
    [Fact]
    public void TryMatch_CreatesPrepareRoomWithoutBattle()
    {
        var manager = createBattleManager();

        manager.JoinQueue("alice");
        manager.JoinQueue("bob");

        var room = manager.TryMatch();

        Assert.NotNull(room);
        Assert.Equal("alice", room.Player1);
        Assert.Equal("bob", room.Player2);
        Assert.Equal(1, room.GetPlayerId("alice"));
        Assert.Equal(2, room.GetPlayerId("bob"));
        Assert.Null(manager.GetBattle("alice"));
        Assert.Null(manager.GetBattle("bob"));
    }

    [Fact]
    public void SubmitPvpTeam_CreatesBattleAfterBothPlayersReady()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();

        var firstSubmit = manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002 });
        var secondSubmit = manager.SubmitPvpTeam("bob", new List<int> { 1003, 1004 });

        Assert.Null(firstSubmit);
        Assert.NotNull(secondSubmit);
        Assert.Same(secondSubmit, manager.GetBattle("alice"));
        Assert.Same(secondSubmit, manager.GetBattle("bob"));
        Assert.Null(manager.GetPrepareRoom("alice"));
    }

    [Fact]
    public void LeaveQueue_RemovesPrepareRoom()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();

        manager.LeaveQueue("alice");

        Assert.Null(manager.GetPrepareRoom("alice"));
        Assert.Null(manager.GetPrepareRoom("bob"));
    }

    // 创建带配置的战斗管理器
    private static BattleManager createBattleManager()
    {
        var configManager = new ConfigManager();
        configManager.LoadAll(Path.Combine(AppContext.BaseDirectory, "Battle", "Data", "json"));
        return new BattleManager(configManager);
    }
}
