/*
* ┌──────────────────────────────────┐
* │  描    述: 更多选项界面
* │  类    名: MoreOptionsView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.View;
using UnityEngine.UI;

namespace Module.View
{
    public class MoreOptionsView : BaseView
    {
        protected override void OnAwake()
        {
            Find<Button>("LeftUpArea/Btn_return").onClick.AddListener(onReturnGameViewBtn);
            Find<Button>("LeftDownArea/btns_1/Btn_hy").onClick.AddListener(onOpenChatViewBtn);
            Find<Button>("LeftDownArea/btns_2/Btn_sz").onClick.AddListener(onOpenSettingViewBtn);
        }

        private void onReturnGameViewBtn()
        {
            GameApp.ViewManager.NavigateBack();
        }

        private void onOpenChatViewBtn()
        {
            ApplyControllerFunc(ControllerType.Chat, EventDefines.OpenChatView);
        }
        
        private void onOpenSettingViewBtn()
        {
            GameApp.ViewManager.Open(ViewType.SettingView);
        }
    }
}