using GameServer.Battle.Core.Entities;

namespace GameServer.Battle.Data
{
    internal class CardSnapshot
    {
        public Dictionary<string, int> HeroActionPoints;

        public List<CardEntity> HandCards;
        public List<CardEntity> DrawPile;
        public List<CardEntity> DiscardPile;
        public Dictionary<int, int> CardStarLevels;
    }
}
