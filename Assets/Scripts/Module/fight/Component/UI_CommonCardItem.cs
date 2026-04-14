/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗卡牌实例(普通卡牌)
* │  类    名: UI_CardItem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Data.card;
using DG.Tweening;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.fight.Component
{
    public class UI_CommonCardItem : BaseItem
    {
        public BattleCardData CardData { get; private set; }

        [Header("动画参数相关")]
        private readonly float _cardWidth = 180f;
        private readonly float _cardSpacing = 10f;
        private readonly float _startX = 90f;
        private readonly float _moveDuration = 0.8f;
        private Vector2 _spawnPos = new Vector2(-2200f, 0);

        [Header("UI组件")] 
        private RectTransform _rect;
        private Image _icon;
        private List<CanvasGroup> _typeGroups = new List<CanvasGroup>();
        
        protected override void OnAwake()
        {
            _rect = GetComponent<RectTransform>();
            _icon = Find<Image>("Img_card");
            
            _typeGroups.Add(Find<CanvasGroup>("type/Attack"));
            _typeGroups.Add(Find<CanvasGroup>("type/DeBuff"));
            _typeGroups.Add(Find<CanvasGroup>("type/Buff"));
            _typeGroups.Add(Find<CanvasGroup>("type/Health"));
            _typeGroups.Add(Find<CanvasGroup>("type/Channel"));
        }

        public void InitCardUI(BattleCardData data)
        {
            CardData = data;
            RefreshUI();
        }

        private void RefreshUI()
        {
            if(CardData == null || CardData.BaseData == null) return;

            _icon.sprite = CardData.BaseData.CardSprite;
            showTypeUI((int)CardData.BaseData.CardType);
        }
        
        private void showTypeUI(int index)
        {
            for (int i = 0; i < _typeGroups.Count; i++)
            {
                _typeGroups[i].alpha = (i == index) ? 1 : 0;
                _typeGroups[i].blocksRaycasts = (i == index);
                _typeGroups[i].interactable = (i == index);
            }
        }

        //TODO:拖拽、打出牌
        //拖拽时卡牌会稍微变大，然后当拖拽到另外一个牌中间时，那个牌会自动让出当前索引的空位,每次换位置的时候只有那个牌会移动，其他牌保持不变(感觉像冒泡一样)
        //打出时卡牌会稍微倾斜（不会变大）,然后移动到打出牌的队列那边，会稍微倾斜（抖动）然后停留在队列区域，后续还有撤回的逻辑。
        
        #region 表现与动画逻辑
        public void PrepareSpawn()
        {
            SetVisible(false);
            _rect.anchoredPosition = _spawnPos;//放置在初始发牌点
        }

        public void MoveToIndex(int index,int totalCount ,float delay = 0f)
        {
            SetVisible(true);
            float targetX = -_startX - (totalCount - 1 - index) * (_cardWidth + _cardSpacing);
            Vector2 targetPos = new Vector2(targetX, 0);

            _rect.DOKill();
            _rect.DOAnchorPos(targetPos, _moveDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(delay);
        }

        public void HideCard()
        {
            _rect.DOKill();
            SetVisible(false);
        }
        #endregion
    }
}