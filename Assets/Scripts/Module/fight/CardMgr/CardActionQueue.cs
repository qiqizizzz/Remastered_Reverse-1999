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
        private int MaxActionCount = 4;

        public CardActionQueue()
        {
            _actionStack = new Stack<CardAction>();
            
            //TODO:加了这个之后就有bug了,卡牌发牌不准确了（就算死了一个英雄也会发8张牌？？但是出牌数量是3，这是为什么
            GameApp.MessageCenter.AddEvent(EventDefines.OnRemoveDiedCharacterCard, onReduceActionCount);
        }

        #region 事件函数
        private void onReduceActionCount(System.Object args)
        {
            MaxActionCount--;
        }

        #endregion
        public bool CanPlayCard() => _actionStack.Count < MaxActionCount;
        
        public bool PlayCard(BattleCardData cardData, int originalIndex, string targetInstanceId)
        {
            var action = new CardAction()
            {
                BattleCardData = cardData,
                OriginalIndex = originalIndex,
                TargetInstanceId = targetInstanceId
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
            MaxActionCount = 4;
        }
        
        public int GetCurrentActionCount() => _actionStack.Count;
    }
}