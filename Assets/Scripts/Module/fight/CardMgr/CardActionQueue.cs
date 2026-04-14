/*
* ┌──────────────────────────────────────────────────────┐
* │  描    述: 卡牌队列(负责管理卡牌的出牌顺序/撤销等操作)                      
* │  类    名: CardActionQueue.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.fight.Component;

namespace Module.fight.CardMgr
{
    public class CardAction
    {
        public BattleCardData CardData;
        public int OriginalIndex;//卡牌在手牌中的原始位置
    }
    
    public class CardActionQueue
    {
        private Stack<CardAction> _actionStack;
        public readonly int MaxActionCount = 4;

        public CardActionQueue()
        {
            _actionStack = new Stack<CardAction>();
        }

        public bool PlayCard(BattleCardData cardData, int originalIndex)
        {
            if (_actionStack.Count >= MaxActionCount)
                return false;
            
            var action = new CardAction()
            {
                CardData = cardData,
                OriginalIndex = originalIndex
            };
            
            _actionStack.Push(action);
            return true;
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