/*
* ┌──────────────────────────────────┐
* │  描    述: 匹配控制器
* │  类    名: MatchmakingController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;

namespace Module.Matchmaking
{
    public class MatchmakingController : BaseController
    {
        public MatchmakingController() : base()
        {
            GameApp.ViewManager.Register(ViewType.MatchmakingView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_MatchmakingView,
                controller = this,
                parentTf = GameApp.ViewManager.canvasTf
            });
            
            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            
        }
    }
}