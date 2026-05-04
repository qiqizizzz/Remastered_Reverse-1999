/*
* ┌──────────────────────────────────┐
* │  描    述: 卡牌目录                      
* │  类    名: CardCatalog.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Linq;
using Data.card;

namespace Config.Catalogs
{
    public interface ICardCatalog : IConfigCatalog<CardDataSO>
    {
        IReadOnlyList<CardDataSO> GetCharacterCards(int characterId);
    }
    
    public class CardCatalog : ICardCatalog
    {
        private readonly Dictionary<int, CardDataSO> cardConfig = new Dictionary<int, CardDataSO>();
        
        public void Init(IEnumerable<CardDataSO> items)
        {
            cardConfig.Clear();
            foreach (var item in items)
            {
                if(item != null) cardConfig[item.Id] = item;
            }
        }

        public CardDataSO Get(int id)  => cardConfig.TryGetValue(id, out var data) ? data : null;

        public IReadOnlyList<CardDataSO> GetAll() => cardConfig.Values.ToList();

        public IReadOnlyList<CardDataSO> GetCharacterCards(int characterId)
        {
            List<CardDataSO> cards = new List<CardDataSO>();

            foreach (var card in cardConfig)
            {
                if (card.Value.OwnerId == characterId)
                {
                    cards.Add(card.Value);
                }
            }
            
            return cards;
        }
    }
}