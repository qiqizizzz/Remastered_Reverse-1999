using GameServer.Battle.Data.Config;

namespace GameServer.Battle.Data
{
    public class CardDataConfig
    {
        public CardType CardType;
        public int Id;
        public string Name;
        public string Description;
        public int OwnerId;
        public CardEffect[] Effects;
    }
}
