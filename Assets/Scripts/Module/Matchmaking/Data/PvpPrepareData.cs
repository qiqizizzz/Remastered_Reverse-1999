/*
* ┌──────────────────────────────────┐
* │  描    述: PvP准备界面打开参数，记录匹配房间与玩家编号
* │  类    名: PvpPrepareData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Matchmaking
{
    public class PvpPrepareData
    {
        public string MatchId { get; private set; }
        public int PlayerId { get; private set; }
        //TODO：需要加一个玩家名字。

        public PvpPrepareData(string matchId, int playerId)
        {
            MatchId = matchId;
            PlayerId = playerId;
        }
    }
}
