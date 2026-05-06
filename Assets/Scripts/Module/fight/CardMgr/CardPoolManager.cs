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
        private readonly List<UI_CommonCardItem> _commonCardPool;
        private readonly List<UI_UltimateCardItem> _ultimateCardPool;
        
        private int _totalPoolToLoad;
        private int _loadedPoolCount;
        
        private readonly Transform _cardDeckTf;

        public CardPoolManager(Transform cardDeckTf)
        {
            _cardDeckTf = cardDeckTf;
            _ultimateCardPool = new List<UI_UltimateCardItem>();
            _commonCardPool = new List<UI_CommonCardItem>();
        }
        
        public void Init()
        {
            PreLoadCardItem();
            PreLoadUltimateCardItem();
        }

        #region 获取卡牌
        public UI_BaseCardItem GetCard(CardType type)
        {
            UI_BaseCardItem item;
            
            if (type == CardType.Ultimate)
            {
                item = _ultimateCardPool.Find(x => !x.gameObject.activeSelf);
            }
            else
            {
                item = _commonCardPool.Find(x => !x.gameObject.activeSelf);
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
        #endregion

        #region 回收卡牌
        public void RecycleAllCards()
        {
            foreach (var item in _commonCardPool)
            {
                if (item != null)
                {
                    RecycleCard(item);
                }
            }
            
            foreach (var item in _ultimateCardPool)
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
        
        public void UnLoadAll()
        {
            foreach (var item in _commonCardPool)
            {
                if (item != null)
                    ResManager.UnLoadInstance(item.gameObject);
            }
            _commonCardPool.Clear();

            foreach (var item in _ultimateCardPool)
            {
                if (item != null)
                    ResManager.UnLoadInstance(item.gameObject);
            }
            _ultimateCardPool.Clear();
        }
        #endregion

        #region 预加载卡牌
        private void PreLoadCardItem()
        {
            _totalPoolToLoad = GameApp.CardManager.maxHandCardCount + 8;
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
                    _commonCardPool.Add(item);

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
                    _ultimateCardPool.Add(item);
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
        #endregion
    }
}