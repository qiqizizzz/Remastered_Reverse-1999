/*
* ┌──────────────────────────────────────────────────────┐
* │  描    述: 卡牌队列(负责管理卡牌的出牌顺序/撤销等操作)                      
* │  类    名: CardActionQueue.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Common.Defines;
using Module.fight.Component;
using UnityEngine;

namespace Module.fight.CardMgr
{
    public class CardAction
    {
        public BattleCardData BattleCardData;
        public int OriginalIndex;//卡牌在手牌中的原始位置
        public string TargetInstanceId;
    }
    
    public class CardActionQueue
    {
        private Stack<CardAction> _actionStack;
        private readonly int MaxActionCount = 4;

        public CardActionQueue()
        {
            _actionStack = new Stack<CardAction>();
        }

        public bool CanPlayCard() => _actionStack.Count < MaxActionCount;
        
        public bool PlayCard(BattleCardData cardData, int originalIndex, string targetInstanceId)
        {
            var action = new CardAction()
            {
                BattleCardData = cardData,
                OriginalIndex = originalIndex
            };
            
            _actionStack.Push(action);
            
            return _actionStack.Count == MaxActionCount;//返回 true 表示队列已满,可以进入战斗结算了
        }

        public List<CardAction> GetAllActionsAndClear()
        {
            List<CardAction> actions = new List<CardAction>(_actionStack);
            actions.Reverse();
            _actionStack.Clear();
            return actions;
        }
        
        public CardAction UndoLastAction()
        {
            if (_actionStack.Count == 0)
                return null;
            
            return _actionStack.Pop();
        }
        
        public void Clear()
        {
            _actionStack.Clear();
        }
        
        public int GetCurrentActionCount() => _actionStack.Count;
    }
}