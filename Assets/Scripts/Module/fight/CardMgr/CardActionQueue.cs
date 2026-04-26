/*
* ┌──────────────────────────────────────────────────────┐
* │  描    述: 卡牌队列(负责管理卡牌的出牌顺序/撤销等操作)                      
* │  类    名: CardActionQueue.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common.Defines;
using Module.fight.Component;
using UnityEngine;

namespace Module.fight.CardMgr
{
    //卡牌操作类型
    public enum CardActionType
    {
        PlayCard, // 出牌
        MoveCard  // 移动卡牌
    }

    //状态快照
    public class CardSnapshot
    {
        public List<BattleCardData> HandCards;
        public List<BattleCardData> DrawPile;
        public List<BattleCardData> DiscardPile;
        public Dictionary<BattleCardData, int> CardStarLevels;
    }
    
    public class CardAction
    {
        public CardActionType ActionType;
        public CardSnapshot Snapshot;
        
        [Header("PlayCard相关数据")]
        public BattleCardData BattleCardData;
        public int OriginalIndex;//卡牌在手牌中的原始位置
        public string TargetInstanceId;
        
        [Header("MoveCard相关数据")]
        public int MoveFromIndex;
        public int MoveToIndex;
    }
    
    public class CardActionQueue
    {
        private Stack<CardAction> _actionStack;
        public int MaxActionCount { get; set; } = 4;

        public CardActionQueue()
        {
            _actionStack = new Stack<CardAction>();
            
            GameApp.MessageCenter.AddEvent(EventDefines.OnRemoveDiedCharacterCard, onReduceActionCount);
        }

        #region 事件函数
        private void onReduceActionCount(System.Object args)
        {
            MaxActionCount = Mathf.Max(0, MaxActionCount - 1);
        }

        #endregion
        
        #region 主要函数
        public bool PushAction(CardAction action)
        {
            _actionStack.Push(action);
            return _actionStack.Count >= MaxActionCount;
        }
        
        public CardAction UndoLastAction()
        {
            if (_actionStack.Count == 0)
                return null;
            
            return _actionStack.Pop();
        }
        #endregion
        
        #region 工具函数
        public int GetCurrentActionCount() => _actionStack.Count;
        public bool CanPlayCard() => _actionStack.Count < MaxActionCount;
        
        public void Clear()
        {
            _actionStack.Clear();
            MaxActionCount = 4;
        }
        
        public List<CardAction> GetAllActionsAndClear()
        {
            List<CardAction> actions = new List<CardAction>(_actionStack);
            actions.Reverse();
            _actionStack.Clear();
            return actions;
        }

        public CardAction[] GetAction()
        {
            CardAction[] arr = _actionStack.ToArray();
            Array.Reverse(arr);
            return arr;
        }
        #endregion
    }
}