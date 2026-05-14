/*
* ┌──────────────────────────────────┐
* │  描    述: CardEntity 单元测试
* │  类    名: CardEntityTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameServer.Battle.Core.Entities;
using Xunit;

namespace GameServer.Tests;

public class CardEntityTests
{
    [Fact]
    public void Constructor_WithConfigId_ShouldAutoIncrementInstanceId()
    {
        var card1 = new CardEntity(1001);
        var card2 = new CardEntity(1002);

        Assert.True(card1.InstanceId > 0);
        Assert.True(card2.InstanceId > card1.InstanceId);
        Assert.Equal(1001, card1.ConfigId);
        Assert.Equal(1, card1.StarLevel);
    }

    [Fact]
    public void Constructor_WithInstanceId_ShouldUseProvidedValues()
    {
        var card = new CardEntity(42, 1001, 3);

        Assert.Equal(42, card.InstanceId);
        Assert.Equal(1001, card.ConfigId);
        Assert.Equal(3, card.StarLevel);
    }

    [Fact]
    public void ClearData_ShouldResetStarLevel()
    {
        var card = new CardEntity(1, 1001, 5);
        card.ClearData();

        Assert.Equal(0, card.StarLevel);
    }
}
