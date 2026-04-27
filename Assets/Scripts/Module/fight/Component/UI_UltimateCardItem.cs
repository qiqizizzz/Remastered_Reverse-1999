/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗卡牌实例(大招卡牌)                      
* │  类    名: UI_UltimateCardItem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

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
    }
}