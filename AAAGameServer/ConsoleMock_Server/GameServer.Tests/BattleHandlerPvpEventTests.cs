/*
* ┌──────────────────────────────────┐
* │  描    述: PvP战斗协议事件过滤单元测试
* │  类    名: BattleHandlerPvpEventTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Reflection;
using GameProtocol;
using Xunit;

namespace GameServer.Tests;

public class BattleHandlerPvpEventTests
{
    [Fact]
    public void FilterVisiblePvpEvents_ForBattleEnd_ShouldUseViewerPerspective()
    {
        var events = new List<BattleEvent>
        {
            new BattleEvent
            {
                EventType = BattleEventType.BattleEnd,
                BattleEnd = new BattleEndParams { IsPlayerWin = true }
            }
        };

        var player1Events = invokeFilterVisiblePvpEvents(events, 1);
        var player2Events = invokeFilterVisiblePvpEvents(events, 2);

        Assert.True(player1Events.Single().BattleEnd.IsPlayerWin);
        Assert.False(player2Events.Single().BattleEnd.IsPlayerWin);
    }

    // 调用PvP事件过滤逻辑
    private static List<BattleEvent> invokeFilterVisiblePvpEvents(List<BattleEvent> events, int viewerPlayerId)
    {
        var handlerType = typeof(Network.BattleHandler);
        var method = handlerType.GetMethod("filterVisiblePvpEvents", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (List<BattleEvent>)method.Invoke(null, new object[] { events, viewerPlayerId })!;
    }
}
