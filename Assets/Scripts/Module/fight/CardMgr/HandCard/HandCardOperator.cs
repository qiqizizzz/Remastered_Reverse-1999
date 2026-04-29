/*
* ┌──────────────────────────────────┐
* │  描    述: 手牌操作器，负责手牌区交互逻辑（拖拽、出牌、合成）
* │  类    名: HandCardOperator.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common;
using Common.Defines;
using Data.card;
using DG.Tweening;
using Module.fight.Component;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Module.fight.CardMgr
{
    public class HandCardOperator
    {
        private HandCardUIManager m_handCardUIManager;
        private CardActionQueue m_actionQueue;
        private Transform m_cardActionTf;
        private Transform m_cardDeckTf;
        private float m_cardActionWidth;

        private Stack<UI_BaseCardItem> m_uiActionStack;
        private int m_dragStartIndex = -1;
        private CardSnapshot m_tempSnapshot;

        public event Action OnQueueFull;
        public event Action OnRefreshMoveIndicators;
        public event Action OnRefreshActionPointUI;

        public HandCardOperator(HandCardUIManager handMgr, CardActionQueue actionQueue,
            Transform cardActionTf, Transform cardDeckTf, float cardActionWidth)
        {
            m_handCardUIManager = handMgr;
            m_actionQueue = actionQueue;
            m_cardActionTf = cardActionTf;
            m_cardDeckTf = cardDeckTf;
            m_cardActionWidth = cardActionWidth;
            m_uiActionStack = new Stack<UI_BaseCardItem>();
        }
        
        public void Init()
        {
            m_handCardUIManager.SetCardEventHandlers(
                onCardBeginDrag, onCardDrag, onCardEndDrag, onCardClick);
        }

        public void Clear()
        {
            m_uiActionStack.Clear();
            m_dragStartIndex = -1;
            m_tempSnapshot = null;
        }

        /// <summary>
        /// 撤销上一次出牌操作，恢复队列中的卡牌到牌堆
        /// </summary>
        public void UndoLastPlayCard()
        {
            if (m_uiActionStack.Count <= 0) return;

            UI_BaseCardItem undoItem = m_uiActionStack.Pop();
            undoItem.Rect.DOKill();
            undoItem.IsInQueue = false;
            undoItem.SetBlockRaycasts(true);
            undoItem.transform.SetParent(m_cardDeckTf, true);
            m_handCardUIManager.InsertCard(0, undoItem);
        }

        /// <summary>
        /// 检查并触发相邻相同卡牌的合成
        /// </summary>
        /// <param name="onCompleteAllMerges">所有合成链完成后的回调</param>
        public void CheckAndTriggerComposite(Action onCompleteAllMerges = null)
        {
            for (int i = 0; i < m_handCardUIManager.Count - 1; i++)
            {
                var cardA = m_handCardUIManager.GetCardAt(i);
                var cardB = m_handCardUIManager.GetCardAt(i + 1);

                if (cardA == null || cardB == null) continue;

                if (cardA.BattleCardData.Equals(cardB.BattleCardData))
                {
                    compositeCard(i, i + 1, onCompleteAllMerges);
                    return;
                }
            }

            onCompleteAllMerges?.Invoke();
        }

        #region 卡牌拖拽、点击事件
        // 卡牌开始拖拽
        private void onCardBeginDrag(UI_BaseCardItem item, PointerEventData eventData)
        {
            if (item.BattleCardData.BaseData.CardType == CardType.Ultimate || item.IsInQueue) return;

            m_dragStartIndex = m_handCardUIManager.GetCardIndex(item);
            if (m_actionQueue.CanPlayCard())
                m_tempSnapshot = GameApp.CardManager.TakeSnapshot();
        }

        // 卡牌拖拽中
        private void onCardDrag(UI_BaseCardItem item, PointerEventData eventData)
        {
            if (!m_actionQueue.CanPlayCard()) return;

            int currentIndex = m_handCardUIManager.GetCardIndex(item);
            if (currentIndex == -1) return;

            float currentX = item.Rect.anchoredPosition.x;

            // 向右拖动
            if (currentIndex < m_handCardUIManager.Count - 1)
            {
                float rightX = m_handCardUIManager.GetCardAt(currentIndex + 1).Rect.anchoredPosition.x;

                if (currentX > rightX - (item.AnimConfig.CardWidth / 2))
                {
                    swapCard(currentIndex, currentIndex + 1);
                    return;
                }
            }

            // 向左拖动
            if (currentIndex > 0)
            {
                float leftX = m_handCardUIManager.GetCardAt(currentIndex - 1).Rect.anchoredPosition.x;
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

            int index = m_handCardUIManager.GetCardIndex(item);
            item.MoveToIndex(index, m_handCardUIManager.Count);

            // 如果位置确实改变了 && 有行动点
            if (m_dragStartIndex != -1 && m_dragStartIndex != index && m_actionQueue.CanPlayCard())
            {
                CardAction action = new CardAction()
                {
                    ActionType = CardActionType.MoveCard,
                    Snapshot = m_tempSnapshot,
                    MoveFromIndex = m_dragStartIndex,
                    MoveToIndex = index
                };

                bool isQueueFull = m_actionQueue.PushAction(action);

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
                m_handCardUIManager.RefreshHandCardLayout();
            }

            
            m_dragStartIndex = -1;
            m_tempSnapshot = null;
            
        }

        // 卡牌点击
        private void onCardClick(UI_BaseCardItem item)
        {
            int index = m_handCardUIManager.GetCardIndex(item);
            if (index != -1)
                playCard(item, index);
        }
        #endregion

        #region 卡牌主要操作
        // 执行出牌
        private void playCard(UI_BaseCardItem item, int index)
        {
            bool isUltimate = item.IsUltimateCard();

            if (!m_actionQueue.CanPlayCard())
            {
                Debug.Log("已达到本轮出牌上限");
                return;
            }

            CardSnapshot snapshot = GameApp.CardManager.TakeSnapshot();

            m_handCardUIManager.RemoveCardAt(index);
            GameApp.CardManager.GetHandCards().Remove(item.BattleCardData);

            m_uiActionStack.Push(item);
            m_handCardUIManager.RefreshHandCardLayout();

            item.transform.SetParent(m_cardActionTf, true);

            Vector2 targetPos = new Vector2(-m_cardActionWidth, 0) +
                                new Vector2(
                                    (m_actionQueue.GetCurrentActionCount()) * (item.AnimConfig.CardWidth * 0.8f + 12f), 0);

            item.PlayToQueueAnim(targetPos);
            item.IsInQueue = true;
            item.SetBlockRaycasts(false);

            CheckAndTriggerComposite();

            CardAction action = new CardAction()
            {
                ActionType = CardActionType.PlayCard,
                Snapshot = snapshot,
                BattleCardData = item.BattleCardData,
                OriginalIndex = index,
                TargetInstanceId = GameApp.CardManager.CurrentSelectedTargetId
            };

            bool isQueueFull = m_actionQueue.PushAction(action);
            OnRefreshMoveIndicators?.Invoke();

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

        // 交换两张手牌位置
        private void swapCard(int indexA, int indexB)
        {
            var handCards = GameApp.CardManager.GetHandCards();

            // 大招卡不参与交换
            if (handCards[indexA].BaseData.CardType == CardType.Ultimate ||
                handCards[indexB].BaseData.CardType == CardType.Ultimate)
                return;

            m_handCardUIManager.SwapCards(indexA, indexB);

            // 数据层同步交换
            (handCards[indexA], handCards[indexB]) = (handCards[indexB], handCards[indexA]);
            m_handCardUIManager.RefreshHandCardLayout();
        }

        // 合成两张相邻手牌
        private void compositeCard(int indexA, int indexB, Action onCompleteAllMerges = null)
        {
            UI_BaseCardItem cardA = m_handCardUIManager.GetCardAt(indexA);
            UI_BaseCardItem cardB = m_handCardUIManager.GetCardAt(indexB);

            Vector2 centerPos = (cardA.Rect.anchoredPosition + cardB.Rect.anchoredPosition) / 2f;

            // 保留cardA并升星，cardB销毁
            cardA.BattleCardData.StarLevel += 1;
            m_handCardUIManager.RemoveCard(cardB);
            GameApp.CardManager.RemoveHandCard(cardB.BattleCardData);

            if (cardA is UI_CommonCardItem commonCardA)
                commonCardA.ShowStarUI(cardA.BattleCardData.StarLevel);

            cardA.PlayCompositeAnim(centerPos);
            cardB.PlayCompositeAnim(centerPos, () =>
            {
                cardB.HideCard();
                cardB.transform.SetParent(m_cardDeckTf, true);

                m_handCardUIManager.RefreshHandCardLayout();
                GameApp.CardManager.AddActionPointToOwner(cardA.GetOwnerId());
                OnRefreshActionPointUI?.Invoke();

                CheckAndTriggerComposite(onCompleteAllMerges);
            });
        }
        #endregion
    }
}
