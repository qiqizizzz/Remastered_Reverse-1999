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
        private readonly int MaxActionCount = 4;

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
            
            //出牌队列满了,触发事件,进入战斗阶段
            if(_actionStack.Count == MaxActionCount)
                GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnOutput);
            
            return true;
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