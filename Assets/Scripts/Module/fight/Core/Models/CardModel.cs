/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗卡牌实体数据(纯抽象状态,无表现层)                      
* │  类    名: CardModel.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.fight.Core.Models
{
    public class CardModel
    {
        /// <summary>
        /// 卡牌实例Id(全局唯一)
        /// </summary>
        public int InstanceId { get; private set; }
        
        /// <summary>
        /// 卡牌配置Id
        /// </summary>
        public int ConfigId { get; private set; }
        
        /// <summary>
        /// 卡牌星级
        /// </summary>
        public int StarLevel {get; set;}

        public CardModel(int instanceId, int configId, int starLevel = 1)
        {
            InstanceId = instanceId;
            ConfigId = configId;
            StarLevel = starLevel;
        }
    }
}