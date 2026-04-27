/*
* ┌──────────────────────────────────┐
* │  描    述: 手牌区UI管理器                      
* │  类    名: HandCardUIManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using DG.Tweening;
using Module.fight.Component;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Module.fight.CardMgr
{
    public class HandCardUIManager
    {
        private List<UI_BaseCardItem> _handCardItems; //当前手牌实例列表
        private Transform _cardDeckTf;
        private CardPoolManager _poolManager;
        
        private Action<UI_BaseCardItem, PointerEventData> m_onBeginDrag;
        private Action<UI_BaseCardItem, PointerEventData> m_onDrag;
        private Action<UI_BaseCardItem, PointerEventData> m_onEndDrag;
        private Action<UI_BaseCardItem> m_onClick;

        public HandCardUIManager(Transform cardDeckTf ,CardPoolManager poolManager)
        {
            _handCardItems = new List<UI_BaseCardItem>();
            _cardDeckTf = cardDeckTf;
            _poolManager = poolManager;
        }

        public void Init()
        {

        }

        public void Clear()
        {
            _handCardItems.Clear();
        }

        public void SetCardEventHandlers(
            Action<UI_BaseCardItem, PointerEventData> onBeginDrag,
            Action<UI_BaseCardItem, PointerEventData> onDrag,
            Action<UI_BaseCardItem, PointerEventData> onEndDrag,
            Action<UI_BaseCardItem> onClick)
        {
            m_onBeginDrag = onBeginDrag;
            m_onDrag = onDrag;
            m_onEndDrag = onEndDrag;
            m_onClick = onClick;
        }
        
        public void UpdateCardsUI(List<BattleCardData> newCards, bool isUndo, Action onLayoutStable)
        {
            if(newCards == null) return;
            
            List<UI_BaseCardItem> newHandItems = new List<UI_BaseCardItem>();
            float maxAnimTime = 0f;
            
            for (int i = 0; i < newCards.Count; i++)
            {
                BattleCardData cardData = newCards[i];
                
                UI_BaseCardItem item = _handCardItems.Find(x => ReferenceEquals(x.BattleCardData, cardData));
                
                bool isNewCard = false;
                if (item == null)
                {
                    item = _poolManager.GetCard(cardData.BaseData.CardType);
                    
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

                    item.RegisterDragAndClickEvent(m_onBeginDrag, m_onDrag, m_onEndDrag, m_onClick);
                    
                    float delay = (isNewCard && !isUndo) ? (newCards.Count - 1 - i) * 0.05f : 0f;
                    
                    if (isNewCard && !isUndo) item.transform.SetAsLastSibling();
                    
                    item.MoveToIndex(i, newCards.Count, delay);

                    float finishTime = delay + item.AnimConfig.MoveDuration;
                    if (finishTime > maxAnimTime) maxAnimTime = finishTime;
                }
            }
            
            foreach (var oldItem in _handCardItems)
            {
                if (!newHandItems.Contains(oldItem) && !oldItem.IsInQueue)
                {
                    _poolManager.RecycleCard(oldItem);
                }
            }
            _handCardItems = newHandItems;
            
            if (onLayoutStable != null)
            {
                if (maxAnimTime > 0f && !isUndo)
                    DOVirtual.DelayedCall(maxAnimTime, () => onLayoutStable());
                else
                    onLayoutStable();
            }
        }
        
        public void HideAllHands(System.Object args)
        {
            foreach (var item in _handCardItems)
            {
                item.HideCard();
            }
        }

        public void RefreshHandCardLayout()
        {
            for (int i = 0; i < _handCardItems.Count; i++)
            {
                _handCardItems[i].MoveToIndex(i, _handCardItems.Count);
            }
        }

        public void RemoveDiedCharacterCard(System.Object args)
        {
            if (args is List<BattleCardData> { Count: >= 0 } removedCards)
            {
                bool layoutChanged = false;
                foreach (var cardData in removedCards)
                {
                    UI_BaseCardItem item = _handCardItems.Find(x => ReferenceEquals(x.BattleCardData, cardData));
                    if (item != null)
                    {
                        _handCardItems.Remove(item);
                        item.PlayFadeOutAnim(() => { item.transform.SetParent(_cardDeckTf, true); });
                        layoutChanged = true;
                    }
                }

                if (layoutChanged)
                    RefreshHandCardLayout();
            }
        }

        #region 查询与操作接口
        public List<UI_BaseCardItem> GetHandItems() => _handCardItems;

        public int GetCardIndex(UI_BaseCardItem item) => _handCardItems.IndexOf(item);
        
        public UI_BaseCardItem GetCardAt(int index) => index >= 0 && index < _handCardItems.Count ? _handCardItems[index] : null;

        public int Count => _handCardItems.Count;

        public void SwapCards(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= _handCardItems.Count || indexB < 0 || indexB >= _handCardItems.Count)
                return;

            (_handCardItems[indexA], _handCardItems[indexB]) = (_handCardItems[indexB], _handCardItems[indexA]);
        }

        public void RemoveCardAt(int index)
        {
            if (index >= 0 && index < _handCardItems.Count)
                _handCardItems.RemoveAt(index);
        }

        public void RemoveCard(UI_BaseCardItem item)
        {
            _handCardItems.Remove(item);
        }

        public void InsertCard(int index, UI_BaseCardItem item)
        {
            _handCardItems.Insert(index, item);
        }
        #endregion
    }
}