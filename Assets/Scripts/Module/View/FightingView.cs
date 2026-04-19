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
        
        [Header("手牌区域相关")] 
        private Transform _cardDeckTf;
        private readonly int _maxHandCardCount = GameApp.CardManager.MaxHandCardCount;
        
        
        protected override void OnAwake()
        {
            _cardActionTf = Find<Transform>("CardAction");
            _cardDeckTf = Find<Transform>("CardDeck");
            _turnInfoText = Find<Text>("FightDetail/Round/Txt_turnNum");

            _cardPool = new List<UI_CommonCardItem>();
            _handCardItems = new List<UI_CommonCardItem>();
            _uiActionStack = new Stack<UI_CommonCardItem>();
        }

        protected override void OnStart()
        {
            _cardActionQueue = GameApp.CardManager.CardActionQueue;
            
            Find<Button>("OperationBtns/Btn_pause").onClick.AddListener(onPauseBtn);
            Find<Button>("CardAction/Btn_Undo").onClick.AddListener(onUndoBtn);
            
            Controller.RegisterFunc(EventDefines.UpdateHandCards, onUpdateHandCards);
            Controller.RegisterFunc(EventDefines.ExitLevel, onExitLevel);
            
            GameApp.MessageCenter.AddEvent(EventDefines.OnPlayerTurnOutput, onHideAllHands);
            GameApp.MessageCenter.AddEvent(EventDefines.OnCardExecuteUI, onCardExecuteUI);
            
            PreLoadCardItem();
        }
        
        protected override void OnDestroy()
        {
            Controller.UnRegisterFunc(EventDefines.UpdateHandCards, onUpdateHandCards);
            Controller.UnRegisterFunc(EventDefines.ExitLevel, onExitLevel);
                
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPlayerTurnOutput, onHideAllHands);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnCardExecuteUI, onCardExecuteUI);
            
            foreach (var item in _cardPool)
            {
                if (item != null)
                    ResManager.UnLoadInstance(item.gameObject);
            }
            _cardPool.Clear();
        }

        public override void Open(params object[] args)
        {
            SetVisible(true);
            
            if(_cardPool.Count == _maxHandCardCount)
                ApplyFunc(EventDefines.FightingViewReady);
        }
        
        #region 按钮回调与UI文字绑定
        private void onPauseBtn()
        {
            ApplyFunc(EventDefines.OpenPauseFightView);
        }

        private void onUndoBtn()
        {
            CardAction lastAction = _cardActionQueue.UndoLastAction();
            if (lastAction == null) return;
            
            if(_uiActionStack.Count == 0) return;
            UI_CommonCardItem undoItem = _uiActionStack.Pop();

            undoItem.Rect.DOKill();
            undoItem.IsInQueue = false;
            undoItem.SetBlockRaycasts(true);
            undoItem.transform.SetParent(_cardDeckTf, true);

            int insertIndex = Mathf.Clamp(lastAction.OriginalIndex, 0, _handCardItems.Count);
            _handCardItems.Insert(insertIndex, undoItem);
            
            RefreshHandCardLayout();
        }
        
        private void onExitLevel(params object[] args)
        {
            foreach (var item in _cardPool)
            {
                if (item != null)
                    item.HideCard();
            }
        }

        private void onUpdateLevelInfo(params object[] args)
        {
            //TODO:更新轮次,后面再说吧。。
        }
        #endregion
        
        #region 卡牌交互逻辑回调
        private void OnCardBeginDrag(UI_CommonCardItem arg1, PointerEventData arg2)
        {
            
        }
        
        private void OnCardDrag(UI_CommonCardItem item, PointerEventData eventData)
        {
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
            int index = _handCardItems.IndexOf(item);
            item.MoveToIndex(index, _handCardItems.Count);
            
            CheckAndTriggerComposite();
            
            //TODO:判断升星以及特殊操作等
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
            
            _handCardItems.RemoveAt(index);
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

            bool isQueueFull =
                _cardActionQueue.PlayCard(item.BattleCardData, index, GameApp.CardManager.CurrentSelectedTargetId);
            if (isQueueFull)
            {
                DOVirtual.DelayedCall(2f, () =>
                {
                    GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnOutput);
                });
            }
            
            

            //TODO: 将出牌逻辑通知控制器进行处理,通知CardActionQueue等进行管理
        }

        private void SwapCard(int indexA, int indexB)
        {
            (_handCardItems[indexA], _handCardItems[indexB]) = (_handCardItems[indexB], _handCardItems[indexA]);

            RefreshHandCardLayout();
        }
        
        private void CompositeCard(int indexA, int indexB)
        {
            UI_CommonCardItem cardA = _handCardItems[indexA];
            UI_CommonCardItem cardB = _handCardItems[indexB];
            
            Vector2 centerPos = (cardA.Rect.anchoredPosition + cardB.Rect.anchoredPosition) / 2f;
            
            //保留cardA并升星,cardB销毁
            cardA.BattleCardData.StarLevel += 1;
            _handCardItems.Remove(cardB);
            cardA.ShowStarUI(cardA.BattleCardData.StarLevel);
            
            cardA.PlayCompositeAnim(centerPos);
            cardB.PlayCompositeAnim(centerPos, () =>
            {
                cardB.HideCard();
                cardB.transform.SetParent(_cardDeckTf, true); // 确保父节点归位
                
                RefreshHandCardLayout();
                
                CheckAndTriggerComposite();
            });
            
            // TODO: 合成卡后角色的行动点数+1(这个先不做)
        }
        
        private void CheckAndTriggerComposite()
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
                    CompositeCard(i, i + 1);
                    return; 
                }
            }
        }
        
        private void PreLoadCardItem()
        {
            int loadedCount = 0;
            for (int i = 0; i < _maxHandCardCount; i++)
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
                    if (loadedCount == _maxHandCardCount)
                    {
                        ApplyFunc(EventDefines.FightingViewReady);
                    }
                });
            }
        }
        #region 手牌UI事件
        private void onUpdateHandCards(params object[] args)
        {
            //args[0] 手牌列表->本轮新抽的牌
            List<BattleCardData> newCards = args[0] as List<BattleCardData>;
            
            if(newCards == null) return;
            
            _handCardItems.Clear();//清空当前手牌实例列表，准备重新分配

            for (int i = 0; i < _maxHandCardCount; i++)
            {
                UI_CommonCardItem item = _cardPool[i];

                if (i < newCards.Count)
                {
                    item.transform.SetParent(_cardDeckTf, true);
                    bool isNewCard = !item.gameObject.activeSelf;
                    if(isNewCard)
                        item.PrepareSpawn();
                    
                    item.InitCardUI(newCards[i]);
                    _handCardItems.Add(item);
                    
                    item.OnBeginDragCallback = OnCardBeginDrag;
                    item.OnDragCallback = OnCardDrag;
                    item.OnEndDragCallback = OnCardEndDrag;
                    item.OnClickCallback = OnCardClick;
                    
                    float delay = isNewCard ? (newCards.Count - 1 - i) * 0.05f : 0f;
                    item.MoveToIndex(i, newCards.Count, delay);
                }
                else
                {
                    item.HideCard();
                    item.OnBeginDragCallback = null;
                    item.OnDragCallback = null;
                    item.OnEndDragCallback = null;
                    item.OnClickCallback = null;
                }
            }
        }

        private void onHideAllHands(System.Object args)
        {
            foreach (var item in _handCardItems)
            {
                item.HideCard();
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
        #endregion
        #endregion
    }
}