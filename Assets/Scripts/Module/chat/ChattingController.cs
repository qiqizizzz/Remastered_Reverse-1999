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

namespace Module.chat
{
    public class ChattingController : BaseController
    {
        public ChatModel Model { get; private set; }
        
        public ChattingController() : base()
        {
            Model = new ChatModel();
            
            //注册视图
            GameApp.ViewManager.Register(ViewType.ChatView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_FriendsView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 2
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenChatView, onOpenChatView);
        }

        private void onOpenChatView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.ChatView);
        }
    }
}