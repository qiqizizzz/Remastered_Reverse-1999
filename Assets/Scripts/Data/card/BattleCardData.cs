/*
* ┌───────────────────────────────────────┐
* │  描    述: 战斗卡牌数据类(记录局内状态)                      
* │  类    名: BattleCard.cs       
* │  创    建: By qiqizizzz
* └───────────────────────────────────────┘
*/

using Data.card;

namespace Module.fight.Component
{
    public class BattleCardData
    {
        public string InstanceId;
        public CardData BaseData;
        public int OwnerEntityId;//持有者Id（玩家或敌人）
        public int StarLevel;//星级

        public BattleCardData(CardData baseData)
        {
            InstanceId = System.Guid.NewGuid().ToString();
            BaseData = baseData;
            OwnerEntityId = baseData.OwnerId;
            StarLevel = 1;
        }
    }
}