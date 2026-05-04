/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗卡牌实例(普通卡牌)
* │  类    名: UI_CardItem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Data.card.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Module.fight.Component
{
    public class UI_CommonCardItem : UI_BaseCardItem
    {
        [Header("UI组件")] 
        private List<CanvasGroup> _typeGroups = new List<CanvasGroup>();
        private List<Image> _stars = new List<Image>();
        
        [Header("其他参数")]
        private readonly int _maxStarLevel = 3;
        
        protected override void OnAwake()
        {
            base.OnAwake();
            _icon = Find<Image>("Img_card");
            
            _typeGroups.Add(Find<CanvasGroup>("type/Attack"));
            _typeGroups.Add(Find<CanvasGroup>("type/DeBuff"));
            _typeGroups.Add(Find<CanvasGroup>("type/Buff"));
            _typeGroups.Add(Find<CanvasGroup>("type/Health"));
            _typeGroups.Add(Find<CanvasGroup>("type/Channel"));
            
            _stars.Add(Find<Image>("Star/star_1/star_open"));
            _stars.Add(Find<Image>("Star/star_2/star_open"));
            _stars.Add(Find<Image>("Star/star_3/star_open"));
        }

        #region UI逻辑
        public void ShowStarUI(int starCount)
        {
            starCount = Mathf.Clamp(starCount, 0, _maxStarLevel);
            
            for (int i = 0; i < _maxStarLevel; i++)
            {
                _stars[i].gameObject.SetActive(i < starCount);
            }
        }

        protected override void RefreshUI()
        {
            base.RefreshUI();
            
            showTypeUI((int)BattleCardData.GetConfig().CardType);
            ShowStarUI(BattleCardData.StarLevel);
            SetBlockRaycasts(true);
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
        #endregion
        
        #region 表现与动画逻辑
        public override void PrepareSpawn()
        {
            base.PrepareSpawn();

            ShowStarUI(1);
        }
        #endregion
    }
}