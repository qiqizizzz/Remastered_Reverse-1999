/*
* ┌──────────────────────────────────┐
* │  描    述: PvP 准备房间单元测试
* │  类    名: PvpPrepareRoomTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Network.Matchmaking;
using Xunit;

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
