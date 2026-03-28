/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡控制器(管理关卡的切换等                      
* │  类    名: LevelController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;

namespace DefaultNamespace.Module.level
{
    public class LevelController : BaseController
    {
        public LevelController() : base()
        {
            GameApp.ViewManager.Register(ViewType.LevelView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_LevelView,
                controller = this,
                parentTf = GameApp.ViewManager.canvasTf
            });
            
            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenLevelView, onOpenLevelView);
        }
        
        private void onOpenLevelView(params object[] args)
        {
            GameApp.ViewManager.Open(ViewType.LevelView);
        }
    }
}