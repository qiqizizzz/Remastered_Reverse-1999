/*
* ┌──────────────────────────────────┐
* │  描    述: PvP会话状态，记录当前匹配与战斗玩家信息
* │  类    名: PvpSession.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Matchmaking
{
    public class PvpSession
    {
        public string MatchId { get; private set; }
        public int CurrentPlayerId { get; private set; }
        public bool IsInPvp { get; private set; }

        // 设置匹配成功后的准备房间信息
        public void SetPrepareRoom(string matchId, int playerId)
        {
            MatchId = matchId;
            CurrentPlayerId = playerId;
            IsInPvp = true;
        }

        // 清理PvP会话状态
        public void Clear()
        {
            MatchId = string.Empty;
            CurrentPlayerId = 0;
            IsInPvp = false;
        }
    }
}
