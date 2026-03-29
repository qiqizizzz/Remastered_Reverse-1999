/*
* ┌──────────────────────────────────┐
* │  描    述: 角色界面                      
* │  类    名: CharacterView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
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

        private void onReturnBtn()
        {
            GameApp.ViewManager.Close(ViewId);
            ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenGameView);
        }
    }
}