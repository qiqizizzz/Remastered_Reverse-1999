/*
* ┌──────────────────────────────────┐
* │  描    述: 聊天控制器                      
* │  类    名: ChatController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;

namespace DefaultNamespace.Module.chat
{
    public class ChatController : BaseController
    {
        public ChatController() : base()
        {
            //注册视图
            GameApp.ViewManager.Register(ViewType.ChatView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_FriendsView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 2
            });
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenChatView, onOpenFriendsView);
        }

        private void onOpenFriendsView(System.Object[] args)
        {
            RegisterFunc(EventDefines.OpenChatView, onOpenFriendsView);
        }
    }
}