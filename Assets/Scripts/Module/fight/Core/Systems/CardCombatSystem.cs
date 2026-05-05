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
using UnityEngine;
using Random = System.Random;

namespace Module.fight.Core.Systems
{
    public class CardCombatSystem
    {
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
        public void InitDeck(int playerId, List<int> CharacterConfigIds, int normalCardMaxLimit = 3)
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

        #region 准备阶段与洗牌
        //为新关卡准备初始手牌
        public void PrepareHandsForNewLevel(int playerId, List<int> CharacterConfigIds)
        {
            //回合开始时（即新进入关卡时）,玩家当前手牌应为每个角色两张普通牌,加起来一共8张（4个角色）
            if(!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return;
            
            foreach (var charId in CharacterConfigIds)
            {
                var charCards = _cardCatalog.GetCharacterCards(charId);

                foreach (var card in charCards)
                {
                    if(card.CardType == CardType.Ultimate) continue;
                    
                    deck.HandCards.Add(new CardEntity(card.Id));
                }
            }
            
            ShuffleCard(deck.HandCards);
            _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));
        }
        
        //Fisher–Yates 洗牌算法
        public void ShuffleCard(List<CardEntity> pile)
        {
            for (int i = 0; i < pile.Count; i++)
            {
                int RandomIndex = _random.Next(i, pile.Count);
                (pile[i], pile[RandomIndex]) = (pile[RandomIndex], pile[i]);
            }
        }
        #endregion

        #region 卡牌主要操作
        //出牌
        public void PlayCard(int playerId, CardEntity card, string targetInstanceId)
        {
            if(!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return;

            // 从手牌移除
            int removeIndex = deck.HandCards.FindIndex(c => c.InstanceId == card.InstanceId);
            if (removeIndex == -1) return;
            deck.HandCards.RemoveAt(removeIndex);

            // 弃置（普通牌进弃牌堆，大招牌销毁）
            var config = _cardCatalog.Get(card.ConfigId);
            if (config.CardType != CardType.Ultimate)
            {
                card.StarLevel = 1;
                deck.DiscardPile.Add(card);
                _eventBus?.OnCardDiscarded?.Invoke(playerId, card);
            }

            // 行动点处理
            if (config.CardType == CardType.Ultimate)
            {
                ClearActionPoint(playerId, config.OwnerId);
            }
            else
            {
                AddActionPoint(playerId, config.OwnerId, 1);
            }

            _eventBus?.OnCardPlayed?.Invoke(playerId, card, targetInstanceId);
            _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));

            // 自动检查合成
            CheckAndAutoMerge(playerId);
        }
        
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
                        Debug.LogWarning("抽牌失败: 牌堆和弃牌堆都没有牌了");
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
        
        //交换手牌
        public void SwapHandCards(int playerId, int indexA, int indexB)
        {
            if(!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return;
            if (indexA < 0 || indexA >= deck.HandCards.Count || indexB < 0 || indexB >= deck.HandCards.Count) return;

            (deck.HandCards[indexA], deck.HandCards[indexB]) = (deck.HandCards[indexB], deck.HandCards[indexA]);

            _eventBus?.OnHandCardSwapped?.Invoke(playerId, indexA, indexB);
            _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));

            // 交换后检查合成
            CheckAndAutoMerge(playerId);
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
                    deck.HandCards.Insert(0, new CardEntity(cardData.Id));
                    
                    _eventBus?.OnUltimateCardGranted?.Invoke(playerId, characterConfigId, new CardEntity(cardData.Id));
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
        
        //自动合成算法
        public bool CheckAndAutoMerge(int playerId)
        {
            if (!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return false;
            List<CardEntity> hands = deck.HandCards;

            for (int i = 0; i < hands.Count - 1; i++)
            {
                var cardA = hands[i];
                var cardB = hands[i + 1];

                if (cardA.ConfigId == cardB.ConfigId && cardA.StarLevel == cardB.StarLevel && cardA.StarLevel < 3) 
                {
                    var configA = _cardCatalog.Get(cardA.ConfigId);
                    if (configA != null && configA.CardType != CardType.Ultimate)
                    {
                        cardA.StarLevel += 1;
                        hands.RemoveAt(i + 1);

                        _eventBus?.OnCardMerged?.Invoke(playerId, cardA, cardB, cardA.StarLevel);
                        _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(hands));
                        
                        UpdateActionPoint(playerId, configA.OwnerId, 1);
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region 行动点操作
        public void AddActionPoint(int playerId, int ownerId, int delta)
        {
            if (_context.Entities.TryGetValue(ownerId.ToString(), out var entity))
            {
                int old = entity.ActionPoint;
                entity.ActionPoint += delta;
                _eventBus?.OnActionPointChanged?.Invoke(playerId, ownerId, entity.ActionPoint);
            }
        }

        public void ClearActionPoint(int playerId, int ownerId)
        {
            if (_context.Entities.TryGetValue(ownerId.ToString(), out var entity))
            {
                entity.ActionPoint = 0;
                _eventBus?.OnActionPointChanged?.Invoke(playerId, ownerId, 0);
            }
        }
        
        private void UpdateActionPoint(int playerId, int ownerId, int delta)
        {
            if (_context.Entities.TryGetValue(ownerId.ToString(), out var entity))
            {
                int old = entity.ActionPoint;
                entity.ActionPoint += delta;
                _eventBus?.OnActionPointChanged?.Invoke(playerId, ownerId, entity.ActionPoint);
            }
        }
        #endregion
    }
}