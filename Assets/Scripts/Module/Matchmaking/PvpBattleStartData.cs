/*
* ┌──────────────────────────────────┐
* │  描    述: PvP战斗开始数据，携带服务端快照与本地玩家编号
* │  类    名: PvpBattleStartData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;

namespace Module.Matchmaking
{
    public class PvpBattleStartData
    {
        public int PlayerId { get; private set; }
        public BattlePack BattlePack { get; private set; }

        public PvpBattleStartData(int playerId, BattlePack battlePack)
        {
            PlayerId = playerId;
            BattlePack = battlePack;
        }
    }
}
