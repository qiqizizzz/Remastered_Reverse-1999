namespace GameServer.Battle.Data
{
    internal interface ICardCatalog
    {
        CardDataConfig Get(int id);
        IReadOnlyList<CardDataConfig> GetCharacterCards(int characterId);
    }
}
