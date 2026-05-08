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
using Data.card;
using Module.fight.Component;
using Module.fight.Core.Entities;
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
        [Header("行动点")] 
        public Dictionary<int, int> HeroActionPoints;
        
        [Header("手牌数据相关")]
        public List<CardEntity> HandCards;
        public List<CardEntity> DrawPile;
        public List<CardEntity> DiscardPile;
        public Dictionary<int, int> CardStarLevels;
    }
    
    //卡牌行动
    public class CardAction
    {
        public CardActionType ActionType;
        public CardSnapshot Snapshot;
        
        [Header("PlayCard相关数据")]
        public CardEntity cardEntity;
        public int OriginalIndex;//卡牌在手牌中的原始位置
        public int TargetInstanceId;
        
        [Header("MoveCard相关数据")]
        public int MoveFromIndex;
        public int MoveToIndex;
    }
    
    public class CardActionQueue
    {
        private readonly Stack<CardAction> _actionStack;
        public int MaxActionCount { get; private set; } = 4;

        public CardActionQueue()
        {
            _actionStack = new Stack<CardAction>();
            
            GameApp.MessageCenter.AddEvent(EventDefines.OnRemoveDiedCharacterCard, onReduceActionCount);
        }

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
        
        #region 事件函数
        private void onReduceActionCount(System.Object args)
        {
            MaxActionCount = Mathf.Max(0, MaxActionCount - 1);
        }

        #endregion
        
        #region 工具函数
        public bool CanPlayCard() => _actionStack.Count < MaxActionCount;
        
        public int Count => _actionStack.Count;
        
        public void Clear()
        {
            _actionStack.Clear();
            MaxActionCount = 4;
        }
        
        public int GetCurrentActionCount() => _actionStack.Count;
        
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