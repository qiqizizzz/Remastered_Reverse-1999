/*
* ┌───────────────────────────────────────────────────┐
* │  描    述: 卡牌管理器(只管卡牌与牌库的逻辑,不管卡牌队列)                      
* │  类    名: CardManager.cs       
* │  创    建: By qiqizizzz
* └───────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Common.Defines;
using Data.card;
using Data.level;
using Module.Character;
using Module.fight.Component;
using UnityEngine;

namespace Module.fight.CardMgr
{
    public class CardManager
    {
        public readonly CardActionQueue CardActionQueue;
        public string CurrentSelectedTargetId { get; set; }
        
        private readonly int singleCardMaxLimit = 3;//单张牌的最大限制数量
        private int m_maxHandCardCount = 8;
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
            m_maxHandCardCount = 8;
            CardActionQueue.Clear();
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

        #region 主要操作
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
                handCards.Insert(0, drawnCard);
            }
        }
        
        //弃牌
        public void DiscardCard(BattleCardData card)
        {
            if (card.BaseData.CardType == CardType.Ultimate)
                return;

            var owner = GameApp.EntityManager.GetCharacterById(card.BaseData.OwnerId);
            if(owner == null || owner.CurrentStateType == CharacterStateType.Die) return;
            
            card.StarLevel = 1;
            discardPile.Add(card);
        }
        
        //发放大招牌
        public bool TryGiveUltimateCard(int ownerId)
        {
            foreach (var card in handCards)
            {
                //该角色大招已经在手牌了
                if(card.BaseData.CardType == CardType.Ultimate && card.BaseData.OwnerId == ownerId)
                    return false;
            }

            //寻找大招
            foreach (var kv in m_cards)
            {
                if(kv.Key.Id != ownerId) continue;

                foreach (var cardData in kv.Value)
                {
                    if(cardData.CardType == CardType.Ultimate)
                    {
                        handCards.Insert(0, new BattleCardData(cardData));
                        return true;
                    }
                }
            }

            return false;
        }
        
        //移除死亡角色的卡牌
        public void RemoveDiedCharacterCard(CharacterData character)
        {
            m_maxHandCardCount = Mathf.Max(0, m_maxHandCardCount - 2);

            drawPile.RemoveAll(card => card.BaseData.OwnerId == character.Id);
            discardPile.RemoveAll(card => card.BaseData.OwnerId == character.Id);

            List<BattleCardData> removedHandCards = handCards.FindAll(card => card.BaseData.OwnerId == character.Id);

            //通知UI销毁手牌UI
            if (removedHandCards.Count > 0)
            {
                handCards.RemoveAll(card => card.BaseData.OwnerId == character.Id);
            }
            
            GameApp.MessageCenter.PostEvent(EventDefines.OnRemoveDiedCharacterCard, removedHandCards);
        }
        
        public void RemoveHandCard(BattleCardData card)
        {
            int index = handCards.FindIndex(x => ReferenceEquals(x, card));
            if (index != -1)
            {
                handCards[index].ClearData();
                handCards.RemoveAt(index);
            }
        }
        #endregion

        #region 快照与撤销机制
        //记录快照
        public CardSnapshot TakeSnapshot()
        {
            var snapshot = new CardSnapshot()
            {
                HeroActionPoints = new Dictionary<string, int>(),
                HandCards = new List<BattleCardData>(handCards),
                DrawPile = new List<BattleCardData>(drawPile),
                DiscardPile = new List<BattleCardData>(discardPile),
                CardStarLevels = new Dictionary<BattleCardData, int>()
            };

            //记录行动点
            foreach (var hero in GameApp.EntityManager.GetAliveHeroes()) 
                snapshot.HeroActionPoints[hero.InstanceID] = hero.ActionPoint;
            
            //记录星级
            foreach (var card in handCards) snapshot.CardStarLevels[card] = card.StarLevel;
            foreach (var card in drawPile) snapshot.CardStarLevels[card] = card.StarLevel;
            foreach (var card in discardPile) snapshot.CardStarLevels[card] = card.StarLevel;

            return snapshot;
        }
        
        //恢复快照
        public void RestoreSnapshot(CardSnapshot snapshot)
        {
            if(snapshot == null) return;
            
            handCards = new List<BattleCardData>(snapshot.HandCards);
            drawPile = new List<BattleCardData>(snapshot.DrawPile);
            discardPile = new List<BattleCardData>(snapshot.DiscardPile);

            //恢复行动点
            foreach (var hero in GameApp.EntityManager.GetAliveHeroes())
            {
                hero.SetActionPoint(snapshot.HeroActionPoints[hero.InstanceID]);
            }
            
            //恢复星级
            foreach (var kvp in snapshot.CardStarLevels)
            {
                kvp.Key.StarLevel = kvp.Value;
            }
        }

        #endregion

        #region 工具函数
        public int GetNormalHandCardCount()
        {
            int count = 0;
            foreach (var card in handCards)
            {
                if(card.BaseData.CardType != CardType.Ultimate)
                    count++;
            }
            return count;
        }
        
        public List<BattleCardData> GetHandCards()
        {
            return handCards;
        }

        public int mMaxHandCardCount
        {
            get { return m_maxHandCardCount; }
        }
        #endregion
    }
}