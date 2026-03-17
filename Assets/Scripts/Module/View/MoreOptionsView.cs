/*
* ┌──────────────────────────────────┐
* │  描    述: 更多选项界面
* │  类    名: MoreOptionsView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

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
        }

        private void onReturnGameViewBtn()
        {
            GameApp.ViewManager.Close(ViewId);
            GameApp.ViewManager.Open(ViewType.GameView);
        }
    }
}