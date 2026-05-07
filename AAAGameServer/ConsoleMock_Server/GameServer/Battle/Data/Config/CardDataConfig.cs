/*
* ┌──────────────────────────────────┐
* │  描    述: 卡牌配置数据（对应JSON导出的hero_cards/enemy_cards）
* │  类    名: CardDataConfig.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace GameServer.Battle.Data.Config
{
    public class CardDataConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int OwnerId { get; set; }
        public CardType CardType { get; set; }
        public CardEffect[] Effects { get; set; }
    }
}
