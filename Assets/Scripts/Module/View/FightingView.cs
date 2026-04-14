/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 战斗HUD(关卡信息、暂停等操作按钮、卡组等)                      
* │  类    名: FightingView.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;
using Common.Defines;
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
        private List<UI_CommonCardItem> _activeCardItems;

        [Header("队列区域相关")] 
        private Transform _cardActionTf;
        private float _cardActionWidth = 550f;
        private CardActionQueue _cardActionQueue;
        
        [Header("手牌区域相关")] 
        private Transform _cardDeckTf;
        private readonly int _maxHandCardCount = 8;
        
        protected override void OnAwake()
        {
            _cardActionTf = Find<Transform>("CardAction");
            _cardDeckTf = Find<Transform>("CardDeck");

            _cardPool = new List<UI_CommonCardItem>();
            _activeCardItems = new List<UI_CommonCardItem>();
            _cardActionQueue = new CardActionQueue();
        }

        protected override void OnStart()
        {
            Find<Button>("OperationBtns/Btn_pause").onClick.AddListener(onPauseBtn);
            
            Controller.RegisterFunc(EventDefines.UpdateHandCards, onUpdateHandCards);
            Controller.RegisterFunc(EventDefines.ExitLevel, onExitLevel);
            
            PreLoadCardItem();
        }

        protected override void OnDestroy()
        {
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
        
        private void onPauseBtn()
        {
            ApplyFunc(EventDefines.OpenPauseFightView);
        }

        private void onExitLevel(params object[] args)
        {
            foreach (var item in _cardPool)
            {
                if (item != null)
                    item.HideCard();
            }
        }
        
        private void onUpdateHandCards(params object[] args)
        {
            //args[0] 手牌列表->本轮新抽的牌
            List<BattleCardData> newCards = args[0] as List<BattleCardData>;
            
            if(newCards == null) return;

            for (int i = 0; i < _maxHandCardCount; i++)
            {
                UI_CommonCardItem item = _cardPool[i];

                if (i < newCards.Count)
                {
                    bool isNewCard = !item.gameObject.activeSelf;
                    if(isNewCard)
                        item.PrepareSpawn();
                    
                    item.InitCardUI(newCards[i]);
                    _activeCardItems.Add(item);
                    
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

        #region 卡牌交互逻辑回调
        private void OnCardBeginDrag(UI_CommonCardItem arg1, PointerEventData arg2)
        {
            
        }
        
        private void OnCardDrag(UI_CommonCardItem item, PointerEventData eventData)
        {
            int currentIndex = _activeCardItems.IndexOf(item);
            if (currentIndex == -1) return;

            float currentX = item.Rect.anchoredPosition.x;
            
            //向右拖动
            if (currentIndex < _activeCardItems.Count - 1)
            {
                float rightX = _activeCardItems[currentIndex + 1].Rect.anchoredPosition.x;

                if (currentX > rightX - (item.CardWidth / 2))
                {
                    SwapCard(currentIndex, currentIndex + 1);
                    return;
                }
            }
            
            //向左拖动
            if (currentIndex > 0)
            {
                float leftX = _activeCardItems[currentIndex - 1].Rect.anchoredPosition.x;
                if (currentX < leftX + (item.CardWidth / 2))
                {
                    SwapCard(currentIndex, currentIndex - 1);
                    return;
                }
            }
        }
        
        private void OnCardEndDrag(UI_CommonCardItem item, PointerEventData eventData)
        {
            int index = _activeCardItems.IndexOf(item);
            item.MoveToIndex(index, _activeCardItems.Count);
            
            //TODO:判断升星以及特殊操作等
        }
        
        private void OnCardClick(UI_CommonCardItem item)
        {
            int index = _activeCardItems.IndexOf(item);
            if (index != -1)
                PlayCard(item, index);
        }
        #endregion

        #region 卡牌具体逻辑
        private void PlayCard(UI_CommonCardItem item, int index)
        {
            if (!_cardActionQueue.PlayCard(item.CardData, index))
            {
                Debug.Log("已达到本轮出牌上限");
                return;
            }
            
            _activeCardItems.RemoveAt(index);

            for (int i = 0; i < _activeCardItems.Count; i++)
            {
                _activeCardItems[i].MoveToIndex(i, _activeCardItems.Count);
            }

            item.transform.SetParent(_cardActionTf, true);

            Vector2 targetPos = new Vector2(-_cardActionWidth, 0) +
                                new Vector2((_cardActionQueue.GetCurrentActionCount() - 1) * (item.CardWidth + 10f), 0);
            
            item.PlayToQueueAnim(targetPos);
            
            //TODO: 将出牌逻辑通知控制器进行处理,通知CardActionQueue等进行管理
        }

        private void SwapCard(int indexA, int indexB)
        {
            (_activeCardItems[indexA], _activeCardItems[indexB]) = (_activeCardItems[indexB], _activeCardItems[indexA]);

            var moveItem = _activeCardItems[indexA];
            moveItem.MoveToIndex(indexA, _activeCardItems.Count);
        }
        
        #endregion
    }
}