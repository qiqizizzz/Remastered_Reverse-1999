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
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.fight.Component
{
    public class UI_CommonCardItem : BaseItem
    {
        public BattleCardData CardData { get; private set; }

        [Header("UI组件")] 
        private Image _icon;
        private List<CanvasGroup> _typeGroups = new List<CanvasGroup>();
        
        protected override void OnAwake()
        {
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
    }
}