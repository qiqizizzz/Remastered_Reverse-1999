/*
* ┌──────────────────────────────────┐
* │  描    述: 手牌操作器，负责手牌区交互逻辑（拖拽、出牌、合成）
* │  类    名: HandCardOperator.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common.Defines;
using Data.card;
using Data.card.Extensions;
using Module.fight.Component;
using Module.fight.Core.Entities;
using Module.fight.Network;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Module.fight.CardMgr
{
    public class HandCardOperator
    {
        private readonly HandCardUIManager _handCardUIManager;
        private readonly CardActionQueue  _actionQueue;
        private readonly BattleNetworkController _battleNetwork;
        private const int LOCAL_PLAYER_ID = 1;
        
        private readonly Stack<UI_BaseCardItem> _uiActionStack;
        
        [Header("UI相关")]
        private int _dragStartIndex = -1;
        private float _lastDragEndTime;
        
        [Header("快照")]
        private CardSnapshot _tempSnapshot;
        
        private bool _pendingUndo;
        
        //事件
        public event Action OnQueueFull;
        public event Action OnRefreshMoveIndicators;
        public event Action OnRefreshActionPointUI;
        
        public bool IsPendingUndo => _pendingUndo;
        public void ClearPendingUndo() => _pendingUndo = false;

        public HandCardOperator(HandCardUIManager handMgr, CardActionQueue actionQueue)
        {
            _handCardUIManager = handMgr;
            _actionQueue = actionQueue;
            _battleNetwork = new BattleNetworkController();
            _uiActionStack = new Stack<UI_BaseCardItem>();
        }
        
        public void Init()
        {
            _handCardUIManager.SetCardEventHandlers(
                onCardBeginDrag, onCardDrag, onCardEndDrag, onCardClick);
            
            GameApp.CardManager.EventBus.OnCardMerged -= OnCardMergedFromCore;
            GameApp.CardManager.EventBus.OnCardMerged += OnCardMergedFromCore;
        }

        public void Clear()
        {
            _uiActionStack.Clear();
            _dragStartIndex = -1;
            _tempSnapshot = null;
            _pendingUndo = false;
            
            GameApp.CardManager.EventBus.OnCardMerged -= OnCardMergedFromCore;
        }

        // 清空出牌队列栈（用于回合切换时重置状态，不回收UI对象）
        public void ClearActionStack()
        {
            _uiActionStack.Clear();
        }

        #region 卡牌事件
        private void OnCardMergedFromCore(int playerId, CardEntity kept, CardEntity destroyed, int newStarLevel)
        {
            var uiKept = _handCardUIManager.FindUIByCardEntity(kept);
            var uiDestroyed = _handCardUIManager.FindUIByCardEntity(destroyed);

            if (uiKept == null || uiDestroyed == null) return;

            uiDestroyed.IsInCompositeAnim = true;
            _handCardUIManager.AnimateCompositeCard(uiKept, uiDestroyed, newStarLevel, () =>
            {
                uiDestroyed.IsInCompositeAnim = false;
                var handCards = GameApp.CardManager.GetHandCards();
                GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, handCards);
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
            
            var card = item.BattleCardData;
            CardSnapshot beforeSnapshot = GameApp.CardManager.TakeSnapshot();
            int dataIndex = beforeSnapshot.HandCards.FindIndex(c => c.InstanceId == card.InstanceId);
            int originalIndex = dataIndex >= 0 ? dataIndex : index;

            _handCardUIManager.RemoveCardAt(index);
            _uiActionStack.Push(item);

            var action = new CardAction
            {
                ActionType = CardActionType.PlayCard,
                cardEntity = card,
                OriginalIndex = originalIndex,
                TargetInstanceId = GameApp.CardManager.CurrentSelectedTargetId,
                Snapshot = beforeSnapshot
            };
            _actionQueue.PushAction(action);
            
            _handCardUIManager.AnimatePlayCard(item, _actionQueue.GetCurrentActionCount() - 1);

            // 发送网络请求，由服务端驱动实际逻辑
            _battleNetwork.SendPlayCard(card.InstanceId, GameApp.CardManager.CurrentSelectedTargetId, originalIndex);
            
            bool isQueueFull = !_actionQueue.CanPlayCard();

            OnRefreshMoveIndicators?.Invoke();
            OnRefreshActionPointUI?.Invoke();

            if (isQueueFull)
            {
                OnQueueFull?.Invoke();
                _battleNetwork.SendEndTurn();
            }
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
            _handCardUIManager.RefreshHandCardLayout();

            // 服务端事件驱动数据更新，此处仅更新UI
        }
        #endregion

        #region 撤销卡牌
        //撤销上一次操作（出牌或移动）
        public void UndoLastPlayCard()
        {
            if (_pendingUndo) return;

            var undoneAction = _actionQueue.UndoLastAction();
            if (undoneAction == null) return;

            if (undoneAction.ActionType == CardActionType.PlayCard)
            {
                if (_uiActionStack.Count > 0)
                {
                    UI_BaseCardItem undoItem = _uiActionStack.Pop();
                    int restoreIndex = undoneAction.OriginalIndex;
                    int snapshotIndex = -1;
                    if (undoneAction.Snapshot?.HandCards != null)
                    {
                        snapshotIndex = undoneAction.Snapshot.HandCards.FindIndex(
                            c => c.InstanceId == undoItem.BattleCardData.InstanceId);
                        if (snapshotIndex >= 0)
                            restoreIndex = snapshotIndex;
                    }
#if UNITY_EDITOR
                    Debug.Log($"[{nameof(HandCardOperator)}] 撤销出牌 InstanceId={undoItem.BattleCardData.InstanceId}, OriginalIndex={undoneAction.OriginalIndex}, SnapshotIndex={snapshotIndex}, RestoreIndex={restoreIndex}");
#endif
                    _handCardUIManager.AnimateUndoCard(undoItem, restoreIndex);
                }
            }
            else if (undoneAction.ActionType == CardActionType.MoveCard)
            {
                _handCardUIManager.RefreshHandCardLayout();
            }
            
            _pendingUndo = true;
            // 发送撤销请求，由服务端驱动数据回滚
            _battleNetwork.SendUndo();
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

            float halfWidth = item.Rect.rect.width / 2f;

            // 向右拖动
            if (currentIndex < _handCardUIManager.Count - 1)
            {
                float rightX = _handCardUIManager.GetCardAt(currentIndex + 1).Rect.anchoredPosition.x;

                if (currentX > rightX - halfWidth)
                {
                    swapCard(currentIndex, currentIndex + 1);
                    return;
                }
            }

            // 向左拖动
            if (currentIndex > 0)
            {
                float leftX = _handCardUIManager.GetCardAt(currentIndex - 1).Rect.anchoredPosition.x;
                if (currentX < leftX + halfWidth)
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
                var moveAction = new CardAction
                {
                    ActionType = CardActionType.MoveCard,
                    MoveFromIndex = safeStartIndex,
                    MoveToIndex = index,
                    Snapshot = _tempSnapshot
                };
                _actionQueue.PushAction(moveAction);
                
                // 发送移动卡牌请求，由服务端驱动数据更新
                _battleNetwork.SendMoveCard(safeStartIndex, index);

                OnRefreshMoveIndicators?.Invoke();
                OnRefreshActionPointUI?.Invoke();
                
                if (!_actionQueue.CanPlayCard())
                {
                    OnQueueFull?.Invoke();
                    _battleNetwork.SendEndTurn();
                }
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
