/*
* ┌──────────────────────────────────┐
* │  描    述:                       
* │  类    名: BulletinView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC.View;
using UnityEngine.UI;

namespace Module.View
{
    public class BulletinView : BaseView
    {
        protected override void OnAwake()
        {
            Find<Button>("TopBar/Btn_close").onClick.AddListener(onCloseBtn);
        }

        private void onCloseBtn()
        {
            GameApp.ViewManager.NavigateBack();
        }
    }
}