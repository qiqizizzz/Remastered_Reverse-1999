/*
* ┌──────────────────────────────────┐
* │  描    述: 角色界面                      
* │  类    名: CharacterView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using Module.Cultivation.ScrollComponents;
using MVC;
using MVC.View;
using UnityEngine.UI;

namespace Module.View
{
    public class CharacterView : BaseView
    {
        protected override void OnAwake()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnBtn);
        }

        public override void Open(params object[] args)
        {
            base.Open(args);
            
            // 每次打开界面时，找到所有卡片并重新播放入场动画
            CharacterCardItem[] cards = GetComponentsInChildren<CharacterCardItem>();
            foreach (var card in cards)
            {
                card.PlayEntryAnimation();
            }
        }

        private void onReturnBtn()
        {
            GameApp.ViewManager.NavigateBack();
        }
    }
}