/*
* ┌──────────────────────────────────┐
* │  描    述: 手牌操作器，负责手牌区交互逻辑（拖拽、出牌、合成）
* │  类    名: HandCardOperator.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Data.card;
using Data.card.Extensions;
using Module.fight.Component;
using Module.fight.Core.Entities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Module.fight.CardMgr
{
    public class HandCardOperator
    {
        private readonly HandCardUIManager _handCardUIManager;
        private const int LOCAL_PLAYER_ID = 1;
        
        [Header("行动队列相关")]
        private readonly CardActionQueue _actionQueue;
        private readonly Stack<UI_BaseCardItem> _uiActionStack;
        
        [Header("UI相关")]
        private int _dragStartIndex = -1;
        private float _lastDragEndTime;
        
        [Header("快照")]
        private CardSnapshot _tempSnapshot;

        //事件
        public event Action OnQueueFull;
        public event Action OnRefreshMoveIndicators;
        public event Action OnRefreshActionPointUI;

        public HandCardOperator(HandCardUIManager handMgr, CardActionQueue actionQueue)
        {
            _handCardUIManager = handMgr;
            _actionQueue = actionQueue;
            _uiActionStack = new Stack<UI_BaseCardItem>();
        }
        
        public void Init()
        {
            _handCardUIManager.SetCardEventHandlers(
                onCardBeginDrag, onCardDrag, onCardEndDrag, onCardClick);
            
            GameApp.CardManager.EventBus.OnCardMerged += OnCardMergedFromCore;
        }

        public void Clear()
        {
            _uiActionStack.Clear();
            _dragStartIndex = -1;
            _tempSnapshot = null;
            
            GameApp.CardManager.EventBus.OnCardMerged -= OnCardMergedFromCore;
        }

        #region 卡牌事件
        private void OnCardMergedFromCore(int playerId, CardEntity kept, CardEntity destroyed, int newStarLevel)
        {
            // 根据 CardEntity 找到对应的 UI 项
            var uiKept = _handCardUIManager.FindUIByCardEntity(kept);
            var uiDestroyed = _handCardUIManager.FindUIByCardEntity(destroyed);

            if (uiKept == null || uiDestroyed == null) return;

            uiDestroyed.IsInCompositeAnim = true;
            _handCardUIManager.AnimateCompositeCard(uiKept, uiDestroyed, newStarLevel, () =>
            {
                uiDestroyed.IsInCompositeAnim = false;
                _handCardUIManager.RefreshHandCardLayout();
            });
        }
        #endregion
        
        #region 卡牌主要操作
        #region 出牌
        // 执行出牌
        private void playCard(UI_BaseCardItem item, int index)
        {
            if (!_actionQueue.CanPlayCard())
            {
                Debug.Log("已达到本轮出牌上限");
                return;
            }

            var snapshot = GameApp.CardManager.TakeSnapshot();
            var card = item.BattleCardData;

            // 1. 先处理UI：移出手牌区并播放入队动画
            // 必须放在 PlayCard 之前，否则 UpdateCardsUI 会回收这个 item
            _handCardUIManager.RemoveCardAt(index);
            _uiActionStack.Push(item);
            _handCardUIManager.AnimatePlayCard(item, _actionQueue.Count);

            // 2. 再调用纯C#层处理数据（内部触发事件时 item 已 IsInQueue=true，不会被回收）
            bool isUltimate = card.GetConfig().CardType == CardType.Ultimate;
            GameApp.CardManager.CombatSystem.PlayCard(
                LOCAL_PLAYER_ID, card, GameApp.CardManager.CurrentSelectedTargetId);

            // 3. 创建行动（用于后续结算和撤销）
            var action = new CardAction()
            {
                ActionType = CardActionType.PlayCard,
                Snapshot = snapshot,
                cardEntity = card,
                OriginalIndex = index,
                TargetInstanceId = GameApp.CardManager.CurrentSelectedTargetId
            };

            bool isQueueFull = _actionQueue.PushAction(action);
            OnRefreshMoveIndicators?.Invoke();
            OnRefreshActionPointUI?.Invoke();

            if (isQueueFull) OnQueueFull?.Invoke();
        }
        #endregion

        #region 交换卡牌
        // 交换两张手牌位置
        private void swapCard(int indexA, int indexB)
        {
            var handCards = GameApp.CardManager.GetHandCards();

            // 大招卡不参与交换
            if (handCards[indexA].GetConfig().CardType == CardType.Ultimate ||
                handCards[indexB].GetConfig().CardType == CardType.Ultimate)
                return;

            _handCardUIManager.SwapCards(indexA, indexB);

            // 数据层同步交换
            GameApp.CardManager.CombatSystem.SwapHandCards(LOCAL_PLAYER_ID, indexA, indexB);
        }
        #endregion

        #region 撤销卡牌
        //撤销上一次出牌操作
        public void UndoLastPlayCard()
        {
            if (_uiActionStack.Count <= 0) return;

            UI_BaseCardItem undoItem = _uiActionStack.Pop();
            _handCardUIManager.AnimateUndoCard(undoItem);
        }
        #endregion
        #endregion

        #region 卡牌拖拽、点击事件
        // 卡牌开始拖拽
        private void onCardBeginDrag(UI_BaseCardItem item, PointerEventData eventData)
        {
            if (item.BattleCardData.GetConfig().CardType == CardType.Ultimate || item.IsInQueue) return;

            _dragStartIndex = _handCardUIManager.GetCardIndex(item);
            if (_actionQueue.CanPlayCard())
                _tempSnapshot = GameApp.CardManager.TakeSnapshot();
        }

        // 卡牌拖拽中
        private void onCardDrag(UI_BaseCardItem item, PointerEventData eventData)
        {
            if (!_actionQueue.CanPlayCard()) return;

            int currentIndex = _handCardUIManager.GetCardIndex(item);
            if (currentIndex == -1) return;

            float currentX = item.Rect.anchoredPosition.x;

            // 向右拖动
            if (currentIndex < _handCardUIManager.Count - 1)
            {
                float rightX = _handCardUIManager.GetCardAt(currentIndex + 1).Rect.anchoredPosition.x;

                if (currentX > rightX - (item.AnimConfig.CardWidth / 2))
                {
                    swapCard(currentIndex, currentIndex + 1);
                    return;
                }
            }

            // 向左拖动
            if (currentIndex > 0)
            {
                float leftX = _handCardUIManager.GetCardAt(currentIndex - 1).Rect.anchoredPosition.x;
                if (currentX < leftX + (item.AnimConfig.CardWidth / 2))
                {
                    swapCard(currentIndex, currentIndex - 1);
                    return;
                }
            }
        }

        // 卡牌结束拖拽
        private void onCardEndDrag(UI_BaseCardItem item, PointerEventData eventData)
        {
            _lastDragEndTime = Time.time;
            int safeStartIndex = _dragStartIndex; 
            _dragStartIndex = -1;                 

            if (item.IsInQueue) 
            {
                _tempSnapshot = null;
                return;
            }

            int index = _handCardUIManager.GetCardIndex(item);
            item.MoveToIndex(index, _handCardUIManager.Count);

            if (safeStartIndex != -1 && safeStartIndex != index && _actionQueue.CanPlayCard())
            {
                var action = new CardAction()
                {
                    ActionType = CardActionType.MoveCard,
                    Snapshot = _tempSnapshot,
                    MoveFromIndex = safeStartIndex,
                    MoveToIndex = index
                };

                bool isQueueFull = _actionQueue.PushAction(action);
                OnRefreshMoveIndicators?.Invoke();

                // 移牌增加行动点
                GameApp.CardManager.CombatSystem.AddActionPoint(LOCAL_PLAYER_ID, item.GetOwnerId(), 1);
                OnRefreshActionPointUI?.Invoke();

                // 拖拽结束后检查合成
                GameApp.CardManager.CombatSystem.CheckAndAutoMerge(LOCAL_PLAYER_ID);

                if (isQueueFull) OnQueueFull?.Invoke();
            }
            else
            {
                _handCardUIManager.RefreshHandCardLayout();
            }

            _tempSnapshot = null;
        }

        // 卡牌点击
        private void onCardClick(UI_BaseCardItem item)
        {
            if (_dragStartIndex != -1) return;

            if (Time.time - _lastDragEndTime < 0.2f) return;
            
            int index = _handCardUIManager.GetCardIndex(item);
            if (index != -1)
                playCard(item, index);
        }
        #endregion
    }
}
