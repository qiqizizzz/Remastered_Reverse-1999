/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡界面                      
* │  类    名: LevelView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class LevelView : BaseView
    {
        protected override void OnAwake()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnBtn);
        }
        
        private void onReturnBtn()
        {
            GameApp.ViewManager.Close(ViewId);
            GameApp.ViewManager.Open(ViewType.GameView);
        }
    }
}