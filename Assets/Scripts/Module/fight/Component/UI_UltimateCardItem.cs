/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗卡牌实例(大招卡牌)                      
* │  类    名: UI_UltimateCardItem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using DG.Tweening;
using UnityEngine.UI;

namespace Module.fight.Component
{
    public class UI_UltimateCardItem : UI_BaseCardItem
    {
        protected override void OnAwake()
        {
            base.OnAwake();
            
            _icon = Find<Image>("mask/Img_card");
        }

        public override void MoveToIndex(int index, int totalCount, float delay = 0f)
        {
            base.MoveToIndex(index, totalCount, delay);
            
            float scale = 0.8f;
            float targetY = (scale - 1f) * (Rect.rect.height / 2f) + 30f;

            Rect.DOAnchorPosY(targetY, AnimConfig.MoveDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(delay);
        }
    
    }
}