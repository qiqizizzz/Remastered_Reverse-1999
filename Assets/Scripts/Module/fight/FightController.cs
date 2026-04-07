/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗管理器                      
* │  类    名: FightController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;

namespace Module.fight
{
    public class FightController : BaseController
    {
        public FightController() : base()
        {
            GameApp.ViewManager.Register(ViewType.FightingView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_FightingView,
                controller = this,
                parentTf = GameApp.ViewManager.canvasTf
            });
            
            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenFightingView, onOpenFightView);
        }
        
        private void onOpenFightView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.FightingView);
        }
    }
}