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
