/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 战斗HUD(关卡信息、暂停等操作按钮、卡组等)                      
* │  类    名: FightingView.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common;
using Common.Defines;
using DG.Tweening;
using Module.fight.CardMgr;
using Module.fight.Component;
using MVC.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.View
{
    public class FightingView : BaseView
    {
        private List<UI_CommonCardItem> _cardPool;
        private List<UI_CommonCardItem> _handCardItems;//当前手牌实例列表
        private Stack<UI_CommonCardItem> _uiActionStack;//出牌队列实例

        [Header("关卡信息相关")]
        private Text _turnInfoText;
        
        [Header("队列区域相关")] 
        private Transform _cardActionTf;
        private float _cardActionWidth = 550f;
        private CardActionQueue _cardActionQueue;
        private List<Transform> m_UIActions;
        
        [Header("手牌区域相关")] 
        private Transform _cardDeckTf;

        [Header("快照相关")] 
        private int _dragStartIndex = -1;
        private CardSnapshot m_tempSnapshot;

        #region 生命函数
        protected override void OnAwake()
        {
            _cardActionTf = Find<Transform>("CardAction");
            _cardDeckTf = Find<Transform>("CardDeck");
            _turnInfoText = Find<Text>("FightDetail/Round/Txt_turnNum");

            _cardPool = new List<UI_CommonCardItem>();
            _handCardItems = new List<UI_CommonCardItem>();
            _uiActionStack = new Stack<UI_CommonCardItem>();

            #region 队列UI
            m_UIActions = new List<Transform>();
            m_UIActions.Add(Find<Transform>("CardAction/queue_1"));
            m_UIActions.Add(Find<Transform>("CardAction/queue_2"));
            m_UIActions.Add(Find<Transform>("CardAction/queue_3"));
            m_UIActions.Add(Find<Transform>("CardAction/queue_4"));
            #endregion
        }
        
        protected override void OnEnable()
        {
            Find<Button>("OperationBtns/Btn_pause").onClick.AddListener(onPauseBtn);
            Find<Button>("CardAction/Btn_Undo").onClick.AddListener(onUndoBtn);
            
            GameApp.MessageCenter.AddEvent(EventDefines.OnPlayerTurnOutput, onHideAllHands);
            GameApp.MessageCenter.AddEvent(EventDefines.OnCardExecuteUI, onCardExecuteUI);
            GameApp.MessageCenter.AddEvent(EventDefines.OnRemoveDiedCharacterCard, onRemoveDiedCharacterCardsUI);
        }
        
        protected override void OnStart()
        {
            Controller.RegisterFunc(EventDefines.UpdateHandCards, onUpdateHandCards);
            Controller.RegisterFunc(EventDefines.ExitLevel, onExitLevel);
            
            _cardActionQueue = GameApp.CardManager.CardActionQueue;
            PreLoadCardItem();
        }
        
        protected override void OnDisable()
        {
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPlayerTurnOutput, onHideAllHands);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnCardExecuteUI, onCardExecuteUI);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnRemoveDiedCharacterCard, onRemoveDiedCharacterCardsUI);
        }

        protected override void OnDestroy()
        {
            Controller.UnRegisterFunc(EventDefines.UpdateHandCards, onUpdateHandCards);
            Controller.UnRegisterFunc(EventDefines.ExitLevel, onExitLevel);
            
            foreach (var item in _cardPool)
            {
                if (item != null)
                    ResManager.UnLoadInstance(item.gameObject);
            }
            _cardPool.Clear();
        }
        #endregion

        public override void Open(params object[] args)
        {
            SetVisible(true);

            for (int i = m_UIActions.Count - 1; i >= 0; i--)
            {
                if (!m_UIActions[i].gameObject.activeSelf)
                {
                    m_UIActions[i].gameObject.SetActive(true);
                }
            }
            
            if (_cardPool.Count == GameApp.CardManager.mMaxHandCardCount)
                ApplyFunc(EventDefines.FightingViewReady);
        }

        #region UI事件

        #region 关卡详情
        private void onUpdateLevelInfo(params object[] args)
        {
            //TODO:更新轮次,后面再说吧。。
        }
        #endregion
        
        #region 队列UI事件
        //刷新Move占位符
        private void RefreshMoveIndicators()
        {
            CardAction[] actions = _cardActionQueue.GetAction();

            for (int i = 0; i < m_UIActions.Count; i++)
            {
                Transform imgMove = m_UIActions[i].Find("Img_move");
                if (imgMove != null)
                {
                    bool isMove = i < actions.Length && actions[i].ActionType == CardActionType.MoveCard;

                    if (imgMove.gameObject.activeSelf != isMove)
                    {
                        imgMove.gameObject.SetActive(isMove);
                        
                        CanvasGroup cg = imgMove.GetComponent<CanvasGroup>();
                        if (cg != null && isMove)
                        {
                            cg.alpha = 0f;
                            cg.DOFade(1f, 0.2f);
                        }
                    }
                }
            }
        }
        #endregion
        
        #region 手牌UI事件
        private void onUpdateHandCards(params object[] args)
        {
            List<BattleCardData> newCards = args[0] as List<BattleCardData>;
            bool isUndo = args.Length > 1 && args[1] is true;
            
            if(newCards == null) return;
            
            List<UI_CommonCardItem> newHandItems = new List<UI_CommonCardItem>();
            float maxAnimTime = 0f;

            for (int i = 0; i < newCards.Count; i++)
            {
                BattleCardData cardData = newCards[i];
                
                UI_CommonCardItem item = _handCardItems.Find(x => ReferenceEquals(x.BattleCardData, cardData));
                
                bool isNewCard = false;
                if (item == null)
                {
                    item = _cardPool.Find(x => !x.gameObject.activeSelf && !newHandItems.Contains(x));
                    if (item != null)
                    {
                        isNewCard = true;
                        item.transform.SetParent(_cardDeckTf, true);
                        
                        if(!isUndo)
                            item.PrepareSpawn(); // 放回最左侧初始点
                    }
                }

                if (item != null)
                {
                    item.InitCardUI(cardData);
                    newHandItems.Add(item);

                    item.OnBeginDragCallback = OnCardBeginDrag;
                    item.OnDragCallback = OnCardDrag;
                    item.OnEndDragCallback = OnCardEndDrag;
                    item.OnClickCallback = OnCardClick;
                    
                    float delay = (isNewCard && !isUndo) ? (newCards.Count - 1 - i) * 0.05f : 0f;
                    
                    if (isNewCard && !isUndo) item.transform.SetAsLastSibling();
                    
                    item.MoveToIndex(i, newCards.Count, delay);

                    float finishTime = delay + item.GetMoveDuration(); 
                    if (finishTime > maxAnimTime) maxAnimTime = finishTime;
                }
            }
            
            foreach (var oldItem in _cardPool)
            {
                if (!newHandItems.Contains(oldItem) && !oldItem.IsInQueue)
                {
                    oldItem.HideCard();
                    oldItem.OnBeginDragCallback = null;
                    oldItem.OnDragCallback = null;
                    oldItem.OnEndDragCallback = null;
                    oldItem.OnClickCallback = null;
                }
            }

            _handCardItems = newHandItems;

            //撤销时不触发检查和补牌
            if (!isUndo)
            {
                // 延迟调用合成与补牌
                DOVirtual.DelayedCall(maxAnimTime, () =>
                {
                    CheckAndTriggerComposite(() =>
                    {
                        if (_handCardItems.Count < GameApp.CardManager.mMaxHandCardCount)
                        {
                            int needCount = GameApp.CardManager.mMaxHandCardCount - _handCardItems.Count;
                            int beforeCount = GameApp.CardManager.GetHandCards().Count;

                            GameApp.CardManager.DrawCard(needCount);
                            
                            if(GameApp.CardManager.GetHandCards().Count > beforeCount)
                                onUpdateHandCards(GameApp.CardManager.GetHandCards());
                        }
                    });
                });
            }
        }

        private void onHideAllHands(System.Object args)
        {
            foreach (var item in _handCardItems)
            {
                item.HideCard();
            }

            for (int i = 0; i < m_UIActions.Count; i++)
            {
                Transform imgMove = m_UIActions[i].Find("Img_move");
                if(imgMove != null) imgMove.gameObject.SetActive(false);
            }
        }
        
        private void RefreshHandCardLayout()
        {
            for (int i = 0; i < _handCardItems.Count; i++)
            {
                _handCardItems[i].MoveToIndex(i, _handCardItems.Count);
            }
        }
        
        private void onCardExecuteUI(System.Object args = null)
        {
            Transform executingCard = null;

            for (int i = 0; i < _cardActionTf.childCount; i++)
            {
                Transform child = _cardActionTf.GetChild(i);
                if (child.GetComponent<UI_CommonCardItem>() != null)
                {
                    executingCard = child;
                    break;
                }
            }
            
            if (executingCard == null) return;
            
            executingCard.SetParent(transform);
            executingCard.SetAsLastSibling();

            RectTransform rect = executingCard.GetComponent<RectTransform>();

            #region 动画序列
            Vector3 centerWorldPos = transform.position;

            Sequence seq = DOTween.Sequence();

            // 阶段 1：甩出卡牌 (抛物线 + 旋转 + 放大)
            seq.Append(rect.DOMoveX(centerWorldPos.x, 0.45f).SetEase(Ease.OutCirc)); // X轴平滑滑出
            seq.Join(rect.DOMoveY(centerWorldPos.y, 0.45f).SetEase(Ease.OutBack, 1.2f)); // Y轴带一点轻微超出再回弹
            seq.Join(rect.DOScale(Vector3.one * 1.5f, 0.45f).SetEase(Ease.OutQuad));
            seq.Join(rect.DORotate(new Vector3(0, 0, -8f), 0.45f).SetEase(Ease.OutQuad)); // 稍微倾斜一点，模拟甩牌手感

            // 阶段 2：悬停展示 (让玩家看清打出了什么)
            seq.AppendInterval(0.5f);

            // 阶段 3：爆发击出/消失 (急速收缩 + 回正)
            seq.Append(rect.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
            seq.Join(rect.DORotate(Vector3.zero, 0.2f));

            seq.OnComplete(() =>
            {
                executingCard.gameObject.SetActive(false);
                executingCard.SetParent(_cardDeckTf);
                
                // 重置状态，防止卡牌下次回到手牌时还是歪的/缩小的
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            });
            #endregion
        }

        private void onRemoveDiedCharacterCardsUI(System.Object args = null)
        {
            #region 移除死亡角色卡牌
            if (args is List<BattleCardData> { Count: >= 0 } removedCards)
            {
                bool layoutChanged = false;
                foreach (var cardData in removedCards)
                {
                    UI_CommonCardItem item = _handCardItems.Find(x => ReferenceEquals(x.BattleCardData, cardData));
                    if (item != null)
                    {
                        _handCardItems.Remove(item);
                        item.PlayFadeOutAnim(() =>
                        {
                            item.transform.SetParent(_cardDeckTf, true);
                        });
                        layoutChanged = true;
                    }
                }

                if (layoutChanged) RefreshHandCardLayout();
            }

            
            #endregion
            
            #region 更新队列UI
            int maxCount = _cardActionQueue.MaxActionCount;
            for (int i = 0; i < m_UIActions.Count; i++)
            {
                int currentIndex = i;
                bool shouldActive = currentIndex < maxCount;

                if (m_UIActions[currentIndex].gameObject.activeSelf && !shouldActive)
                {
                    CanvasGroup cg = m_UIActions[i].GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.DOKill();
                        cg.DOFade(0,0.5f).onComplete = () =>
                        {
                            m_UIActions[currentIndex].gameObject.SetActive(false);
                            cg.alpha = 1f;
                        };
                    }
                    else
                    {
                        m_UIActions[currentIndex].gameObject.SetActive(false);
                    }
                }
                
                
            }
            #endregion
        }
        #endregion

        #endregion
        
        #region 按钮回调
        private void onPauseBtn()
        {
            ApplyFunc(EventDefines.OpenPauseFightView);
        }

        private void onUndoBtn()
        {
            CardAction lastAction = _cardActionQueue.UndoLastAction();
            if (lastAction == null) return;
            
            GameApp.CardManager.RestoreSnapshot(lastAction.Snapshot);
            
            if (lastAction.ActionType == CardActionType.PlayCard)
            {
                if (_uiActionStack.Count > 0)
                {
                    UI_CommonCardItem undoItem = _uiActionStack.Pop();
                    undoItem.Rect.DOKill();
                    undoItem.IsInQueue = false;
                    undoItem.SetBlockRaycasts(true);
                    undoItem.transform.SetParent(_cardDeckTf, true);
                    undoItem.HideCard();
                }
            }

            foreach (var item in _handCardItems)
            {
                if (!item.IsInQueue)
                {
                    item.HideCard();
                    item.transform.SetParent(_cardDeckTf, true);
                }
            }
            _handCardItems.Clear();
            
            // 统一刷新移动占位符 UI
            RefreshMoveIndicators();

            // 全量重建当前手牌 UI
            onUpdateHandCards(GameApp.CardManager.GetHandCards(), true);
        }
        
        private void onExitLevel(params object[] args)
        {
            //注意:所有离开关卡的行为都需要经过这个事件,不然动画UI等效果会出错
            
            _uiActionStack.Clear();
            _handCardItems.Clear();
            
            foreach (var item in _cardPool)
            {
                if (item != null)
                {
                    item.transform.SetParent(_cardDeckTf, false);
                    item.HideCard();
                    item.PrepareSpawn();
                }
            }
            
            onHideAllHands(null);
        }
        
        #endregion
        
        #region 卡牌交互逻辑回调
        private void OnCardBeginDrag(UI_CommonCardItem item, PointerEventData eventData)
        {
            if(item.IsInQueue) return;

            _dragStartIndex = _handCardItems.IndexOf(item);
            if (_cardActionQueue.CanPlayCard())
                m_tempSnapshot = GameApp.CardManager.TakeSnapshot();
        }
        
        private void OnCardDrag(UI_CommonCardItem item, PointerEventData eventData)
        {
            if(!_cardActionQueue.CanPlayCard()) return;
            
            int currentIndex = _handCardItems.IndexOf(item);
            if (currentIndex == -1) return;

            float currentX = item.Rect.anchoredPosition.x;
            
            //向右拖动
            if (currentIndex < _handCardItems.Count - 1)
            {
                float rightX = _handCardItems[currentIndex + 1].Rect.anchoredPosition.x;

                if (currentX > rightX - (item.CardWidth / 2))
                {
                    SwapCard(currentIndex, currentIndex + 1);
                    return;
                }
            }
            
            //向左拖动
            if (currentIndex > 0)
            {
                float leftX = _handCardItems[currentIndex - 1].Rect.anchoredPosition.x;
                if (currentX < leftX + (item.CardWidth / 2))
                {
                    SwapCard(currentIndex, currentIndex - 1);
                    return;
                }
            }
        }
        
        private void OnCardEndDrag(UI_CommonCardItem item, PointerEventData eventData)
        {
            if(item.IsInQueue) return;
            
            int index = _handCardItems.IndexOf(item);
            item.MoveToIndex(index, _handCardItems.Count);
            
            //如果位置确实改变了 && 有行动点
            if (_dragStartIndex != -1 && _dragStartIndex != index && _cardActionQueue.CanPlayCard())
            {
                CardAction action = new CardAction()
                {
                    ActionType = CardActionType.MoveCard,
                    Snapshot = m_tempSnapshot,
                    MoveFromIndex = _dragStartIndex,
                    MoveToIndex = index
                };

                bool isQueueFull = _cardActionQueue.PushAction(action);
                
                RefreshMoveIndicators();
                CheckAndTriggerComposite();

                if (isQueueFull)
                {
                    DOVirtual.DelayedCall(2f, () =>
                    {
                        GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnOutput);
                    });
                }
                
            }
            else
            {
                //位置没变或者没有行动点了,只进行基础检查
                RefreshHandCardLayout();
            }

            _dragStartIndex = -1;
            m_tempSnapshot = null;
            
            //TODO:玩家行动点+1等逻辑
        }
        
        private void OnCardClick(UI_CommonCardItem item)
        {
            int index = _handCardItems.IndexOf(item);
            if (index != -1)
                PlayCard(item, index);
        }
        #endregion

        #region 卡牌具体逻辑
        private void PlayCard(UI_CommonCardItem item, int index)
        {
            if (!_cardActionQueue.CanPlayCard())
            {
                Debug.Log("已达到本轮出牌上限");
                return;
            }

            //在数据修改前记录快照
            CardSnapshot snapshot = GameApp.CardManager.TakeSnapshot();
            
            _handCardItems.RemoveAt(index);
            GameApp.CardManager.GetHandCards().Remove(item.BattleCardData);
            
            _uiActionStack.Push(item);
            RefreshHandCardLayout();

            item.transform.SetParent(_cardActionTf, true);

            Vector2 targetPos = new Vector2(-_cardActionWidth, 0) +
                                new Vector2(
                                    (_cardActionQueue.GetCurrentActionCount()) * (item.CardWidth * 0.8f + 12f), 0);
            
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

            bool isQueueFull = _cardActionQueue.PushAction(action);
            
            RefreshMoveIndicators();
            
            if (isQueueFull)
            {
                DOVirtual.DelayedCall(2f, () =>
                {
                    GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnOutput);
                });
            }
        }

        private void SwapCard(int indexA, int indexB)
        {
            //UI层交换
            (_handCardItems[indexA], _handCardItems[indexB]) = (_handCardItems[indexB], _handCardItems[indexA]);
            
            //数据层同步交换
            var handCards = GameApp.CardManager.GetHandCards();
            (handCards[indexA], handCards[indexB]) = (handCards[indexB], handCards[indexA]);
            RefreshHandCardLayout();
        }
        
        private void CompositeCard(int indexA, int indexB, Action onCompleteAllMerges = null)
        {
            UI_CommonCardItem cardA = _handCardItems[indexA];
            UI_CommonCardItem cardB = _handCardItems[indexB];
            
            Vector2 centerPos = (cardA.Rect.anchoredPosition + cardB.Rect.anchoredPosition) / 2f;
            
            //保留cardA并升星,cardB销毁
            cardA.BattleCardData.StarLevel += 1;
            _handCardItems.Remove(cardB);
            GameApp.CardManager.RemoveHandCard(cardB.BattleCardData);
            cardA.ShowStarUI(cardA.BattleCardData.StarLevel);
            
            cardA.PlayCompositeAnim(centerPos);
            cardB.PlayCompositeAnim(centerPos, () =>
            {
                cardB.HideCard();
                cardB.transform.SetParent(_cardDeckTf, true); // 确保父节点归位
                
                RefreshHandCardLayout();
                
                CheckAndTriggerComposite(onCompleteAllMerges);
            });
            
            // TODO: 合成卡后角色的行动点数+1(这个先不做)
        }
        
        private void CheckAndTriggerComposite(Action onCompleteAllMerges = null)
        {
            // 从左到右遍历手牌，寻找相邻且相同的牌
            for (int i = 0; i < _handCardItems.Count - 1; i++)
            {
                var cardA = _handCardItems[i];
                var cardB = _handCardItems[i + 1];

                //相邻牌若星级和种类且拥有者相同则合成
                //如：两张相同的1星卡合成一张2星卡，三张相同的2星卡合成一张3星卡
                if (cardA.BattleCardData.Equals(cardB.BattleCardData))
                {
                    // 找到一对可以合成的牌，触发合成并立即中断循环
                    // 一次只处理一对，靠动画回调实现连锁反应
                    CompositeCard(i, i + 1, onCompleteAllMerges);
                    return; 
                }
            }
            
            onCompleteAllMerges?.Invoke();
        }
        
        private void PreLoadCardItem()
        {
            int loadedCount = 0;
            for (int i = 0; i < GameApp.CardManager.mMaxHandCardCount; i++)
            {
                ResManager.InstantiateAsync(AddressDefines.UI_small_CommonCard, (go) =>
                {
                    if (go == null)
                    {
                        Debug.LogError("加载卡牌预制体失败");
                        return;
                    }

                    go.transform.SetParent(_cardDeckTf, false);
                    UI_CommonCardItem item = go.GetComponent<UI_CommonCardItem>();
                    
                    item.SetVisible(false);
                    _cardPool.Add(item);

                    loadedCount++;
                    if (loadedCount == GameApp.CardManager.mMaxHandCardCount)
                    {
                        ApplyFunc(EventDefines.FightingViewReady);
                    }
                });
            }
        }
        #endregion
    }
}