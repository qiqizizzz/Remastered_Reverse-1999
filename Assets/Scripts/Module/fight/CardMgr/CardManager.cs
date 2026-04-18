/*
* ┌───────────────────────────────────────────────────┐
* │  描    述: 卡牌管理器(只管卡牌与牌库的逻辑,不管卡牌队列)                      
* │  类    名: CardManager.cs       
* │  创    建: By qiqizizzz
* └───────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Data.card;
using Data.level;
using Module.fight.Component;
using UnityEngine;

namespace Module.fight.CardMgr
{
    public class CardManager
    {
        public readonly CardActionQueue CardActionQueue;
        public string CurrentSelectedTargetId { get; set; }
        
        private readonly int singleCardMaxLimit = 3;//单张牌的最大限制数量
        private readonly Dictionary<CharacterData, List<CardData>> m_cards;
        
        [Header("牌堆")]
        private List<BattleCardData> drawPile; //抽牌堆
        private List<BattleCardData> handCards; //手牌
        private List<BattleCardData> discardPile; //弃牌堆

        public CardManager()
        {
            CardActionQueue = new CardActionQueue();
            
            m_cards = new Dictionary<CharacterData, List<CardData>>();
            drawPile = new List<BattleCardData>();
            handCards = new List<BattleCardData>();
            discardPile = new List<BattleCardData>();
        }
        
        public void InitCards(LevelInitData initData)
        {
            m_cards.Clear();
            drawPile.Clear();
            handCards.Clear();
            discardPile.Clear();

            foreach (var character in initData.Characters)
            {
                if(character == null) continue;
                
                List<CardData> characterCards = character.GetAllCards();
                m_cards.Add(character, characterCards);

                foreach (var card in characterCards)
                {
                    if(card.CardType == CardType.Ultimate) continue;
                    
                    for (int i = 0; i < singleCardMaxLimit; i++)
                    {
                        drawPile.Add(new BattleCardData(card));
                    }
                }
            }
            
            ShuffleCard(drawPile);
            
            //TODO:敌人的牌堆暂时不做
        }

        //新关卡开始时准备手牌
        public void PrepareHandsForNewLevel()
        {
            //回合开始时（即新进入关卡时）,玩家当前手牌应为每个角色两张普通牌,加起来一共8张（4个角色）

            foreach (var kv in m_cards)
            {
                List<CardData> cards = kv.Value;

                foreach (var card in cards)
                {
                    if(card.CardType == CardType.Ultimate) continue;
                    
                    handCards.Add(new BattleCardData(card));
                }
            }
            
            ShuffleCard(handCards);
        }
        
        //Fisher–Yates 洗牌算法
        public void ShuffleCard(List<BattleCardData> pile)
        {
            for (int i = 0; i < pile.Count; i++)
            {
                int RandomIndex = Random.Range(i, pile.Count);
                (pile[i], pile[RandomIndex]) = (pile[RandomIndex], pile[i]);
            }
        }

        //抽牌
        public void DrawCard(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0)
                    {
                        Debug.LogWarning("抽牌失败: 牌堆和弃牌堆都没有牌了");
                        break;
                    }
                    
                    drawPile.AddRange(discardPile);
                    discardPile.Clear();
                    ShuffleCard(drawPile);
                }
                
                BattleCardData drawnCard = drawPile[^1];
                drawPile.RemoveAt(drawPile.Count - 1);
                handCards.Add(drawnCard);
            }
        }
        
        //弃牌
        public void DiscardCard(BattleCardData card)
        {
            if (card.BaseData.CardType == CardType.Ultimate)
                return;

            card.StarLevel = 1;
            discardPile.Add(card);
        }

        #region 工具函数
        public List<BattleCardData> GetHandCards()
        {
            return handCards;
        }
        #endregion
    }
}