/*
* ┌──────────────────────────────────┐
* │  描    述: PVP战斗流程单元测试
* │  类    名: PvpBattleFlowTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;
using GameServer.Battle;
using GameServer.Battle.Data;
using Xunit;

namespace GameServer.Tests;

public class PvpBattleFlowTests
{
    [Fact]
    public void BattleStart_InPvp_ShouldNotCarryDefaultViewerEntities()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002 });
        Assert.NotNull(battle);

        var battleStartEvents = battle.CollectEvents().Where(e => e.EventType == BattleEventType.BattleStart).ToList();

        Assert.NotEmpty(battleStartEvents);
        Assert.All(battleStartEvents, evt => Assert.Empty(evt.BattleStart.Entities));
    }

    [Fact]
    public void EndTurn_InPvp_ShouldEmitPlayerExecuteSourceAndNextPlayerTurnStart()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002 });
        Assert.NotNull(battle);
        battle.CollectEvents();

        var player1Card = battle.Context.PlayerDecks[1].HandCards[0];
        var player2Target = battle.AllEntities.First(e => e.OwnerPlayerId == 2);
        var player1Caster = battle.AllEntities.First(e => e.OwnerPlayerId == 1 && e.ConfigId == battle.Context.CardCatalog.Get(player1Card.ConfigId).OwnerId);

        Assert.True(battle.PlayCard(1, player1Card.InstanceId, player2Target.InstanceId, 0));
        battle.CollectEvents();
        battle.EndTurn(1);

        var events = battle.CollectEvents();
        var executeEvent = events.First(e => e.EventType == BattleEventType.EnqueueCard);
        var nextTurnEvent = events.First(e => e.EventType == BattleEventType.TurnStart);

        Assert.Equal(1, executeEvent.EventOwnerId);
        Assert.Equal(player1Caster.InstanceId, executeEvent.SourceId);
        Assert.Equal(player2Target.InstanceId, executeEvent.TargetId);
        Assert.Equal(2, nextTurnEvent.EventOwnerId);
        Assert.True(nextTurnEvent.TurnStart.IsPlayerTurn);
    }

    [Fact]
    public void EndTurn_InPvp_ShouldAllowNextPlayerToPlayCard()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002 });
        Assert.NotNull(battle);
        battle.CollectEvents();

        int maxActions = battle.Context.ActionQueue.MaxQueueSize;
        for (int i = 0; i < maxActions; i++)
        {
            var card = battle.Context.PlayerDecks[1].HandCards[0];
            var target = battle.AllEntities.First(e => e.OwnerPlayerId == 2);
            Assert.True(battle.PlayCard(1, card.InstanceId, target.InstanceId, 0));
            battle.CollectEvents();
        }

        battle.EndTurn(1);
        battle.CollectEvents();
        var player2Card = battle.Context.PlayerDecks[2].HandCards[0];
        var player1Target = battle.AllEntities.First(e => e.OwnerPlayerId == 1);

        Assert.True(battle.PlayCard(2, player2Card.InstanceId, player1Target.InstanceId, 0));
    }

    // 创建带配置的战斗管理器
    private static BattleManager createBattleManager()
    {
        var configManager = new ConfigManager();
        configManager.LoadAll(Path.Combine(AppContext.BaseDirectory, "Battle", "Data", "json"));
        return new BattleManager(configManager);
    }
}
