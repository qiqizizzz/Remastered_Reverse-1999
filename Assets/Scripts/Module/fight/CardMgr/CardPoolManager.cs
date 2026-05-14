/*
* ┌──────────────────────────────────┐
* │  描    述: 卡牌池管理器                      
* │  类    名: CardPoolManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Defines;
using Data.card;
using Module.fight.Component;
using UnityEngine;

namespace Module.fight.CardMgr
{
    public class CardPoolManager
    {
        private const int COMMON_CARD_TEMP_BUFFER = 4;
        private const int ULTIMATE_CARD_POOL_COUNT = 6;

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
            
            if (item == null)
            {
                logPoolState(type, "卡牌对象池耗尽，自动扩容");
                item = createCardItem(type);
            }

            if (item != null)
            {
                item.transform.SetParent(_cardDeckTf, true);
                item.PrepareSpawn(); // 放回最左侧初始点
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

        public void TryNotifyReady()
        {
            if(_loadedPoolCount >= _totalPoolToLoad && _totalPoolToLoad > 0)
                GameApp.MessageCenter.PostEvent(EventDefines.FightingViewReady);
        }
        
        private void PreLoadCardItem()
        {
            _totalPoolToLoad = getCommonCardPoolCount();
            QLog.Info($"[{nameof(CardPoolManager)}] 普通卡对象池预加载数量: {_totalPoolToLoad}, 手牌上限: {GameApp.CardManager.maxHandCardCount}, 行动队列上限: {GameApp.CardManager.CardActionQueue.MaxActionCount}, 临时缓冲: {COMMON_CARD_TEMP_BUFFER}");
            _loadedPoolCount = 0;
            for (int i = 0; i < _totalPoolToLoad; i++)
            {
                ResManager.InstantiateAsync(AddressDefines.UI_small_CommonCard, (go) =>
                {
                    if (go == null)
                    {
                        QLog.Error("加载卡牌预制体失败");
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
            _totalPoolToLoad += ULTIMATE_CARD_POOL_COUNT;
            for (int i = 0; i < ULTIMATE_CARD_POOL_COUNT; i++)
            {
                ResManager.InstantiateAsync(AddressDefines.UI_small_UltimateCard, (go) =>
                {
                    if (go == null)
                    {
                        QLog.Error("加载大招卡牌预制体失败");
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

        // 创建卡牌对象
        private UI_BaseCardItem createCardItem(CardType type)
        {
            string keyName = type == CardType.Ultimate
                ? AddressDefines.UI_small_UltimateCard
                : AddressDefines.UI_small_CommonCard;
            GameObject go = ResManager.Instantiate(keyName, _cardDeckTf);
            if (go == null) return null;

            UI_BaseCardItem item = go.GetComponent<UI_BaseCardItem>();
            item.SetVisible(false);
            if (item is UI_UltimateCardItem ultimateItem)
                _ultimateCardPool.Add(ultimateItem);
            else if (item is UI_CommonCardItem commonItem)
                _commonCardPool.Add(commonItem);

            logPoolState(type, "卡牌对象池扩容完成");
            return item;
        }

        // 计算普通卡池容量
        private int getCommonCardPoolCount()
        {
            return GameApp.CardManager.maxHandCardCount +
                   GameApp.CardManager.CardActionQueue.MaxActionCount +
                   COMMON_CARD_TEMP_BUFFER;
        }

        // 输出对象池诊断信息
        private void logPoolState(CardType type, string reason)
        {
            int commonActive = _commonCardPool.Count(x => x != null && x.gameObject.activeSelf);
            int commonInactive = _commonCardPool.Count - commonActive;
            int ultimateActive = _ultimateCardPool.Count(x => x != null && x.gameObject.activeSelf);
            int ultimateInactive = _ultimateCardPool.Count - ultimateActive;
            QLog.Info($"[{nameof(CardPoolManager)}] {reason} Type={type}, Common={commonActive}/{_commonCardPool.Count} active, CommonIdle={commonInactive}, Ultimate={ultimateActive}/{_ultimateCardPool.Count} active, UltimateIdle={ultimateInactive}");
        }
        #endregion
    }
}