/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗卡牌数据扩展类                      
* │  类    名: BattleCardDataExtension.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.fight.Core.Entities;

namespace Data.card.Extensions
{
    public static class CardModelExtension
    {
        /// <summary>
        /// 扩展方法：直接获取卡牌的只读配置表数据
        /// </summary>
        public static CardDataSO GetConfig(this CardEntity cardData)
        {
            return GameApp.ConfigManager.Card.Get(cardData.ConfigId);
        }
    }
}