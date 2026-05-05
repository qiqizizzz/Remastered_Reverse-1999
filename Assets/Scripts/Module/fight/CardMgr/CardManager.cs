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
using Data.card.Extensions;
using Data.level;
using Module.Character;
using Module.fight.Core.Entities;
using Module.fight.Core.EventBus;
using Module.fight.Core.Systems;
using UnityEngine;

namespace Module.fight.CardMgr
{
    public class CardManager
    {
        public readonly CardActionQueue CardActionQueue;
        public string CurrentSelectedTargetId { get; set; }

        [Header("数据基座与计算系统")] 
        public readonly CombatContext BattleContext;
        public readonly CardCombatSystem CombatSystem;
        public readonly CombatEventBus  EventBus;
        
        private const int LOCAL_PLAYER_ID = 1;
        private int m_maxHandCardCount = 8;
        private List<int> _currentCharacterConfigIds;

        public CardManager()
        {
            CardActionQueue = new CardActionQueue();
            EventBus = new CombatEventBus();
            
            BattleContext = new CombatContext();
            CombatSystem = new CardCombatSystem(BattleContext, GameApp.ConfigManager.Card, EventBus);
            _currentCharacterConfigIds = new List<int>();
            
            InitEventBus();
        }
        
        public void InitCards(LevelModel model)
        {
            m_maxHandCardCount = 8;
            CardActionQueue.Clear();
            _currentCharacterConfigIds.Clear();

            foreach (var character in model.Characters)
            {
                if(character != null) _currentCharacterConfigIds.Add(character.Id);
            }
            
            CombatSystem.InitDeck(LOCAL_PLAYER_ID, _currentCharacterConfigIds);
        }

        private void InitEventBus()
        {
            // 桥接手牌变化 → 旧UI更新
            EventBus.OnHandCardsUpdated += (playerId, handCards) =>
            {
                GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, handCards);
            };

            // 桥接合成事件
            EventBus.OnCardMerged += (playerId, kept, destroyed, newStar) =>
            {
                GameApp.MessageCenter.PostEvent(EventDefines.OnHandCardMerged, new object[] { kept, destroyed });
            };

            // 桥接行动点变化 → 同步 HeroEntity 并刷新 HUD
            EventBus.OnActionPointChanged += (playerId, ownerId, current) =>
            {
                var hero = GameApp.EntityManager.GetCharacterById(ownerId) as HeroEntity;
                if (hero == null) return;
                
                hero.SetActionPoint(current);
                hero.HUD?.UpdateActionPoint(current);
            };
        }
        
        #region 主要操作
        public void PrepareHandsForNewLevel()
        {
            CombatSystem.PrepareHandsForNewLevel(LOCAL_PLAYER_ID, _currentCharacterConfigIds);
        }

        public void DrawCard(int count)
        {
            CombatSystem.DrawCard(LOCAL_PLAYER_ID, count);
        }
        
        public void DiscardCard(CardEntity card)
        {
            CombatSystem.DiscardCard(LOCAL_PLAYER_ID, card);
        }
        
        public bool TryGiveUltimateCard(int ownerId)
        {
            return CombatSystem.TryGiveUltimateCard(LOCAL_PLAYER_ID, ownerId);
        }
        
        public void RemoveDiedCharacterCard(CharacterDataSO character)
        {
            m_maxHandCardCount = Mathf.Max(0, m_maxHandCardCount - 2);
            CombatSystem.RemoveCardsOfCharacter(LOCAL_PLAYER_ID, character.Id);
        }
        
        // 回合初始化时的手牌修正：自动合成 → 补牌 → 发大招
        public void ProcessRoundStartHandFix()
        {
            // 连环合成
            while (CombatSystem.CheckAndAutoMerge(LOCAL_PLAYER_ID)) { }
            
            // 补牌
            int normalCount = GetNormalHandCardCount();
            if (normalCount < mMaxHandCardCount)
            {
                int needCount = mMaxHandCardCount - normalCount;
                DrawCard(needCount);
                // 补牌后可能又触发合成
                while (CombatSystem.CheckAndAutoMerge(LOCAL_PLAYER_ID)) { }
            }
            
            // 发大招
            foreach (var hero in GameApp.EntityManager.GetAliveHeroes())
            {
                if (hero.ActionPoint >= HeroEntity.MaxActionPoint)
                {
                    TryGiveUltimateCard(hero.CharacterData.Id);
                }
            }
        }
        #endregion

        #region 快照与撤销机制
        //记录快照
        public CardSnapshot TakeSnapshot()
        {
            var deck = BattleContext.PlayerDecks[LOCAL_PLAYER_ID];
            var snapshot = new CardSnapshot()
            {
                HeroActionPoints = new Dictionary<string, int>(),
                HandCards = new List<CardEntity>(deck.HandCards),
                DrawPile = new List<CardEntity>(deck.DrawPile),
                DiscardPile = new List<CardEntity>(deck.DiscardPile),
                CardStarLevels = new Dictionary<int, int>()
            };
            
            //记录星级
            foreach (var card in deck.HandCards) snapshot.CardStarLevels[card.InstanceId] = card.StarLevel;
            foreach (var card in deck.DrawPile) snapshot.CardStarLevels[card.InstanceId] = card.StarLevel;
            foreach (var card in deck.DiscardPile) snapshot.CardStarLevels[card.InstanceId] = card.StarLevel;
            
            // 记录行动点
            snapshot.HeroActionPoints = new Dictionary<string, int>();
            foreach (var kvp in BattleContext.Entities)
            {
                snapshot.HeroActionPoints[kvp.Key] = kvp.Value.ActionPoint;
            }

            return snapshot;
        }
        
        //恢复快照
        public void RestoreSnapshot(CardSnapshot snapshot)
        {
            if(snapshot == null) return;
            var deck = BattleContext.PlayerDecks[LOCAL_PLAYER_ID];
            
            deck.HandCards = new List<CardEntity>(snapshot.HandCards);
            deck.DrawPile = new List<CardEntity>(snapshot.DrawPile);
            deck.DiscardPile = new List<CardEntity>(snapshot.DiscardPile);

            foreach (var kvp in snapshot.CardStarLevels)
            {
                var card = FindCardByInstanceId(kvp.Key);
                if (card != null) card.StarLevel = kvp.Value;
            }

            foreach (var kvp in snapshot.HeroActionPoints)
            {
                if (BattleContext.Entities.TryGetValue(kvp.Key, out var entity))
                    entity.ActionPoint = kvp.Value;
            }

            // 统一广播一次手牌更新
            EventBus.OnHandCardsUpdated?.Invoke(LOCAL_PLAYER_ID, new List<CardEntity>(deck.HandCards));
        }

        private CardEntity FindCardByInstanceId(int instanceId)
        {
            var deck = BattleContext.PlayerDecks[LOCAL_PLAYER_ID];
            var all = new List<CardEntity>();
            all.AddRange(deck.HandCards);
            all.AddRange(deck.DrawPile);
            all.AddRange(deck.DiscardPile);
            return all.Find(c => c.InstanceId == instanceId);
        }
        #endregion

        #region 工具函数
        public int GetNormalHandCardCount()
        {
            var deck = BattleContext.PlayerDecks[LOCAL_PLAYER_ID];
            int count = 0;
            foreach (var card in deck.HandCards)
            {
                if(card.GetConfig().CardType != CardType.Ultimate)
                    count++;
            }
            return count;
        }
        
        public List<CardEntity> GetHandCards()
        {
            return BattleContext.PlayerDecks[LOCAL_PLAYER_ID].HandCards;
        }

        public int mMaxHandCardCount => m_maxHandCardCount;
        #endregion
    }
}