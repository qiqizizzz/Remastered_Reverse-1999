/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗上下文扩展                      
* │  类    名: CombatContextExtension.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.fight.Core.Entities;

namespace Data.card.Extensions
{
    public static class CombatContextExtension
    {
        #region 行动点相关
        public static void AddActionPoint(this CombatContext context, int playerId, int ownerId, int delta)
        {
            if (context.Entities.TryGetValue(ownerId, out var entity))
            {
                entity.ActionPoint += delta;
                context.EventBus?.OnActionPointChanged?.Invoke(playerId, ownerId, entity.ActionPoint);
            }
        }

        public static void ClearActionPoint(this CombatContext context, int playerId, int ownerId)
        {
            if (context.Entities.TryGetValue(ownerId, out var entity))
            {
                entity.ActionPoint = 0;
                context.EventBus?.OnActionPointChanged?.Invoke(playerId, ownerId, 0);
            }
        }
        #endregion
        
        //自动合成算法
        public static bool CheckAndAutoMerge(this CombatContext context, int playerId)
        {
            if (!context.PlayerDecks.TryGetValue(playerId, out var deck)) return false;
            List<CardEntity> hands = deck.HandCards;

            for (int i = 0; i < hands.Count - 1; i++)
            {
                var cardA = hands[i];
                var cardB = hands[i + 1];

                if (cardA.ConfigId == cardB.ConfigId && cardA.StarLevel == cardB.StarLevel && cardA.StarLevel < 3) 
                {
                    var configA = context.CardCatalog.Get(cardA.ConfigId);
                    if (configA != null && configA.CardType != CardType.Ultimate)
                    {
                        cardA.StarLevel += 1;
                        hands.RemoveAt(i + 1);

                        context.EventBus?.OnCardMerged?.Invoke(playerId, cardA, cardB, cardA.StarLevel);
                        context.EventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(hands));

                        context.AddActionPoint(playerId, configA.OwnerId, 1);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
