namespace GameServer.Battle.Data
{
    public interface ICardCatalog
    {
        CardDataConfig Get(int id);
        IReadOnlyList<CardDataConfig> GetCharacterCards(int characterId);
    }
}
