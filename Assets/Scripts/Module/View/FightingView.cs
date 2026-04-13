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
using Module.fight.Component;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class FightingView : BaseView
    {
        private Transform cardDeckTf;
        
        private List<UI_CommonCardItem>  _cardPool = new List<UI_CommonCardItem>();
        private List<UI_CommonCardItem> _activeCardItems = new List<UI_CommonCardItem>();

        [Header("手牌区域相关")] 
        private readonly int _maxHandCardCount = 8;
        
        protected override void OnAwake()
        {
            cardDeckTf = Find<Transform>("CardDeck");
        }

        protected override void OnStart()
        {
            Find<Button>("OperationBtns/Btn_pause").onClick.AddListener(onPauseBtn);
            
            Controller.RegisterFunc(EventDefines.UpdateHandCards, onUpdateHandCards);
            
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

                    go.transform.SetParent(cardDeckTf, false);
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

        private void onUpdateHandCards(params object[] args)
        {
            //args[0] 手牌列表->本轮新抽的牌
            List<BattleCardData> newCards = args[0] as List<BattleCardData>;
            
            Debug.Log("渲染手牌UI，当前手牌数量：" + newCards.Count);

            int dealCount = 0;//记录发牌数量

            foreach (var cardData in newCards)
            {
                if (_activeCardItems.Count >= _maxHandCardCount)
                {
                    Debug.LogWarning("手牌已满，无法再添加新卡牌");
                    break;
                }
                
                UI_CommonCardItem freeItem = _cardPool.Find(item => !item.gameObject.activeSelf);
                if (freeItem == null)
                {
                    Debug.LogError("没有可用的卡牌UI预制体了，无法显示新卡牌");
                    break;
                }
                
                freeItem.PrepareSpawn();
                freeItem.InitCardUI(cardData);
                _activeCardItems.Add(freeItem);

                dealCount++;
            }
            
            //排列当前的手牌
            for (int i = 0; i < _activeCardItems.Count; i++)
            {
                UI_CommonCardItem item = _activeCardItems[i];
                bool isNewCard = i >= (_activeCardItems.Count - dealCount);
                float delay = isNewCard ? (i - (_activeCardItems.Count - dealCount)) * 0.05f : 0f;//新加的牌有延迟

                item.MoveToIndex(i, delay);
            }
        }
    }
}