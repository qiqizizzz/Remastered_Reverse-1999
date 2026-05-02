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
using Module.fight.Component;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Module.fight.CardMgr
{
    public class HandCardOperator
    {
        private readonly HandCardUIManager _handCardUIManager;
        
        [Header("行动队列相关")]
        private readonly CardActionQueue _actionQueue;
        private readonly Stack<UI_BaseCardItem> _uiActionStack;
        
        [Header("UI相关")]
        private int _dragStartIndex = -1;
        
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
        }

        public void Clear()
        {
            _uiActionStack.Clear();
            _dragStartIndex = -1;
            _tempSnapshot = null;
        }
        
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

            //获取快照,修改数据 
            CardSnapshot snapshot = GameApp.CardManager.TakeSnapshot();
            _handCardUIManager.RemoveCardAt(index);
            GameApp.CardManager.GetHandCards().Remove(item.BattleCardData);
            _uiActionStack.Push(item);
            
            //刷新UI
            _handCardUIManager.RefreshHandCardLayout();
            _handCardUIManager.AnimatePlayCard(item, index);

            //检查合成
            CheckAndTriggerComposite();

            //创建行动
            CardAction action = new CardAction()
            {
                ActionType = CardActionType.PlayCard,
                Snapshot = snapshot,
                BattleCardData = item.BattleCardData,
                OriginalIndex = index,
                TargetInstanceId = GameApp.CardManager.CurrentSelectedTargetId
            };

            bool isQueueFull = _actionQueue.PushAction(action);
            OnRefreshMoveIndicators?.Invoke();

            //行动点处理
            bool isUltimate = item.IsUltimateCard();
            if (isUltimate)
            {
                // 打出大招后，清空该玩家的行动点
                GameApp.CardManager.ClearActionPointOfOwner(item.GetOwnerId());
            }
            else
            {
                // 打出普通卡牌，增加行动点
                GameApp.CardManager.AddActionPointToOwner(item.GetOwnerId());
            }
            
            OnRefreshActionPointUI?.Invoke();

            if (isQueueFull)
            {
                OnQueueFull?.Invoke();
            }
        }
        #endregion

        #region 交换卡牌
        // 交换两张手牌位置
        private void swapCard(int indexA, int indexB)
        {
            var handCards = GameApp.CardManager.GetHandCards();

            // 大招卡不参与交换
            if (handCards[indexA].BaseData.CardType == CardType.Ultimate ||
                handCards[indexB].BaseData.CardType == CardType.Ultimate)
                return;

            _handCardUIManager.SwapCards(indexA, indexB);

            // 数据层同步交换
            (handCards[indexA], handCards[indexB]) = (handCards[indexB], handCards[indexA]);
            _handCardUIManager.RefreshHandCardLayout();
        }
        #endregion

        #region 合成卡牌
        // 合成两张相邻手牌
        private void compositeCard(int indexA, int indexB, Action onCompleteAllMerges = null)
        {
            UI_BaseCardItem cardA = _handCardUIManager.GetCardAt(indexA);
            UI_BaseCardItem cardB = _handCardUIManager.GetCardAt(indexB);
            
            // 保留cardA并升星，cardB销毁
            cardA.BattleCardData.StarLevel += 1;
            _handCardUIManager.RemoveCard(cardB);
            GameApp.CardManager.RemoveHandCard(cardB.BattleCardData);
            
            //合成动画
            _handCardUIManager.AnimateCompositeCard(cardA, cardB, cardA.BattleCardData.StarLevel, () =>
            {
                _handCardUIManager.RefreshHandCardLayout();
                GameApp.CardManager.AddActionPointToOwner(cardA.GetOwnerId());
                OnRefreshActionPointUI?.Invoke();

                CheckAndTriggerComposite(onCompleteAllMerges);
            });
        }
        //检查并触发相邻相同卡牌的合成
        public void CheckAndTriggerComposite(Action onCompleteAllMerges = null)
        {
            for (int i = 0; i < _handCardUIManager.Count - 1; i++)
            {
                var cardA = _handCardUIManager.GetCardAt(i);
                var cardB = _handCardUIManager.GetCardAt(i + 1);

                if (cardA == null || cardB == null) continue;

                if (cardA.BattleCardData.Equals(cardB.BattleCardData))
                {
                    compositeCard(i, i + 1, onCompleteAllMerges);
                    return;
                }
            }

            onCompleteAllMerges?.Invoke();
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
            if (item.BattleCardData.BaseData.CardType == CardType.Ultimate || item.IsInQueue) return;

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
            if (item.IsInQueue) return;

            int index = _handCardUIManager.GetCardIndex(item);
            item.MoveToIndex(index, _handCardUIManager.Count);

            // 如果位置确实改变了 && 有行动点
            if (_dragStartIndex != -1 && _dragStartIndex != index && _actionQueue.CanPlayCard())
            {
                CardAction action = new CardAction()
                {
                    ActionType = CardActionType.MoveCard,
                    Snapshot = _tempSnapshot,
                    MoveFromIndex = _dragStartIndex,
                    MoveToIndex = index
                };

                bool isQueueFull = _actionQueue.PushAction(action);

                OnRefreshMoveIndicators?.Invoke();
                
                //只有位置真实改变了才增加行动点
                GameApp.CardManager.AddActionPointToOwner(item.GetOwnerId());
                OnRefreshActionPointUI?.Invoke();
                
                CheckAndTriggerComposite();

                if (isQueueFull)
                {
                    OnQueueFull?.Invoke();
                }
            }
            else
            {
                // 位置没变或者没有行动点了，只进行基础检查
                _handCardUIManager.RefreshHandCardLayout();
            }

            
            _dragStartIndex = -1;
            _tempSnapshot = null;
            
        }

        // 卡牌点击
        private void onCardClick(UI_BaseCardItem item)
        {
            int index = _handCardUIManager.GetCardIndex(item);
            if (index != -1)
                playCard(item, index);
        }
        #endregion
    }
}
