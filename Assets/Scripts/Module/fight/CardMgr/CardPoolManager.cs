/*
* ┌──────────────────────────────────┐
* │  描    述: 卡牌池管理器                      
* │  类    名: CardPoolManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;
using Common.Defines;
using Data.card;
using Module.fight.Component;
using UnityEngine;

namespace Module.fight.CardMgr
{
    public class CardPoolManager
    {
        private List<UI_CommonCardItem> m_commonCardPool;
        private List<UI_UltimateCardItem> m_ultimateCardPool;
        
        private int _totalPoolToLoad;
        private int _loadedPoolCount;
        
        private Transform _cardDeckTf;

        public CardPoolManager(Transform cardDeckTf)
        {
            _cardDeckTf = cardDeckTf;
            m_ultimateCardPool = new List<UI_UltimateCardItem>();
            m_commonCardPool = new List<UI_CommonCardItem>();
        }
        
        public void Init()
        {
            PreLoadCardItem();
            PreLoadUltimateCardItem();
        }
        
        public void UnLoadAll()
        {
            foreach (var item in m_commonCardPool)
            {
                if (item != null)
                    ResManager.UnLoadInstance(item.gameObject);
            }
            m_commonCardPool.Clear();

            foreach (var item in m_ultimateCardPool)
            {
                if (item != null)
                    ResManager.UnLoadInstance(item.gameObject);
            }
            m_ultimateCardPool.Clear();
        }

        public UI_BaseCardItem GetCard(CardType type)
        {
            UI_BaseCardItem item;
            
            if (type == CardType.Ultimate)
            {
                item = m_ultimateCardPool.Find(x => !x.gameObject.activeSelf);
            }
            else
            {
                item = m_commonCardPool.Find(x => !x.gameObject.activeSelf);
            }
            
            if (item != null)
            {
                item.transform.SetParent(_cardDeckTf, true);
                
                item.PrepareSpawn(); // 放回最左侧初始点
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError($"[{nameof(CardPoolManager)}] 卡牌对象池耗尽！请检查 {type} 生成配置。");
#endif
            }
            
            return item;
        }

        public void RecycleAllCards()
        {
            foreach (var item in m_commonCardPool)
            {
                if (item != null)
                {
                    RecycleCard(item);
                }
            }
            
            foreach (var item in m_ultimateCardPool)
            {
                if (item != null)
                {
                    RecycleCard(item);
                }
            }
        }
        
        public void RecycleCard(UI_BaseCardItem item)
        {
            if(item == null) return;

            item.RegisterDragAndClickEvent(null, null, null, null);
            item.HideCard();
            item.PrepareSpawn();
            item.transform.SetParent(_cardDeckTf, true);
        }
        
        private void PreLoadCardItem()
        {
            _totalPoolToLoad = GameApp.CardManager.mMaxHandCardCount + 8;
            _loadedPoolCount = 0;
            for (int i = 0; i < _totalPoolToLoad; i++)
            {
                ResManager.InstantiateAsync(AddressDefines.UI_small_CommonCard, (go) =>
                {
                    if (go == null)
                    {
                        Debug.LogError("加载卡牌预制体失败");
                        CheckAllPoolLoaded();
                        return;
                    }

                    go.transform.SetParent(_cardDeckTf, false);
                    UI_CommonCardItem item = go.GetComponent<UI_CommonCardItem>();
                    
                    item.SetVisible(false);
                    m_commonCardPool.Add(item);

                    CheckAllPoolLoaded();
                });
            }
        }
        
        private void PreLoadUltimateCardItem()
        { 
            int maxUltimateCount = 6;
            _totalPoolToLoad += maxUltimateCount;
            for (int i = 0; i < maxUltimateCount; i++)
            {
                ResManager.InstantiateAsync(AddressDefines.UI_small_UltimateCard, (go) =>
                {
                    if (go == null)
                    {
                        Debug.LogError("加载大招卡牌预制体失败");
                        CheckAllPoolLoaded();
                        return;
                    }

                    go.transform.SetParent(_cardDeckTf, false);
                    UI_UltimateCardItem item = go.GetComponent<UI_UltimateCardItem>();
                    
                    item.SetVisible(false);
                    m_ultimateCardPool.Add(item);
                    CheckAllPoolLoaded();
                });
            }
        }
        
        private void CheckAllPoolLoaded()
        {
            _loadedPoolCount++;
            if (_loadedPoolCount >= _totalPoolToLoad)
            {
                GameApp.MessageCenter.PostEvent(EventDefines.FightingViewReady);
            }
        }
    }
}