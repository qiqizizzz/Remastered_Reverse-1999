using GameServer.Battle.Core.Entities;

namespace GameServer.Battle.Data
{
    public class CardSnapshot
    {
        public Dictionary<int, int> HeroActionPoints;

        public List<CardEntity> HandCards;
        public List<CardEntity> DrawPile;
        public List<CardEntity> DiscardPile;
        public Dictionary<int, int> CardStarLevels;
    }
}
