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
        public string Player1Name { get; private set; }
        public string Player2Name { get; private set; }

        public PvpPrepareData(string matchId, int playerId, string p1Name = "玩家1", string p2Name = "玩家2")
        {
            MatchId = matchId;
            PlayerId = playerId;
            Player1Name = p1Name;
            Player2Name = p2Name;
        }
    }
}
