/*
* ┌───────────────────────────────────────────────────┐
* │  描    述: 卡牌管理器(只管卡牌与牌库的逻辑,不管卡牌队列)                      
* │  类    名: CardManager.cs       
* │  创    建: By qiqizizzz
* └───────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Data.level;
using Module.fight.Component;
using UnityEngine;

namespace Module.fight.CardMgr
{
    public class CardManager
    {
        [Header("牌堆")]
        private List<BattleCard> drawPile; //抽牌堆
        private List<BattleCard> handCards; //手牌
        private List<BattleCard> discardPile; //弃牌堆

        public CardManager()
        {
            drawPile = new List<BattleCard>();
            handCards = new List<BattleCard>();
            discardPile = new List<BattleCard>();
        }
        
        
        public void InitCards(LevelInitData initData)
        {
            
        }
    }
}