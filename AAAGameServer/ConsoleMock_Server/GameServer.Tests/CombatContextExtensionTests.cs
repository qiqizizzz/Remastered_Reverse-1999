/*
* ┌──────────────────────────────────┐
* │  描    述: CombatContextExtension 单元测试
* │  类    名: CombatContextExtensionTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameServer.Battle.Core.Entities;
using GameServer.Battle.Core.EventBus;
using GameServer.Battle.Core.Extensions;
using GameServer.Battle.Data;
using GameServer.Battle.Data.Config;
using Xunit;

namespace GameServer.Tests;

public class CombatContextExtensionTests
{
    private static CombatContext CreateContext()
    {
        return new CombatContext(new FakeCardCatalog(), new CombatEventBus());
    }

    [Fact]
    public void CheckAndAutoMerge_TwoAdjacentSameCards_ShouldMerge()
    {
        var ctx = CreateContext();
        var deck = new PlayerDeckEntity();

        var card1 = new CardEntity(1, 1001, 1);
        var card2 = new CardEntity(2, 1001, 1);
        deck.HandCards.Add(card1);
        deck.HandCards.Add(card2);
        ctx.PlayerDecks[1] = deck;

        bool merged = ctx.CheckAndAutoMerge(1);

        Assert.True(merged);
        Assert.Single(deck.HandCards);
        Assert.Equal(2, deck.HandCards[0].StarLevel);
    }

    [Fact]
    public void CheckAndAutoMerge_DifferentConfigIds_ShouldNotMerge()
    {
        var ctx = CreateContext();
        var deck = new PlayerDeckEntity();

        var card1 = new CardEntity(1, 1001, 1);
        var card2 = new CardEntity(2, 2002, 1);
        deck.HandCards.Add(card1);
        deck.HandCards.Add(card2);
        ctx.PlayerDecks[1] = deck;

        bool merged = ctx.CheckAndAutoMerge(1);

        Assert.False(merged);
        Assert.Equal(2, deck.HandCards.Count);
    }

    [Fact]
    public void CheckAndAutoMerge_NonAdjacent_ShouldNotMerge()
    {
        var ctx = CreateContext();
        var deck = new PlayerDeckEntity();

        var card1 = new CardEntity(1, 1001, 1);
        var card2 = new CardEntity(2, 2002, 1);
        var card3 = new CardEntity(3, 1001, 1);
        deck.HandCards.Add(card1);
        deck.HandCards.Add(card2);
        deck.HandCards.Add(card3);
        ctx.PlayerDecks[1] = deck;

        bool merged = ctx.CheckAndAutoMerge(1);

        Assert.False(merged);
        Assert.Equal(3, deck.HandCards.Count);
    }

    private class FakeCardCatalog : ICardCatalog
    {
        public CardDataConfig Get(int id) => new CardDataConfig
        {
            Id = id,
            CardType = CardType.Attack,
            OwnerId = 1,
            Name = "",
            Description = "",
            Effects = Array.Empty<CardEffect>()
        };

        public IReadOnlyList<CardDataConfig> GetCharacterCards(int characterId)
            => Array.Empty<CardDataConfig>();
    }
}
