/*
* ┌──────────────────────────────────┐
* │  描    述: PVP战斗流程单元测试
* │  类    名: PvpBattleFlowTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;
using GameServer.Battle;
using GameServer.Battle.Core.Entities;
using GameServer.Battle.Data;
using GameServer.Battle.Data.Config;
using Xunit;

namespace GameServer.Tests;

public class PvpBattleFlowTests
{
    [Fact]
    public void InitDeck_InPvp_ShouldCreateFiveCopiesPerNormalCard()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002, 1003, 1004 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002, 1003, 1004 });
        Assert.NotNull(battle);

        int totalPlayer2NormalCards = countAllNormalCards(battle, 2);

        Assert.Equal(40, totalPlayer2NormalCards);
    }

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
    public void BattleStart_InPvp_ShouldEmitPlayer2TurnStart()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002 });
        Assert.NotNull(battle);

        var events = battle.CollectEvents();
        var turnStartEvent = events.Last(e => e.EventType == BattleEventType.TurnStart);

        Assert.Equal(2, turnStartEvent.EventOwnerId);
        Assert.True(turnStartEvent.TurnStart.IsPlayerTurn);
    }

    [Fact]
    public void PvpBattle_ShouldStartFromPlayer2AndBlockPlayer1Actions()
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
        var player2Card = battle.Context.PlayerDecks[2].HandCards[0];
        var player1Target = battle.AllEntities.First(e => e.OwnerPlayerId == 1);
        var player2Target = battle.AllEntities.First(e => e.OwnerPlayerId == 2);

        Assert.False(battle.PlayCard(1, player1Card.InstanceId, player2Target.InstanceId, 0));
        Assert.True(battle.PlayCard(2, player2Card.InstanceId, player1Target.InstanceId, 0));
    }

    [Fact]
    public void EndTurn_InPvp_ShouldSwitchFromPlayer2ToPlayer1()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002 });
        Assert.NotNull(battle);
        battle.CollectEvents();

        playCardsUntilQueueFull(battle, 2, 1);

        battle.EndTurn(2);
        var events = battle.CollectEvents();
        var nextTurnEvent = events.First(e => e.EventType == BattleEventType.TurnStart);

        Assert.Equal(1, nextTurnEvent.EventOwnerId);
        Assert.True(nextTurnEvent.TurnStart.IsPlayerTurn);
        var player1Card = battle.Context.PlayerDecks[1].HandCards[0];
        var player2Target = battle.AllEntities.First(e => e.OwnerPlayerId == 2);
        Assert.True(battle.PlayCard(1, player1Card.InstanceId, player2Target.InstanceId, 0));
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

        var player2Card = battle.Context.PlayerDecks[2].HandCards[0];
        var player1Target = battle.AllEntities.First(e => e.OwnerPlayerId == 1);
        var player2Caster = battle.AllEntities.First(e => e.OwnerPlayerId == 2 && e.ConfigId == battle.Context.CardCatalog.Get(player2Card.ConfigId).OwnerId);

        Assert.True(battle.PlayCard(2, player2Card.InstanceId, player1Target.InstanceId, 0));
        battle.CollectEvents();
        playCardsUntilQueueFull(battle, 2, 1);
        battle.EndTurn(2);

        var events = battle.CollectEvents();
        var executeEvent = events.First(e => e.EventType == BattleEventType.EnqueueCard);
        var nextTurnEvent = events.First(e => e.EventType == BattleEventType.TurnStart);

        Assert.Equal(2, executeEvent.EventOwnerId);
        Assert.Equal(player2Caster.InstanceId, executeEvent.SourceId);
        Assert.Equal(player1Target.InstanceId, executeEvent.TargetId);
        Assert.Equal(1, nextTurnEvent.EventOwnerId);
        Assert.True(nextTurnEvent.TurnStart.IsPlayerTurn);
    }

    [Fact]
    public void MultiTargetCard_InPvpPlayer2Turn_ShouldOnlyDamagePlayer1Entities()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002 });
        Assert.NotNull(battle);
        battle.CollectEvents();

        var multiTargetCard = battle.Context.PlayerDecks[2].HandCards.First(c => c.ConfigId == 2001 || c.ConfigId == 2004);
        var player1Target = battle.AllEntities.First(e => e.OwnerPlayerId == 1);
        Assert.True(battle.PlayCard(2, multiTargetCard.InstanceId, player1Target.InstanceId, 0));
        battle.CollectEvents();
        playCardsUntilQueueFull(battle, 2, 1);

        battle.EndTurn(2);
        var damageEvents = battle.CollectEvents().Where(e => e.EventType == BattleEventType.DamageTaken).ToList();

        Assert.NotEmpty(damageEvents);
        Assert.All(damageEvents, evt => Assert.Equal(1, battle.AllEntities.First(e => e.InstanceId == evt.TargetId).OwnerPlayerId));
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
            var card = battle.Context.PlayerDecks[2].HandCards[0];
            var target = battle.AllEntities.First(e => e.OwnerPlayerId == 1);
            Assert.True(battle.PlayCard(2, card.InstanceId, target.InstanceId, 0));
            battle.CollectEvents();
        }

        battle.EndTurn(2);
        battle.CollectEvents();
        var player1Card = battle.Context.PlayerDecks[1].HandCards[0];
        var player2Target = battle.AllEntities.First(e => e.OwnerPlayerId == 2);

        Assert.True(battle.PlayCard(1, player1Card.InstanceId, player2Target.InstanceId, 0));
    }

    [Fact]
    public void InitDeck_InPvpOneHero_ShouldKeepFourNormalCardsForRound()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001 });
        Assert.NotNull(battle);

        Assert.Equal(4, countNormalHandCards(battle, 2));
    }

    [Fact]
    public void InitDeck_InPvpThreeHeroes_ShouldKeepSixNormalCards()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002, 1003 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002, 1003 });
        Assert.NotNull(battle);

        Assert.Equal(6, countNormalHandCards(battle, 2));
        Assert.Equal(3, battle.Context.ActionQueue.MaxQueueSize);
    }

    [Fact]
    public void InitDeck_InPvpFourHeroes_ShouldKeepFourActionSlots()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002, 1003, 1004 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002, 1003, 1004 });
        Assert.NotNull(battle);

        Assert.Equal(8, countNormalHandCards(battle, 2));
        Assert.Equal(4, battle.Context.ActionQueue.MaxQueueSize);
    }

    [Fact]
    public void EndTurn_InPvpBeforeQueueFull_ShouldNotResolveActions()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002, 1003, 1004 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002, 1003, 1004 });
        Assert.NotNull(battle);
        battle.CollectEvents();

        var player2Card = battle.Context.PlayerDecks[2].HandCards[0];
        var player1Target = battle.AllEntities.First(e => e.OwnerPlayerId == 1);
        float hpBefore = player1Target.CurrentHp;

        Assert.True(battle.PlayCard(2, player2Card.InstanceId, player1Target.InstanceId, 0));
        battle.CollectEvents();
        battle.EndTurn(2);
        var events = battle.CollectEvents();

        Assert.DoesNotContain(events, e => e.EventType == BattleEventType.DamageTaken);
        Assert.Equal(hpBefore, player1Target.CurrentHp);
        Assert.Equal(BattleState.PlayerTurn, battle.State);
    }

    [Fact]
    public void EndTurn_InPvpWhenOpponentAllDead_ShouldEmitBattleEnd()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001 });
        Assert.NotNull(battle);
        battle.CollectEvents();

        var player2Card = battle.Context.PlayerDecks[2].HandCards[0];
        var player1Target = battle.AllEntities.First(e => e.OwnerPlayerId == 1);
        player1Target.CurrentHp = 1;

        Assert.True(battle.PlayCard(2, player2Card.InstanceId, player1Target.InstanceId, 0));
        battle.CollectEvents();
        battle.EndTurn(2);
        var events = battle.CollectEvents();

        Assert.Equal(BattleState.BattleEnd, battle.State);
        Assert.Contains(events, e => e.EventType == BattleEventType.BattleEnd);
    }

    [Fact]
    public void EntityDied_InPvp_ShouldOnlyRemoveCardsFromDeadOwnerDeck()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002 });
        Assert.NotNull(battle);
        battle.CollectEvents();

        var deadPlayer2Hero = battle.AllEntities.First(e => e.OwnerPlayerId == 2 && e.ConfigId == 1001);
        deadPlayer2Hero.CurrentHp = 0;
        int player1OwnerCardCountBefore = countCardsOwnedBy(battle, 1, 1001);

        playCardsUntilQueueFull(battle, 2, 1);
        battle.EndTurn(2);

        Assert.True(player1OwnerCardCountBefore > 0);
        Assert.True(countCardsOwnedBy(battle, 1, 1001) > 0);
        Assert.Equal(0, countCardsOwnedBy(battle, 2, 1001));
    }

    [Fact]
    public void InitDeck_InPvp_ShouldKeepSameHeroConfigSeparatedBetweenBothPlayers()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002, 1003 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002, 1003 });
        Assert.NotNull(battle);

        Assert.Equal(3, battle.AllEntities.Count(e => e.OwnerPlayerId == 1));
        Assert.Equal(3, battle.AllEntities.Count(e => e.OwnerPlayerId == 2));
        Assert.Equal(6, countNormalHandCards(battle, 1));
        Assert.Equal(6, countNormalHandCards(battle, 2));
    }

    [Fact]
    public void EndTurn_InPvp_ShouldKeepCardsSeparatedWhenOneSideHasOneHero()
    {
        var manager = createBattleManager();
        manager.JoinQueue("alice");
        manager.JoinQueue("bob");
        manager.TryMatch();
        manager.SubmitPvpTeam("alice", new List<int> { 1001 });
        var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002, 1003 });
        Assert.NotNull(battle);
        battle.CollectEvents();

        var player1Hero = battle.AllEntities.First(e => e.OwnerPlayerId == 1);
        var player2Hero = battle.AllEntities.First(e => e.OwnerPlayerId == 2);
        Assert.Equal(4, countNormalHandCards(battle, 1));
        Assert.Equal(6, countNormalHandCards(battle, 2));
        Assert.Equal(3, battle.Context.ActionQueue.MaxQueueSize);

        playCardsUntilQueueFull(battle, 2, 1);
        battle.EndTurn(2);
        battle.CollectEvents();

        Assert.Equal(4, countNormalHandCards(battle, 1));
        Assert.Equal(3, countNormalHandCards(battle, 2));
        Assert.Equal(1, battle.Context.ActionQueue.MaxQueueSize);
        Assert.Equal(4, battle.GetStateSnapshot(1).PlayerDeck.HandCards.Count);
        Assert.Equal(3, battle.GetStateSnapshot(2).PlayerDeck.HandCards.Count);
        Assert.True(player1Hero.CurrentHp > 0);
        Assert.True(player2Hero.CurrentHp > 0);
    }

    // 将指定玩家行动队列补满
    private static void playCardsUntilQueueFull(BattleInstance battle, int playerId, int targetOwnerPlayerId)
    {
        while (battle.Context.ActionQueue.QueuedCards.Count < battle.Context.ActionQueue.MaxQueueSize)
        {
            if (battle.Context.PlayerDecks[playerId].HandCards.Count == 0)
                break;

            var card = battle.Context.PlayerDecks[playerId].HandCards[0];
            var target = battle.AllEntities.First(e => e.OwnerPlayerId == targetOwnerPlayerId && e.CurrentHp > 0);
            Assert.True(battle.PlayCard(playerId, card.InstanceId, target.InstanceId, 0));
            battle.CollectEvents();
        }
    }

    // 统计指定玩家牌库中指定角色拥有的卡牌数量
    private static int countCardsOwnedBy(BattleInstance battle, int playerId, int ownerId)
    {
        var deck = battle.Context.PlayerDecks[playerId];
        return deck.HandCards.Concat(deck.DrawPile).Concat(deck.DiscardPile)
            .Count(c => battle.Context.CardCatalog.Get(c.ConfigId).OwnerId == ownerId);
    }

    // 统计指定玩家所有区域中的普通卡牌数量
    private static int countAllNormalCards(BattleInstance battle, int playerId)
    {
        var deck = battle.Context.PlayerDecks[playerId];
        return deck.HandCards.Concat(deck.DrawPile).Concat(deck.DiscardPile)
            .Count(c => battle.Context.CardCatalog.Get(c.ConfigId).CardType != CardType.Ultimate);
    }

    // 统计指定玩家手牌中的普通卡牌数量
    private static int countNormalHandCards(BattleInstance battle, int playerId)
    {
        var deck = battle.Context.PlayerDecks[playerId];
        return deck.HandCards.Count(c => battle.Context.CardCatalog.Get(c.ConfigId).CardType != CardType.Ultimate);
    }

    // 创建带配置的战斗管理器
    private static BattleManager createBattleManager()
    {
        var configManager = new ConfigManager();
        configManager.LoadAll(Path.Combine(AppContext.BaseDirectory, "Battle", "Data", "json"));
        return new BattleManager(configManager);
    }
}
