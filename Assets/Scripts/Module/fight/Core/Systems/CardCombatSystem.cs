/*
* ┌──────────────────────────────────┐
* │  描    述: 卡牌战斗数值处理系统                      
* │  类    名: CardCombatSystem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Config.Catalogs;
using Data.card;
using Data.card.Extensions;
using Module.Character;
using Module.fight.Core.Entities;
using Module.fight.Core.EventBus;
using Common;
using UnityEngine;
using Random = System.Random;

namespace Module.fight.Core.Systems
{
    public class CardCombatSystem
    {
        private const int NORMAL_CARD_COPY_COUNT = 5;

        private readonly CombatContext _context;
        private readonly ICardCatalog _cardCatalog;
        private readonly CombatEventBus _eventBus;
        private readonly Random _random;

        public CardCombatSystem(CombatContext context, ICardCatalog cardCatalog, CombatEventBus eventBus)
        {
            _context = context;
            _cardCatalog = cardCatalog;
            _eventBus = eventBus;
            _random = new Random();
        }

        //初始化玩家牌库
        public void InitDeck(int playerId, List<int> CharacterConfigIds, int normalCardMaxLimit = NORMAL_CARD_COPY_COUNT)
        {
            if(!_context.PlayerDecks.TryGetValue(playerId, out var deck))
            {
                deck = new PlayerDeckEntity { PlayerId = playerId };
                _context.PlayerDecks[playerId] = deck;
            }
            
            deck.DrawPile.Clear();
            deck.HandCards.Clear();
            deck.DiscardPile.Clear();

            foreach (var charId in CharacterConfigIds)
            {
                var charCards = _cardCatalog.GetCharacterCards(charId);
                
                foreach (var card in charCards)
                {
                    if(card.CardType == CardType.Ultimate) continue;
                    
                    for (int i = 0; i < normalCardMaxLimit; i++)
                    {
                        deck.DrawPile.Add(new CardEntity(card.Id));
                    }
                }
            }
            
            ShuffleCard(deck.DrawPile);
        }

        #region 洗牌
        //Fisher–Yates 洗牌算法
        private void ShuffleCard(List<CardEntity> pile)
        {
            for (int i = 0; i < pile.Count; i++)
            {
                int RandomIndex = _random.Next(i, pile.Count);
                (pile[i], pile[RandomIndex]) = (pile[RandomIndex], pile[i]);
            }
        }
        #endregion

        #region 卡牌主要操作
        //抽牌
        public void DrawCard(int playerId, int count)
        {
            if(!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return;
            
            for (int i = 0; i < count; i++)
            {
                if (deck.DrawPile.Count == 0)
                {
                    if (deck.DiscardPile.Count == 0)
                    {
                        QLog.Warning("抽牌失败: 牌堆和弃牌堆都没有牌了");
                        break;
                    }
                    
                    deck.DrawPile.AddRange(deck.DiscardPile);
                    deck.DiscardPile.Clear();
                    ShuffleCard(deck.DrawPile);
                }
                
                CardEntity drawnCard = deck.DrawPile[^1];
                deck.DrawPile.RemoveAt(deck.DrawPile.Count - 1);
                deck.HandCards.Insert(0, drawnCard);
            }
            
            _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));
        }
        
        //弃牌
        public void DiscardCard(int playerId, CardEntity card)
        {
            if(!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return;
            
            var config = _cardCatalog.Get(card.ConfigId);
            if(config.CardType == CardType.Ultimate) return;
            
            card.StarLevel = 1;
            deck.DiscardPile.Add(card);
            
            _eventBus?.OnCardDiscarded?.Invoke(playerId, card);
            _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));
        }
        
        //发放大招牌
        public bool TryGiveUltimateCard(int playerId, int characterConfigId)
        {
            if(!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return false;
            
            foreach (var card in deck.HandCards)
            {
                var config = _cardCatalog.Get(card.ConfigId);
                if (config.CardType == CardType.Ultimate) return false;
            }

            
            var charCards = _cardCatalog.GetCharacterCards(characterConfigId);
            //寻找大招
            foreach (var cardData in charCards)
            {
                if(cardData.CardType == CardType.Ultimate)
                {
                    var ultimateCard = new CardEntity(cardData.Id);
                    deck.HandCards.Insert(0, ultimateCard);
                    
                    _eventBus?.OnUltimateCardGranted?.Invoke(playerId, characterConfigId, ultimateCard);
                    _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));
                    
                    return true;
                }
            }
            
            return false;
        }

        //移除角色卡牌
        public List<CardEntity> RemoveCardsOfCharacter(int playerId, int characterConfigId)
        {
            if (!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return new List<CardEntity>();

            deck.DrawPile.RemoveAll(c => _cardCatalog.Get(c.ConfigId).OwnerId == characterConfigId);
            deck.DiscardPile.RemoveAll(c => _cardCatalog.Get(c.ConfigId).OwnerId == characterConfigId);

            List<CardEntity> removedHandCards =
                deck.HandCards.FindAll(c => _cardCatalog.Get(c.ConfigId).OwnerId == characterConfigId);
            if (removedHandCards.Count > 0)
            {
                deck.HandCards.RemoveAll(c => _cardCatalog.Get(c.ConfigId).OwnerId == characterConfigId);
            }
            
            _eventBus?.OnCharacterCardsRemoved?.Invoke(playerId, characterConfigId, removedHandCards);
            _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));

            return removedHandCards;
        }
        #endregion
    }
}