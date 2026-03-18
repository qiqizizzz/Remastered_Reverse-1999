/*
* ┌──────────────────────────────────┐
* │  描    述: 好友界面                      
* │  类    名: ChatView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC.View;
using UnityEngine.UI;

namespace Module.View
{
    public class ChatView : BaseView
    {
        protected override void OnAwake()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnMoreOptionBtn);
        }

        private void onReturnMoreOptionBtn()
        {
            GameApp.ViewManager.Close(ViewId);
        }
    }
}